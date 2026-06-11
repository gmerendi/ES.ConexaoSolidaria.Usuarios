namespace Usuarios.Application.Features.Usuarios
{
    public record AlterarUsuarioResponse(
        Guid Guid,
        string NomeCompleto,
        string Cpf,
        string Email,
        string Perfil,
        string Status        
    );
}
