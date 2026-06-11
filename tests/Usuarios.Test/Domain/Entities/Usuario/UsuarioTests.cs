using FluentAssertions;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Xunit;

namespace Usuarios.Tests.Domain;

/// <summary>
/// Testes unitários para o Aggregate Root Usuario.
/// Cobre criação, alterações de dados, perfil, status e todas as validações de negócio.
/// </summary>
public class UsuarioTests
{
    // -------------------------------------------------------------------------
    // Criação — Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_DeveCriarUsuario_QuandoDadosValidos()
    {
        // Arrange
        var email = Email.Create("joao@email.com");
        var cpf = Cpf.Create("529.982.247-25");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var usuario = new Usuario("João da Silva Santos", senha, email, cpf, "admin@ong.org");

        // Assert
        usuario.Should().NotBeNull();
        usuario.NomeCompleto.Should().Be("João da Silva Santos");
        usuario.Email.Should().Be(email);
        usuario.Cpf.Should().Be(cpf);
        usuario.SenhaHash.Should().Be(UsuarioFactory.SenhaHashFake);
        usuario.CriadoPor.Should().Be("admin@ong.org");
    }

    [Fact]
    public void Constructor_DeveCriarUsuarioComPerfilDoador_Sempre()
    {
        // Act
        var usuario = UsuarioFactory.CriarDoador();

        // Assert — Regra de negócio: nunca criar com perfil administrativo
        usuario.Perfil.Should().Be(Perfil.DOADOR);
    }

    [Fact]
    public void Constructor_DeveCriarUsuarioComStatusAtivo_Sempre()
    {
        // Act
        var usuario = UsuarioFactory.CriarDoador();

        // Assert
        usuario.Status.Should().Be(EntityStatus.ACTIVE);
    }

    // -------------------------------------------------------------------------
    // Criação — Cenários de FALHA
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_DeveLancarDomainException_QuandoNomeVazioOuNulo(string nome)
    {
        // Arrange
        var email = Email.Create("joao@email.com");
        var cpf = Cpf.Create("529.982.247-25");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var act = () => new Usuario(nome, senha, email, cpf, "admin@ong.org");

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_NAME_REQUIRED");
    }

    [Theory]
    [InlineData("abc")]      // 3 chars — abaixo do mínimo de 5
    [InlineData("ab")]
    public void Constructor_DeveLancarDomainException_QuandoNomeMenorQueCincoCaracteres(string nome)
    {
        // Arrange
        var email = Email.Create("joao@email.com");
        var cpf = Cpf.Create("529.982.247-25");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var act = () => new Usuario(nome, senha, email, cpf, "admin@ong.org");

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_NAME_LENGTH_INVALID");
    }

    [Fact]
    public void Constructor_DeveLancarDomainException_QuandoNomeMaiorQueDuzentosCaracteres()
    {
        // Arrange
        var nomeGigante = new string('a', 201);
        var email = Email.Create("joao@email.com");
        var cpf = Cpf.Create("529.982.247-25");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var act = () => new Usuario(nomeGigante, senha, email, cpf, "admin@ong.org");

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_NAME_LENGTH_INVALID");
    }

    [Fact]
    public void Constructor_DeveLancarDomainException_QuandoEmailNulo()
    {
        // Arrange
        var cpf = Cpf.Create("529.982.247-25");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var act = () => new Usuario("João da Silva", senha, null!, cpf, "admin@ong.org");

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_EMAIL_REQUIRED");
    }

    [Fact]
    public void Constructor_DeveLancarDomainException_QuandoCpfNulo()
    {
        // Arrange
        var email = Email.Create("joao@email.com");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var act = () => new Usuario("João da Silva", senha, email, null!, "admin@ong.org");

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_CPF_REQUIRED");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_DeveLancarDomainException_QuandoSolicitanteVazio(string solicitante)
    {
        // Arrange
        var email = Email.Create("joao@email.com");
        var cpf = Cpf.Create("529.982.247-25");
        var senha = new Password(UsuarioFactory.SenhaHashFake);

        // Act
        var act = () => new Usuario("João da Silva Santos", senha, email, cpf, solicitante);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_REQUESTER_REQUIRED");
    }

    // -------------------------------------------------------------------------
    // AlterarUsuario — Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public void AlterarUsuario_DeveAtualizarNomeECpf_QuandoDadosValidos()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();
        var novoCpf = Cpf.Create("111.444.777-35");

        // Act
        usuario.AlterarUsuario("Carlos Eduardo Lima", novoCpf, "admin@ong.org");

        // Assert
        usuario.NomeCompleto.Should().Be("Carlos Eduardo Lima");
        usuario.Cpf.Should().Be(novoCpf);
        usuario.ModificadoPor.Should().Be("admin@ong.org");
    }

    // -------------------------------------------------------------------------
    // AlterarSenha — Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public void AlterarSenha_DeveAtualizarSenhaHash_QuandoDadosValidos()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();
        var novaSenha = new Password("$2a$12$novoHashAqui");

        // Act
        usuario.AlterarSenha(novaSenha, "joao.silva@email.com");

        // Assert
        usuario.SenhaHash.Should().Be("$2a$12$novoHashAqui");
        usuario.ModificadoPor.Should().Be("joao.silva@email.com");
    }

    // -------------------------------------------------------------------------
    // AlterarPerfil — Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public void AlterarPerfilParaGestor_DeveDefinirPerfilGestorOng()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();

        // Act
        usuario.AlterarPerfilParaGestor("admin@ong.org");

        // Assert
        usuario.Perfil.Should().Be(Perfil.GESTOR_ONG);
        usuario.ModificadoPor.Should().Be("admin@ong.org");
    }

    [Fact]
    public void AlterarPerfilParaDoador_DeveDefinirPerfilDoador_QuandoUsuarioEraGestor()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarGestor();
        usuario.Perfil.Should().Be(Perfil.GESTOR_ONG); // pré-condição

        // Act
        usuario.AlterarPerfilParaDoador("admin@ong.org");

        // Assert
        usuario.Perfil.Should().Be(Perfil.DOADOR);
    }

    // -------------------------------------------------------------------------
    // Suspender / Ativar — Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Fact]
    public void Suspender_DeveDefinirStatusSuspended()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();

        // Act
        usuario.Suspender("admin@ong.org");

        // Assert
        usuario.Status.Should().Be(EntityStatus.SUSPENDED);
        usuario.ModificadoPor.Should().Be("admin@ong.org");
    }

    [Fact]
    public void Ativar_DeveDefinirStatusActive_QuandoUsuarioEstaSuspenso()
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoadorSuspenso();
        usuario.Status.Should().Be(EntityStatus.SUSPENDED); // pré-condição

        // Act
        usuario.Ativar("admin@ong.org");

        // Assert
        usuario.Status.Should().Be(EntityStatus.ACTIVE);
        usuario.ModificadoPor.Should().Be("admin@ong.org");
    }

    // -------------------------------------------------------------------------
    // Suspender / Ativar — Cenários de FALHA (solicitante vazio)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Suspender_DeveLancarDomainException_QuandoSolicitanteVazio(string solicitante)
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoador();

        // Act
        var act = () => usuario.Suspender(solicitante);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_REQUESTER_REQUIRED");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Ativar_DeveLancarDomainException_QuandoSolicitanteVazio(string solicitante)
    {
        // Arrange
        var usuario = UsuarioFactory.CriarDoadorSuspenso();

        // Act
        var act = () => usuario.Ativar(solicitante);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_REQUESTER_REQUIRED");
    }
}
