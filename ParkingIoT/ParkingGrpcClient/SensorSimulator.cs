using Grpc.Net.Client;
using ParkingGrpc.Protos;

namespace ParkingGrpcClient;

public class SensorSimulator
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════╗");
        Console.WriteLine("║   CLIENT STREAMING - Simulation Capteur     ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝\n");

        using var channel = GrpcChannel.ForAddress("http://localhost:5000");
        var client = new PlaceService.PlaceServiceClient(channel);

        Console.WriteLine("📡 Simulation d'un capteur envoyant des données...\n");

        // Créer le stream client
        using var call = client.UploadSensorData();

        var random = new Random();
        int messageCount = 20;

        Console.WriteLine($"Envoi de {messageCount} mesures...\n");

        // Envoyer des données en flux
        for (int i = 0; i < messageCount; i++)
        {
            int placeId = random.Next(1, 13); // Places 1 à 12
            double value = random.NextDouble(); // 0.0 à 1.0

            var data = new SensorData
            {
                SensorId = placeId, // 1 capteur par place
                PlaceId = placeId,
                Value = value,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            await call.RequestStream.WriteAsync(data);

            string status = value >= 0.5 ? "DÉTECTÉ (occupée)" : "RIEN (libre)";
            Console.WriteLine($"📤 Message {i + 1}/{messageCount} : Capteur {data.SensorId}, Place {data.PlaceId}, Valeur {value:F2} → {status}");

            // Attendre un peu entre chaque mesure (simulation temps réel)
            await Task.Delay(100);
        }

        // Signaler la fin du stream client
        await call.RequestStream.CompleteAsync();

        Console.WriteLine("\n✅ Tous les messages envoyés. En attente du résumé...\n");

        // Recevoir le résumé du serveur
        var summary = await call.ResponseAsync;

        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        Console.WriteLine($"📊 RÉSUMÉ :");
        Console.WriteLine($"   Messages traités : {summary.TotalMessages}");
        Console.WriteLine($"   Places mises à jour : {summary.PlacesUpdated}");
        Console.WriteLine($"   Début : {summary.StartTime}");
        Console.WriteLine($"   Fin : {summary.EndTime}");
        Console.WriteLine($"   Statut : {(summary.Success ? "✅ Succès" : "❌ Échec")}");
        Console.WriteLine($"   Message : {summary.Message}");
    }
}
