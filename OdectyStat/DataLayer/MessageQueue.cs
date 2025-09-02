using Microsoft.Extensions.Options;
using OdectyMVC.Contracts;
using OdectyStat1.Dto;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdectyStat1.DataLayer;
public class MessageQueue : IMessageQueue
{
    private readonly IModel model;
    private readonly IOptions<OdectySettings> options;

    public MessageQueue(RabbitMQProvider rabbitMQProvider, IOptions<OdectySettings> options)
    {
        model = rabbitMQProvider.CreateModel();
        this.options = options;
    }
    public Task Publish(object message, string routingKey)
    {
        model.BasicPublish(exchange: options.Value.ExchangeName,
                             routingKey: routingKey,
                             basicProperties: null,
                             body: Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message)));
        return Task.CompletedTask;
    }
}
