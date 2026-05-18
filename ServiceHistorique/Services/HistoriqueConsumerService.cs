using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceHistorique.Models;

namespace ServiceHistorique.Services;

public class HistoriqueConsumerService : IHostedService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly HistoriqueRepository _repo;
    private readonly ILogger<HistoriqueConsumerService> _logger;
    private readonly string _rabbitHost;
    private const string ExchangeName = "parking.alertes";
    private const string QueueName = "historique.parking";

    public HistoriqueConsumerService(
        HistoriqueRepository repo,
        ILogger<HistoriqueConsumerService> logger,
        IConfiguration config)
    {
        _repo = repo;
        _logger = logger;
        _rabbitHost = config["RabbitMq:Host"] ?? "localhost";
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory { HostName = _rabbitHost };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "", cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var alerte = JsonSerializer.Deserialize<JsonElement>(body, options);

                _repo.Ajouter(new EvenementHistorique
                {
                    PlaceId = alerte.TryGetProperty("placeId", out var pid) ? pid.GetInt32() : 0,
                    Numero = alerte.TryGetProperty("numero", out var num) ? num.GetString()! : "",
                    EstOccupee = alerte.TryGetProperty("tauxOccupation", out var t) && t.GetInt32() >= 80,
                    Timestamp = DateTime.UtcNow,
                    Source = "AMQP"
                });

                _logger.LogInformation("[Historique] Événement stocké");
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Historique] Erreur traitement");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: ct);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: ct);
        _logger.LogInformation("[Historique] En écoute sur '{Ex}'", ExchangeName);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_channel is not null) await _channel.CloseAsync(ct);
        if (_connection is not null) await _connection.CloseAsync(ct);
    }
}