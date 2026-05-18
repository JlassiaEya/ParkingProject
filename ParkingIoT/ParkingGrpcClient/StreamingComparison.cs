using Grpc.Core;
using Grpc.Net.Client;
using ParkingGrpc.Protos;
using System.Diagnostics;

namespace ParkingGrpcClient;

public class StreamingComparison
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║   COMPARAISON DES 4 TYPES gRPC              ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");

        using var channel = GrpcChannel.ForAddress("http://localhost:5000");
        var client = new PlaceService.PlaceServiceClient(channel);

        // 1. UNARY
        Console.WriteLine("━━━ 1. UNARY (GetPlace) ━━━");
        var sw = Stopwatch.StartNew();
        var place = await client.GetPlaceAsync(new PlaceRequest { Id = 1 });
        sw.Stop();
        Console.WriteLine($"✅ Temps : {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   1 requête → 1 réponse immédiate\n");

        // 2. SERVER STREAMING
        Console.WriteLine("━━━ 2. SERVER STREAMING (WatchPlaces - 5 sec) ━━━");
        sw.Restart();
        int serverStreamCount = 0;
        using var watchCall = client.WatchPlaces(new Empty());
        var watchTask = Task.Run(async () =>
        {
            await foreach (var update in watchCall.ResponseStream.ReadAllAsync())
            {
                serverStreamCount++;
            }
        });
        await Task.Delay(5000); // Attendre 5 secondes
        watchCall.Dispose();
        sw.Stop();
        Console.WriteLine($"✅ Temps : {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   1 requête → {serverStreamCount} réponses en continu\n");

        // 3. CLIENT STREAMING
        Console.WriteLine("━━━ 3. CLIENT STREAMING (UploadSensorData - 10 messages) ━━━");
        sw.Restart();
        using var uploadCall = client.UploadSensorData();
        for (int i = 0; i < 10; i++)
        {
            await uploadCall.RequestStream.WriteAsync(new SensorData
            {
                SensorId = i,
                PlaceId = i + 1,
                Value = 0.5,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        await uploadCall.RequestStream.CompleteAsync();
        var summary = await uploadCall.ResponseAsync;
        sw.Stop();
        Console.WriteLine($"✅ Temps : {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   10 requêtes → 1 réponse finale\n");

        // 4. BIDIRECTIONAL STREAMING
        Console.WriteLine("━━━ 4. BIDIRECTIONAL STREAMING (ControlPlaces - 5 commandes) ━━━");
        sw.Restart();
        using var controlCall = client.ControlPlaces();
        var responses = new List<ControlResponse>();

        var readTask = Task.Run(async () =>
        {
            await foreach (var resp in controlCall.ResponseStream.ReadAllAsync())
            {
                responses.Add(resp);
            }
        });

        for (int i = 0; i < 5; i++)
        {
            await controlCall.RequestStream.WriteAsync(new ControlMessage
            {
                Command = i % 2 == 0 ? "OCCUPY" : "FREE",
                PlaceId = 1,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
            await Task.Delay(100);
        }

        await controlCall.RequestStream.CompleteAsync();
        await readTask;
        sw.Stop();
        Console.WriteLine($"✅ Temps : {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   5 requêtes ↔ 5 réponses (full-duplex)\n");

        // RÉSUMÉ
        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║              RÉSUMÉ COMPARATIF               ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");
        Console.WriteLine("Type                 | Usage principal");
        Console.WriteLine("---------------------|----------------------------------");
        Console.WriteLine("Unary                | Requête-réponse simple");
        Console.WriteLine("Server Streaming     | Dashboard temps réel, notifications");
        Console.WriteLine("Client Streaming     | Upload batch, capteurs IoT");
        Console.WriteLine("Bidirectional        | Chat, contrôle temps réel");
    }
}
