namespace Usuarios.Domain.Events
{
    public record UsuarioCriadoEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, string? correlationId);
    public record DoacaoRecebidaEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, Guid guidCampanha, Guid guidDoacao, decimal valorDoacao, string? correlationId);
    public record EnvioEmailDoacaoEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, Guid guidCampanha, Guid guidDoacao, decimal valorDoacao, string? correlationId);
}
