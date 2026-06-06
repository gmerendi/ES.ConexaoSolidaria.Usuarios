using System.ComponentModel.DataAnnotations;

namespace Usuarios.Application.Features.Auth
{
    public record LogarUsuarioCommand(
        [Required(ErrorMessage = "400_EMAIL_REQUIRED")]
        [EmailAddress(ErrorMessage = "422_EMAIL_INVALID_FORMAT")]
        [StringLength(100, ErrorMessage = "422_EMAIL_LENGTH_INVALID")]
        string Email,

        [Required(ErrorMessage = "400_PASSWORD_REQUIRED")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "422_PASSWORD_TOO_SHORT")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\da-zA-Z]).{8,}$",
        ErrorMessage = "422_PASSWORD_COMPLEXITY")]
        string Password
    );
}
        