using System.ComponentModel.DataAnnotations;

namespace Usuarios.Application.Features.Usuarios
{
    public record ModificarUsuarioCommand(
        [Required(ErrorMessage = "400_NAME_REQUIRED")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "422_NAME_LENGTH_INVALID")]
        string NomeCompleto,

        [Required(ErrorMessage = "400_CPF_REQUIRED")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "422_CPF_INVALID_LENGTH")]
        string Cpf
    );
}
