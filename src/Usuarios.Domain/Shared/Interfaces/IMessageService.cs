namespace Usuarios.Domain.Shared.Interfaces
{
    public interface IMessageService
    {
        Task SendUserCreatedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct);
    }
}
