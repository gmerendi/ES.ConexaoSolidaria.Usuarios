namespace Usuarios.Domain.Shared.Interfaces
{
    public interface IMessageService
    {
        Task SendUserCreatedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct);
        Task SendUserRemovedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct);
        Task SendUserSuspendedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct);
        Task SendUserActivatedEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct);
        Task SendUserResetPasswordEventMessage(Guid guidUser, string nome, string email, string cpf, CancellationToken ct);
    }
}
