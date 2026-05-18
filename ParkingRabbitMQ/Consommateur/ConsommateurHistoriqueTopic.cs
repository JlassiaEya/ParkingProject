// ConsommateurHistoriqueTopic.cs
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ParkingRabbitMQ.Models;

public class ConsommateurHistoriqueTopic
{
    private const string EXCHANGE = "parking.events.topic";
    private const string QUEUE    = "historique.topic.queue";
    private const string PATTERN  = "parking.#";   // Reçoit TOUT

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel    = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(EXCHANGE, ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync(QUEUE, durable: true,
            exclusive: false, autoDelete: false, arguments: null);
        await channel.QueueBindAsync(QUEUE, EXCHANGE, PATTERN);

        Console.WriteLine($"[📜 Historique] Abonné au pattern : {PATTERN} (reçoit tout)");
        int total = 0;
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            total++;
            var rk  = ea.RoutingKey;
            var evt = JsonSerializer.Deserialize<EvenementParking>(
                Encoding.UTF8.GetString(ea.Body.ToArray()),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            Console.WriteLine($"  [{total:D4}] [HISTORIQUE] [{rk,-30}] " +
                              $"{evt?.Type,-20} {evt?.Timestamp:HH:mm:ss}");
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };
        await channel.BasicConsumeAsync(QUEUE, autoAck: false, consumer: consumer);
        Console.WriteLine("En attente... (Entrée pour quitter)");
        Console.ReadLine();
    }
}
