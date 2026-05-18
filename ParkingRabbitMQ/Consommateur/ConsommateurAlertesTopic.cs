// ConsommateurAlertesTopic.cs
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ParkingRabbitMQ.Models;



public class ConsommateurAlertesTopic
{
    private const string EXCHANGE = "parking.events.topic";
    private const string QUEUE    = "alertes.topic.queue";
    private const string PATTERN  = "parking.alertes.*";

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel    = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(EXCHANGE, ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync(QUEUE, durable: true,
            exclusive: false, autoDelete: false, arguments: null);
        await channel.QueueBindAsync(QUEUE, EXCHANGE, PATTERN);

        Console.WriteLine($"[🚨 Alertes] Abonné au pattern : {PATTERN}");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var rk  = ea.RoutingKey;
            var evt = JsonSerializer.Deserialize<EvenementParking>(
                Encoding.UTF8.GetString(ea.Body.ToArray()),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            var estCritique = evt?.Type?.StartsWith("Alerte") == true;
            var icone = estCritique ? "⚠️ " : "ℹ️ ";
            Console.WriteLine($"  {icone} [ALERTES] [{rk}] {evt?.Message}");
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(QUEUE, autoAck: false, consumer: consumer);
        Console.WriteLine("En attente... (Entrée pour quitter)");
        Console.ReadLine();
    }
}
