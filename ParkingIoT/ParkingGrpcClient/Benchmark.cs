using System.Diagnostics;
using Grpc.Net.Client;
using ParkingGrpc.Protos;

namespace ParkingGrpcClient;

public class Benchmark
{
    private readonly PlaceService.PlaceServiceClient _grpcClient;
    private readonly HttpClient _restClient;

    public Benchmark()
    {
        // Client gRPC
        var channel = GrpcChannel.ForAddress("http://localhost:5000");
        _grpcClient = new PlaceService.PlaceServiceClient(channel);

        // Client REST
        _restClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5119") // Port de l'API REST
        };
    }

    public async Task RunBenchmark(int iterations = 1000)
    {
        Console.WriteLine($"\n━━━ BENCHMARK : {iterations} itérations ━━━\n");

        // Warm-up (ignorer les premières requêtes)
        await _grpcClient.GetAllPlacesAsync(new Empty());
        await _restClient.GetAsync("/api/place");

        // Benchmark gRPC
        var grpcTime = await BenchmarkGrpc(iterations);

        // Benchmark REST
        var restTime = await BenchmarkRest(iterations);

        // Afficher les résultats
        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║          RÉSULTATS DU BENCHMARK              ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");

        Console.WriteLine($"gRPC  : {grpcTime:F2} ms pour {iterations} requêtes");
        Console.WriteLine($"        Moyenne : {grpcTime / iterations:F3} ms/requête\n");

        Console.WriteLine($"REST  : {restTime:F2} ms pour {iterations} requêtes");
        Console.WriteLine($"        Moyenne : {restTime / iterations:F3} ms/requête\n");

        double speedup = restTime / grpcTime;
        Console.WriteLine($"🚀 gRPC est {speedup:F2}× plus rapide que REST\n");
    }

    private async Task<double> BenchmarkGrpc(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await _grpcClient.GetAllPlacesAsync(new Empty());
        }

        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private async Task<double> BenchmarkRest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await _restClient.GetAsync("/api/place");
        }

        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
