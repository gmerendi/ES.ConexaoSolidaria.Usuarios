using Prometheus;
using Usuarios.Domain.Shared.Interfaces;

namespace Usuarios.Infrastructure.Services.Metrics;

/// <summary>
/// Implementação do serviço de métricas usando prometheus-net.
/// Registra contadores de negócio e histogramas de latência HTTP.
/// As métricas são expostas automaticamente em /metrics pelo middleware do Prometheus.
/// </summary>
public class MetricsService : IMetricsService
{
    // ── Contadores de negócio ─────────────────────────────────────────────────

    private static readonly Counter _loginCounter = Prometheus.Metrics.CreateCounter(
        "usuarios_login_total",
        "Número total de logins realizados com sucesso.");

    private static readonly Counter _logoutCounter = Prometheus.Metrics.CreateCounter(
        "usuarios_logout_total",
        "Número total de logouts realizados.");

    private static readonly Counter _usuarioCriadoCounter = Prometheus.Metrics.CreateCounter(
        "usuarios_criados_total",
        "Número total de novos usuários cadastrados.");

    private static readonly Counter _usuarioRemovidoCounter = Prometheus.Metrics.CreateCounter(
        "usuarios_removidos_total",
        "Número total de usuários removidos.");

    // ── Histograma de latência HTTP (alimenta p90/p95/p99) ────────────────────

    private static readonly Histogram _duracaoRequisicao = Prometheus.Metrics.CreateHistogram(
        "http_request_duration_seconds",
        "Duração das requisições HTTP em segundos.",
        new HistogramConfiguration
        {
            // Buckets calibrados para APIs REST típicas (ms até segundos)
            Buckets = new[] { 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1.0, 2.5, 5.0 },
            LabelNames = new[] { "method", "route", "status_code" }
        });

    // ── Implementação da interface ────────────────────────────────────────────

    public void IncrementarLogin() => _loginCounter.Inc();
    public void IncrementarLogout() => _logoutCounter.Inc();
    public void IncrementarUsuarioCriado() => _usuarioCriadoCounter.Inc();
    public void IncrementarUsuarioRemovido() => _usuarioRemovidoCounter.Inc();

    public void RegistrarDuracaoRequisicao(string metodo, string rota, int statusCode, double duracaoSegundos)
        => _duracaoRequisicao
               .WithLabels(metodo, rota, statusCode.ToString())
               .Observe(duracaoSegundos);
}
