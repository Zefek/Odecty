using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyStat1.Dto;
using RabbitMQ.Client;

namespace OdectyStat1.DataLayer;
public class RabbitMQProvider : IDisposable
{
    private IConnection connection;
    private readonly IOptions<OdectySettings> options;
    private readonly ILogger<RabbitMQProvider> logger;
    private bool first = true;
    private readonly ConnectionFactory factory;
    private bool isConnected = false;
    private TimeSpan mqttConnectionTimeout = TimeSpan.Zero;
    private readonly Random random = new Random();
    private DateTime? lastAttemptTime = null;
    private TimeSpan? connectionDelay = null;

    public EventHandler ConnectionShutdown = new EventHandler((s, e) => { });

    public RabbitMQProvider(IOptions<OdectySettings> options, ILogger<RabbitMQProvider> logger)
    {
        factory = new ConnectionFactory();
        factory.HostName = options.Value.RabbitMQHost;
        factory.UserName = options.Value.RabbitMQUsername;
        factory.Password = options.Value.RabbitMQPassword;
        factory.VirtualHost = options.Value.RabbitMQVHost;
        this.options = options;
        this.logger = logger;
    }

    private void Connect()
    {
        if(lastAttemptTime.HasValue && connectionDelay.HasValue  && (DateTime.Now - lastAttemptTime.Value) < connectionDelay || connection != null)
        {
            return;
        }
        try
        {
            connection = factory.CreateConnection();
            connection.ConnectionShutdown += Connection_ConnectionShutdown;
            isConnected = true;
            logger.LogInformation("Successfully connected to RabbitMQ at {HostName}", factory.HostName);
            lastAttemptTime = null;
            connectionDelay = null;
            return;
        }
        catch (Exception ex)
        {
            isConnected = false;
            lastAttemptTime = DateTime.Now;
            connectionDelay = mqttConnectionTimeout = TimeSpan.FromMilliseconds(Math.Min(mqttConnectionTimeout.TotalMilliseconds * 2 + random.Next(0, 5000), 300000));

            logger.LogWarning(ex, "Failed to connect to RabbitMQ at {HostName}, retrying in {Delay}ms)",
                factory.HostName, connectionDelay.Value.TotalMilliseconds);
        }
    }


    private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        connection?.Close();
        connection?.Dispose();
        connection = null;
        isConnected = false;
    }

    public IModel CreateModel()
    {
        if(!isConnected)
        {
            Connect();
        }
        if(!isConnected)
        {
            return null;
        }
        if (first)
        {
            using var model = connection.CreateModel();
            model.ExchangeDeclare(options.Value.ExchangeName, ExchangeType.Direct, true, false, null);
            foreach (var exchange in options.Value.QueueMappings.Select(q => q.ExchangeName).Distinct())
            {
                if (exchange.StartsWith("amq."))
                {
                    continue;
                }
                model.ExchangeDeclare(exchange, ExchangeType.Direct, true, false, null);
            }
            foreach (var queue in options.Value.QueueMappings.Select(q => q.QueueName).Distinct())
            {
                model.QueueDeclare(queue, true, false, false, null);
            }
            foreach (var map in options.Value.QueueMappings)
            {
                model.QueueBind(map.QueueName, map.ExchangeName, map.RoutingKey);
            }
            first = false;
        }
        return connection.CreateModel();
    }

    public void Dispose()
    {
        connection?.Close();
        connection = null;
    }
}
