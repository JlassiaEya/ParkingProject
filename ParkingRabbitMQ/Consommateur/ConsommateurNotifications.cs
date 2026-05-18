// ConsommateurNotifications.cs — Second abonné au Fanout

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ParkingRabbitMQ.Models;

public class ConsommateurNotifications
{
    private const string EXCHANGE_NAME = "parking.events.fanout";
    private const string QUEUE_NAME = "notifications.queue";

    // Bonus 3 : Compteur en mémoire
    private static Dictionary<int, bool> _etatPlaces = new();

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: EXCHANGE_NAME, type: ExchangeType.Fanout, durable: true
        );
        await channel.QueueDeclareAsync(
            queue: QUEUE_NAME, durable: true,
            exclusive: false, autoDelete: false, arguments: null
        );
        await channel.QueueBindAsync(
            queue: QUEUE_NAME, exchange: EXCHANGE_NAME, routingKey: ""
        );

        Console.WriteLine("[🔔 Notifications] En attente d'événements...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<EvenementParking>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (evt != null)
            {
                // Mise à jour du compteur en mémoire
                _etatPlaces[evt.PlaceId] = evt.Type == "PlaceOccupee";

                if (evt.Type == "PlaceOccupee")
                    Console.WriteLine($"  [🔔 NOTIF] ⚠️  Alerte : Place {evt.PlaceId} vient d'être OCCUPÉE");
                else
                    Console.WriteLine($"  [🔔 NOTIF] ✅  Place {evt.PlaceId} est LIBRE");

                // Bonus 3 : Seuil d'occupation (>= 4 places)
                int placesOccupees = _etatPlaces.Values.Count(v => v);
                if (placesOccupees >= 4)
                {
                    var alerte = new EvenementParking
                    {
                        Type = "Alerte",
                        PlaceId = 0,
                        Message = $"Seuil critique atteint ! {placesOccupees}/5 places occupées."
                    };
                    var bodyAlerte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(alerte));

                    await channel.BasicPublishAsync(
                        exchange: "parking.events.direct",
                        routingKey: "alerte",
                        mandatory: false,
                        basicProperties: new BasicProperties { Persistent = true },
                        body: bodyAlerte
                    );
                    Console.WriteLine($"  [🔔 NOTIF] 🚨 PUBLICATION ALERTE AU SERVICE D'ALERTE : {alerte.Message}");
                }
            }

            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(QUEUE_NAME, autoAck: false, consumer: consumer);
        Console.ReadLine();
    }
}
