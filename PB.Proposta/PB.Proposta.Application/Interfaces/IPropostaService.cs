using PB.Proposta.Application.Events;
using PB.Proposta.Application.Response;

namespace PB.Proposta.Application.Interfaces
{
    public interface IPropostaService
    {
        Task ProcessarAsync(ClienteCadastradoEvent evento);
        Task<PropostaResponse> ObterPropostaPorIdCliente(Guid id);
    }
}