// ConsommateurPlacesDirect.cs

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ParkingRabbitMQ.Models;

public class ConsommateurPlacesDirect
{
    private const string EXCHANGE_NAME = "parking.events.direct";
    private const string QUEUE_NAME = "places.queue";

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: EXCHANGE_NAME, type: ExchangeType.Direct, durable: true
        );
        await channel.QueueDeclareAsync(
            queue: QUEUE_NAME, durable: true,
            exclusive: false, autoDelete: false, arguments: null
        );

        // ← Binding avec DEUX routing keys → cette queue reçoit les deux
        await channel.QueueBindAsync(QUEUE_NAME, EXCHANGE_NAME, ProducteurDirect.RK_PLACE_OCCUPEE);
        await channel.QueueBindAsync(QUEUE_NAME, EXCHANGE_NAME, ProducteurDirect.RK_PLACE_LIBREE);

        Console.WriteLine("[🏪 Places] Abonné à 'place.occupee' et 'place.libree'");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var evt = JsonSerializer.Deserialize<EvenementParking>(
                Encoding.UTF8.GetString(ea.Body.ToArray()),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            var icone = evt?.Type == "PlaceOccupee" ? "🔴" : "🟢";
            Console.WriteLine($"  {icone} [PLACES] Mise à jour → {evt?.Message}");
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(QUEUE_NAME, autoAck: false, consumer: consumer);
        Console.ReadLine();
    }
}

