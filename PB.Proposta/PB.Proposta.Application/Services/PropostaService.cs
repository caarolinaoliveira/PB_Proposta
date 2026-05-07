using PB.Proposta.Application.Events;
using PB.Proposta.Application.Response;
using PB.Proposta.Application.Interfaces;
using PB.Proposta.Domain.Entities;
using PB.Proposta.Domain.Interfaces;
using PB.Proposta.Domain.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace PB.Proposta.Application.Services
{
    public class PropostaService : IPropostaService
    {
        #region Propriedades

        private readonly IPropostaRepository _propostaRepository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IEmailService _emailService;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
        private readonly ILogger<PropostaService> _logger;


        #endregion 

        #region Construtor
        
        public PropostaService(
            IPropostaRepository propostaRepository,
            IMessagePublisher messagePublisher,
            IEmailService emailService,
            AsyncCircuitBreakerPolicy circuitBreaker,
            ILogger<PropostaService> logger)
        {
            _propostaRepository = propostaRepository;
            _messagePublisher = messagePublisher;
            _emailService = emailService;
            _circuitBreaker = circuitBreaker;
            _logger = logger;
        }

        #endregion
        #region Métodos Públicos
        public async Task ProcessarAsync(ClienteCadastradoEvent evento)
        {
            _logger.LogInformation("Processando evento ClienteCadastradoEvent para ClienteId: {ClienteId}", evento.ClienteId);

            var propostaExistente = await _propostaRepository
                .ObterPorClienteIdAsync(evento.ClienteId);

            if (propostaExistente != null)
                throw new InvalidOperationException("Proposta já existe para este cliente.");

            var score = GerarScore();
            var proposta = new PropostaEntity(evento.ClienteId, score);

            await _propostaRepository.AdicionarAsync(proposta);

            _logger.LogInformation("[PROPOSTA] Score {Score} | Aprovada: {Aprovada} | ClienteId: {Id}",score, proposta.IsAprovada(), evento.ClienteId);

            if (!proposta.IsAprovada())
            {
                await _emailService.EnviarPropostaNegadaAsync(
                    evento.Email, evento.Nome);
                return;
            }

            await _emailService.EnviarPropostaAprovadaAsync(
                evento.Email,
                evento.Nome,
                proposta.LimiteAprovado,
                proposta.QuantidadeCartoes);

            var creditoAprovado = new CreditoAprovadoEvent
            {
                PropostaId = proposta.Id,
                ClienteId = proposta.ClienteId,
                Email = evento.Email,
                Nome = evento.Nome,
                LimiteAprovado = proposta.LimiteAprovado,
                QuantidadeCartoes = proposta.QuantidadeCartoes,
                OcorridoEm = DateTime.UtcNow
            };
            await _circuitBreaker.ExecuteAsync(() => _messagePublisher.PublicarAsync(creditoAprovado, "credito.aprovado"));
            
            _logger.LogInformation("[PROPOSTA] Evento publicado | ClienteId: {Id}", evento.ClienteId);

        }

        public async Task<PropostaResponse> ObterPropostaPorIdCliente (Guid id)
        {
            var proposta = await _propostaRepository.ObterPorIdAsync(id);
            if (proposta == null)
                throw new NotFoundException("Proposta não encontrada.");

            return new PropostaResponse
            {

                PropostaId = proposta.Id,
                ClienteId = proposta.ClienteId,
                LimiteAprovado = proposta.LimiteAprovado,
                QuantidadeCartoes = proposta.QuantidadeCartoes,
                OcorridoEm = proposta.CriadaEm
            };
        }

        #endregion

        #region Métodos Privados 

        private int GerarScore()
        {
            return new Random().Next(0, 1001);
        }
        #endregion
    }
}