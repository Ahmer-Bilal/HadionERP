# Platform.Api

Shared API conventions: a base controller, OData-style query parsing, a paged-result envelope, idempotency-key
handling, and a unified error envelope. See docs/architecture/04-data-and-api.md #2.

Every module's API controller inherits `PlatformApiController` and gets these conventions for free — the same
route-prefix pattern, error shape, and paging behavior across every endpoint. This is NOT a heavy framework;
it's base classes + result types that make the conventions uniform and hard to get wrong.

## What's built
- `PlatformApiController`: the base controller. Route convention `api/v1/[controller]` (URL-segment major
  version, §2.1). Provides `Paged()`, `ValidationError()`, `BadRequestError()`, `ConflictError()`,
  `ForbiddenError()` helpers — `ForbiddenError` added alongside Modules.MasterData's Business Partner
  wiring `Platform.Security` for the first time: a module's Application layer throws
  `UnauthorizedAccessException` on an authorization denial, and its controller catches that and returns
  `ForbiddenError(ex.Message)`.
- `PagedResult<T>`: the standard envelope for list endpoints — `Items`, `TotalCount`, `Skip`, `Top`. A
  consistent shape so the frontend and integrators always know how to page through a list.
- `ODataQuery`: parses `$top`, `$skip`, `$orderby`, `$filter`, `$select`, `$count` from the query string
  into a typed object, with validation (non-negative, max-page-size clamp). The standard input every List
  Report endpoint receives.
- `ApiErrorEnvelope`: the unified error response (RFC 7807 problem-details-inspired) — `Type`, `Title`,
  `Status`, `Detail`, `Errors` (field-level validation). Every error uses this shape.
- `IdempotencyKeyAttribute`: an action filter requiring an `Idempotency-Key` header on POST/state-transition
  endpoints (§2.2). Caches the response per key so a retried request returns the cached result instead of
  re-executing — critical for financial documents where a retried network call must never double-post.

Gateway.Api's SystemController inherits `PlatformApiController`, proving the base works on real running code.

## Deferred
- **`$filter` expression engine** — parsing `$filter=Amount gt 1000` into a real predicate/expression tree
  needs a proper grammar (OData ABNF / ANTLR). The query structure + validation is built now so the contract
  is stable; the engine lands when a real list endpoint exists to drive it.
- **Real idempotency store** — in-memory cache proves the mechanism; a real deployment needs Redis/DB so the
  cache survives restarts (and works across multiple Gateway instances).
- **OpenAPI 3.1 contract-first spec generation** — Swashbuckle/Swagger is wired in Gateway.Api (dev-only);
  full per-module contract publishing is deferred to when modules exist.
- **`$batch` / bulk operations** (§2.2) — needs real bulk operations to drive it.
- **Webhooks/event subscriptions** (§2.3) — needs Platform.Integration.
- **Header-based minor version negotiation** (§2.1) — the major-version URL segment is established; minor
  negotiation is deferred.
