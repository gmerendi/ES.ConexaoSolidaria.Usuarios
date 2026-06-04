using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.Controllers.DTOs
{
    public record ModificarUsuarioRequest(
        [Required(ErrorMessage = "400_GUID_REQUIRED")]
        string Guid,

        [Required(ErrorMessage = "400_NAME_REQUIRED")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "422_NAME_LENGTH_INVALID")]
        string NomeCompleto,

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