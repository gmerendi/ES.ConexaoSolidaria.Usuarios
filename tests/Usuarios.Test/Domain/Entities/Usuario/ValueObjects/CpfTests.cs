using FluentAssertions;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Shared.Exceptions;
using Xunit;

namespace Usuarios.Tests.Domain;

/// <summary>
/// Testes unitários para o Value Object Cpf.
/// Cobre criação com formatação, validação matemática e anonimização.
/// </summary>
public class CpfTests
{
    // -------------------------------------------------------------------------
    // Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]   // com máscara
    [InlineData("52998224725", "52998224725")]   // sem máscara
    [InlineData("111.444.777-35", "11144477735")]
    [InlineData("242.202.675-30", "24220267530")]
    public void Create_DeveRetornarCpfLimpo_QuandoCpfMatematicoValido(string cpfEntrada, string cpfEsperado)
    {
        // Act
        var cpf = Cpf.Create(cpfEntrada);

        // Assert
        cpf.Should().NotBeNull();
        cpf.Numero.Should().Be(cpfEsperado);
    }

    [Fact]
    public void Equals_DeveRetornarTrue_QuandoDoisCpfsComMesmoNumeroSaoComparados()
    {
        // Arrange
        var cpf1 = Cpf.Create("529.982.247-25");
        var cpf2 = Cpf.Create("52998224725");

        // Act & Assert
        cpf1.Should().Be(cpf2);
    }

    [Fact]
    public void Equals_DeveRetornarFalse_QuandoDoisCpfsComNumerosDiferentesSaoComparados()
    {
        // Arrange
        var cpf1 = Cpf.Create("529.982.247-25");
        var cpf2 = Cpf.Create("111.444.777-35");

        // Act & Assert
        cpf1.Should().NotBe(cpf2);
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — campo obrigatório
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_DeveLancarDomainException_QuandoCpfVazioOuNulo(string cpf)
    {
        // Act
        var act = () => Cpf.Create(cpf);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("400_CPF_REQUIRED");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — tamanho
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("123")]           // curto demais
    [InlineData("1234567890123")] // longo demais (13 dígitos)
    public void Create_DeveLancarDomainException_QuandoCpfComTamanhoInvalido(string cpfInvalido)
    {
        // Act
        var act = () => Cpf.Create(cpfInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("422_CPF_INVALID_LENGTH");
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — estrutura matemática
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("11111111111")] // todos iguais — sequência inválida
    [InlineData("22222222222")]
    [InlineData("00000000000")]
    [InlineData("12345678900")] // dígitos verificadores errados
    [InlineData("99988877766")]
    public void Create_DeveLancarDomainException_QuandoCpfInvalidoMatematicamente(string cpfInvalido)
    {
        // Act
        var act = () => Cpf.Create(cpfInvalido);

        // Assert
        act.Should().Throw<DomainException>()
           .Which.ErrorCode.Should().Be("422_CPF_INVALID");
    }

    // -------------------------------------------------------------------------
    // Anonimização
    // -------------------------------------------------------------------------

    [Fact]
    public void Anonymize_DeveRetornarTresPrimeirosDigitosMaisAsteriscos()
    {
        // Arrange
        var cpf = "529.982.247-25";

        // Act
        var anonimizado = Cpf.Anonymize(cpf);

        // Assert
        anonimizado.Should().Be("529********");
    }

    [Fact]
    public void Anonymize_DeveRetornarStringVazia_QuandoCpfNuloOuVazio()
    {
        Cpf.Anonymize("").Should().BeEmpty();
        Cpf.Anonymize("   ").Should().BeEmpty();
    }

    [Fact]
    public void Anonymize_DeveRetornarStringVazia_QuandoCpfComTamanhoInvalido()
    {
        // Arrange — CPF com menos de 11 dígitos
        var cpfCurto = "123456";

        // Act
        var resultado = Cpf.Anonymize(cpfCurto);

        // Assert
        resultado.Should().BeEmpty();
    }
}
