using Microsoft.EntityFrameworkCore;
using PB.Proposta.Domain.Entities;

namespace PB.Proposta.Infrastructure.Context
{
    public class PropostaDbContext : DbContext
    {
        public PropostaDbContext(DbContextOptions<PropostaDbContext> options)
            : base(options) { }

        public DbSet<PropostaEntity> Propostas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PropostaEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ClienteId)
                    .IsRequired();

                entity.Property(e => e.Score)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>(); 

                entity.Property(e => e.LimiteAprovado)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.QuantidadeCartoes)
                    .IsRequired();

                entity.Property(e => e.CriadaEm)
                    .IsRequired();

                entity.Property(e => e.AtualizadaEm);

                entity.HasIndex(e => e.ClienteId)
                    .IsUnique(); 
            });
        }
    }
}