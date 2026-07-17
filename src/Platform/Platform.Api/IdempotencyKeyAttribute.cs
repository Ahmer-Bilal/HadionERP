using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Platform.Api;

/// <summary>
/// Requires an <c>Idempotency-Key</c> header on POST/state-transition endpoints
/// (docs/architecture/05-data-and-api.md #2.2: "required on all POST/state-transition endpoints — critical
/// for financial documents where a retried network call must never double-post"). Apply to any endpoint
/// whose execution must not be repeated if the network retries it.
///
/// How it works: the first request with a given key executes the action and caches the response; a retried
/// request with the SAME key returns the cached response without re-executing. A request with a different
/// (or missing) key either executes fresh or is rejected.
///
/// For Phase 0 the cache is in-memory (same swap-for-real-store pattern as everything else). A real
/// deployment needs a persistent store (Redis/DB) so the cache survives restarts — documented as deferred.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class IdempotencyKeyAttribute : ActionFilterAttribute
{
    /// <summary>The request header name, per the common convention.</summary>
    public const string HeaderName = "Idempotency-Key";

    private static readonly Dictionary<string, CachedResponse> Cache = new();
    private static readonly object Lock = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key)
            || string.IsNullOrWhiteSpace(key))
        {
            context.Result = new BadRequestObjectResult(
                ApiErrorEnvelope.BadRequest(
                    $"An '{HeaderName}' header is required for this endpoint. " +
                    "Send a unique key (e.g. a GUID) per logical operation so a retried request does not re-execute."));
            return;
        }

        // If this key was already used, return the cached response instead of re-executing.
        lock (Lock)
        {
            if (Cache.TryGetValue(key.ToString(), out var cached))
            {
                context.Result = new ObjectResult(cached.Body) { StatusCode = cached.StatusCode };
                return;
            }
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // Only cache successful responses — a failed request (validation error, exception) should be
        // retryable with the same key, not permanently cached as the failure.
        if (context.Result is not ObjectResult { StatusCode: >= 200 and < 300 } result)
        {
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key))
        {
            return;
        }

        lock (Lock)
        {
            Cache[key.ToString()] = new CachedResponse(result.StatusCode ?? 200, result.Value);
        }
    }

    private sealed record CachedResponse(int StatusCode, object? Body);

    /// <summary>Clears the in-memory cache. Test-only — a real store has its own TTL/eviction.</summary>
    public static void ClearCache()
    {
        lock (Lock)
        {
            Cache.Clear();
        }
    }
}
