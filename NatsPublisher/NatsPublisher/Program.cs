using NATS.Client.Core;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
              ?? "nats://172.22.4.106:4222";

var message = args.Length > 0
    ? string.Join(" ", args)
    : $"Hola NATS desde Console ({DateTime.Now:HH:mm:ss})";

Console.WriteLine($"Conectando a {natsUrl}");

await using var nc = new NatsConnection(new NatsOpts
{
    Url = natsUrl
});

await nc.PublishAsync("test.saludo", message);

Console.WriteLine("✅ Mensaje enviado");