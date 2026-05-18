using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using ServicePlaces.Models;
using ServicePlaces.Services;

namespace ServicePlaces.Services;

public class MqttBackgroundService : IHostedService, IAsyncDisposable
{
    private readonly IPlaceRepository _repository;
    private readonly ILogger<MqttBackgroundService> _logger;
    private readonly MqttOptions _options;
    private readonly PlaceChangeNotifier _notifier;  // ✅ ajouté
    private IMqttClient? _mqttClient;
    private bool _isStopping = false;
    private readonly IConfiguration _config;

    public MqttBackgroundService(
        IPlaceRepository repository,
        ILogger<MqttBackgroundService> logger,
        IOptions<MqttOptions> options,
        IConfiguration config,
        PlaceChangeNotifier notifier)  // ✅ ajouté
    {
        _repository = repository;
        _logger = logger;
        _options = options.Value;
        _config = config;
        _notifier = notifier;        // ✅ ajouté
    }

    // ───────────── START ─────────────
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        var mqttOptions = BuildOptions();

        try
        {
            await _mqttClient.ConnectAsync(mqttOptions, cancellationToken);
            _logger.LogInformation("[MQTT] Connecté au broker {Host}:{Port}",
                _options.Host, _options.Port);

            await SubscribeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Échec de connexion au broker");
        }
    }

    // ───────────── MESSAGE HANDLER ─────────────
    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

        _logger.LogDebug("[MQTT] Message reçu | Topic: {Topic} | Payload: {Payload}",
            topic, payload);

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var placeIdStr = root.GetProperty("placeId").GetString();
            if (!int.TryParse(placeIdStr!.TrimStart('A'), out int placeId))
            {
                _logger.LogWarning("[MQTT] PlaceId invalide : {PlaceId}", placeIdStr);
                return Task.CompletedTask;
            }

            var etat = root.GetProperty("etat").GetString();
            bool estOccupee = etat == "occupee";
            var timestamp = root.GetProperty("timestamp").GetDateTime();

            _repository.UpdateEtat(placeId, estOccupee, timestamp);

            // ✅ Notifier les clients gRPC connectés
            var place = _repository.GetById(placeId);
            if (place is not null)
            {
                _notifier.Notify(new PlaceUpdate
                {
                    PlaceId = place.Id,
                    Numero = place.Numero,
                    EstOccupee = place.EstOccupee,
                    Timestamp = DateTime.UtcNow.ToString("O")
                });
            }

            _logger.LogInformation("[MQTT] Place {Id} → {Etat}",
                placeId, estOccupee ? "OCCUPÉE" : "LIBRE");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[MQTT] Erreur JSON");
        }

        return Task.CompletedTask;
    }

    // ───────────── RECONNEXION AUTO ─────────────
    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        if (_isStopping) return;

        _logger.LogWarning("[MQTT] Connexion perdue. Reconnexion...");

        int delay = 1;
        const int maxDelay = 60;

        while (_mqttClient is not null && !_mqttClient.IsConnected && !_isStopping)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), CancellationToken.None);
                await _mqttClient.ConnectAsync(BuildOptions());
                _logger.LogInformation("[MQTT] Reconnecté !");
                await SubscribeAsync();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] Échec reconnexion, nouvelle tentative dans {Delay}s", delay);
                delay = Math.Min(delay * 2, maxDelay);
            }
        }
    }

    // ───────────── SUBSCRIBE ─────────────
    private async Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        if (_mqttClient is null) return;

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(_options.Topic)
                .WithQualityOfServiceLevel(
                    MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
        _logger.LogInformation("[MQTT] Abonné au topic {Topic}", _options.Topic);
    }

    // ───────────── OPTIONS MQTT ─────────────
    private MqttClientOptions BuildOptions()
    {
        var useTls = _config.GetValue<bool>("Mqtt:UseTls");
        var caCertPath = _config["Mqtt:CaCertPath"];

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithClientId(_options.ClientId)
            .WithCleanSession(true);

        if (useTls)
        {
            optionsBuilder.WithTlsOptions(tls =>
            {
                if (!string.IsNullOrEmpty(caCertPath))
                {
                    var caCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(caCertPath);
                    tls.WithCertificateValidationHandler(args =>
                    {
                        var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
                        chain.ChainPolicy.ExtraStore.Add(caCert);
                        chain.ChainPolicy.VerificationFlags =
                            System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllowUnknownCertificateAuthority;
                        chain.ChainPolicy.RevocationMode =
                            System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                        bool isValid = chain.Build(
                            new System.Security.Cryptography.X509Certificates.X509Certificate2(args.Certificate));
                        if (!isValid)
                            _logger.LogWarning("[MQTT TLS] Validation du certificat serveur echouee");
                        return isValid;
                    });
                }
            });
        }
        return optionsBuilder.Build();
    }

    // ───────────── STOP ─────────────
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isStopping = true;

        if (_mqttClient is not null)
        {
            _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;

            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync(
                    new MqttClientDisconnectOptionsBuilder().Build(),
                    cancellationToken);
            }
            _logger.LogInformation("[MQTT] Déconnecté proprement");
        }
    }

    // ───────────── DISPOSE ─────────────
    public ValueTask DisposeAsync()
    {
        _mqttClient?.Dispose();
        return ValueTask.CompletedTask;
    }
}