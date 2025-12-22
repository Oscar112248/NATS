using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Net;
namespace NatsSubscriber2
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger; public Worker(ILogger<Worker> logger) { _logger = logger; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL2") ?? "nats://172.22.4.106:4222";
            var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT2") ?? "pago.*";
            var durableName = Environment.GetEnvironmentVariable("NATS_DURABLE2") ?? "SUB_PAGOS_P2P";
            _logger.LogInformation("Conectando a NATS en {Url}", natsUrl);

            await using var nc = new NatsConnection(new NatsOpts { Url = natsUrl });

            var js = nc.CreateJetStreamContext();

            // 1) Asegura stream
            await js.CreateOrUpdateStreamAsync(
                new StreamConfig
                {
                    Name = "PAGOS",
                    Subjects = new[] { "pago.*" }
                },
                cancellationToken: stoppingToken);


            // 2) Crea/actualiza consumer DURABLE correctamente
            var consumerCfg = new ConsumerConfig
            {
                Name = durableName,
                DurableName = durableName,
                FilterSubject = subject,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All
            };
            var consumer = await js.CreateOrUpdateConsumerAsync(
                stream: "PAGOS",
                config: consumerCfg,
                cancellationToken: stoppingToken);

            _logger.LogInformation("JetStream consumer 2 listo. Durable 2 ={durableName} Subject 2 ={Subject}", durableName, subject);

            await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Recibido 2 : {Msg}", msg.Data);
                    await msg.AckAsync(cancellationToken: stoppingToken);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error procesando (sin ACK => reintento)"); }
            }
        }
    }
}