using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.Controllers.DTOs
{
    public record AlterarPerfilParaGestorRequest(
        [Required(ErrorMessage = "400_EMAIL_REQUIRED")]
        [EmailAddress(ErrorMessage = "422_EMAIL_INVALID_FORMAT")]
        [StringLength(100, ErrorMessage = "422_EMAIL_LENGTH_INVALID")]
        string Email
    );
}