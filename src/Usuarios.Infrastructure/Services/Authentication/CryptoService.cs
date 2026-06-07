using BCrypt.Net;
using Usuarios.Domain.Shared.Interfaces;

namespace Usuarios.Infrastructure.Services.Security;

public class CryptoService : ICryptoService
{
    public bool VerifyPassword(string password, string dbHashPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(dbHashPassword))
            return false;

        // O BCrypt descriptografa o salt de dentro do próprio hash e faz a checagem com segurança
        return BCrypt.Net.BCrypt.Verify(password, dbHashPassword);
    }
}