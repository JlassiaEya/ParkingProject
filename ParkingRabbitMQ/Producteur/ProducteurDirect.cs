// ProducteurDirect.cs

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ParkingRabbitMQ.Models;

public class ProducteurDirect
{
    private const string EXCHANGE_NAME = "parking.events.direct";

    // Routing keys disponibles
    public const string RK_PLACE_OCCUPEE = "place.occupee";
    public const string RK_PLACE_LIBREE = "place.libree";
    public const string RK_ALERTE = "alerte";

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Déclarer l'exchange Direct
        await channel.ExchangeDeclareAsync(
            exchange: EXCHANGE_NAME,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        );

        Console.WriteLine($"[✅] Exchange Direct '{EXCHANGE_NAME}' déclaré");

        var random = new Random();
        int i = 0;
        while (true)
        {
            var tirage = random.NextDouble();
            EvenementParking evt;
            string routingKey;

            if (tirage < 0.1) // 10% d'alertes
            {
                evt = new EvenementParking
                {
                    Type = "Alerte",
                    PlaceId = 0,
                    Message = "Parking presque plein (>80%)"
                };
                routingKey = RK_ALERTE;
            }
            else if (tirage < 0.55) // 45% occupées
            {
                int placeId = random.Next(1, 6);
                evt = new EvenementParking
                {
                    Type = "PlaceOccupee",
                    PlaceId = placeId,
                    Message = $"Place {placeId} OCCUPÉE"
                };
                routingKey = RK_PLACE_OCCUPEE;
            }
            else // 45% libres
            {
                int placeId = random.Next(1, 6);
                evt = new EvenementParking
                {
                    Type = "PlaceLibree",
                    PlaceId = placeId,
                    Message = $"Place {placeId} LIBRE"
                };
                routingKey = RK_PLACE_LIBREE;
            }

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

            await channel.BasicPublishAsync(
                exchange: EXCHANGE_NAME,
                routingKey: routingKey,  // ← Clé de routing
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true },
                body: body
            );

            Console.WriteLine($"[{++i:D4}] Direct [{routingKey,-15}] → {evt.Message}");
            await Task.Delay(1500);
        }
    }
}
