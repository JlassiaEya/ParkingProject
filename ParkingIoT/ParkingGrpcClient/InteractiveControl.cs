using Grpc.Core;
using Grpc.Net.Client;
using ParkingGrpc.Protos;

namespace ParkingGrpcClient;

public class InteractiveControl
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║   BIDIRECTIONAL STREAMING - Contrôle         ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");

        using var channel = GrpcChannel.ForAddress("http://localhost:5000");
        var client = new PlaceService.PlaceServiceClient(channel);

        using var call = client.ControlPlaces();

        // Tâche pour lire les réponses du serveur
        var readTask = Task.Run(async () =>
        {
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                string statusEmoji = response.Status switch
                {
                    "OK" => "✅",
                    "ERROR" => "❌",
                    "INFO" => "ℹ️",
                    _ => "📢"
                };

                Console.WriteLine($"\n{statusEmoji} [{response.Timestamp}] {response.Status}");
                Console.WriteLine($"   {response.Message}");

                if (response.Place != null)
                {
                    Console.WriteLine($"   Place n°{response.Place.Numero} - {(response.Place.EstOccupee ? "OCCUPÉE" : "LIBRE")}");
                }

                Console.Write("\n> ");
            }
        });

        // Boucle interactive
        Console.WriteLine("Commandes disponibles :");
        Console.WriteLine("  STATUS <id>  - Obtenir l'état d'une place");
        Console.WriteLine("  OCCUPY <id>  - Occuper une place");
        Console.WriteLine("  FREE <id>    - Libérer une place");
        Console.WriteLine("  PING         - Test de connexion");
        Console.WriteLine("  EXIT         - Quitter\n");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToUpper();

            if (command == "EXIT")
                break;

            int placeId = 0;
            if (parts.Length > 1 && !int.TryParse(parts[1], out placeId))
            {
                Console.WriteLine("❌ ID de place invalide\n");
                continue;
            }

            var message = new ControlMessage
            {
                Command = command == "STATUS" ? "GET_STATUS" : command,
                PlaceId = placeId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            await call.RequestStream.WriteAsync(message);
        }

        await call.RequestStream.CompleteAsync();
        await readTask;
    }
}
