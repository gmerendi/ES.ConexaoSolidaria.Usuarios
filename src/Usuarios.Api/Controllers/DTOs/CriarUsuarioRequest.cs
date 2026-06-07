using System.ComponentModel.DataAnnotations;
using Usuarios.Domain.Shared.Resources;

namespace Usuarios.Api.Controllers.DTOs
{
    public record CriarUsuarioRequest(
        [Required(ErrorMessage = "400_NAME_REQUIRED")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "400_NAME_LENGTH_INVALID")]
        string NomeCompleto,

        [Required(ErrorMessage = "400_EMAIL_REQUIRED")]
        [EmailAddress(ErrorMessage = "422_EMAIL_INVALID_FORMAT")]
        [StringLength(100, ErrorMessage = "422_EMAIL_LENGTH_INVALID")]
        string Email,

        [Required(ErrorMessage = "400_CPF_REQUIRED")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "422_CPF_INVALID_LENGTH")]
        string Cpf,

        [Required(ErrorMessage = "400_PASSWORD_REQUIRED")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "422_PASSWORD_TOO_SHORT")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\da-zA-Z]).{8,}$",
        ErrorMessage = "422_PASSWORD_COMPLEXITY")]
        string Password
    );
}