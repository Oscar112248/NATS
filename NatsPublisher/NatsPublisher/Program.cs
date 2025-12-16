using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://nats:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";

async Task<(NatsConnection nc, INatsJSContext js)> ConnectAsync()
{
    var nc = new NatsConnection(new NatsOpts
    {
        Url = natsUrl,
        RequestTimeout = TimeSpan.FromSeconds(30), // 👈 importante para PubAck
    });

    var js = nc.CreateJetStreamContext();

    // “Warm up” de la conexión (evita estados raros al primer request)
    await nc.PingAsync();

    return (nc, js);
}

var (nc, js) = await ConnectAsync();

// Para pruebas OK. En prod: muévelo a un init job/flag.
await js.CreateOrUpdateStreamAsync(new StreamConfig
{
    Name = "PAGOS",
    Subjects = new[] { "pago.*" }
});

for (var contador = 0; contador < 50; contador++)
{
    var evento = new PagoConfirmadoEvent
    {
        Referencia = Guid.NewGuid().ToString("N"),
        Monto = 12.50m,
        Moneda = "USD",
        Fecha = DateTime.UtcNow,
        Canal = "WEB",
        Contador = contador + 1
    };

    var payload = JsonSerializer.SerializeToUtf8Bytes(evento);

    var published = false;
    var attempt = 0;

    while (!published && attempt < 3)
    {
        attempt++;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await js.PublishAsync(subject, payload, cancellationToken: cts.Token);

            Console.WriteLine($"Publicado #{contador + 1} (intento {attempt})");
            published = true;
        }
        catch (NatsJSPublishNoResponseException ex)
        {
            Console.WriteLine($"No response en #{contador + 1} (intento {attempt}). Reconectando... {ex.Message}");

            // Reconecta y reintenta
            try { await nc.DisposeAsync(); } catch { /* ignore */ }
            (nc, js) = await ConnectAsync();

            // (opcional) pequeño backoff
            await Task.Delay(300 * attempt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falló publish #{contador + 1} (intento {attempt}): {ex.GetType().Name} - {ex.Message}");
            await Task.Delay(300 * attempt);
        }
    }

    await Task.Delay(1000);
}

await nc.DisposeAsync();

public sealed class PagoConfirmadoEvent
{
    public string Referencia { get; set; } = default!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTime Fecha { get; set; }
    public string Canal { get; set; } = "WEB";
    public int Contador { get; set; }
}
