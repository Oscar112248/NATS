using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
              ?? "nats://172.22.4.106:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";

await using var nc = new NatsConnection(new NatsOpts { Url = natsUrl });

var js = nc.CreateJetStreamContext();

//  Crea o actualiza el stream (si no existe lo crea)
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

    var json = JsonSerializer.SerializeToUtf8Bytes(evento);

    try
    {
        await js.PublishAsync(subject, json);
        Console.WriteLine($"Publicado #{contador + 1}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falló publish #{contador + 1}: {ex.Message}");
    }

    await Task.Delay(1000);
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