using Microsoft.AspNetCore.Mvc;
using Usuarios.Api.Controllers.DTOs;
using Usuarios.Application.Interfaces;

namespace Users.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IBaseLogger<AuthController> _logger;

    public AuthController(IBaseLogger<AuthController> logger)
    {
        _logger = logger;
    }


    /// <summary>UC-02 - Cadastrar novo doador</summary>


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
