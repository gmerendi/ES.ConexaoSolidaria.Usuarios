using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using Usuarios.Domain.Enums;

namespace Usuarios.Domain.Entities.Usuarios.DTO
{
    public class UsuarioDTO
    {
        public Guid Guid { get; private set; }
        public string NomeCompleto { get; private set; }
        public string Cpf { get; private set; }
        public string Email { get; private set; }
        public string Perfil { get; private set; }
        public string Status { get; private set; }

        [SetsRequiredMembers]
        public UsuarioDTO(Guid guid, string nomeCompleto, Cpf cpf, Email email, Perfil perfil, EntityStatus status)
        {
            Guid = guid;
            NomeCompleto = nomeCompleto;
            Email = email.Endereco;
            Perfil = perfil.ToString();
            Status = status.ToString();
        }
    }
}
