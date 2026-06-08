using FluentAssertions;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Xunit;

namespace Usuarios.Tests.Domain;

/// <summary>
/// Testes unitários para UsuarioDomainService.
/// Cobre as 3 regras de negócio: PodeRemover, PodeAlterar e PodeAlterarPerfilEStatus.
/// </summary>
public class UsuarioDomainServiceTests
{
    private readonly UsuarioDomainService _service = new();

    // =========================================================================
    // PodeRemoverUsuario
    // =========================================================================

    [Fact]
    public void PodeRemoverUsuario_NaoDeveLancar_QuandoSolicitanteEGestor()
    {
        // Arrange
        var gestor = UsuarioFactory.CriarGestor();
        var alvo = UsuarioFactory.CriarDoador();

        // Act & Assert
        var act = () => _service.PodeRemoverUsuario(gestor, alvo);
        act.Should().NotThrow();
    }

    [Fact]
    public void PodeRemoverUsuario_NaoDeveLancar_QuandoUsuarioRemoveASiMesmo()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();

        // Act & Assert — mesmo Guid (mesmo objeto)
        var act = () => _service.PodeRemoverUsuario(usuario, usuario);
        act.Should().NotThrow();
    }

    [Fact]
    public void PodeRemoverUsuario_DeveLancarDomainException_QuandoDoadorTentaRemoverOutro()
    {
        // Arrange
        var solicitante = UsuarioFactory.CriarDoador(email: "solicitante@email.com", cpf: "529.982.247-25");
        var alvo = UsuarioFactory.CriarDoador(email: "alvo@email.com", cpf: "111.444.777-35");

        // Act
        var act = () => _service.PodeRemoverUsuario(solicitante, alvo);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("403_USER_CANNOT_REMOVE");
    }

    // =========================================================================
    // PodeAlterarUsuario
    // =========================================================================

    [Fact]
    public void PodeAlterarUsuario_NaoDeveLancar_QuandoSolicitanteEGestor()
    {
        // Arrange
        var gestor = UsuarioFactory.CriarGestor();
        var alvo = UsuarioFactory.CriarDoador();

        // Act & Assert
        var act = () => _service.PodeAlterarUsuario(gestor, alvo);
        act.Should().NotThrow();
    }

    [Fact]
    public void PodeAlterarUsuario_NaoDeveLancar_QuandoUsuarioAlteraASiMesmoEstaAtivo()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();
        usuario.Status.Should().Be(EntityStatus.ACTIVE); // pré-condição

        // Act & Assert
        var act = () => _service.PodeAlterarUsuario(usuario, usuario);
        act.Should().NotThrow();
    }

    [Fact]
    public void PodeAlterarUsuario_DeveLancarDomainException_QuandoUsuarioAlteraASiMesmoEStaSuspenso()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoadorSuspenso();

        // Act
        var act = () => _service.PodeAlterarUsuario(usuario, usuario);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("403_USER_SUSPENDED");
    }

    [Fact]
    public void PodeAlterarUsuario_DeveLancarDomainException_QuandoUsuarioAlteraASiMesmoEstaRemovido()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoadorRemovido();

        // Act
        var act = () => _service.PodeAlterarUsuario(usuario, usuario);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("403_USER_REMOVED");
    }

    [Fact]
    public void PodeAlterarUsuario_DeveLancarDomainException_QuandoDoadorTentaAlterarOutroUsuario()
    {
        // Arrange
        var solicitante = UsuarioFactory.CriarDoador(email: "solicitante@email.com", cpf: "529.982.247-25");
        var alvo = UsuarioFactory.CriarDoador(email: "alvo@email.com", cpf: "111.444.777-35");

        // Act
        var act = () => _service.PodeAlterarUsuario(solicitante, alvo);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("403_USER_CANNOT_ALTER");
    }

    // =========================================================================
    // PodeAlterarPerfilEStatus
    // =========================================================================

    [Fact]
    public void PodeAlterarPerfilEStatus_NaoDeveLancar_QuandoGestorAlteraOutroUsuario()
    {
        // Arrange
        var gestor = UsuarioFactory.CriarGestor();
        var alvo = UsuarioFactory.CriarDoador();

        // Act & Assert
        var act = () => _service.PodeAlterarPerfilEStatus(gestor, alvo);
        act.Should().NotThrow();
    }

    [Fact]
    public void PodeAlterarPerfilEStatus_DeveLancarDomainException_QuandoSolicitanteNaoEGestor()
    {
        // Arrange
        var doador = UsuarioFactory.CriarDoador(email: "doador@email.com", cpf: "529.982.247-25");
        var alvo = UsuarioFactory.CriarDoador(email: "alvo@email.com", cpf: "111.444.777-35");

        // Act
        var act = () => _service.PodeAlterarPerfilEStatus(doador, alvo);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("403_USER_CANNOT_MODIFY_ACCESS_LEVEL");
    }

    [Fact]
    public void PodeAlterarPerfilEStatus_DeveLancarDomainException_QuandoGestorTentaAlterarProprioPerfilOuStatus()
    {
        // Arrange — gestor tentando modificar a si mesmo (violação de segurança)
        var gestor = UsuarioFactory.CriarGestor();

        // Act
        var act = () => _service.PodeAlterarPerfilEStatus(gestor, gestor);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("403_USER_CANNOT_MODIFY_OWN_ACCESS_LEVEL");
    }
}
