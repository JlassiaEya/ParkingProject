using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using ServiceQualite.Models;

namespace ServiceQualite.Services;

public class MqttQualiteBackgroundService : IHostedService, IAsyncDisposable
{
    private readonly IQualiteRepository _repository;
    private readonly ILogger<MqttQualiteBackgroundService> _logger;
    private readonly IConfiguration _config;
    private IMqttClient? _mqttClient;

    public MqttQualiteBackgroundService(
        IQualiteRepository repository,
        ILogger<MqttQualiteBackgroundService> logger,
        IConfiguration config)
    {
        _repository = repository;
        _logger = logger;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        var useTls = _config.GetValue<bool>("Mqtt:UseTls");
        var host = _config["Mqtt:Host"] ?? "localhost";
        var port = _config.GetValue<int>("Mqtt:Port", 1883);
        var clientId = _config["Mqtt:ClientId"] ?? "service-qualite";
        var caCertPath = _config["Mqtt:CaCertPath"];

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(clientId)
            .WithCleanSession(true);

        if (useTls && !string.IsNullOrEmpty(caCertPath))
        {
            optionsBuilder.WithTlsOptions(tls =>
            {
                var caCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(caCertPath);
                tls.WithCertificateValidationHandler(args =>
                {
                    var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
                    chain.ChainPolicy.ExtraStore.Add(caCert);
                    chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain.ChainPolicy.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                    return chain.Build(new System.Security.Cryptography.X509Certificates.X509Certificate2(args.Certificate));
                });
            });
        }

        try
        {
            await _mqttClient.ConnectAsync(optionsBuilder.Build(), cancellationToken);
            _logger.LogInformation("[MQTT] Connecté au broker sur port {Port}", port);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic("parking/qualite/co2"))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Erreur lors de la connexion");
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mesure = JsonSerializer.Deserialize<DonneesQualite>(payload, options);
            if (mesure is not null)
            {
                _repository.AjouterMesure(mesure);
                _logger.LogInformation("[Qualite] CO2 recu : {ppm} ppm", mesure.Co2Ppm);
            }
        }
        catch (JsonException) { }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient is not null && _mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        _mqttClient?.Dispose();
        return ValueTask.CompletedTask;
    }
}
