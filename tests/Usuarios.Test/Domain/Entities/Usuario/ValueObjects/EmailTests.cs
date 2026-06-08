using FluentAssertions;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Shared.Exceptions;
using Xunit;

namespace Usuarios.Tests.Domain;

/// <summary>
/// Testes unitários para o Value Object Email.
/// Cobre criação, validações de formato e igualdade estrutural.
/// </summary>
public class EmailTests
{
    // -------------------------------------------------------------------------
    // Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("usuario@email.com")]
    [InlineData("joao.silva@empresa.com.br")]
    [InlineData("admin@ong.org")]
    [InlineData("teste+tag@dominio.io")]
    [InlineData("x@y.z")]
    public void Create_DeveRetornarEmail_QuandoEnderecoValido(string endereco)
    {
        // Act
        var email = Email.Create(endereco);

        // Assert
        email.Should().NotBeNull();
        email.Endereco.Should().Be(endereco);
    }

    [Fact]
    public void Equals_DeveRetornarTrue_QuandoDoisEmailsComMesmoEnderecoSaoComparados()
    {
        // Arrange
        var email1 = Email.Create("usuario@email.com");
        var email2 = Email.Create("usuario@email.com");

        // Act & Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_DeveRetornarFalse_QuandoDoisEmailsComEnderecoDiferenteSaoComparados()
    {
        // Arrange
        var email1 = Email.Create("a@email.com");
        var email2 = Email.Create("b@email.com");

        // Act & Assert
        email1.Should().NotBe(email2);
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — campo obrigatório
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_DeveLancarDomainException_QuandoEnderecoVazioOuNulo(string endereco)
    {
        // Act
        var act = () => Email.Create(endereco);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_EMAIL_REQUIRED");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — formato inválido
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("semArroba.com")]
    [InlineData("@semlocal.com")]
    [InlineData("semdominio@")]
    [InlineData("dois@@arrobas.com")]
    [InlineData("espacos no meio@email.com")]
    public void Create_DeveLancarDomainException_QuandoFormatoInvalido(string enderecoInvalido)
    {
        // Act
        var act = () => Email.Create(enderecoInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("422_EMAIL_INVALID_FORMAT");
    }

    [Fact]
    public void Create_DeveLancarDomainException_QuandoEnderecoUltrapassaLimiteDeCaracteres()
    {
        // Arrange — 101 caracteres
        var enderecoLongo = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@email.com"; // > 100 chars

        // Act
        var act = () => Email.Create(enderecoLongo);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("422_EMAIL_LENGTH_INVALID");
    }
}
