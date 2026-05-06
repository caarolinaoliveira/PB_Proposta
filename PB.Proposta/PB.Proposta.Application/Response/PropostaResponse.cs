namespace PB.Proposta.Application.Response
{
    public sealed record PropostaResponse
    {
        public Guid PropostaId { get; init; }
        public Guid ClienteId { get; init; }
        public decimal LimiteAprovado { get; init; }
        public int QuantidadeCartoes { get; init; }
        public DateTime OcorridoEm { get; init; } 
    }

}