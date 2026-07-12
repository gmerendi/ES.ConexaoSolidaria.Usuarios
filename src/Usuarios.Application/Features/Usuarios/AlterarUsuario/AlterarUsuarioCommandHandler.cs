using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public sealed class AlterarUsuarioCommandHandler : IUseCaseHandler<AlterarUsuarioCommand, Result<AlterarUsuarioResponse>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<AlterarUsuarioCommandHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IUsuarioDomainService _usuarioDomainService;

        public AlterarUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<AlterarUsuarioCommandHandler> logger, ICacheService cacheService, IUsuarioDomainService usuarioDomainService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _cacheService = cacheService;
            _usuarioDomainService = usuarioDomainService;
        }

        public async Task<Result<AlterarUsuarioResponse>> HandleAsync(AlterarUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                //2 - Buscar solicitante. O usuario modifica somente a si mesmo          
                var solicitante = _userContext.GetUser() ?? null;
                var usuarioSolicitante = await _usuarioRepository.ObterPorEmailAsync(solicitante.Email);
                var usuario = await _usuarioRepository.ObterPorEmailAsync(solicitante.Email);

                _logger.LogInformation("Tentativa de alteracao de usuario iniciada para o email: {Email}", BaseLogType.LOG, 
                    new { Email = usuario.Email.Endereco, Nome = command.NomeCompleto, Cpf = command.Cpf });

                if (usuario == null)
                {
                    throw new DomainException("400_USER_NOT_FOUND");
                }

                if (solicitante == null) 
                {
                    throw new DomainException("400_REQUESTER_REQUIRED");
                }

                _usuarioDomainService.PodeAlterarUsuario(usuarioSolicitante, usuario);

                // 3 - Modifica o cpf e nome do usuario
                usuario.AlterarUsuario(command.NomeCompleto, Cpf.Create(command.Cpf), usuarioSolicitante.Email.Endereco);
                await _usuarioRepository.AlterarAsync(usuario);

                // 4 - Remove usuario do cache
                var cacheKey = $"usuario:{usuario.Email.Endereco}";
                await _cacheService.RemoveAsync(cacheKey);

                UsuarioDTO usuarioResponse = UsuarioDTO.FromEntity(usuario);
                var response = new AlterarUsuarioResponse
                    (
                        usuarioResponse.Guid,
                        usuarioResponse.NomeCompleto,
                        usuarioResponse.Cpf,
                        usuarioResponse.Email,
                        usuarioResponse.Perfil,
                        usuarioResponse.Status
                    );
               

                return Result<AlterarUsuarioResponse>.Success(response);
            }
            catch (DomainException)
            {
                throw; 
            }
            catch (Exception ex) {
                _logger.LogError("Erro ao alterar usuario: {ExceptionMsg}", BaseLogType.LOG, ex);
                throw new ApplicationException("Ocorreu um erro ao alterar o usuário. " + ex.Message);
            }
        }   
    }
}
