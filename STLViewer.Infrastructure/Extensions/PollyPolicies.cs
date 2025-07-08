using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Net.Http;

namespace STLViewer.Infrastructure.Extensions;

/// <summary>
/// Provides pre-configured Polly resilience policies for HTTP clients.
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Creates a retry policy with exponential backoff.
    /// </summary>
    /// <returns>An async retry policy for HTTP responses.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(System.Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("Retry {RetryCount} for {OperationKey} in {Timespan}ms",
                        retryCount, context.OperationKey, timespan.TotalMilliseconds);
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy.
    /// </summary>
    /// <returns>An async circuit breaker policy for HTTP responses.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (response, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {timespan}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker closed");
                });
    }

    /// <summary>
    /// Creates a timeout policy.
    /// </summary>
    /// <returns>An async timeout policy.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Creates a bulkhead isolation policy.
    /// </summary>
    /// <returns>An async bulkhead policy for HTTP responses.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateBulkheadPolicy()
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: 10,
            maxQueuingActions: 20);
    }

    /// <summary>
    /// Creates a combined policy with all resilience patterns.
    /// </summary>
    /// <returns>A combined async policy for HTTP responses.</returns>
    public static IAsyncPolicy<HttpResponseMessage> CreateCombinedPolicy()
    {
        var retryPolicy = CreateRetryPolicy();
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        var timeoutPolicy = CreateTimeoutPolicy();
        var bulkheadPolicy = CreateBulkheadPolicy();

        // Wrap policies in the correct order: Bulkhead -> Circuit Breaker -> Retry -> Timeout
        return Policy.WrapAsync(bulkheadPolicy, circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }

    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("logger", out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}
