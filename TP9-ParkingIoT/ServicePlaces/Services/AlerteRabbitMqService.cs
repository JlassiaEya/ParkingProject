using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ServicePlaces.Services;

public class AlerteRabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<AlerteRabbitMqService> _logger;
    private const string ExchangeName = "parking.alertes";

    public AlerteRabbitMqService(ILogger<AlerteRabbitMqService> logger, IConfiguration config)
    {
        _logger = logger;
        var host = config["RabbitMq:Host"] ?? "localhost";

        var factory = new ConnectionFactory { HostName = host };
        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();

        // Exchange fanout : diffuse a toutes les queues connectees
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
        _logger.LogInformation("[RabbitMQ] Connecte, exchange '{Ex}' pret", ExchangeName);
    }

    public void PublierAlerte(double tauxOccupation, int placesLibres)
    {
        var message = new
        {
            Type = "OCCUPATION_CRITIQUE",
            TauxOccupation = tauxOccupation,
            PlacesLibres = placesLibres,
            Timestamp = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: "",
            basicProperties: props,
            body: body);

        _logger.LogWarning(
            "[Alerte] Occupation critique publiee sur RabbitMQ : {Taux}% ({Libres} places libres)",
            tauxOccupation, placesLibres);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
