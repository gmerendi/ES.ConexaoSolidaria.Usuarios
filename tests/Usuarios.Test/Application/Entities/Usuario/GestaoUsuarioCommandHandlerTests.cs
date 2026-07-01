using FluentAssertions;
using Moq;
using Usuarios.Application.Features.Usuarios;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;
using Xunit;

namespace Usuarios.Tests.Application;

// =============================================================================
// SuspenderUsuarioCommandHandler
// =============================================================================

/// <summary>
/// Testes unitários para SuspenderUsuarioCommandHandler.
/// Cobre suspensão bem-sucedida, permissões e usuário não encontrado.
/// </summary>
public class SuspenderUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IBaseLogger<RemoverUsuarioCommandHandler>> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IUsuarioDomainService> _domainServiceMock = new();

    private SuspenderUsuarioCommandHandler CriarHandler() =>
        new(_repositoryMock.Object, _userContextMock.Object, _loggerMock.Object,
            _cacheServiceMock.Object, _messageServiceMock.Object, _domainServiceMock.Object);

    [Fact]
    public async Task HandleAsync_DeveRetornarSucesso_QuandoGestorSuspendOutroUsuario()
    {
        // Arrange
        var command = new SuspenderUsuarioCommand("alvo@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");
        var alvo = UsuarioFactory.CriarDoador(email: "alvo@email.com", cpf: "111.444.777-35");

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("alvo@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(alvo);
        _repositoryMock.Setup(r => r.AlterarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _domainServiceMock.Setup(d => d.PodeAlterarPerfilEStatus(gestor, alvo));

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoCommandNulo()
    {
        var handler = CriarHandler();
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioAlvoNaoEncontrado()
    {
        // Arrange
        var command = new SuspenderUsuarioCommand("inexistente@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("inexistente@email.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_USER_NOT_FOUND");
    }
}

// =============================================================================
// AtivarUsuarioCommandHandler
// =============================================================================

/// <summary>
/// Testes unitários para AtivarUsuarioCommandHandler.
/// Cobre ativação bem-sucedida e usuário não encontrado.
/// </summary>
public class AtivarUsuarioCommandHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IBaseLogger<AtivarUsuarioCommandHandler>> _loggerMock = new();
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IUsuarioDomainService> _domainServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private AtivarUsuarioCommandHandler CriarHandler() =>
        new(_repositoryMock.Object, _userContextMock.Object, _loggerMock.Object,
            _messageServiceMock.Object, _domainServiceMock.Object, _cacheServiceMock.Object);

    [Fact]
    public async Task HandleAsync_DeveRetornarSucesso_QuandoGestorAtivaUsuarioSuspenso()
    {
        // Arrange
        var command = new AtivarUsuarioCommand("alvo@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");
        var alvo = UsuarioFactory.CriarDoadorSuspenso();

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("alvo@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(alvo);
        _repositoryMock.Setup(r => r.AlterarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _domainServiceMock.Setup(d => d.PodeAlterarPerfilEStatus(gestor, alvo));

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoCommandNulo()
    {
        var handler = CriarHandler();
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioNaoEncontrado()
    {
        // Arrange
        var command = new AtivarUsuarioCommand("naoexiste@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("naoexiste@email.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_USER_NOT_FOUND");
    }
}

// =============================================================================
// AlterarPerfilParaGestorCommandHandler
// =============================================================================

/// <summary>
/// Testes unitários para AlterarPerfilParaGestorCommandHandler.
/// Cobre promoção de perfil, permissões e remoção do cache.
/// </summary>
public class AlterarPerfilParaGestorCommandHandlerTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IBaseLogger<AlterarPerfilParaGestorCommandHandler>> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IUsuarioDomainService> _domainServiceMock = new();

    private AlterarPerfilParaGestorCommandHandler CriarHandler() =>
        new(_repositoryMock.Object, _userContextMock.Object, _loggerMock.Object,
            _cacheServiceMock.Object, _domainServiceMock.Object);

    [Fact]
    public async Task HandleAsync_DeveRetornarSucesso_QuandoGestorPromoveDoador()
    {
        // Arrange
        var command = new AlterarPerfilParaGestorCommand("doador@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");
        var doador = UsuarioFactory.CriarDoador(email: "doador@email.com", cpf: "111.444.777-35");

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("doador@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(doador);
        _repositoryMock.Setup(r => r.AlterarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _domainServiceMock.Setup(d => d.PodeAlterarPerfilEStatus(gestor, doador));

        var handler = CriarHandler();

        // Act
        var resultado = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_DeveRemoverDoCache_QuandoPerfilAlterado()
    {
        // Arrange
        var command = new AlterarPerfilParaGestorCommand("doador@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");
        var doador = UsuarioFactory.CriarDoador(email: "doador@email.com", cpf: "111.444.777-35");

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("doador@email.com", It.IsAny<CancellationToken>())).ReturnsAsync(doador);
        _repositoryMock.Setup(r => r.AlterarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _domainServiceMock.Setup(d => d.PodeAlterarPerfilEStatus(gestor, doador));

        var handler = CriarHandler();

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _cacheServiceMock.Verify(c => c.RemoveAsync("usuario:doador@email.com"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoCommandNulo()
    {
        var handler = CriarHandler();
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_DeveLancarDomainException_QuandoUsuarioAlvoNaoEncontrado()
    {
        // Arrange
        var command = new AlterarPerfilParaGestorCommand("fantasma@email.com");
        var gestor = UsuarioFactory.CriarGestor();
        var gestorContext = new SystemUser(gestor.Guid, gestor.NomeCompleto, "", gestor.Email.Endereco, "GESTOR_ONG", "ACTIVE");

        _userContextMock.Setup(u => u.GetUser()).Returns(gestorContext);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(gestorContext.Email, It.IsAny<CancellationToken>())).ReturnsAsync(gestor);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("fantasma@email.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var handler = CriarHandler();

        // Act
        var act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.ErrorCode == "400_USER_NOT_FOUND");
    }
}
