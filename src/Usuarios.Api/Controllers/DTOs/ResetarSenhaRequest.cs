using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.Controllers.DTOs
{
    public record ResetarSenhaRequest(
        [Required(ErrorMessage = "400_PASSWORD_REQUIRED")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "422_PASSWORD_TOO_SHORT")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\da-zA-Z]).{8,}$",
        ErrorMessage = "422_PASSWORD_COMPLEXITY")]
        string PasswordAtual,

        [Required(ErrorMessage = "400_PASSWORD_REQUIRED")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "422_PASSWORD_TOO_SHORT")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\da-zA-Z]).{8,}$",
        ErrorMessage = "422_PASSWORD_COMPLEXITY")]
        string PasswordNovo
    );
}