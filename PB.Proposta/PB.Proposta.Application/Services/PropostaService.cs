using PB.Proposta.Application.Events;
using PB.Proposta.Application.Response;
using PB.Proposta.Application.Interfaces;
using PB.Proposta.Domain.Entities;
using PB.Proposta.Domain.Interfaces;
using PB.Proposta.Domain.Exceptions;

namespace PB.Proposta.Application.Services
{
    public class PropostaService : IPropostaService
    {
        #region Propriedades

        private readonly IPropostaRepository _propostaRepository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IEmailService _emailService;

        #endregion 

        #region Construtor
        
        public PropostaService(
            IPropostaRepository propostaRepository,
            IMessagePublisher messagePublisher,
            IEmailService emailService)
        {
            _propostaRepository = propostaRepository;
            _messagePublisher = messagePublisher;
            _emailService = emailService;
        }

        #endregion
        #region Métodos Públicos
        public async Task ProcessarAsync(ClienteCadastradoEvent evento)
        {
            var propostaExistente = await _propostaRepository
                .ObterPorClienteIdAsync(evento.ClienteId);

            if (propostaExistente != null)
                return;

            var score = GerarScore();
            var proposta = new PropostaEntity(evento.ClienteId, score);

            await _propostaRepository.AdicionarAsync(proposta);

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

            await _messagePublisher.PublicarAsync(creditoAprovado, "credito.aprovado");
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