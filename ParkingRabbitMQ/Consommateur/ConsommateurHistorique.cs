// ConsommateurHistorique.cs — S'abonne à l'exchange Fanout

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ParkingRabbitMQ.Models;

public class ConsommateurHistorique
{
    private const string EXCHANGE_NAME = "parking.events.fanout";
    private const string QUEUE_NAME = "historique.queue";

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // ── Déclarer l'exchange (idempotent) ──────────────────────
        await channel.ExchangeDeclareAsync(
            exchange: EXCHANGE_NAME,
            type: ExchangeType.Fanout,
            durable: true
        );

        // ── Déclarer la queue de ce service ───────────────────────
        await channel.QueueDeclareAsync(
            queue: QUEUE_NAME, durable: true,
            exclusive: false, autoDelete: false, arguments: null
        );

        // ── Lier la queue à l'exchange (binding) ──────────────────
        // Avec Fanout, routingKey est ignorée
        await channel.QueueBindAsync(
            queue: QUEUE_NAME,
            exchange: EXCHANGE_NAME,
            routingKey: ""
        );

        Console.WriteLine("[📜 Historique] En attente d'événements...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var evt = JsonSerializer.Deserialize<EvenementParking>(
                Encoding.UTF8.GetString(ea.Body.ToArray()),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            Console.WriteLine($"  [📜 HISTORIQUE] Archivé → {evt?.Type} Place {evt?.PlaceId} @ {evt?.Timestamp:HH:mm:ss}");
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(QUEUE_NAME, autoAck: false, consumer: consumer);
        Console.ReadLine();
    }
}
