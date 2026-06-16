using FluentAssertions;
using Moq;
using Usuarios.Application.Features.Usuarios;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;
using Xunit;

namespace Usuarios.Tests.Application;

/// <summary>
/// Testes unitários para CriarUsuarioCommandHandler.
/// Cobre cadastro bem-sucedido, duplicações e validações de command.
/// </summary>
public class CriarUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IBaseLogger<CriarUsuarioCommandHandler>> _loggerMock = new();
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IMetricsService> _metricsServiceMock = new();

    private CriarUsuarioCommandHandler CriarHandler() =>
        new(_repositoryMock.Object, _userContextMock.Object, _loggerMock.Object,
            _messageServiceMock.Object, _metricsServiceMock.Object);

    private static CriarUsuarioCommand ComandoValido(
        string nome = "João da Silva Santos",
        string email = "joao@email.com",
        string cpf = "529.982.247-25",
        string senha = "Senha@123") =>
        new(nome, email, cpf, senha);

    // -------------------------------------------------------------------------
    // Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveRetornarSucesso_QuandoUsuarioNaoExisteEDadosSaoValidos()
    {
        // Arrange
        var command = ComandoValido();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _repositoryMock
            .Setup(r => r.ObterPorCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _repositoryMock
            .Setup(r => r.CadastrarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _messageServiceMock
            .Setup(m => m.SendUserCreatedEventMessage(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userContextMock.Setup(u => u.GetUser()).Returns((SystemUser?)null);

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value!.Email.Should().Be(command.Email);
        resultado.Value.NomeCompleto.Should().Be(command.NomeCompleto);
    }

    [Fact]
    public async Task HandleAsync_DeveChamarRepositorioCadastrar_UmaVez_QuandoSucesso()
    {
        // Arrange
        var command = ComandoValido();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        _repositoryMock
            .Setup(r => r.ObterPorCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        _repositoryMock
            .Setup(r => r.CadastrarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _messageServiceMock
            .Setup(m => m.SendUserCreatedEventMessage(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userContextMock.Setup(u => u.GetUser()).Returns((SystemUser?)null);

        var handler = CriarHandler();

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.CadastrarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DeveEnviarEventoDeCriacao_UmaVez_QuandoSucesso()
    {
        // Arrange
        var command = ComandoValido();

        _repositoryMock.Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.CadastrarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _messageServiceMock.Setup(m => m.SendUserCreatedEventMessage(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userContextMock.Setup(u => u.GetUser()).Returns((SystemUser?)null);

        var handler = CriarHandler();

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(m => m.SendUserCreatedEventMessage(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — command nulo
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoCommandNulo()
    {
        // Arrange
        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_COMMAND_INVALID");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — email duplicado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoEmailJaCadastrado()
    {
        // Arrange
        var command = ComandoValido();
        var usuarioExistente = UsuarioFactory.CriarDoador();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioExistente);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "422_USER_DUPLICATED");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — CPF duplicado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoCpfJaCadastrado()
    {
        // Arrange
        var command = ComandoValido();
        var usuarioComMesmoCpf = UsuarioFactory.CriarDoador();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _repositoryMock
            .Setup(r => r.ObterPorCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioComMesmoCpf);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "422_CPF_DUPLICATED");
    }

    // -------------------------------------------------------------------------
    // Cenários — uso do email do context como solicitante
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveUsarEmailDoContextComoSolicitante_QuandoUsuarioLogado()
    {
        // Arrange
        var command = ComandoValido();
        var usuarioLogado = new SystemUser(Guid.NewGuid(), "Admin", "000", "admin@ong.org", "GESTOR_ONG", "ACTIVE");

        _repositoryMock.Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _repositoryMock.Setup(r => r.CadastrarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _messageServiceMock.Setup(m => m.SendUserCreatedEventMessage(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userContextMock.Setup(u => u.GetUser()).Returns(usuarioLogado);

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.CadastrarAsync(
            It.Is<Usuario>(u => u.CriadoPor == "admin@ong.org"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
