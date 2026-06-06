using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entity.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public class CriarUsuarioCommandHandler : IUseCaseHandler<CriarUsuarioCommand, Result<CriarUsuarioResponse>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<CriarUsuarioCommandHandler> _logger;
        private readonly IUsuarioDomainService _usuarioDomainService;
        private readonly IMessageService _messageService;

        public CriarUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<CriarUsuarioCommandHandler> logger, IUsuarioDomainService usuarioDomainService, 
            IMessageService messageService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _usuarioDomainService = usuarioDomainService;
            _messageService = messageService;
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
                _logger.LogInformation("Tentativa de criacao de usuario iniciada para o e-mail: " + command.Email, BaseLogType.LOG, command.Email);
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

                // 4 - Criar o usuário
                var email = Email.Create(command.Email);
                var cpf = Cpf.Create(command.Cpf);
                var password = Password.CreatePasswordHash(command.Password).Value;
                var usuario = new Usuario(command.NomeCompleto, password, email, cpf, solicitante);

                // 5 - Gravar o usuário
                await _usuarioRepository.CadastrarAsync(usuario);

                // 6 - Enviar mensagem de usuario  criado
                await _messageService.SendUserCreatedEventMessage(usuario.Guid, usuario.NomeCompleto, usuario.Email.Endereco, usuario.Cpf.Numero, ct);

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
                throw; 
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao cadastrar usuario: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao cadastrar o usuário. " + ex.Message);
            }
        }   
    }
}
