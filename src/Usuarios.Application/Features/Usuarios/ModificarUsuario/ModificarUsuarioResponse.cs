namespace Usuarios.Application.Features.Usuarios
{
    public record ModificarUsuarioResponse(
        Guid Guid,
        string NomeCompleto,
        string Cpf,
        string Email,
        string Perfil,
        string Status        
    );
}
