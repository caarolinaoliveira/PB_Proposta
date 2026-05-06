namespace PB.Proposta.Application.Interfaces
{
    public interface IEmailService
    {
        Task EnviarPropostaAprovadaAsync(string email, string nome, decimal limite, int quantidadeCartoes);
        Task EnviarPropostaNegadaAsync(string email, string nome);
    }
}