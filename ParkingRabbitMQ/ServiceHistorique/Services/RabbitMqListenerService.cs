using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceHistorique.EventStore;
using ServiceHistorique.Models;

namespace ServiceHistorique.Services;

public class RabbitMqListenerService : IHostedService, IAsyncDisposable
{
    private readonly IEventStore _store;
    private readonly ILogger<RabbitMqListenerService> _logger;
    private IConnection? _connection;
    private IChannel?    _channel;

    private const string EXCHANGE = "parking.events.topic";
    private const string QUEUE    = "historique.eventsource.queue";
    private const string PATTERN  = "parking.#";

    public RabbitMqListenerService(IEventStore store,
                                   ILogger<RabbitMqListenerService> logger)
    {
        _store  = store;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = await factory.CreateConnectionAsync();
        _channel    = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(EXCHANGE, ExchangeType.Topic, durable: true);
        await _channel.QueueDeclareAsync(QUEUE, durable: true,
            exclusive: false, autoDelete: false, arguments: null);
        await _channel.QueueBindAsync(QUEUE, EXCHANGE, PATTERN);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;

        await _channel.BasicConsumeAsync(QUEUE, autoAck: false, consumer: consumer);
        _logger.LogInformation("[EventStore] Écoute RabbitMQ sur pattern: {P}", PATTERN);
    }
    private async Task OnMessageAsync(object sender,
                                  BasicDeliverEventArgs ea)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<EvenementParking>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (evt is not null)
            {
                _store.Append(ea.RoutingKey, evt);
                _logger.LogDebug(
                    "[EventStore] #{Seq} | {Rk} | {Type}",
                    _store.Count, ea.RoutingKey, evt.Type);
            }

            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventStore] Erreur ingestion");
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync();
        if (_connection?.IsOpen == true) await _connection.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        await Task.CompletedTask;
    }

}
