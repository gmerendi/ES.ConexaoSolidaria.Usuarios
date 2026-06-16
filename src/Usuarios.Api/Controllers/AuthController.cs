using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Usuarios.Api.Controllers.DTOs;
using Usuarios.Application.Features.Auth;
using Usuarios.Application.Shared;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Users.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IBaseLogger<AuthController> _logger;
    private readonly IUseCaseHandler<LogarUsuarioCommand, Result<LogarUsuarioResponse>> _logarUsuarioHandler;
    private readonly IUseCaseHandler<DeslogarUsuarioCommand, Result<bool>> _deslogarUsuarioHandler;
    private readonly IUseCaseHandler<ResetarSenhaCommand, Result<string>> _resetPasswordCommandHandler;

    public AuthController(IBaseLogger<AuthController> logger, IUseCaseHandler<LogarUsuarioCommand, Result<LogarUsuarioResponse>> logarUsuarioHandler,
        IUseCaseHandler<DeslogarUsuarioCommand, Result<bool>> deslogarUsuarioHandler, IUseCaseHandler<ResetarSenhaCommand, Result<string>> resetPasswordCommandHandler)
    {
        _logger = logger;
        _logarUsuarioHandler = logarUsuarioHandler;
        _deslogarUsuarioHandler = deslogarUsuarioHandler;
        _resetPasswordCommandHandler = resetPasswordCommandHandler;
    }


    /// <summary>
    /// UC-02 - Realizar Login
    /// </summary>
    /// <remarks>
    /// 
    /// **Efetua a autenticação do usuário no sistema.**
    /// 
    /// Se as credenciais estiverem corretas, gera um Token JWT e registra a sessão ativa no cache (Redis).
    /// 
    /// **Regras de Negócio e Fluxo:**
    /// * Valida se o formato do e-mail é válido.
    /// * Verifica se o usuário existe e se o status **não** está como `SUSPENDED` ou `REMOVED`.
    /// * Compara a senha informada com o hash criptografado (BCrypt) no banco de dados.
    /// * Retorna o Token e suas informações de expiração.
    /// </remarks>
    /// <param name="request">Objeto contendo as credenciais de acesso (E-mail e Senha).</param>
    /// <param name="ct">Token de cancelamento da requisição (CancellationToken).</param>
    /// <returns>Retorna os dados da sessão criada incluindo o Token JWT.</returns>
    /// <response code="200">Login realizado com sucesso. Token gerado.</response>
    /// <response code="400">Requisição malformada ou parâmetros inválidos.</response>
    /// <response code="401">Credenciais inválidas (E-mail ou Senha incorretos).</response>
    /// <response code="403">Usuário bloqueado, suspenso ou removido do sistema.</response>
    /// <response code="500">Erro interno no servidor ao processar a autenticação.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LogarUsuarioResponse), StatusCodes.Status200OK)] 
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LogarUsuarioRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando login de usuario: " + request.Email, BaseLogType.LOG, request.Email);
        var command = new LogarUsuarioCommand(request.Email, request.Password);

        var result = await _logarUsuarioHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario logado com sucesso: " + request.Email, BaseLogType.LOG, result.Value.Email);
        return Ok(result.Value);
    }



    /// <summary>
    /// UC-03 - Realizar Logout
    /// </summary>
    /// <remarks>
    /// 
    /// Invalida o token JWT atual enviando-o para uma Blacklist no Redis pelo tempo restante de vida do token 
    /// e limpa as informações do usuário que estavam armazenadas em cache.
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// </remarks>
    /// <param name="ct">Token de cancelamento da requisição (CancellationToken).</param>
    /// <response code="200">Logout realizado com sucesso. Retorna true se o token foi invalidado.</response>
    /// <response code="400">Requisição inválida. Token não informado ou malformado.</response>
    /// <response code="401">Não autorizado. Token JWT ausente, expirado ou inválido.</response>
    /// <response code="403">Proibido. Usuário autenticado não possui o perfil necessário (Requer: GESTOR_ONG ou DOADOR).</response>
    /// <response code="500">Erro interno do servidor ao processar a invalidação do token.</response>
    [Authorize(Roles = "GESTOR_ONG,DOADOR")]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var emailLogado = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                          ?? User.FindFirst("email")?.Value;

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        _logger.LogInformation("Iniciando logout de usuario: " + emailLogado, BaseLogType.LOG, token);

        var command = new DeslogarUsuarioCommand(token);

        var result = await _deslogarUsuarioHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario deslogado com sucesso: " + emailLogado, BaseLogType.LOG, null);
        return Ok(result.Value);
    }





    /// <summary>
    /// UC-06 - Resetar senha de usuario
    /// </summary>
    /// <remarks>   
    /// 
    ///  Reseta senha do usuário. O usuário receberá um email notificando que a senha foi resetada.
    ///  
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Senha Anterior:**
    ///   - `O campo Senha é obrigatório.`
    ///   - `A senha deve ter no mínimo 8 caracteres.`
    ///   - `A Senha deve conter: letra maiúscula, minúscula, número e caracter especial.`
    ///   
    /// * ** Nova Senha:**
    ///   - `O campo Senha é obrigatório.`
    ///   - `A senha deve ter no mínimo 8 caracteres.`
    ///   - `A Senha deve conter: letra maiúscula, minúscula, número e caracter especial.`
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="200">Token novo gerado.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG,DOADOR")]
    [HttpPut("reset-password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetarSenhaRequest request, CancellationToken ct)
    {
        var emailLogado = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                          ?? User.FindFirst("email")?.Value;

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        _logger.LogInformation("Iniciando reset de senha para usuario: " + emailLogado, BaseLogType.LOG, null);

        var command = new ResetarSenhaCommand(request.PasswordAtual, request.PasswordNovo, token);

        var result = await _resetPasswordCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Senha resetada com sucesso para usuario: " + emailLogado, BaseLogType.LOG, null);
        return Ok(result.Value);
    }
}
