using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using PB.Proposta.Application.Interfaces;
using PB.Proposta.Application.Services;
using PB.Proposta.Domain.Interfaces;
using PB.Proposta.Infrastructure.Context;
using PB.Proposta.Infrastructure.Messaging;
using PB.Proposta.Infrastructure.Repository;
using PB.Proposta.Infrastructure.Services;
using Polly.CircuitBreaker;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PropostaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"],
        UserName = builder.Configuration["RabbitMQ:User"],
        Password = builder.Configuration["RabbitMQ:Password"],
        DispatchConsumersAsync = true
    };
    return factory.CreateConnection();
});


builder.Services.AddScoped<IPropostaRepository, PropostaRepository>();
builder.Services.AddScoped<IPropostaService, PropostaService>();
builder.Services.AddScoped<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddSingleton<AsyncCircuitBreakerPolicy>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PropostaService>>();

    return Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (ex, duration) =>
                logger.LogError("[CIRCUIT BREAKER] Aberto por {Segundos}s. Erro: {Msg}",
                    duration.TotalSeconds, ex.Message),
            onReset: () =>
                logger.LogInformation("[CIRCUIT BREAKER] Fechado — retomando"),
            onHalfOpen: () =>
                logger.LogInformation("[CIRCUIT BREAKER] Half-open — testando")
        );
});

builder.Services.AddHostedService<RabbitMQConsumer>();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
    await db.Database.MigrateAsync();
}

app.MapControllers();

app.Run();