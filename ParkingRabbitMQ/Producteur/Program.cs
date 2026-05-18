using ParkingRabbitMQ.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

Console.WriteLine("=== Producteur RabbitMQ — Parking Intelligent ===");
Console.WriteLine();

// ─────────────────────────────────────────────
// 1. Connexion
// ─────────────────────────────────────────────
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

// ─────────────────────────────────────────────
// 2. DLX + DLQ
// ─────────────────────────────────────────────
await channel.ExchangeDeclareAsync(
    exchange: "parking.dlx",
    type: ExchangeType.Fanout,
    durable: true
);

await channel.QueueDeclareAsync(
    queue: "parking.dlq",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

await channel.QueueBindAsync(
    queue: "parking.dlq",
    exchange: "parking.dlx",
    routingKey: ""
);

// ─────────────────────────────────────────────
// 3. Queue principale AVEC DLQ
// ─────────────────────────────────────────────
var dlqArgs = new Dictionary<string, object?>
{
    { "x-dead-letter-exchange", "parking.dlx" },
    { "x-message-ttl", 30000 } // Bonus 2 : TTL de 30 secondes
};

const string QUEUE_NAME = "parking.evenements";

await channel.QueueDeleteAsync(QUEUE_NAME);

await channel.QueueDeclareAsync(
    queue: QUEUE_NAME,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: dlqArgs
);

Console.WriteLine($"[✅] Queue '{QUEUE_NAME}' avec DLQ activée");

// ─────────────────────────────────────────────
// 4. MENU
// ─────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("Choisissez le mode :");
Console.WriteLine("  1 - Producteur simple");
Console.WriteLine("  2 - Producteur Fanout");
Console.WriteLine("  3 - Producteur Topic");
Console.Write("Votre choix : ");

var choix = Console.ReadLine();

switch (choix)
{
    case "2":
        await ProducteurFanout.RunAsync();
        break;

    case "3":
        await ProducteurTopic.RunAsync();
        break;

    default:
        await RunProducteurSimple(channel);
        break;
}

// ─────────────────────────────────────────────
// 5. PRODUCTEUR SIMPLE
// ─────────────────────────────────────────────
async Task RunProducteurSimple(IChannel channel)
{
    Console.WriteLine("Publication toutes les 2 secondes...");
    Console.WriteLine();
var random = new Random();
var placeIds = new[] { 1, 2, 3, 4, 5 };
int compteur = 0;

while (true)
{
    var placeId = placeIds[random.Next(placeIds.Length)];
    var estOccupee = random.NextDouble() > 0.4;

    var evenement = new EvenementParking
    {
        Type = estOccupee ? "PlaceOccupee" : "PlaceLibree",
        PlaceId = placeId,
        Timestamp = DateTime.UtcNow,
        Message = $"Place {placeId} est {(estOccupee ? "OCCUPÉE" : "LIBRE")}"
    };

    var json = JsonSerializer.Serialize(evenement);
    var body = Encoding.UTF8.GetBytes(json);

    var props = new BasicProperties { Persistent = true };

    await channel.BasicPublishAsync(
        exchange: "",
        routingKey: QUEUE_NAME,
        mandatory: false,
        basicProperties: props,
        body: body
    );

    compteur++;
    Console.WriteLine($"[{compteur:D4}] {evenement.Type} | Place {placeId}");

    await Task.Delay(2000);
}

}