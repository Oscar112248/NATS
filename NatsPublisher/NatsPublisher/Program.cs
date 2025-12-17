using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://nats:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";

await using var nc = new NatsConnection(new NatsOpts
{
    Url = natsUrl,
    RequestTimeout = TimeSpan.FromSeconds(30), // timeout base para PubAck / requests
});

// JetStream context (extensión del paquete NATS.Client.JetStream)
var js = nc.CreateJetStreamContext();

// Warm-up
await nc.PingAsync();

// Stream (para pruebas OK; en prod muévelo a un init/seed)
await js.CreateOrUpdateStreamAsync(new StreamConfig
{
    Name = "PAGOS",
    Subjects = new[] { "pago.*" },
    Storage = StreamConfigStorage.File // más estable que Memory en Docker
});

Console.WriteLine($"Conectado a {natsUrl}. Publicando a subject '{subject}' ...");

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

    var publicado = false;

    for (var attempt = 1; attempt <= 3 && !publicado; attempt++)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Publica a JetStream (espera PubAck)
            await js.PublishAsync(subject, payload, cancellationToken: cts.Token);

            Console.WriteLine($"Publicado #{contador + 1} (intento {attempt})");
            publicado = true;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"Timeout publish #{contador + 1} (intento {attempt}). Reintentando...");
            await Task.Delay(1000 * attempt);
        }
        catch (NatsJSPublishNoResponseException ex)
        {
            Console.WriteLine($"No response #{contador + 1} (intento {attempt}): {ex.Message}");
            await Task.Delay(1000 * attempt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falló publish #{contador + 1} (intento {attempt}): {ex.GetType().Name} - {ex.Message}");
            await Task.Delay(1000 * attempt);
        }
    }

    // pausa entre mensajes (ajústala)
    await Task.Delay(3000);
}

Console.WriteLine("Listo. Saliendo...");

public sealed class PagoConfirmadoEvent
{
    public string Referencia { get; set; } = default!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTime Fecha { get; set; }
    public string Canal { get; set; } = "WEB";
    public int Contador { get; set; }
}



//using NATS.Client.Core;
//using NATS.Client.JetStream;
//using NATS.Client.JetStream.Models;
//using NATS.Net;
//using System.Text;

//var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://nats:4222";
//var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";

//// 👉 Leer mensaje desde argumentos
//if (args.Length == 0)
//{
//    Console.WriteLine("Uso: publisher \"mensaje a enviar\"");
//    return;
//}

//var mensaje = args[0];

//await using var nc = new NatsConnection(new NatsOpts
//{
//    Url = natsUrl,
//    RequestTimeout = TimeSpan.FromSeconds(30)
//});

//var js = nc.CreateJetStreamContext();

//// Para pruebas (en prod no)
//await js.CreateOrUpdateStreamAsync(new StreamConfig
//{
//    Name = "PAGOS",
//    Subjects = new[] { "pago.*" }
//});

//var payload = Encoding.UTF8.GetBytes(mensaje);

//await js.PublishAsync(subject, payload);

//Console.WriteLine($"✅ Mensaje enviado: {mensaje}");



