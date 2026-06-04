using Usuarios.Application.Interfaces;
using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entity.Usuarios;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public class CriarUsuarioCommandHandler : IUseCaseHandler<CriarUsuarioCommand, Result<CriarUsuarioResponse>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<CriarUsuarioCommandHandler> _logger;
        private readonly IUsuarioDomainService _usuarioDomainService;

        public CriarUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<CriarUsuarioCommandHandler> logger, IUsuarioDomainService usuarioDomainService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _usuarioDomainService = usuarioDomainService;
        }

        public async Task<Result<CriarUsuarioResponse>> HandleAsync(CriarUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                // 2 - Verificar se usuario já existe
                var usuarioExistente = await _usuarioRepository.ObterPorEmailAsync(command.Email, ct);

                if (usuarioExistente != null)
                {
                    throw new DomainException("422_USER_DUPLICATED");
                }

                // 2 - Verificar se usuario já existe
                var cpfExistente = await _usuarioRepository.ObterPorCpfAsync(command.Cpf, ct);

                if (cpfExistente != null)
                {
                    throw new DomainException("422_CPF_DUPLICATED");
                }

                // 3 - Verificar se o solicitante é um usuario logado.
                var solicitante = _userContext.GetUser()?.Email ?? command.Email;

                // 3 - Criar o usuário
                var email = Email.Create(command.Email);
                var cpf = Cpf.Create(command.Cpf);
                var password = new Password(command.Password);
                var usuario = new Usuario(command.NomeCompleto, password, email, cpf, solicitante);

                await _usuarioRepository.CadastrarAsync(usuario);

                var response = new CriarUsuarioResponse
                (
                    usuario.Guid,
                    usuario.NomeCompleto,
                    Cpf.Anonymize(usuario.Cpf.Numero),
                    usuario.Email.Endereco,
                    usuario.Perfil.ToString(),
                    usuario.Status.ToString()
                );
                return Result<CriarUsuarioResponse>.Success(response);
            }
            catch (DomainException)
            {
                throw; // Repassa a DomainException intacta para o Middleware
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao cadastrar usuario: " + ex.Message, ex);
                throw new ApplicationException("Ocorreu um erro ao cadastrar o usuário. " + ex.Message);
            }
        }   
    }
}
