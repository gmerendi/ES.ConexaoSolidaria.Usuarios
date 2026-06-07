using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Entity.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Exceptions;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Application.Features.Usuarios
{
    public class ObterUsuarioCommandHandler : IUseCaseHandler<ObterUsuarioCommand, Result<ObterUsuarioResponse>>
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUserContext _userContext;
        private readonly IBaseLogger<ObterUsuarioCommandHandler> _logger;
        private readonly ICacheService _cacheService;

        public ObterUsuarioCommandHandler(IUsuarioRepository usuarioRepository, IUserContext userContext,
            IBaseLogger<ObterUsuarioCommandHandler> logger, ICacheService cacheService)
        {
            _usuarioRepository = usuarioRepository;
            _userContext = userContext;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<ObterUsuarioResponse>> HandleAsync(ObterUsuarioCommand command, CancellationToken ct)
        {
            // 1 - Verificar se o command não é nulo 
            if (command == null)
            {
                throw new DomainException("400_COMMAND_INVALID");
            }

            try
            {
                _logger.LogInformation("Tentativa de busca de usuario iniciada para o email: " + command.Email, BaseLogType.LOG, command);

                //2 - Buscar solicitante. Caso seja Doador, somente pode obter o próprio perfil
                var solicitante = _userContext.GetUser() ?? null;

                if (solicitante == null) 
                {
                    throw new DomainException("400_REQUESTER_REQUIRED");
                }

                if (solicitante.Perfil == Perfil.DOADOR.ToString() && solicitante.Email != command.Email)
                {
                    throw new DomainException("403_USER_NOT_ALLOWED");
                }


                // 3 - Tentar obter usuario do cache
                var cacheKey = $"usuario:{command.Email}";
                var usuario = await _cacheService.GetAsync<UsuarioDTO>(cacheKey);


                // 4 - Buscar usuario - não encontrado no cache
                if (usuario == null)
                {
                    _logger.LogInformation("Usuario não encontrado no cache, buscando no banco: " + command.Email, BaseLogType.LOG, command.Email);

                    var usuarioDb = await _usuarioRepository.ObterPorEmailAsync(command.Email, ct);
                    if (usuarioDb == null)
                    {
                        throw new DomainException("400_USER_NOT_FOUND");
                    }
                    // 5 - Insere usuario no cache
                    usuario = UsuarioDTO.FromEntity(usuarioDb);
                    await _cacheService.SetAsync(cacheKey, usuario, TimeSpan.FromMinutes(30));
                }
                else
                {
                    _logger.LogInformation("Usuario encontrado no cache: " + command.Email, BaseLogType.LOG, command.Email);
                }

                var response = new ObterUsuarioResponse
                (
                    usuario.Guid,
                    usuario.NomeCompleto,
                    usuario.Cpf,
                    usuario.Email,
                    usuario.Perfil,
                    usuario.Status
                );
                return Result<ObterUsuarioResponse>.Success(response);
            }
            catch (DomainException)
            {
                throw; 
            }
            catch (Exception ex) {  
                _logger.LogError("Erro ao obter usuario: " + ex.Message, BaseLogType.LOG, ex.Message);
                throw new ApplicationException("Ocorreu um erro ao obter o usuário. " + ex.Message);
            }
        }   
    }
}
