// ProducteurTopic.cs
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ParkingRabbitMQ.Models;

public class ProducteurTopic
{
    public const string EXCHANGE = "parking.events.topic";

    // Routing keys hiérarchiques
    public const string RK_PLACE_OCCUPEE   = "parking.places.occupee";
    public const string RK_PLACE_LIBREE    = "parking.places.libree";
    public const string RK_ALERTE_CO2      = "parking.alertes.co2";
    public const string RK_ALERTE_PLACES   = "parking.alertes.places";
    public const string RK_SYSTEME_BOOT    = "parking.systeme.demarrage";

    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel    = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange:    EXCHANGE,
            type:        ExchangeType.Topic,
            durable:     true,
            autoDelete:  false
        );

        Console.WriteLine($"[✅] Exchange Topic '{EXCHANGE}' déclaré\n");

        // Publier un événement de démarrage système
        await PublierAsync(channel, RK_SYSTEME_BOOT,
            new EvenementParking { Type = "SystemeDemarrage", Message = "Service Places démarré" });

        Console.WriteLine("Publication continue... (Ctrl+C pour arrêter)\n");

        var random    = new Random();
        var placeIds  = new[] { 1, 2, 3, 4, 5 };
        int compteur  = 0;
        int occupees  = 0; // compteur pour détecter le seuil d'alerte

        while (true)
        {
            compteur++;
            var placeId   = placeIds[random.Next(placeIds.Length)];
            var estOccupee = random.NextDouble() > 0.4;
            // 1. Événement place
            var rkPlace = estOccupee ? RK_PLACE_OCCUPEE : RK_PLACE_LIBREE;
            var evtPlace = new EvenementParking
            {
                Type     = estOccupee ? "PlaceOccupee" : "PlaceLibree",
                PlaceId  = placeId,
                Message  = $"Place {placeId} → {(estOccupee ? "OCCUPÉE" : "LIBRE")}"
            };
            await PublierAsync(channel, rkPlace, evtPlace);
            Console.WriteLine($"[{compteur:D4}] {rkPlace,-28} → {evtPlace.Message}");
            // 2. Simuler une alerte CO2 tous les 8 messages
            if (compteur % 8 == 0)
            {
                var co2  = random.Next(400, 1200);
                var evtCo2 = new EvenementParking
                {
                    Type    = co2 > 800 ? "AlerteCO2" : "CO2Normal",
                    Message = $"CO2 mesuré : {co2} ppm{(co2 > 800 ? " ⚠️" : "")}"
                };
                await PublierAsync(channel, RK_ALERTE_CO2, evtCo2);
                Console.WriteLine($"[{compteur:D4}] {RK_ALERTE_CO2,-28} → {evtCo2.Message}");
            }
            // 3. Alerte taux d'occupation tous les 15 messages
            if (compteur % 15 == 0)
            {
                occupees = random.Next(0, 6);
                var taux = (occupees * 100) / 5;
                var evtOccup = new EvenementParking
                {
                    Type    = taux >= 80 ? "AlerteOccupation" : "OccupationNormale",
                    Message = $"Taux occupation : {taux}% ({occupees}/5 places)"
                };
                await PublierAsync(channel, RK_ALERTE_PLACES, evtOccup);
                Console.WriteLine($"[{compteur:D4}] {RK_ALERTE_PLACES,-28} → {evtOccup.Message}");
            }

            await Task.Delay(1500);
        }
    }

    private static async Task PublierAsync(IChannel channel, string rk,
                                           EvenementParking evt)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        await channel.BasicPublishAsync(
            exchange:        EXCHANGE,
            routingKey:      rk,
            mandatory:       false,
            basicProperties: new BasicProperties { Persistent = true },
            body:            body
        );
    }
}
