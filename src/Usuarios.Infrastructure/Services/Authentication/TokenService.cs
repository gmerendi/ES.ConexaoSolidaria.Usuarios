using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Enums;
using Usuarios.Domain.Shared.Interfaces;
using Usuarios.Domain.Shared.Primitives;
using Usuarios.Infrastructure.Services.Cache;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Usuarios.Infrastructure.Services.Security;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime DataExpiracao) GetToken(Usuario usuarioReal)
    {
        Usuario usuario = usuarioReal;

        // Recupera as configurações do JWT
        var chaveSecreta = _configuration["Jwt:SecretKey"] ?? "";
        var issuer = _configuration["Jwt:Issuer"] ?? "";
        var audience = _configuration["Jwt:Audience"] ?? "";
        var horasExpiracao = double.Parse(_configuration["Jwt:ExpirationHours"] ?? "2");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(chaveSecreta);

        var dataExpiracao = DateTime.UtcNow.AddHours(horasExpiracao);

        // Define as Claims 
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, usuario.Guid.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email.Endereco),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
                new Claim(ClaimTypes.NameIdentifier, Cpf.Anonymize(usuario.Cpf.Numero)),
            }),

            Expires = dataExpiracao,
            Issuer = issuer,
            Audience = audience,
            // Configura a assinatura digital do Token usando o algoritmo HMAC SHA256
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        // Cria e criptografa o Token de fato
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, dataExpiracao);
    }

    public TimeSpan GetTokenTimeToExpire(string token)
    {

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var dataExpiracaoToken = jwtToken.ValidTo; // Em UTC
        var tempoRestante = dataExpiracaoToken - DateTime.UtcNow;

        return tempoRestante > TimeSpan.Zero ? tempoRestante : TimeSpan.Zero;
    }
}