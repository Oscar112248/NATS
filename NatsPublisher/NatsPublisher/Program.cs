using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
              ?? "nats://172.22.4.106:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";
var evento = new PagoConfirmadoEvent
{
    Referencia = Guid.NewGuid().ToString("N"),
    Monto = 12.50m,
    Moneda = "USD",
    Fecha = DateTime.UtcNow,
    Canal = "WEB",
    Contador =0
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

var contador = 0;
while (contador != 50)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
   string mensaje = $"Publicando evento de pago confirmado #{contador + 1}";
    // Publicar persistente
    await js.PublishAsync(subject, mensaje, cancellationToken: cts.Token);
    await Task.Delay(1000);
    contador++;
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