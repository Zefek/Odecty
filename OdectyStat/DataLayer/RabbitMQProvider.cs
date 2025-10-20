using Microsoft.Extensions.Options;
using OdectyStat1.Dto;
using RabbitMQ.Client;

namespace OdectyStat1.DataLayer;
public class RabbitMQProvider : IDisposable
{
    private readonly IConnection connection;
    private readonly IOptions<OdectySettings> options;
    private bool first = true;

    public RabbitMQProvider(IOptions<OdectySettings> options)
    {
        var factory = new ConnectionFactory();
        factory.HostName = options.Value.RabbitMQHost;
        factory.UserName = options.Value.RabbitMQUsername;
        factory.Password = options.Value.RabbitMQPassword;
        factory.VirtualHost = options.Value.RabbitMQVHost;

        connection = factory.CreateConnection();
        this.options = options;
    }

    public IModel CreateModel()
    {
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
    }
}
