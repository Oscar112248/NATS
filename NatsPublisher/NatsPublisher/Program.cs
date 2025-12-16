using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
              ?? "nats://172.22.4.106:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "test.saludo";
var evento = new PagoConfirmadoEvent
{
    Referencia = Guid.NewGuid().ToString("N"),
    Monto = 12.50m,
    Moneda = "USD",
    Fecha = DateTime.UtcNow,
    Canal = "WEB"
};
await using var nc = new NatsConnection(new NatsOpts { Url = natsUrl });

var js = nc.CreateJetStreamContext(); 

//  Crea o actualiza el stream (si no existe lo crea)
await js.CreateOrUpdateStreamAsync(new StreamConfig
{
    Name = "PAGOS",
    Subjects = new[] { "pago.*" }
}); 

var json = JsonSerializer.SerializeToUtf8Bytes(evento);

// Publicar persistente
await js.PublishAsync(subject, json);


public sealed class PagoConfirmadoEvent
{
    public string Referencia { get; set; } = default!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTime Fecha { get; set; }
    public string Canal { get; set; } = "WEB";
}