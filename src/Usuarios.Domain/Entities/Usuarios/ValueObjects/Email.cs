using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Helpers;
using Usuarios.Domain.Shared.ValueObjects;

namespace Usuarios.Domain.Entities.Usuarios
{
    public class Email : ValueObject
    {
        public string Endereco { get; private set; } = String.Empty;

        protected Email() { } // Para EF

        public Email(string endereco)
        {
            Endereco = endereco;
        }



        // -----------------------------------------------------------------------------
        // Metodos
        // -----------------------------------------------------------------------------
        public static Email Create(string endereco)
        {
            AssertionConcern.AssertArgumentNotEmpty(endereco, "EMAIL_REQUIRED");

            AssertionConcern.AssertArgumentLength(endereco, 0, 100, "EMAIL_LENGTH_INVALID");

            // Validação do formato
            if (!IsValidEmailFormat(endereco))
            {
                throw new DomainException("EMAIL_INVALID_FORMAT");
            }

            // Se for válido, retorna Sucesso com a nova instância
            return new Email(endereco);
        }


        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Endereco;
        }


        // -----------------------------------------------------------------------------
        // Validações
        // -----------------------------------------------------------------------------
        private static bool IsValidEmailFormat(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }
    }
}
