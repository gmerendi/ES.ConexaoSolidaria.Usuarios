namespace CS.Domain.Events
{
    public record UserCreatedEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, string? correlationId);
    public record DoanationIntentEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, Guid guidCampanha, Guid guidDoacao, decimal valorDoacao, string? correlationId);
    public record DonationFinalizedEvent(Guid guidUsuario, string nomeCompleto, string email, string cpf, Guid guidCampanha, Guid guidDoacao, decimal valorDoacao, string? correlationId);
}
