using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Usuarios.Api.Controllers.DTOs;
using Usuarios.Application.Features.Usuarios;
using Usuarios.Application.Shared;
using Usuarios.Domain.Entities.Usuarios.DTO;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;

namespace Users.API.Controllers;

[ApiController]
[Route("api/v1/usuario")]
[Produces("application/json")]
public class UsuarioController : ControllerBase
{
    private readonly IBaseLogger<UsuarioController> _logger;
    private readonly IUseCaseHandler<CriarUsuarioCommand, Result<CriarUsuarioResponse>> _criarUsuarioCommandHandler;
    private readonly IUseCaseHandler<ObterUsuarioQuery, Result<ObterUsuarioResponse>> _obterUsuarioQueryHandler;
    private readonly IUseCaseHandler<RemoverUsuarioCommand, Result<bool>> _removerUsuarioCommandHandler;
    private readonly IUseCaseHandler<SuspenderUsuarioCommand, Result<bool>> _suspenderUsuarioCommandHandler;
    private readonly IUseCaseHandler<AtivarUsuarioCommand, Result<bool>> _ativarUsuarioCommandHandler;
    private readonly IUseCaseHandler<AlterarUsuarioCommand, Result<AlterarUsuarioResponse>> _alterarUsuarioCommandHandler;
    private readonly IUseCaseHandler<AlterarPerfilParaGestorCommand, Result<bool>> _alterarPerfilParaGestorCommandHandler;
    private readonly IUseCaseHandler<AlterarPerfilParaDoadorCommand, Result<bool>> _alterarPerfilParaDoadorCommandHandler;

    public UsuarioController(IBaseLogger<UsuarioController> logger,
        IUseCaseHandler<CriarUsuarioCommand, Result<CriarUsuarioResponse>> criarUsuarioCommandHandler,
        IUseCaseHandler<ObterUsuarioQuery, Result<ObterUsuarioResponse>> obterUsuarioQueryHandler,
        IUseCaseHandler<RemoverUsuarioCommand, Result<bool>> removerUsuarioCommandHandler,
        IUseCaseHandler<SuspenderUsuarioCommand, Result<bool>> suspenderUsuarioCommandHandler,
        IUseCaseHandler<AtivarUsuarioCommand, Result<bool>> ativarUsuarioCommandHandler,
        IUseCaseHandler<AlterarUsuarioCommand, Result<AlterarUsuarioResponse>> alterarUsuarioCommandHandler,
        IUseCaseHandler<AlterarPerfilParaGestorCommand, Result<bool>> alterarPerfilParaGestorCommandHandler,
        IUseCaseHandler<AlterarPerfilParaDoadorCommand, Result<bool>> alterarPerfilParaDoadorCommandHandler)
    {
        _logger = logger;
        _criarUsuarioCommandHandler = criarUsuarioCommandHandler;
        _obterUsuarioQueryHandler = obterUsuarioQueryHandler;
        _removerUsuarioCommandHandler = removerUsuarioCommandHandler;
        _suspenderUsuarioCommandHandler = suspenderUsuarioCommandHandler;
        _ativarUsuarioCommandHandler = ativarUsuarioCommandHandler;
        _alterarUsuarioCommandHandler = alterarUsuarioCommandHandler;
        _alterarPerfilParaGestorCommandHandler = alterarPerfilParaGestorCommandHandler;
        _alterarPerfilParaDoadorCommandHandler = alterarPerfilParaDoadorCommandHandler;
    }





    /// <summary>
    /// UC-01 - Cadastrar um novo usuario
    /// </summary>
    /// <remarks>   
    /// 
    /// Cadastra um novo usuário no sistema.
    /// O novo usuario sempre será um doador.
    /// 
    /// **Esse endpoint não requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Nome:**
    ///   - `O campo NomeCompleto é obrigatório.`
    ///   - `O NomeCompleto deve ter no minimo 5 e no máximo 100 caracteres.`
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   - `O e-mail não pode ter sido previamente cadastrado.`
    /// * **CPF:** 
    ///   - `O campo E-mail é obrigatório.`
    ///   - `O cpf não pode ter sido previamente cadastrado.`
    ///   - `O cpf deve ser digitado somente com números, sem ponto (.) e traco (-)`
    ///   - `O cpf deve ser válido.`
    /// * **Senha:**
    ///   - `O campo Senha é obrigatório.`
    ///   - `A senha deve ter no mínimo 8 caracteres.`
    ///   - `A Senha deve conter: letra maiúscula, minúscula, número e caracter especial.`
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>Usuario {Nome, E-mail, Nive de Acesso, Status, Biblioteca }</returns>
    /// <response code="201">Usuário cadastrado com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(CriarUsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando criação de usuario: " + request.Email, BaseLogType.LOG, request.Email);
        var command = new CriarUsuarioCommand(request.NomeCompleto, request.Email, request.Cpf, request.Password);
        
        var result = await _criarUsuarioCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario criado com sucesso: " + result.Value.Email, BaseLogType.LOG, result.Value.Email);
        return Created("Usuario criado com sucesso", result);
    }




    /// <summary>
    /// UC-04 - Visualizar os dados de um usuario
    /// </summary>
    /// <remarks>  
    /// 
    /// Visualiza os dados de um usuário no sistema pelo e-mail.
    /// 
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   Doadores consultam seu próprio perfil.
    ///   Gestores podem consultar qualquer perfil
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>UsuarioDTO {Nome, E-mail, CPF, Perfil, Status}</returns>
    /// <response code="201">Usuário obtido com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG,DOADOR")]
    [HttpGet]
    [ProducesResponseType(typeof(ObterUsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObterUsuario([FromQuery] ObterUsuarioRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando busca de usuario: " + request.Email, BaseLogType.LOG, request);

        var command = new ObterUsuarioQuery(request.Email);

        var result = await _obterUsuarioQueryHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario obtido com sucesso: " + result.Value?.Email, BaseLogType.LOG, result);
        return Ok(result.Value);
    }





    /// <summary>
    /// UC-05 - Remover usuario 
    /// </summary>
    /// <remarks>   
    /// 
    /// De acordo com LGPD o usuário pode solicitar a exclusão de seus dados pessoais.
    /// Os dados do usuário serão removidos fisicamente do banco de dados,
    /// Porém as doações realizadas por ele permanecerão no sistema, para efeito de auditoria,
    /// podendo ser consultadas em caso de auditoria pela Receita Federal ou para fins de transparência e prestação de contas da ONG.
    /// 
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   Apenas o próprio usuário pode solicitar a exclusão de seus dados
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="201">Usuário removido com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG,DOADOR")]
    [HttpDelete]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoverUsuario([FromQuery] RemoverUsuarioRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando remocao de usuario: " + request.Email, BaseLogType.LOG, request);

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        var command = new RemoverUsuarioCommand(request.Email, token);

        var result = await _removerUsuarioCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario removido com sucesso: " + request.Email, BaseLogType.LOG, result);
        return Ok("Usuario removido com sucesso: " + request.Email);
    }




    /// <summary>
    /// UC-07 - Suspender usuario
    /// </summary>
    /// <remarks>   
    /// 
    /// Atualiza status do usuário para SUSPENDED
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   Apenas o próprio usuário pode solicitar a exclusão de seus dados
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="201">Usuário suspenso com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG")]
    [HttpPut("suspender")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SuspenderUsuario([FromQuery] SuspenderUsuarioRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando suspensao de usuario: " + request.Email, BaseLogType.LOG, request);

        var command = new SuspenderUsuarioCommand(request.Email);

        var result = await _suspenderUsuarioCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario suspenso com sucesso: " + request.Email, BaseLogType.LOG, result);
        return Ok("Usuario suspenso com sucesso: " + request.Email);
    }




    /// <summary>
    /// UC-08 - Ativar usuario
    /// </summary>
    /// <remarks> 
    /// 
    /// Atualiza status do usuário para ACTIVE
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   Apenas o próprio usuário pode solicitar a exclusão de seus dados
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="201">Usuário ativado com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG")]
    [HttpPut("ativar")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AtivarUsuario([FromQuery] AtivarUsuarioRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando ativação de usuario: " + request.Email, BaseLogType.LOG, request);

        var command = new AtivarUsuarioCommand(request.Email);

        var result = await _ativarUsuarioCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario ativado com sucesso: " + request.Email, BaseLogType.LOG, result);
        return Ok("Usuario ativado com sucesso: " + request.Email);
    }




    /// <summary>
    /// UC-09 - Alterar perfil de usuario para GESTOR_ONG
    /// </summary>
    /// <remarks>   
    /// 
    /// Atualiza perfil para GESTOR_ONG, permitindo que o usuário tenha acesso a 
    /// funcionalidades administrativas, como ativar ou suspender outros usuários.
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   Apenas o próprio usuário pode solicitar a exclusão de seus dados
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="201">Perfil atualizado com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG")]
    [HttpPut("alterar-para-gestor")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AlterarPerfilParaGestor([FromQuery] AlterarPerfilParaGestorRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando alteração de perfil para gestor: " + request.Email, BaseLogType.LOG, request);

        var command = new AlterarPerfilParaGestorCommand(request.Email);

        var result = await _alterarPerfilParaGestorCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Perfil atualizado com sucesso: " + request.Email, BaseLogType.LOG, result);
        return Ok("Perfil atualizado com sucesso: " + request.Email);
    }




    /// <summary>
    /// UC-10 - Alterar perfil de usuario para DOADOR
    /// </summary>
    /// <remarks>   
    /// 
    /// Atualiza perfil para DOADOR, permitindo que o usuário tenha acesso a 
    /// funcionalidades específicas, como realizar doações.
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Email:**
    ///   - `O campo E-mail é obrigatório.`
    ///   - `Formato de e-mail deve ser válido.`
    ///   Apenas o próprio usuário pode solicitar a exclusão de seus dados
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="201">Perfil atualizado com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG")]
    [HttpPut("alterar-para-doador")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AlterarPerfilParaDoador([FromQuery] AlterarPerfilParaDoadorRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Iniciando alteração de perfil para doador: " + request.Email, BaseLogType.LOG, request);

        var command = new AlterarPerfilParaDoadorCommand(request.Email);

        var result = await _alterarPerfilParaDoadorCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Perfil atualizado com sucesso: " + request.Email, BaseLogType.LOG, result);
        return Ok("Perfil atualizado com sucesso: " + request.Email);
    }




    /// <summary>
    /// UC-11 - Alterar usuario
    /// </summary>
    /// <remarks>   
    /// 
    /// Atualiza o Nome Completo e Cpf para o Usuario
    /// 
    /// **Esse endpoint requer autenticacao**
    /// 
    /// **Regras de Validação:**
    /// 
    /// * **Nome:**
    ///   - `O campo NomeCompleto é obrigatório.`
    ///   - `O NomeCompleto deve ter no minimo 5 e no máximo 100 caracteres.`
    /// * **CPF:** 
    ///   - `O campo E-mail é obrigatório.`
    ///   - `O cpf não pode ter sido previamente cadastrado.`
    ///   - `O cpf deve ser digitado somente com números, sem ponto (.) e traco (-)`
    ///   - `O cpf deve ser válido.`
    /// 
    /// </remarks>
    /// <param name="request"></param>
    /// <returns>true</returns>
    /// <response code="201">Usuário modificado com sucesso.</response>
    /// <response code="400">Dados Inválidos</response>
    /// <response code="422">Entidade não processada</response>
    /// <response code="500">Erro interno do servidor</response>
    [Authorize(Roles = "GESTOR_ONG,DOADOR")]
    [HttpPut("alterar")]
    [ProducesResponseType(typeof(AlterarUsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AlterarUsuario([FromQuery] AlterarUsuarioRequest request, CancellationToken ct)
    {
        var emailLogado = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                         ?? User.FindFirst("email")?.Value;

        _logger.LogInformation("Iniciando alteracao de usuario: " + emailLogado, BaseLogType.LOG, request);

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        var command = new AlterarUsuarioCommand(request.NomeCompleto, request.Cpf);

        var result = await _alterarUsuarioCommandHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(result.Error, BaseLogType.LOG, result);
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Usuario alterado com sucesso: " + emailLogado, BaseLogType.LOG, result.Value);
        return Ok(result.Value);
    }
}
