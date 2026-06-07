using System.ComponentModel.DataAnnotations;

namespace Usuarios.Application.Features.Auth
{
    public record DeslogarUsuarioCommand(
        [Required(ErrorMessage = "400_TOKEN_REQUIRED")]
        string Token
    );
}
        