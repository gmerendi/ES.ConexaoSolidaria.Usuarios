using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;

namespace Usuarios.Tests;

/// <summary>
/// Fábrica de objetos de domínio válidos para uso nos testes.
/// Centraliza a criação para evitar duplicação e facilitar manutenção.
/// </summary>
public static class UsuarioFactory
{
    // CPFs matematicamente válidos para testes
    public const string CpfValido1 = "529.982.247-25";
    public const string CpfValido2 = "111.444.777-35";
    public const string CpfValido3 = "321.860.780-04";

    public const string EmailValido1 = "joao.silva@email.com";
    public const string EmailValido2 = "maria.souza@email.com";
    public const string EmailValido3 = "admin@ong.org";

    public const string SenhaHashFake = "$2a$12$fakehashfakehashfakehashfakehashfakehash";

    /// <summary>
    /// Cria um Usuario DOADOR ativo com dados válidos.
    /// </summary>
    public static Usuario CriarDoador(
        string nome = "João da Silva Santos",
        string email = EmailValido1,
        string cpf = CpfValido1,
        string solicitante = EmailValido1)
    {
        var emailObj = Email.Create(email);
        var cpfObj = Cpf.Create(cpf);
        var senha = new Password(SenhaHashFake);
        return new Usuario(nome, senha, emailObj, cpfObj, solicitante);
    }

    /// <summary>
    /// Cria um Usuario GESTOR_ONG ativo com dados válidos.
    /// </summary>
    public static Usuario CriarGestor(
        string nome = "Maria Admin ONG",
        string email = EmailValido3,
        string cpf = CpfValido2,
        string solicitante = EmailValido3)
    {
        var emailObj = Email.Create(email);
        var cpfObj = Cpf.Create(cpf);
        var senha = new Password(SenhaHashFake);
        var gestor = new Usuario(nome, senha, emailObj, cpfObj, solicitante);
        gestor.AlterarPerfilParaGestor(solicitante);
        return gestor;
    }

    /// <summary>
    /// Cria um Usuario DOADOR com status SUSPENDED.
    /// </summary>
    public static Usuario CriarDoadorSuspenso(string modificadoPor = EmailValido3)
    {
        var usuario = CriarDoador();
        usuario.Suspender(modificadoPor);
        return usuario;
    }

    /// <summary>
    /// Cria um Usuario DOADOR com status REMOVED.
    /// </summary>
    public static Usuario CriarDoadorRemovido(string modificadoPor = EmailValido3)
    {
        var usuario = CriarDoador();
        // Simulate removed via reflection since there's no public Remove method
        var statusProp = typeof(Usuarios.Domain.Shared.Entity.EntityBase)
            .GetProperty("Status")!;
        statusProp.SetValue(usuario, EntityStatus.REMOVED);
        return usuario;
    }
}
