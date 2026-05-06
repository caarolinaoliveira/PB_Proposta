using PB.Proposta.Domain.Enums;

namespace PB.Proposta.Domain.Entities
{
    public class PropostaEntity
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public int Score { get; private set; }
        public StatusProposta Status { get; private set; }
        public decimal LimiteAprovado { get; private set; }
        public int QuantidadeCartoes { get; private set; }
        public DateTime CriadaEm { get; private set; }
        public DateTime? AtualizadaEm { get; private set; }

        // EF Core
        protected PropostaEntity() { }

        public PropostaEntity(Guid clienteId, int score)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            Score = score;
            Status = StatusProposta.Pendente;
            CriadaEm = DateTime.UtcNow;

            AplicarRegraDeScore();
        }

        private void AplicarRegraDeScore()
        {
            if (Score <= 100)
            {
                Negar();
                return;
            }

            if (Score <= 500)
            {
                Aprovar(quantidadeCartoes: 1, limiteAprovado: 1000m);
                return;
            }

            Aprovar(quantidadeCartoes: 2, limiteAprovado: 5000m);
        }

        private void Aprovar(int quantidadeCartoes, decimal limiteAprovado)
        {
            Status = StatusProposta.Aprovada;
            QuantidadeCartoes = quantidadeCartoes;
            LimiteAprovado = limiteAprovado;
            AtualizadaEm = DateTime.UtcNow;
        }

        private void Negar()
        {
            Status = StatusProposta.Negada;
            QuantidadeCartoes = 0;
            LimiteAprovado = 0m;
            AtualizadaEm = DateTime.UtcNow;
        }

        public bool IsAprovada() => Status == StatusProposta.Aprovada;
    }
}