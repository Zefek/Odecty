using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyStat1.Dto;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace OdectyStat1.DataLayer;
public class RabbitMQProvider : IAsyncDisposable
{
    private IConnection? connection;
    private readonly IOptions<OdectySettings> options;
    private readonly ILogger<RabbitMQProvider> logger;
    private bool first = true;
    private readonly ConnectionFactory factory;
    private TimeSpan mqttConnectionTimeout = TimeSpan.Zero;
    private long? lastAttemptTimestamp = null;
    private TimeSpan? connectionDelay = null;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim semaphoreConnect = new SemaphoreSlim(1, 1);


    public AsyncEventHandler<ShutdownEventArgs> ConnectionShutdown = new AsyncEventHandler<ShutdownEventArgs>((s, e) => Task.CompletedTask);

    public bool IsConnected => connection?.IsOpen == true;

    public RabbitMQProvider(IOptions<OdectySettings> options, ILogger<RabbitMQProvider> logger)
    {
        factory = new ConnectionFactory();
        factory.HostName = options.Value.RabbitMQHost;
        factory.UserName = options.Value.RabbitMQUsername;
        factory.Password = options.Value.RabbitMQPassword;
        factory.VirtualHost = options.Value.RabbitMQVHost;
        if (options.Value.RabbitMQUseTls)
        {
            factory.Port = options.Value.RabbitMQPort ?? 5671;
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = options.Value.RabbitMQTlsServerName ?? options.Value.RabbitMQHost,
                Version = SslProtocols.Tls12 | SslProtocols.Tls13
            };
        }
        else if (options.Value.RabbitMQPort.HasValue)
        {
            factory.Port = options.Value.RabbitMQPort.Value;
        }
        this.options = options;
        this.logger = logger;
    }

    private async Task Connect()
    {
        if (lastAttemptTimestamp.HasValue && connectionDelay.HasValue && Stopwatch.GetElapsedTime(lastAttemptTimestamp.Value) < connectionDelay || connection?.IsOpen == true)
        {
            return;
        }
        await semaphoreConnect.WaitAsync();
        try
        {
            if (connection?.IsOpen != true)
            {
                if (connection != null)
                {
                    try
                    {
                        await connection.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Failed to close stale RabbitMQ connection before reconnect.");
                    }
                    await connection.DisposeAsync();
                    connection = null;
                    first = true;
                }
                connection = await factory.CreateConnectionAsync();
                connection.ConnectionShutdownAsync += Connection_ConnectionShutdown;
                logger.LogInformation("Successfully connected to RabbitMQ at {HostName}", factory.HostName);
                lastAttemptTimestamp = null;
                connectionDelay = null;
                first = true;
            }
        }
        catch (Exception ex)
        {
            lastAttemptTimestamp = Stopwatch.GetTimestamp();
            connectionDelay = mqttConnectionTimeout = TimeSpan.FromMilliseconds(Math.Min(mqttConnectionTimeout.TotalMilliseconds * 2 + RandomNumberGenerator.GetInt32(0, 5000), 300000));

            logger.LogWarning(ex, "Failed to connect to RabbitMQ at {HostName}, retrying in {Delay}ms)",
                factory.HostName, connectionDelay.Value.TotalMilliseconds);
        }
        finally
        {
            semaphoreConnect.Release();
        }
    }


    private async Task Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        if (connection != null)
        {
            foreach (AsyncEventHandler<ShutdownEventArgs> handler in ConnectionShutdown.GetInvocationList())
            {
                try
                {
                    await handler(sender, e);
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }
            }
            await connection.CloseAsync();
            connection.Dispose();
            connection = null;
        }
    }

    public async Task<IChannel?> CreateModel()
    {
        if (connection?.IsOpen != true)
        {
            await Connect();
        }
        if (connection?.IsOpen != true)
        {
            return null;
        }
        try
        {
            if (first)
            {
                await semaphore.WaitAsync();
                try
                {
                    if (first)
                    {
                        using var model = await connection.CreateChannelAsync();
                        await model.ExchangeDeclareAsync(options.Value.ExchangeName, ExchangeType.Direct, true, false, null);
                        foreach (var exchange in options.Value.QueueMappings.Select(q => q.ExchangeName).Distinct())
                        {
                            if (exchange == null || exchange.StartsWith("amq."))
                            {
                                continue;
                            }
                            await model.ExchangeDeclareAsync(exchange, ExchangeType.Direct, true, false, null);
                        }
                        foreach (var queue in options.Value.QueueMappings.Select(q => q.QueueName).Distinct())
                        {
                            await model.QueueDeclareAsync(queue, true, false, false, null);
                        }
                        foreach (var map in options.Value.QueueMappings)
                        {
                            await model.QueueBindAsync(map.QueueName, map.ExchangeName, map.RoutingKey ?? string.Empty);
                        }
                        first = false;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
            return await connection.CreateChannelAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create RabbitMQ channel, will retry.");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection != null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }
        connection = null;
    }
}
