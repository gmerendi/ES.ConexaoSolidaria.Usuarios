using FluentAssertions;
using Moq;
using Usuarios.Application.Features.Auth;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Xunit;

namespace Usuarios.Tests.Application;

/// <summary>
/// Testes unitários para LogarUsuarioCommandHandler.
/// Cobre login bem-sucedido, usuário não encontrado, senha incorreta e status bloqueados.
/// </summary>
public class LogarUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<IBaseLogger<LogarUsuarioCommandHandler>> _loggerMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ICryptoService> _cryptoServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private LogarUsuarioCommandHandler CriarHandler() =>
        new(_repositoryMock.Object, _loggerMock.Object,
            _tokenServiceMock.Object, _cryptoServiceMock.Object, _cacheServiceMock.Object);

    private static LogarUsuarioCommand ComandoValido(
        string email = "joao@email.com",
        string senha = "Senha@123") => new(email, senha);

    // -------------------------------------------------------------------------
    // Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveRetornarToken_QuandoCredenciaisValidas()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoador();
        var expiracao = DateTime.UtcNow.AddHours(1);

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _cryptoServiceMock
            .Setup(c => c.VerifyPassword(command.Password, usuario.SenhaHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(t => t.GetToken(usuario))
            .Returns(("jwt-token-gerado", expiracao));

        _cacheServiceMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UsuarioDTO>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value!.Token.Should().Be("jwt-token-gerado");
        //resultado.Value.Expiracao.Should().Be(expiracao);
        resultado.Value.Email.Should().Be(usuario.Email.Endereco);
    }

    [Fact]
    public async Task HandleAsync_DeveArmazenarUsuarioNoCache_QuandoLoginBemSucedido()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoador();

        _repositoryMock.Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _cryptoServiceMock.Setup(c => c.VerifyPassword(command.Password, usuario.SenhaHash)).Returns(true);
        _tokenServiceMock.Setup(t => t.GetToken(usuario)).Returns(("token", DateTime.UtcNow.AddHours(1)));
        _cacheServiceMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UsuarioDTO>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

        var handler = CriarHandler();

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(c => c.SetAsync(
            $"usuario:{usuario.Email.Endereco}",
            It.IsAny<UsuarioDTO>(),
            TimeSpan.FromMinutes(30)), Times.Once);
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
    // Cenários de FALHA — usuário não encontrado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioNaoExiste()
    {
        // Arrange
        var command = ComandoValido();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_USER_NOT_FOUND");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — senha incorreta
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoSenhaIncorreta()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoador();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _cryptoServiceMock
            .Setup(c => c.VerifyPassword(command.Password, usuario.SenhaHash))
            .Returns(false); // senha errada

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "401_INVALID_CREDENTIALS");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — status bloqueado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioSuspenso()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoadorSuspenso();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "403_USER_SUSPENDED");
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioRemovido()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoadorRemovido();

        _repositoryMock
            .Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "403_USER_REMOVED");
    }

    // -------------------------------------------------------------------------
    // Cenários — token não deve ser gerado para usuário bloqueado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_NaoDeveGerarToken_QuandoUsuarioSuspenso()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoadorSuspenso();

        _repositoryMock.Setup(r => r.ObterPorEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var handler = CriarHandler();

        // Act
        try { await handler.HandleAsync(command, CancellationToken.None); } catch { }

        // Assert — GetToken nunca deve ser chamado para usuário suspenso
        _tokenServiceMock.Verify(t => t.GetToken(It.IsAny<Usuario>()), Times.Never);
    }
}
