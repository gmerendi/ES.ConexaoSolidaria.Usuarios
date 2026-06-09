namespace Usuarios.Domain.Shared.Interfaces;

public interface IMetricsService
{
    // ── Contadores de negócio ─────────────────────────────────────────────────
    void IncrementarLogin();
    void IncrementarLogout();
    void IncrementarUsuarioCriado();
    void IncrementarUsuarioRemovido();

    // ── Latência de requisições HTTP (para p90/p95/p99) ───────────────────────
    void RegistrarDuracaoRequisicao(string metodo, string rota, int statusCode, double duracaoSegundos);
}
