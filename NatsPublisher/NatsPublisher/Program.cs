using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL")
              ?? "nats://172.22.4.106:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "test.saludo";
var message = args.Length > 0
    ? string.Join(" ", args)
    : $"Hola JetStream desde consola! {DateTimeOffset.Now:O}";

await using var nc = new NatsConnection(new NatsOpts { Url = natsUrl });

// JetStream context (incluye “management”)
var js = nc.CreateJetStreamContext(); // :contentReference[oaicite:2]{index=2}

//  Crea o actualiza el stream (si no existe lo crea)
await js.CreateOrUpdateStreamAsync(new StreamConfig
{
    Name = "TEST",
    Subjects = new[] { "test.*" }   // cubre test.saludo, test.otro
}); // :contentReference[oaicite:3]{index=3}

//  Publica persistente en JetStream
await js.PublishAsync(subject, message); // :contentReference[oaicite:4]{index=4}

Console.WriteLine($"Publicado en JetStream: {subject} -> {message}");