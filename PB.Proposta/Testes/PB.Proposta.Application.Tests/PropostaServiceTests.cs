using PB.Proposta.Application.Services;
using PB.Proposta.Application.Interfaces;
using PB.Proposta.Application.Response;
using PB.Proposta.Application.Events;
using PB.Proposta.Domain.Interfaces;
using PB.Proposta.Domain.Entities;
using PB.Proposta.Domain.Exceptions; 
using FluentAssertions;
using Moq;
using Xunit;

namespace PB.Proposta.Application.Tests;

public class PropostaServiceTests
{
    private readonly Mock<IPropostaRepository> _repositoryMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly PropostaService _service;

    public PropostaServiceTests()
    {
        _repositoryMock = new Mock<IPropostaRepository>();
        _publisherMock = new Mock<IMessagePublisher>();
        _emailServiceMock = new Mock<IEmailService>();
        _service = new PropostaService(_repositoryMock.Object, _publisherMock.Object, _emailServiceMock.Object);
    }

    private ClienteCadastradoEvent CriarEventoPadrao() => new()
    {
        ClienteId = Guid.NewGuid(),
        Nome = "Carolina Oliveira",
        Email = "carolina@teste.com",
        Cpf = "12345678901",
        Telefone = "41999999999",
        DataNascimento = new DateTime(1999, 3, 15),
        OcorridoEm = DateTime.UtcNow
    };

    [Fact]
    public async Task ProcessarAsync_ClienteJaProcessado_DeveIgnorar()
    {
        // Arrange
        var evento = CriarEventoPadrao();

        _repositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(evento.ClienteId))
            .ReturnsAsync(new PropostaEntity(evento.ClienteId, 500));

        // Act
        await _service.ProcessarAsync(evento);

        // Assert
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<PropostaEntity>()), Times.Never);
        _publisherMock.Verify(p => p.PublicarAsync(It.IsAny<CreditoAprovadoEvent>(), It.IsAny<string>()), Times.Never);
        _emailServiceMock.Verify(e => e.EnviarPropostaAprovadaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<int>()), Times.Never);
        _emailServiceMock.Verify(e => e.EnviarPropostaNegadaAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessarAsync_ClienteNovo_DevePersistirProposta()
    {
        // Arrange
        var evento = CriarEventoPadrao();

        _repositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(evento.ClienteId))
            .ReturnsAsync((PropostaEntity?)null);

        _publisherMock
            .Setup(p => p.PublicarAsync(It.IsAny<CreditoAprovadoEvent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessarAsync(evento);

        // Assert
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<PropostaEntity>()), Times.Once);
    }

    [Fact]
    public async Task ObterPropostaPorIdCliente_PropostaNaoEncontrada_DeveLancarNotFoundException()
    {
        // Arrange
        var propostaId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(propostaId))
            .ReturnsAsync((PropostaEntity?)null);

        // Act
        Func<Task> act = async () => await _service.ObterPropostaPorIdCliente(propostaId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Proposta não encontrada.");

        _repositoryMock.Verify(r => r.ObterPorIdAsync(propostaId), Times.Once);
    }
}