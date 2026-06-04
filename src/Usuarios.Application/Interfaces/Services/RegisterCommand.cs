using System.ComponentModel.DataAnnotations;

namespace Usuarios.Api.Controllers.DTOs
{
    public record RegisterCommand(
        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "O {0} deve ter no mínimo 5 e no máximo 100 caracteres.")]
        string NomeCompleto,

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail em formato inválido.")]
        [StringLength(100, ErrorMessage = "O {0} deve ter no máximo 100 caracteres.")]
        string Email,

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "O {0} must conter exatamente 11 dígitos numéricos.")]
        string Cpf,

        [Required(ErrorMessage = "O campo {0} é obrigatório.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A {0} deve ter no mínimo 8 caracteres.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\da-zA-Z]).{8,}$",
        ErrorMessage = "A {0} deve conter: letra maiúscula, minúscula, número e caracter especial.")]
        string Password
    );
}