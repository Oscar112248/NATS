using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
              ?? "nats://nats:4222"; // puede ser el nombre del servicio nats o la ip
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";

(NatsConnection nc, INatsJSContext js) = await ConnectAsync(natsUrl);


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

    try
    {
        //  Publish con retry + reconexión si "No response"
        (nc, js) = await PublishWithRetryAsync(natsUrl, nc, js, subject, payload);
        Console.WriteLine($"Publicado #{contador + 1}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falló publish #{contador + 1}: {ex.GetType().Name} - {ex.Message}");
    }

    await Task.Delay(1000);
}

await nc.DisposeAsync();

static async Task<(NatsConnection nc, INatsJSContext js)> ConnectAsync(string url)
{
    var nc = new NatsConnection(new NatsOpts
    {
        Url = url,
        RequestTimeout = TimeSpan.FromSeconds(30), //  clave
        // (Opcional) PingInterval, Reconnect, etc. dependen de versión
    });

    var js = nc.CreateJetStreamContext();
    return (nc, js);
}

static async Task<(NatsConnection nc, INatsJSContext js)> PublishWithRetryAsync(
    string url,
    NatsConnection nc,
    INatsJSContext js,
    string subject,
    byte[] payload)
{
    const int maxRetries = 3;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await js.PublishAsync(subject, payload, cancellationToken: cts.Token);
            return (nc, js); 
        }
        catch (NatsJSPublishNoResponseException) when (attempt < maxRetries)
        {
            try { await nc.DisposeAsync(); } catch { /* ignore */ }

            await Task.Delay(250 * attempt); 
            (nc, js) = await ConnectAsync(url);
        }
        catch (OperationCanceledException) when (attempt < maxRetries)
        {
            await Task.Delay(250 * attempt);
        }
    }

    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
    {
        await js.PublishAsync(subject, payload, cancellationToken: cts.Token);
    }

    return (nc, js);
}

public sealed class PagoConfirmadoEvent
{
    public string Referencia { get; set; } = default!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTime Fecha { get; set; }
    public string Canal { get; set; } = "WEB";
    public int Contador { get; set; }
}
