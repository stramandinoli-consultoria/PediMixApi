using Polly;
using Polly.Extensions.Http;

namespace PediMix.Infrastructure.Policies;

/// <summary>
/// Políticas de resiliência para chamadas HTTP a APIs musicais externas.
/// Usadas via <c>AddPolicyHandler</c> no DI (Program.cs).
/// </summary>
public static class HttpPolicies
{
    /// <summary>
    /// Retry com exponential backoff: 2s, 4s, 8s.
    /// Aplicado em todas as chamadas a APIs externas (Spotify, YouTube, Lyrics).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx + 408
            .OrResult(msg => (int)msg.StatusCode == 429) // rate limit
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timeSpan, attempt, _) =>
                {
                    var reason = outcome.Exception?.Message
                                 ?? outcome.Result?.StatusCode.ToString()
                                 ?? "unknown";
                    Console.WriteLine($"[Polly][Retry] Tentativa {attempt} em {timeSpan.TotalSeconds}s — motivo: {reason}");
                });

    /// <summary>
    /// Circuit Breaker: abre após 5 falhas consecutivas por 30s.
    /// Evita banimento por flood em caso de indisponibilidade da API.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"[Polly][CircuitBreaker] ABERTO por {timespan.TotalSeconds}s. " +
                                      $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                },
                onReset: () => Console.WriteLine("[Polly][CircuitBreaker] FECHADO — chamadas voltaram a passar."),
                onHalfOpen: () => Console.WriteLine("[Polly][CircuitBreaker] HALF-OPEN — testando próxima chamada."));

    /// <summary>Timeout máximo por requisição.</summary>
    public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy(int seconds = 10)
        => Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(seconds));
}
