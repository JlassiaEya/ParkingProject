using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ParkingRabbitMQ.Models;

Console.WriteLine("=== Consommateur RabbitMQ — Service Historique ===");
Console.WriteLine();

// ── MENU ─────────────────────────────────────────────
Console.WriteLine("Choisissez le mode :");
Console.WriteLine("  1 - Consommateur simple (queue directe)");
Console.WriteLine("  2 - Consommateur Historique (Fanout)");
Console.WriteLine("  3 - Consommateur Notifications (Fanout)");
Console.WriteLine("  4 - Consommateur Places (Topic)");
Console.WriteLine("  5 - Consommateur Alertes (Topic)");
Console.WriteLine("  6 - Consommateur Historique (Topic)");
Console.Write("Votre choix : ");

var choix = Console.ReadLine();

switch (choix)
{
    case "2":
        await ConsommateurHistorique.RunAsync();
        break;

    case "3":
        await ConsommateurNotifications.RunAsync();
        break;

    case "4": await ConsommateurPlacesTopic.RunAsync(); break;
    case "5": await ConsommateurAlertesTopic.RunAsync(); break;
    case "6": await ConsommateurHistoriqueTopic.RunAsync(); break;

    default:
        await RunConsommateurSimple();
        break;
}

// ── CONSOMMATEUR SIMPLE + DLQ ─────────────────────
async Task RunConsommateurSimple()
{
    var factory = new ConnectionFactory()
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };

    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    Console.WriteLine("[✅] Connecté à RabbitMQ");

    const string QUEUE_NAME = "parking.evenements";

    var dlqArgs = new Dictionary<string, object?>
    {
        { "x-dead-letter-exchange", "parking.dlx" },
        { "x-message-ttl", 30000 }
    };

    await channel.QueueDeleteAsync(QUEUE_NAME);

    await channel.QueueDeclareAsync(
        queue: QUEUE_NAME,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: dlqArgs
    );

    await channel.BasicQosAsync(0, 1, false);

    Console.WriteLine($"[✅] En attente de messages sur '{QUEUE_NAME}'...");
    Console.WriteLine();

    var consumer = new AsyncEventingBasicConsumer(channel);

    consumer.ReceivedAsync += async (model, ea) =>
    {
        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            var evenement = JsonSerializer.Deserialize<EvenementParking>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // 🔥 TEST DLQ
            if (evenement?.PlaceId == 3)
            {
                throw new Exception("Erreur simulée pour DLQ");
            }

            if (evenement is not null)
            {
                var icone = evenement.Type == "PlaceOccupee" ? "🔴" : "🟢";
                Console.WriteLine($"{icone} [{evenement.Timestamp:HH:mm:ss}] " +
                                  $"{evenement.Type} | Place {evenement.PlaceId}");
            }

            // ✅ ACK si OK
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[❌] Erreur : {ex.Message}");

            // ❗ DLQ ACTIVÉ ICI
            await channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: false // 🚨 envoie vers DLQ
            );
        }
    };

    await channel.BasicConsumeAsync(
        queue: QUEUE_NAME,
        autoAck: false,
        consumer: consumer
    );

    Console.ReadLine();
}