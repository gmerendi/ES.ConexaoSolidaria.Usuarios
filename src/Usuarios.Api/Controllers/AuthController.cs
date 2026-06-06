using Microsoft.AspNetCore.Mvc;
using Usuarios.Api.Controllers.DTOs;
using Usuarios.Application.Features.Auth;
using Usuarios.Application.Features.Usuarios;
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

    public AuthController(IBaseLogger<AuthController> logger, IUseCaseHandler<LogarUsuarioCommand, Result<LogarUsuarioResponse>> logarUsuarioHandler)
    {
        _logger = logger;
        _logarUsuarioHandler = logarUsuarioHandler;
    }


    /// <summary>UC-02 - Realizar Login</summary>
    /// <summary>Cadastrar novo doador</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CriarUsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
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
        return Created(string.Empty, result.Value);
    }


    /*
    /// <summary>Autenticar usuário</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _login.ExecuteAsync(request, ct);
        if (!result.IsSuccess) return MapError(result.Error!);
        return Ok(result.Value);
    }

    /// <summary>Renovar access token via refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _refresh.ExecuteAsync(request, ct);
        if (!result.IsSuccess) return MapError(result.Error!);
        return Ok(result.Value);
    }

    /// <summary>Solicitar reset de senha (resposta sempre genérica)</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _forgotPassword.ExecuteAsync(request, GetCorrelationId(), ct);
        return Ok(new { message = ApplicationErrors.Auth_ForgotPassword_Message });
    }

    /// <summary>Confirmar nova senha com token recebido por e-mail</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _resetPassword.ExecuteAsync(request, ct);
        if (!result.IsSuccess) return MapError(result.Error!);
        return Ok(new { message = ApplicationErrors.Auth_ResetPassword_Success });
    }

    private string? GetCorrelationId() => HttpContext.Items["CorrelationId"]?.ToString();

    private IActionResult MapError(string error)
    {
        var cid = GetCorrelationId();
        if (error == ApplicationErrors.Auth_EmailAlreadyExists) return Conflict(ErrorResponse.Conflict(error, cid));
        if (error == ApplicationErrors.Auth_CpfAlreadyExists)   return Conflict(ErrorResponse.Conflict(error, cid));
        if (error == ApplicationErrors.Auth_InvalidCredentials)  return Unauthorized(ErrorResponse.Unauthorized(cid));
        if (error == ApplicationErrors.Auth_AccountSuspended)    return StatusCode(403, ErrorResponse.Forbidden(error, cid));
        if (error == ApplicationErrors.Auth_InvalidRefreshToken) return Unauthorized(ErrorResponse.Unauthorized(cid));
        return UnprocessableEntity(ErrorResponse.Validation(error, new(), cid));
    }
    */
}
