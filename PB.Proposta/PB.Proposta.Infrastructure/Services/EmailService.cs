using PB.Proposta.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PB.Proposta.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task EnviarPropostaAprovadaAsync(
            string email, string nome, decimal limite, int quantidadeCartoes)
        {
            _logger.LogInformation(
                "[EMAIL] Proposta aprovada enviado para {Email} | Cliente: {Nome} | " +
                "Limite: {Limite:C} | Cartões: {Cartoes}",
                email, nome, limite, quantidadeCartoes);

            await Task.Delay(100); 
        }

        public async Task EnviarPropostaNegadaAsync(string email, string nome)
        {
            _logger.LogInformation(
                "[EMAIL] Proposta negada enviado para {Email} | Cliente: {Nome}",
                email, nome);

            await Task.Delay(100);
        }
    }
}