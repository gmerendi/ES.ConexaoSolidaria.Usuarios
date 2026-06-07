using System.ComponentModel.DataAnnotations;

namespace Usuarios.Application.Features.Usuarios
{
    public record AlterarPerfilParaGestorCommand(
        [Required(ErrorMessage = "400_EMAIL_REQUIRED")]
        [EmailAddress(ErrorMessage = "422_EMAIL_INVALID_FORMAT")]
        [StringLength(100, ErrorMessage = "422_EMAIL_LENGTH_INVALID")]
        string Email
    );
}
