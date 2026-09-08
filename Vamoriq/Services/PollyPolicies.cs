using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Net;

namespace Vamoriq.Services;

public static class PollyPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider sp)
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("HttpClientPolicy.OpenAI");
        var retryCount = TryInt(configuration["OpenAI:RetryCount"]) ?? 2;
        var delays = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(250), retryCount);

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(delays, (outcome, delay, attempt, context) =>
            {
                if (outcome.Exception != null)
                {
                    logger.LogWarning(outcome.Exception, "OpenAI retry: attempt={Attempt}, delayMs={Delay}", attempt, (int)delay.TotalMilliseconds);
                }
                else
                {
                    logger.LogWarning("OpenAI retry: attempt={Attempt}, status={Status}, delayMs={Delay}", attempt, (int)outcome.Result.StatusCode, (int)delay.TotalMilliseconds);
                }
            });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(IServiceProvider sp)
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var timeoutSeconds = TryInt(configuration["OpenAI:TimeoutSeconds"]) ?? 60;
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy(IServiceProvider sp)
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("HttpClientPolicy.OpenAI");

        var retry = GetRetryPolicy(sp);
        var timeout = GetTimeoutPolicy(sp);

        var failures = TryInt(configuration["OpenAI:CircuitBreakConsecutiveFailures"]) ?? 5;
        var duration = TimeSpan.FromSeconds(TryInt(configuration["OpenAI:CircuitBreakDurationSeconds"]) ?? 30);

        var breaker = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: failures,
                durationOfBreak: duration,
                onBreak: (outcome, ts) =>
                {
                    if (outcome.Exception != null)
                        logger.LogError(outcome.Exception, "OpenAI circuit OPEN for {Sec}s", ts.TotalSeconds);
                    else
                        logger.LogError("OpenAI circuit OPEN for {Sec}s, status={Status}", ts.TotalSeconds, (int)outcome.Result.StatusCode);
                },
                onReset: () => logger.LogInformation("OpenAI circuit RESET"),
                onHalfOpen: () => logger.LogWarning("OpenAI circuit HALF-OPEN")
            );

        return Policy.WrapAsync(breaker, retry, timeout);
    }

    private static int? TryInt(string? s) => int.TryParse(s, out var v) ? v : null;
}
