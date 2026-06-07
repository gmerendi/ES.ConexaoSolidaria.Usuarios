namespace Usuarios.Application.Features.Usuarios
{
    public record ObterUsuarioResponse(
        Guid Guid,
        string NomeCompleto,
        string Cpf,
        string Email,
        string Perfil,
        string Status        
    );
}
