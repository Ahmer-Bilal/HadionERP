using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Platform.Api.Tests;

/// <summary>
/// Proves <see cref="IdempotencyKeyAttribute"/> enforces the idempotency contract (§2.2): a missing header
/// is rejected, and a retried request with the same key returns the cached response instead of re-executing.
/// This is the guarantee that a retried network call never double-posts a financial document.
/// </summary>
public class IdempotencyKeyAttributeTests
{
    private static ActionExecutingContext MakeContext(string? idempotencyKey)
    {
        var httpContext = new DefaultHttpContext();
        if (idempotencyKey is not null)
        {
            httpContext.Request.Headers[IdempotencyKeyAttribute.HeaderName] = idempotencyKey;
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private static ActionExecutedContext MakeExecutedContext(ActionExecutingContext executing, IActionResult result)
    {
        // ActionExecutingContext derives from ActionContext, so it can be passed directly.
        return new ActionExecutedContext(executing, new List<IFilterMetadata>(), executing.Controller)
        {
            Result = result,
        };
    }

    [Fact]
    public void Missing_header_returns_a_400_error_envelope()
    {
        IdempotencyKeyAttribute.ClearCache();
        var filter = new IdempotencyKeyAttribute();
        var context = MakeContext(idempotencyKey: null);

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var envelope = Assert.IsType<ApiErrorEnvelope>(result.Value);
        Assert.Equal(400, envelope.Status);
        Assert.Contains("Idempotency-Key", envelope.Detail!);
    }

    [Fact]
    public void Empty_header_is_rejected()
    {
        IdempotencyKeyAttribute.ClearCache();
        var filter = new IdempotencyKeyAttribute();
        var context = MakeContext(idempotencyKey: "   ");

        filter.OnActionExecuting(context);

        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    [Fact]
    public void First_request_with_a_key_proceeds_to_the_action()
    {
        IdempotencyKeyAttribute.ClearCache();
        var filter = new IdempotencyKeyAttribute();
        var context = MakeContext(idempotencyKey: "key-001");

        filter.OnActionExecuting(context);

        // No Result set means the action proceeds normally.
        Assert.Null(context.Result);
    }

    [Fact]
    public void Retried_request_with_same_key_returns_cached_response_without_re_executing()
    {
        IdempotencyKeyAttribute.ClearCache();
        var filter = new IdempotencyKeyAttribute();
        var key = "key-retry";

        // First request: executes the action and returns 200 OK with a body.
        var firstContext = MakeContext(key);
        filter.OnActionExecuting(firstContext);
        Assert.Null(firstContext.Result); // proceeds

        var firstResult = new OkObjectResult(new { docNumber = "PROC-PO-2026-000123" });
        filter.OnActionExecuted(MakeExecutedContext(firstContext, firstResult));

        // Second request with the SAME key: must return the cached response, not re-execute.
        var secondContext = MakeContext(key);
        filter.OnActionExecuting(secondContext);

        var cachedResult = Assert.IsType<ObjectResult>(secondContext.Result);
        Assert.Equal(200, cachedResult.StatusCode);

        // The cached body is the same object from the first execution.
        dynamic body = cachedResult.Value!;
        Assert.Equal("PROC-PO-2026-000123", (string)body.docNumber);
    }

    [Fact]
    public void Different_key_executes_fresh()
    {
        IdempotencyKeyAttribute.ClearCache();
        var filter = new IdempotencyKeyAttribute();

        var firstContext = MakeContext("key-A");
        filter.OnActionExecuting(firstContext);
        filter.OnActionExecuted(MakeExecutedContext(firstContext, new OkObjectResult("first")));

        // A different key must not hit the cache — the action proceeds.
        var secondContext = MakeContext("key-B");
        filter.OnActionExecuting(secondContext);

        Assert.Null(secondContext.Result);
    }

    [Fact]
    public void Failed_response_is_not_cached_so_it_can_be_retried()
    {
        IdempotencyKeyAttribute.ClearCache();
        var filter = new IdempotencyKeyAttribute();
        var key = "key-fail";

        // First request fails with a 400 — this should NOT be cached (a failure should be retryable).
        var firstContext = MakeContext(key);
        filter.OnActionExecuting(firstContext);
        filter.OnActionExecuted(MakeExecutedContext(firstContext, new BadRequestObjectResult("validation error")));

        // Same key again: the action should proceed (not return the cached failure).
        var secondContext = MakeContext(key);
        filter.OnActionExecuting(secondContext);

        Assert.Null(secondContext.Result);
    }
}
