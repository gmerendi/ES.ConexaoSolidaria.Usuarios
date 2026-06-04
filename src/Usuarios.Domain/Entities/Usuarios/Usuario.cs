using System.Diagnostics.CodeAnalysis;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Entity;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Helpers;

namespace Usuarios.Domain.Entities.Usuarios
{
    public class Usuario : EntityBase
    {
        public string NomeCompleto { get; private set; } = String.Empty;
        public Perfil Perfil { get; private set; } = Perfil.DOADOR;
        public string SenhaHash { get; private set; } = String.Empty;
        public Email Email { get; private set; }
        public Cpf Cpf { get; private set; }

        protected Usuario() { } // Para EF Core

        [SetsRequiredMembers]
        public Usuario(string nomeCompleto, Password senhaHash, Email email,
                       Cpf cpf, string solicitanteEmail)
        {
            // Usuario sempre será criado com perfil DOADOR, para garantir a segurança
            // e evitar que usuários sejam criados com perfis administrativos sem autorização.

            // Validacoes
            NomeAssertions(nomeCompleto);
            SenhaAssertions(senhaHash.Hash);
            EmailAssertions(email);
            CpfAssertions(cpf);
            SolicitanteAssertions(solicitanteEmail);


            NomeCompleto = nomeCompleto;
            SenhaHash = senhaHash.Hash;
            Email = email;
            Cpf = cpf;
            CriadoPor = solicitanteEmail;
        }




        // -----------------------------------------------------------------------------
        // Metodos
        // -----------------------------------------------------------------------------
        public void AlterarUsuario(string nomeCompleto, Cpf cpf, string modificadoPor)
        {
            NomeAssertions(nomeCompleto);
            CpfAssertions(cpf);
            SolicitanteAssertions(modificadoPor);

            NomeCompleto = nomeCompleto;
            Cpf = cpf;
            ModificadoPor = modificadoPor;
        }


        public void AlterarSenha(Password novaSenha, string modificadoPor)
        {
            SenhaAssertions(novaSenha.Hash);
            SolicitanteAssertions(modificadoPor);

            SenhaHash = novaSenha.Hash;
            ModificadoPor = modificadoPor;
        }


        public void AlterarPerfilParaGestor(string modificadoPor)
        {
            SolicitanteAssertions(modificadoPor);

            Perfil = Perfil.GESTOR_ONG;
            ModificadoPor = modificadoPor;
        }

        public void AlterarPerfilParaDoador(string modificadoPor)
        {
            SolicitanteAssertions(modificadoPor);

            Perfil = Perfil.DOADOR;
            ModificadoPor = modificadoPor;
        }


        public void Suspender(string modificadoPor)
        {
            SolicitanteAssertions(modificadoPor);

            Status = EntityStatus.SUSPENDED;
            ModificadoPor = modificadoPor;
        }

        public void Ativar(string modificadoPor)
        {
            SolicitanteAssertions(modificadoPor);

            Status = EntityStatus.ACTIVE;
            ModificadoPor = modificadoPor;
        }

        public void Remover(string modificadoPor)
        {
            SolicitanteAssertions(modificadoPor);

            // O usuario não é removido. Ele é marcado como REMOVED
            // para manter o histórico de acordo com a política de dados.
            // O worker removerá os dados pessoais após o período de retenção definido.
            Status = EntityStatus.REMOVED;
            ModificadoPor = modificadoPor;
        }


        // -----------------------------------------------------------------------------
        // Validações
        // -----------------------------------------------------------------------------
        private static void NomeAssertions(string nome)
        {
            AssertionConcern.AssertArgumentNotEmpty(nome, "NAME_REQUIRED");
            AssertionConcern.AssertArgumentLength(nome, 5, 200, "NAME_LENGTH_INVALID");
        }

        private static void SenhaAssertions(string senha)
        {
            AssertionConcern.AssertArgumentNotEmpty(senha, "PASSWORD_REQUIRED");
        }

        private static void SolicitanteAssertions(string solicitanteEmail)
        {
            AssertionConcern.AssertArgumentNotEmpty(solicitanteEmail, "REQUESTER_REQUIRED");//Aqui já nao testa se o solicitante existe
        }


        private static void EmailAssertions(Email email)
        {
            AssertionConcern.AssertArgumentNotNull(email, "EMAIL_REQUIRED");
        }


        private static void CpfAssertions(Cpf cpf)
        {
            AssertionConcern.AssertArgumentNotNull(cpf, "CPF_REQUIRED");
        }


        private static void PerfilAssertions(Perfil perfil)
        {
            if (!Enum.IsDefined(typeof(Perfil), perfil))
            {
                throw new DomainException("PROFILE_INVALID");
            }
        }

        private static void StatusAssertions(EntityStatus statusCorrente, EntityStatus statusNovo)
        {
            if (!Enum.IsDefined(typeof(EntityStatus), statusNovo))
            {
                throw new DomainException("STATUS_INVALID");
            } 
        }
    }
}
