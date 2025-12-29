using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using System.Text.Json;
using System.Threading;

var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://172.22.4.106:4222";
var subject = Environment.GetEnvironmentVariable("NATS_SUBJECT") ?? "pago.saludo";

// Guardamos conexión/contexto como “actuales”
NatsConnection nc = await CreateConnectionAsync();
INatsJSContext js = nc.CreateJetStreamContext();

// Para evitar que 2 reconexiones se pisen (aunque hoy sea 1 solo publisher, esto te blinda)
var reconnectLock = new SemaphoreSlim(1, 1);

// Warm-up
await nc.PingAsync();
await EnsureStreamAsync(js);

Console.WriteLine($"Listo. Publicando a '{subject}' en {natsUrl}");

for (var contador = 0; contador < 500; contador++)
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

    var ok = await PublishWithRecoveryAsync(
        subject,
        payload,
        maxAttempts: 6,                 // más intentos porque el problema es intermitente
        perAttemptTimeoutSeconds: 30,
        onLog: Console.WriteLine);

    if (!ok)
    {
        Console.WriteLine($"No se pudo publicar #{contador + 1} luego de reintentos. Cortando proceso.");
        break;
    }

    Console.WriteLine($"Publicado #{contador + 1}");
    await Task.Delay(1000);
}

await nc.DisposeAsync();
Console.WriteLine("Fin.");

// -------------------- helpers --------------------

async Task<NatsConnection> CreateConnectionAsync()
{
    var conn = new NatsConnection(new NatsOpts
    {
        Url = natsUrl,
        RequestTimeout = TimeSpan.FromSeconds(30),
    });

    // Ping para validar que hay canal
    await conn.PingAsync();
    return conn;
}

async Task EnsureStreamAsync(INatsJSContext jsLocal)
{
    // Crea/actualiza el stream (si JetStream aún recupera estado, puede fallar -> reintenta)
    for (int i = 1; i <= 10; i++)
    {
        try
        {
            await jsLocal.CreateOrUpdateStreamAsync(new StreamConfig
            {
                Name = "PAGOS",
                Subjects = new[] { "pago.*" },
                Storage = StreamConfigStorage.File
            });
            return;
        }
        catch
        {
            await Task.Delay(1000 * i);
        }
    }

    throw new Exception("No se pudo crear/actualizar el Stream PAGOS.");
}

async Task<bool> PublishWithRecoveryAsync(
    string subj,
    byte[] payload,
    int maxAttempts,
    int perAttemptTimeoutSeconds,
    Action<string> onLog)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(perAttemptTimeoutSeconds));

            // JetStream publish (espera PubAck)
            await js.PublishAsync(subj, payload, cancellationToken: cts.Token);
            return true;
        }
        catch (Exception ex) when (
            ex is OperationCanceledException ||
            ex is NatsJSPublishNoResponseException ||
            ex is NatsJSException ||
            ex is NatsException)
        {
            onLog($"Publish falló (intento {attempt}/{maxAttempts}): {ex.GetType().Name} - {ex.Message}");

            // Backoff progresivo
            await Task.Delay(500 * attempt);

            // Si fue un bache, reintentar suele bastar.
            // Pero si ya vamos en intentos 3+ y sigue fallando,
            // hacemos “recovery” (recrear JS y si hace falta reconectar).
            if (attempt == 3 || attempt == 5)
            {
                await RecoverHardAsync(onLog);
            }
        }
    }

    return false;
}

async Task RecoverHardAsync(Action<string> onLog)
{
    await reconnectLock.WaitAsync();
    try
    {
        onLog("Recovery HARD: recreando conexión completa...");

        var old = nc;

        nc = await CreateConnectionAsync();        // ping adentro
        js = nc.CreateJetStreamContext();

        // opcional: validar JetStream API antes de seguir
        await js.GetAccountInfoAsync();

        // opcional: asegurar stream
        await EnsureStreamAsync(js);

        try { await old.DisposeAsync(); } catch { /* ignore */ }

        onLog("Recovery HARD OK.");
    }
    finally
    {
        reconnectLock.Release();
    }
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



