namespace Gateway.Api.Events;

/// <summary>Gateway.Api's own registered event contracts — real, permanent operational events (not a
/// business-module event, since no module exists yet to own one). A business module registers its own
/// events into the same IEventCatalog when it's built.</summary>
public static class GatewayApiEventTypes
{
    public const string ApplicationStarted = "Platform.System.ApplicationStarted.v1";
}

public sealed record ApplicationStartedPayload(string Application, DateTimeOffset StartedAtUtc);
