namespace Usuarios.Application.Interfaces
{
    public interface IBaseLogger<T>
    {
        void LogInformation(string message, object? data, string? correlationId = null);
        void LogError(string message, object? data, string? correlationId = null);
        void LogWarning(string message, object? data, string? correlationId = null);
    }
}
