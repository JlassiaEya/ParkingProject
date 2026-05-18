using CapteurSimule.Models;
using CapteurSimule.Services;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CapteurSimule.Services;

public class CapteurService
{
    // ── Configuration ──────────────────────────────────────
    private const string BROKER_HOST = "localhost";
    private const int BROKER_PORT = 8883;
    private const string CA_CERT_PATH = "C:/certs-mqtt/ca.crt";
    private const int NB_PLACES = 10;     // Nombre de places simulées
    private const int INTERVALLE_MS = 500;
    private readonly IMqttClient _client;
    private readonly Random _random = new();
    private readonly string _clientId;

    // État en mémoire de chaque place (simule la mémoire du capteur)
    private readonly Dictionary<string, string> _etatsPlaces = new();

    public CapteurService()
    {
        _clientId = $"capteur-simulateur-{Guid.NewGuid().ToString("N")[..8]}";
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();
        InitialiserEtats();
    }

    // ── Initialisation des états des places ─────────────────
    private void InitialiserEtats()
    {
        for (int i = 1; i <= NB_PLACES; i++)
            _etatsPlaces[$"A{i}"] = "libre";
    }

    // ── Connexion avec retry ────────────────────────────────
    public async Task ConnecterAsync(CancellationToken ct)
    {
        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(BROKER_HOST, BROKER_PORT)
            .WithClientId(_clientId)
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

        optionsBuilder.WithTlsOptions(tls =>
        {
            var caCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(CA_CERT_PATH);
            tls.WithCertificateValidationHandler(args =>
            {
                var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
                chain.ChainPolicy.ExtraStore.Add(caCert);
                chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                return chain.Build(new System.Security.Cryptography.X509Certificates.X509Certificate2(args.Certificate));
            });
        });

        var options = optionsBuilder.Build();

        // Handlers d'événements
        _client.ConnectedAsync += async e =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connecte au broker MQTT");
            Console.ResetColor();
            await Task.CompletedTask;
        };

        _client.DisconnectedAsync += async e =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Deconnecte — Reconnexion dans 5s...");
            Console.ResetColor();
            await Task.Delay(5000, ct);
            try { await _client.ConnectAsync(options, ct); }
            catch { /* Le handler sera rappele automatiquement */ }
        };

        // Connexion initiale avec retry
        int tentative = 1;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Tentative {tentative} de connexion...");
                await _client.ConnectAsync(options, ct);
                break;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERREUR] Connexion echouee : {ex.Message}");
                Console.ResetColor();
                tentative++;
                await Task.Delay(5000, ct);
            }
        }
    }

    // ── Boucle principale de publication ────────────────────
    public async Task DemarrerPublicationAsync(CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Simulation demarree — {NB_PLACES} places");
        Console.WriteLine(new string('-', 60));

        while (!ct.IsCancellationRequested)
        {
            // Choisir une place aleatoire a mettre a jour
            int index = _random.Next(1, NB_PLACES + 1);
            string placeId = $"A{index}";

            // Simuler un changement d'etat (70% de chances de changer)
            if (_random.NextDouble() < 0.7)
            {
                _etatsPlaces[placeId] = _etatsPlaces[placeId] == "libre"
                    ? "occupee" : "libre";
            }

            var etat = new PlaceEtat
            {
                PlaceId = placeId,
                Etat = _etatsPlaces[placeId],
                Timestamp = DateTime.UtcNow.ToString("o"),
                CapteurId = $"CAP-{index:D3}",
                NiveauBatterie = _random.Next(60, 100),
                Rssi = _random.Next(-80, -40),
            };

            await PublierEtatAsync(placeId, etat, ct);
            AfficherEtat(etat);
            
            await PublierQualiteAirAsync(ct);

            await Task.Delay(INTERVALLE_MS, ct);
        }
    }

    private async Task PublierQualiteAirAsync(CancellationToken ct)
    {
        if (!_client.IsConnected) return;

        var co2 = new
        {
            co2Ppm = _random.Next(350, 1500),
            temperature = Math.Round(18 + _random.NextDouble() * 10, 1),
            timestamp = DateTime.UtcNow.ToString("o")
        };

        string json = JsonSerializer.Serialize(co2, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("parking/qualite/co2")
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    // ── Publication d'un message ─────────────────────────────
    private async Task PublierEtatAsync(string placeId, PlaceEtat etat, CancellationToken ct)
    {
        if (!_client.IsConnected) return;

        string json = JsonSerializer.Serialize(etat, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"parking/places/{placeId}/etat")
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(true)   // Conserver le dernier etat connu
            .Build();

        await _client.PublishAsync(message, ct);
    }

    // ── Affichage console ────────────────────────────────────
    private void AfficherEtat(PlaceEtat etat)
    {
        Console.ForegroundColor = etat.Etat == "libre"
            ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($"  [{DateTime.Now:HH:mm:ss}] ");
        Console.Write($"Place {etat.PlaceId,-4} → {etat.Etat,-8}");
        Console.ResetColor();
        Console.WriteLine($"  Batt:{etat.NiveauBatterie}%  RSSI:{etat.Rssi}dBm");
    }

    public async Task ArretAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
    }

    // A ajouter dans CapteurService.cs
    private async Task PublierStatistiquesAsync(CancellationToken ct)
    {
        int nbOccupees = _etatsPlaces.Values.Count(e => e == "occupee");
        int nbLibres = _etatsPlaces.Values.Count(e => e == "libre");

        var stats = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            total = _etatsPlaces.Count,
            occupees = nbOccupees,
            libres = nbLibres,
            tauxOccupation = Math.Round((double)nbOccupees / _etatsPlaces.Count * 100, 1),
        };

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic("parking/statistiques/resume")
            .WithPayload(JsonSerializer.Serialize(stats))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(true)
            .Build();

        await _client.PublishAsync(msg, ct);
    }
    private async Task VerifierAlerteCapaciteAsync(CancellationToken ct)
    {
        double taux = (double)_etatsPlaces.Values.Count(e => e == "occupee")
                      / _etatsPlaces.Count * 100;

        if (taux >= 90.0)
        {
            var alerte = new
            {
                niveau = "CRITIQUE",
                message = "Capacite maximale presque atteinte",
                taux = taux,
                timestamp = DateTime.UtcNow.ToString("o"),
            };

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic("parking/alertes/capacite")
                .WithPayload(JsonSerializer.Serialize(alerte))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce) // QoS 2
                .Build();

            await _client.PublishAsync(msg, ct);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[ALERTE] Taux occupation : {taux:F1}% — Message QoS2 envoye");
            Console.ResetColor();
        }
    }

}
