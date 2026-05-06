using PB.Proposta.Domain.Entities;

namespace PB.Proposta.Domain.Interfaces
{
    public interface IPropostaRepository : IDisposable
    {
        Task AdicionarAsync(PropostaEntity proposta);
        Task<PropostaEntity?> ObterPorIdAsync(Guid id);
        Task<PropostaEntity?> ObterPorClienteIdAsync(Guid clienteId);
        Task<List<PropostaEntity>> ObterTodosAsync();
        Task AtualizarAsync(PropostaEntity proposta);
        Task<int> SaveChangesAsync();
    }
}