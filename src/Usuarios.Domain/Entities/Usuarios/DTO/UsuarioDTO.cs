using System.Diagnostics.CodeAnalysis;
using Usuarios.Domain.Enums;

namespace Usuarios.Domain.Entities.Usuarios.DTO
{
    public class UsuarioDTO
    {
        // ✅ IMUTÁVEL: O 'init' permite que o Serializador escreva UMA VEZ na criação,
        // mas impede qualquer código de alterar o valor depois (bloqueia injeção de dados).
        public Guid Guid { get; init; }
        public string NomeCompleto { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Cpf { get; init; } = string.Empty;
        public string Perfil { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;

        public UsuarioDTO() { }


        [SetsRequiredMembers]
        public UsuarioDTO(Guid guid, string nomeCompleto, string cpf, string email, string perfil, string status)
        {
            Guid = guid;
            NomeCompleto = nomeCompleto;
            Cpf = cpf;
            Email = email;
            Perfil = perfil;
            Status = status;
        }


        public static UsuarioDTO FromEntity(Usuario usuario)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));

            return new UsuarioDTO
            {
                Guid = usuario.Guid,
                NomeCompleto = usuario.NomeCompleto,
                Cpf = global::Usuarios.Domain.Entities.Usuarios.Cpf.Anonymize(usuario.Cpf.Numero),
                Email = usuario.Email.Endereco,
                Perfil = usuario.Perfil.ToString(),
                Status = usuario.Status.ToString()
            };
        }
    }
}