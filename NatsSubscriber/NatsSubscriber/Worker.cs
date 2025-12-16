using NATS.Client;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Net;

namespace NatsSubscriber
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;


        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
                          ?? "nats://172.22.4.106:4222";
            var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "test.saludo";


            _logger.LogInformation("Conectando a NATS en {Url}", natsUrl);
            await using var nc = new NatsConnection(new NatsOpts { Url = natsUrl });

            var js = nc.CreateJetStreamContext();

            // 1) Asegura stream
            await js.CreateOrUpdateStreamAsync(new StreamConfig
            {
                Name = "TEST",
                Subjects = new[] { "test.pruebas" }
            }, cancellationToken: stoppingToken);


            // 2) Crea/actualiza consumer DURABLE correctamente
            var consumerCfg = new ConsumerConfig
            {
                Name = "SUB_TEST",              // 👈 importante (nombre del consumer)
                DurableName = "SUB_TEST",       // 👈 importante (durable)
                FilterSubject = subject,        // 👈 solo este subject
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All
            };

            var consumer = await js.CreateOrUpdateConsumerAsync(
             stream: "TEST",
             config: consumerCfg,
             cancellationToken: stoppingToken);

            _logger.LogInformation("JetStream consumer listo. Stream=TEST Subject={Subject}", subject);


            await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Recibido: {Msg}", msg.Data);
                    await msg.AckAsync(cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando (sin ACK => reintento)");
                }
            }


        }
    }
}
