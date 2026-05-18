// ProducteurFanout.cs — Producteur avec exchange Fanout

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ParkingRabbitMQ.Models;

public class ProducteurFanout
{
    private const string EXCHANGE_NAME = "parking.events.fanout";

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // ── Déclarer l'exchange Fanout ─────────────────────────────
        // ExchangeType.Fanout = broadcast vers toutes les queues liées
        await channel.ExchangeDeclareAsync(
            exchange: EXCHANGE_NAME,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false
        );

        Console.WriteLine($"[✅] Exchange Fanout '{EXCHANGE_NAME}' déclaré");
        Console.WriteLine("Publication d'événements Fanout... (Ctrl+C pour arrêter)");

        var random = new Random();
        int i = 0;
        while (true)
        {
            var evt = new EvenementParking
            {
                Type = random.NextDouble() > 0.3 ? "PlaceOccupee" : "PlaceLibree",
                PlaceId = random.Next(1, 6),
            };
            evt.Message = $"Place {evt.PlaceId} → {evt.Type}";

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

            // Avec Fanout, la routingKey est ignorée — on met une chaîne vide
            await channel.BasicPublishAsync(
                exchange: EXCHANGE_NAME,
                routingKey: "",     // ignorée par Fanout
                mandatory: false,
                basicProperties: new BasicProperties { Persistent = true },
                body: body
            );

            Console.WriteLine($"[{++i:D4}] Fanout → {evt.Type} | Place {evt.PlaceId}");
            await Task.Delay(2000);
        }
    }
}
