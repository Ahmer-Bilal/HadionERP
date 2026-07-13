# Platform.Events

Cross-module messaging: a versioned event catalog, the outbox pattern (stage-then-relay, so an event is
never lost even if the bus is briefly down), and an event bus abstraction. See
docs/architecture/03-platform-services.md #3.

Domain events (`Platform.Core.Events.IDomainEvent`, raised by `BusinessObject`/`WorkflowInstance`) stay
strictly in-process — nothing here automatically converts one into an integration event. A module's
Application layer deliberately decides which domain events also matter to other modules and translates
those explicitly via `IIntegrationEventPublisher`; that is a design choice per event, not a generic bridge.

## What's built
- `IntegrationEvent`: the versioned contract envelope (`"{Module}.{Event}.v{N}"`, e.g.
  `"Procurement.PurchaseOrderApproved.v1"`), JSON payload.
- `IEventCatalog`/`InMemoryEventCatalog`: the registered set of contracts — publishing an unregistered
  event type is rejected, so nothing undocumented/unversioned ever gets published.
- `Outbox/IOutboxStore`/`InMemoryOutboxStore` + `OutboxRelay`: stage an event, then relay pending ones to
  the bus, marking each published on success (retried on the next relay run if it fails).
- `IIntegrationEventPublisher`: the single entry point modules call — validates against the catalog, then
  stages into the outbox. Modules never call `IEventBus` directly to publish.
- `IEventBus`/`InMemoryEventBus`: in-process pub/sub, proven end-to-end in
  `tests/UnitTests/Platform.Events.Tests/EndToEndPipelineTests.cs`.

Gateway.Api publishes one real, permanent event today — `Platform.System.ApplicationStarted.v1` — at
boot, and a subscriber logs its receipt, proving enqueue → outbox → relay → bus → subscriber works in the
actual running application, not just in tests.

## Deferred
- **RabbitMQ** (ADR-5): `InMemoryEventBus` proves the contract; swapping in a real broker is an
  infrastructure change behind the same `IEventBus` interface, no module code changes.
- **The scheduler** that calls `OutboxRelay.RelayPendingAsync()` periodically — needs a hosted background
  service in Gateway.Api (same deferral as Platform.Workflow's escalation scan). Today it's called once at
  startup, which is enough to prove the mechanism.
- **A real outbox table** — `InMemoryOutboxStore` proves enqueue/relay/mark-published; a real deployment
  needs the outbox write to happen in the same database transaction as the business change, which needs
  an actual database (not wired up yet).
- **No business-module events are registered yet** — no module (e.g. Procurement) exists yet to own a
  real "PurchaseOrderApproved" event; a module registers its own events into this same catalog when built.
