using Grpc.Core;
using Grpc.Net.Client;
using ParkingGrpc.Protos;

namespace ParkingGrpcClient;

public class WatchPlacesClient
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║   SERVER STREAMING - WatchPlaces            ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");

        using var channel = GrpcChannel.ForAddress("http://localhost:5000");
        var client = new PlaceService.PlaceServiceClient(channel);

        Console.WriteLine("📡 Connexion au stream WatchPlaces...\n");

        using var cts = new CancellationTokenSource();

        // Gérer Ctrl+C pour arrêter proprement
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            // Appel streaming : le serveur envoie des mises à jour en continu
            using var call = client.WatchPlaces(new Empty(), cancellationToken: cts.Token);

            Console.WriteLine("✅ Connecté ! En attente de mises à jour...");
            Console.WriteLine("   (Appuyez sur Ctrl+C pour arrêter)\n");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Lire les mises à jour en continu
            await foreach (var update in call.ResponseStream.ReadAllAsync(cts.Token))
            {
                string eventEmoji = update.EventType switch
                {
                    "OCCUPIED" => "🚗",
                    "FREED" => "✅",
                    "CREATED" => "🆕",
                    "DELETED" => "🗑️",
                    _ => "📢"
                };

                Console.WriteLine($"{eventEmoji} [{update.Timestamp}] {update.EventType}");
                Console.WriteLine($"   Place n°{update.Place.Numero} (Étage {update.Place.Etage})");
                Console.WriteLine($"   État : {(update.Place.EstOccupee ? "OCCUPÉE" : "LIBRE")}\n");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n🛑 Stream interrompu par l'utilisateur");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erreur : {ex.Message}");
        }
    }
}
