using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PB.Proposta.Application.Events;
using PB.Proposta.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Polly;
using Polly.CircuitBreaker;

namespace PB.Proposta.Infrastructure.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly IConnection _connection;
        private IModel? _channel;

        public RabbitMQConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMQConsumer> logger,
            IConnection connection)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connection = connection;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("dlx", ExchangeType.Direct, durable: true);

            _channel.QueueDeclare(
                queue: "cliente.cadastrado.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            _channel.QueueBind("cliente.cadastrado.dlq", "dlx", "cliente.cadastrado");
            
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx" },
                { "x-dead-letter-routing-key", "cliente.cadastrado" }
            };

            _channel.QueueDeclare(
                queue: "cliente.cadastrado",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var tentativas = 0;

                if (ea.BasicProperties.Headers != null &&
                    ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryObj))
                {
                    tentativas = Convert.ToInt32(retryObj);
                }

                try
                {
                    var evento = JsonSerializer.Deserialize<ClienteCadastradoEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evento is null)
                    {
                        _logger.LogWarning("[CONSUMER] Evento null — descartando.");
                        _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var propostaService = scope.ServiceProvider.GetRequiredService<IPropostaService>();
                    await propostaService.ProcessarAsync(evento);

                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("[CONSUMER] Processado | ClienteId: {Id}", evento.ClienteId);
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.LogError(ex, "[CONSUMER] Circuit breaker aberto — enviando para DLQ.");
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: false); 
                }
                catch (Exception ex)
                {
                    tentativas++;

                    if (tentativas >= 3)
                    {
                        _logger.LogError(ex, "[CONSUMER] Máximo de tentativas atingido — enviando para DLQ.");
                        _channel.BasicNack(ea.DeliveryTag, false, requeue: false); 
                        return;
                    }

                    _logger.LogWarning("[CONSUMER] Tentativa {N} falhou — reenfileirando.", tentativas);

                    var props = _channel.CreateBasicProperties();
                    props.Persistent = true;
                    props.Headers = new Dictionary<string, object>
                    {
                        { "x-retry-count", tentativas }
                    };

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativas))); 

                    _channel.BasicPublish("", "cliente.cadastrado", props, ea.Body);
                    _channel.BasicAck(ea.DeliveryTag, false);  
                }
            };

            _channel.BasicConsume(
                queue: "cliente.cadastrado",
                autoAck: false, 
                consumer: consumer);

            _logger.LogInformation("[CONSUMER] Aguardando mensagens na fila cliente.cadastrado...");

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}