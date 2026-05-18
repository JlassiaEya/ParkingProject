using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceNotifications.Models;

namespace ServiceNotifications.Services;

public class NotificationConsumerService : IHostedService, IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationConsumerService> _logger;
    private const string ExchangeName = "parking.alertes";
    private const string QueueName    = "notifications.parking";

    public NotificationConsumerService(
        INotificationRepository repo,
        ILogger<NotificationConsumerService> logger,
        IConfiguration config)
    {
        _repo   = repo;
        _logger = logger;
        var host = config["RabbitMq:Host"] ?? "localhost";
        var factory = new ConnectionFactory { HostName = host };
        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _channel!.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, ExchangeName, "");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            var body    = Encoding.UTF8.GetString(ea.Body.ToArray());
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var alerte  = JsonSerializer.Deserialize<AlerteMessage>(body, options);
            if (alerte is not null)
            {
                _repo.AjouterNotification(alerte);
                _logger.LogWarning("[Notif] Alerte recue : {Taux}% occupation",
                    alerte.TauxOccupation);
            }
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };

        _channel.BasicConsume(QueueName, autoAck: false, consumer);
        _logger.LogInformation("[Notif] En ecoute sur l'exchange '{Ex}'", ExchangeName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) { Dispose(); return Task.CompletedTask; }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
