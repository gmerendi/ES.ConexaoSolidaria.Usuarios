using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Usuarios.Domain.Entities.Usuarios;
using Usuarios.Domain.Shared.Interfaces;

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
        var horasExpiracao = double.Parse(_configuration["Jwt:ExpirationHours"] ?? "2");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(chaveSecreta);

        var dataExpiracao = DateTime.UtcNow.AddHours(horasExpiracao);

        // Define as Claims 
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Guid.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email.Endereco),
                new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
            }),

            Expires = dataExpiracao,
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
}