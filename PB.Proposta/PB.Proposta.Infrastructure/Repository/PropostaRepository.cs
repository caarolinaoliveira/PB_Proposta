using Microsoft.EntityFrameworkCore;
using PB.Proposta.Domain.Entities;
using PB.Proposta.Domain.Interfaces;
using PB.Proposta.Infrastructure.Context;

namespace PB.Proposta.Infrastructure.Repository
{
    public class PropostaRepository : IPropostaRepository
    {
        private readonly PropostaDbContext _db;
        private readonly DbSet<PropostaEntity> _dbSet;

        public PropostaRepository(PropostaDbContext db)
        {
            _db = db;
            _dbSet = db.Set<PropostaEntity>();
        }

        public async Task AdicionarAsync(PropostaEntity proposta)
        {
            _dbSet.Add(proposta);
            await SaveChangesAsync();
        }

        public async Task<PropostaEntity?> ObterPorIdAsync(Guid id)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(p => p.ClienteId == id);
        }

        public async Task<PropostaEntity?> ObterPorClienteIdAsync(Guid clienteId)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(p => p.ClienteId == clienteId);
        }

        public async Task<List<PropostaEntity>> ObterTodosAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task AtualizarAsync(PropostaEntity proposta)
        {
            _dbSet.Update(proposta);
            await SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}