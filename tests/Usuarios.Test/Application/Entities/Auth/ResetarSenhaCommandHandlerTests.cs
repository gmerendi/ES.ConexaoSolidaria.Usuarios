using FluentAssertions;
using Moq;
using Usuarios.Application.Features.Auth;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;
using Xunit;

namespace Usuarios.Tests.Application;

/// <summary>
/// Testes unitários para ResetarSenhaCommandHandler.
/// Cobre reset bem-sucedido, senha atual incorreta, usuário suspenso e blacklist do token.
/// </summary>
public class ResetarSenhaCommandHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<IBaseLogger<ResetarSenhaCommandHandler>> _loggerMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ICryptoService> _cryptoServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IMessageService> _messageServiceMock = new();

    private ResetarSenhaCommandHandler CriarHandler() =>
        new(_repositoryMock.Object, _loggerMock.Object, _tokenServiceMock.Object,
            _cryptoServiceMock.Object, _cacheServiceMock.Object,
            _userContextMock.Object, _messageServiceMock.Object);

    private static ResetarSenhaCommand ComandoValido(
        string senhaAtual = "SenhaAtual@1",
        string senhaNova = "NovaSenha@2",
        string token = "token-jwt-valido") =>
        new(senhaAtual, senhaNova, token);

    // -------------------------------------------------------------------------
    // Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveRetornarNovoToken_QuandoResetBemSucedido()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoador();
        var userContext = new SystemUser(usuario.Guid, usuario.NomeCompleto, "", usuario.Email.Endereco, "DOADOR", "ACTIVE");

        _userContextMock.Setup(u => u.GetUser()).Returns(userContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(userContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _cryptoServiceMock.Setup(c => c.VerifyPassword(command.PasswordAtual, usuario.SenhaHash)).Returns(true);
        _tokenServiceMock.Setup(t => t.GetTokenTimeToExpire(command.Token)).Returns(TimeSpan.FromMinutes(15));
        _tokenServiceMock.Setup(t => t.GetToken(usuario)).Returns(("novo-token-jwt", DateTime.UtcNow.AddHours(1)));
        _repositoryMock.Setup(r => r.AlterarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cacheServiceMock.Setup(c => c.SetBlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be("novo-token-jwt");
    }

    [Fact]
    public async Task HandleAsync_DeveInvalidarTokenAntigo_AdicionandoNaBlacklist()
    {
        // Arrange
        var command = ComandoValido(token: "token-antigo");
        var usuario = UsuarioFactory.CriarDoador();
        var userContext = new SystemUser(usuario.Guid, usuario.NomeCompleto, "", usuario.Email.Endereco, "DOADOR", "ACTIVE");
        var tempoExpiracao = TimeSpan.FromMinutes(20);

        _userContextMock.Setup(u => u.GetUser()).Returns(userContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(userContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _cryptoServiceMock.Setup(c => c.VerifyPassword(command.PasswordAtual, usuario.SenhaHash)).Returns(true);
        _tokenServiceMock.Setup(t => t.GetTokenTimeToExpire(command.Token)).Returns(tempoExpiracao);
        _tokenServiceMock.Setup(t => t.GetToken(usuario)).Returns(("novo-token", DateTime.UtcNow.AddHours(1)));
        _repositoryMock.Setup(r => r.AlterarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cacheServiceMock.Setup(c => c.SetBlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = CriarHandler();

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert — token antigo deve ir para a blacklist com o tempo correto
        _cacheServiceMock.Verify(c => c.SetBlacklistAsync(
            "token-antigo", tempoExpiracao, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — senha nova vazia
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_DeveLancarDomainException_QuandoSenhaNovaVazia(string senhaNova)
    {
        // Arrange
        var command = new ResetarSenhaCommand("SenhaAtual@1", senhaNova, "token");
        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_PASSWORD_REQUIRED");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — usuário não encontrado
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioNaoExiste()
    {
        // Arrange
        var command = ComandoValido();
        var userContext = new SystemUser(Guid.NewGuid(), "X", "", "naoexiste@email.com", "DOADOR", "ACTIVE");

        _userContextMock.Setup(u => u.GetUser()).Returns(userContext);
        _tokenServiceMock.Setup(t => t.GetTokenTimeToExpire(command.Token)).Returns(TimeSpan.FromMinutes(10));
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(userContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_USER_NOT_FOUND");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — usuário suspenso
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioSuspenso()
    {
        // Arrange
        var command = ComandoValido();
        var usuario = UsuarioFactory.CriarDoadorSuspenso();
        var userContext = new SystemUser(usuario.Guid, usuario.NomeCompleto, "", usuario.Email.Endereco, "DOADOR", "SUSPENDED");

        _userContextMock.Setup(u => u.GetUser()).Returns(userContext);
        _tokenServiceMock.Setup(t => t.GetTokenTimeToExpire(command.Token)).Returns(TimeSpan.FromMinutes(10));
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(userContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "403_USER_SUSPENDED");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — senha atual incorreta
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoSenhaAtualIncorreta()
    {
        // Arrange
        var command = ComandoValido(senhaAtual: "SenhaErrada@1");
        var usuario = UsuarioFactory.CriarDoador();
        var userContext = new SystemUser(usuario.Guid, usuario.NomeCompleto, "", usuario.Email.Endereco, "DOADOR", "ACTIVE");

        _userContextMock.Setup(u => u.GetUser()).Returns(userContext);
        _tokenServiceMock.Setup(t => t.GetTokenTimeToExpire(command.Token)).Returns(TimeSpan.FromMinutes(10));
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(userContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _cryptoServiceMock.Setup(c => c.VerifyPassword(command.PasswordAtual, usuario.SenhaHash)).Returns(false);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "403_PASSWORD_INCORRECT");
    }
}
