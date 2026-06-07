using Usuarios.Domain.Shared.Helpers;
using Usuarios.Domain.Shared.Primitives;

namespace Usuarios.Domain.Entities.Usuarios;

public sealed class Password
{
    public string Hash { get; }

    public Password(string hash) => Hash = hash;




    // -----------------------------------------------------------------------------
    // Metodos
    // -----------------------------------------------------------------------------
    public static Result<Password> CreatePasswordHash(string password)
    {
        AssertionConcern.AssertArgumentNotEmpty(password, "400_PASSWORD_REQUIRED");

        var errors = new List<string>();

        if (password.Length < 8)
            errors.Add("422_PASSWORD_TOO_SHORT");

        if (!password.Any(char.IsUpper))
            errors.Add("422_PASSWORD_REQUIRES_UPPERCASE");

        if (!password.Any(char.IsLower))
            errors.Add("422_PASSWORD_REQUIRES_LOWERCASE");

        if (!password.Any(char.IsDigit))
            errors.Add("422_PASSWORD_REQUIRES_DIGIT");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("422_PASSWORD_REQUIRES_SPECIAL_CHAR");

        if (errors.Count > 0)
            return Result<Password>.Failure(string.Join(" ", errors));

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        return Result<Password>.Success(new Password(hash));
    }

    public bool VerifyPasswordHash(string plainText) => BCrypt.Net.BCrypt.Verify(plainText, Hash);
}
