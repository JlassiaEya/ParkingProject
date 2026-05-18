using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

namespace Subscriber.Services;

public class SubscriberService
{
    private const string BROKER_HOST = "localhost";
    private const int BROKER_PORT = 1883;
    private const string TOPIC_FILTER = "parking/places/#";

    private readonly IMqttClient _client;
    private int _messagesRecus = 0;

    // ✅ Stockage état des places
    private readonly Dictionary<string, (string Etat, DateTime LastUpdate)> _etatConnu = new();

    public SubscriberService()
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
    }

    // ── Connexion et abonnement ──────────────────────────────
    public async Task ConnecterEtAbonnerAsync(CancellationToken ct)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(BROKER_HOST, BROKER_PORT)
            .WithClientId($"subscriber-dashboard-{Guid.NewGuid():N[..8]}")
            .WithCleanSession(false)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .Build();

        // Handler : message recu
        _client.ApplicationMessageReceivedAsync += OnMessageRecuAsync;

        // Handler : connecte
        _client.ConnectedAsync += async e =>
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connecte — En attente de messages...");
            Console.ResetColor();

            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(TOPIC_FILTER)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.SubscribeAsync(topicFilter, ct);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Abonne a : {TOPIC_FILTER}");
            Console.WriteLine(new string('=', 70));
        };

        // Handler : reconnexion
        _client.DisconnectedAsync += async e =>
        {
            if (!ct.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Deconnecte — Tentative dans 5s");
                Console.ResetColor();

                await Task.Delay(5000, ct);
                try { await _client.ConnectAsync(options, ct); }
                catch { }
            }
        };

        await _client.ConnectAsync(options, ct);

        // ✅ Lancer le dashboard toutes les 10 secondes
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(10000, ct);
                AfficherTableauDeBord();
            }
        });
    }

    // ── Réception message ─────────────────────────
    private async Task OnMessageRecuAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        _messagesRecus++;

        string topic = e.ApplicationMessage.Topic;
        string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

        // Extraire ID
        string[] parts = topic.Split('/');
        string placeId = parts.Length >= 3 ? parts[2] : "???";

        // Désérialisation
        PlaceInfo? info = null;

        try
        {
            info = JsonSerializer.Deserialize<PlaceInfo>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { }

        Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] ");
        Console.Write($"#{_messagesRecus,-5} ");

        if (info != null)
        {
            // ✅ Mise à jour du dashboard
            _etatConnu[info.PlaceId] = (info.Etat, DateTime.Now);

            Console.ForegroundColor = info.Etat == "libre"
                ? ConsoleColor.Green : ConsoleColor.Red;

            Console.Write($"Place {info.PlaceId,-4} [{info.Etat,-8}]");
            Console.ResetColor();

            Console.Write($"  {info.CapteurId,-10}");
            Console.WriteLine($"  Batt:{info.NiveauBatterie}%  QoS:{(int)e.ApplicationMessage.QualityOfServiceLevel}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Topic: {topic} → {payload}");
            Console.ResetColor();
        }

        await Task.CompletedTask;
    }

    // ── Dashboard ─────────────────────────
    private void AfficherTableauDeBord()
    {
        Console.Clear();

        Console.WriteLine("=== TABLEAU DE BORD PARKING ===");
        Console.WriteLine($"Mise a jour : {DateTime.Now:HH:mm:ss}");
        Console.WriteLine(new string('-', 40));

        foreach (var (place, (etat, ts)) in _etatConnu.OrderBy(k => k.Key))
        {
            Console.ForegroundColor = etat == "libre"
                ? ConsoleColor.Green : ConsoleColor.Red;

            Console.WriteLine($"  {place,-6} : {etat,-8}  (MAJ: {ts:HH:mm:ss})");

            Console.ResetColor();
        }
    }

    public async Task AttendreAsync(CancellationToken ct)
    {
        await Task.Delay(Timeout.Infinite, ct);
    }

    public async Task ArretAsync()
    {
        Console.WriteLine($"\n[INFO] Total messages recus : {_messagesRecus}");

        if (_client.IsConnected)
            await _client.DisconnectAsync();
    }
}

// Record pour JSON
internal record PlaceInfo(
    string PlaceId,
    string Etat,
    string Timestamp,
    string CapteurId,
    int NiveauBatterie,
    int Rssi
);