using FluentAssertions;
using Usuarios.Domain.Entities.Usuarios;
using Xunit;

namespace Usuarios.Tests.Domain;

/// <summary>
/// Testes unitários para o Value Object Password.
/// Cobre validações de complexidade, geração de hash e verificação.
/// </summary>
public class PasswordTests
{
    // -------------------------------------------------------------------------
    // Cenários de SUCESSO
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Senha@123")]
    [InlineData("MinhaSenha!9")]
    [InlineData("Ab1#abcdefgh")]
    public void CreatePasswordHash_DeveRetornarSucesso_QuandoSenhaAtendeTodosRequisitos(string senha)
    {
        // Act
        var resultado = Password.CreatePasswordHash(senha);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value!.Hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreatePasswordHash_DeveGerarHashBCrypt_QuandoSenhaValida()
    {
        // Arrange
        var senha = "Senha@123";

        // Act
        var resultado = Password.CreatePasswordHash(senha);

        // Assert — Hash BCrypt começa com $2a$ ou $2b$
        resultado.Value!.Hash.Should().StartWith("$2");
    }

    [Fact]
    public void VerifyPasswordHash_DeveRetornarTrue_QuandoSenhaCorreta()
    {
        // Arrange
        var senhaPlana = "Senha@123";
        var password = Password.CreatePasswordHash(senhaPlana).Value!;

        // Act
        var valido = password.VerifyPasswordHash(senhaPlana);

        // Assert
        valido.Should().BeTrue();
    }

    [Fact]
    public void VerifyPasswordHash_DeveRetornarFalse_QuandoSenhaIncorreta()
    {
        // Arrange
        var password = Password.CreatePasswordHash("Senha@123").Value!;

        // Act
        var valido = password.VerifyPasswordHash("SenhaErrada!1");

        // Assert
        valido.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — campo obrigatório
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreatePasswordHash_DeveLancarException_QuandoSenhaVazia(string senha)
    {
        // Act
        var act = () => Password.CreatePasswordHash(senha);

        // Assert
        act.Should().Throw<Exception>();
    }

    // -------------------------------------------------------------------------
    // Cenários de FALHA — regras de complexidade
    // -------------------------------------------------------------------------

    [Fact]
    public void CreatePasswordHash_DeveRetornarFalha_QuandoSenhaMenorQueOitoCaracteres()
    {
        // Act
        var resultado = Password.CreatePasswordHash("Ab1@xyz"); // 7 chars

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Contain("422_PASSWORD_TOO_SHORT");
    }

    [Fact]
    public void CreatePasswordHash_DeveRetornarFalha_QuandoSenhaSemLetraMaiuscula()
    {
        // Act
        var resultado = Password.CreatePasswordHash("senha@123");

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_UPPERCASE");
    }

    [Fact]
    public void CreatePasswordHash_DeveRetornarFalha_QuandoSenhaSemLetraMinuscula()
    {
        // Act
        var resultado = Password.CreatePasswordHash("SENHA@123");

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_LOWERCASE");
    }

    [Fact]
    public void CreatePasswordHash_DeveRetornarFalha_QuandoSenhaSemDigito()
    {
        // Act
        var resultado = Password.CreatePasswordHash("Senha@abc");

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_DIGIT");
    }

    [Fact]
    public void CreatePasswordHash_DeveRetornarFalha_QuandoSenhaSemCaractereEspecial()
    {
        // Act
        var resultado = Password.CreatePasswordHash("Senha1234");

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_SPECIAL_CHAR");
    }

    [Fact]
    public void CreatePasswordHash_DeveAcumularMultiplosErros_QuandoSenhaViolaVariasRegras()
    {
        // Arrange — senha fraca: curta, só minúsculas, sem dígito, sem especial
        var senha = "abc";

        // Act
        var resultado = Password.CreatePasswordHash(senha);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        // Deve ter mais de um código de erro concatenado
        resultado.ErrorCode.Should().Contain("422_PASSWORD_TOO_SHORT");
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_UPPERCASE");
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_DIGIT");
        resultado.ErrorCode.Should().Contain("422_PASSWORD_REQUIRES_SPECIAL_CHAR");
    }
}
