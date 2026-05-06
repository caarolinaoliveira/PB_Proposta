namespace PB.Proposta.Application.Events
{
    public class ClienteCadastradoEvent
    {
        public Guid ClienteId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Cpf { get; init; } = string.Empty;
        public DateTime DataNascimento { get; init; }
        public string Telefone { get; init; } = string.Empty;
        public DateTime OcorridoEm { get; init; }
    }
}