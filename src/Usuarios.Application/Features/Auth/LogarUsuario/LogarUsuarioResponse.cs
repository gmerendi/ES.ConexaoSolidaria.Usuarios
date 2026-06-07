namespace Usuarios.Application.Features.Auth
{
    public record LogarUsuarioResponse(
        string Token,
        DateTime DataExpiracao,
        Guid UsuarioId,
        string Email,
        string Status        
    );
}
