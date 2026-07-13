# Platform.Configuration

The multi-level override hierarchy (System → Tenant → Company → Branch → User), a business rule engine
(decision-table style), feature flags, and versioned configuration packages for environment promotion.
See docs/architecture/04-data-and-api.md #3.

## What's built
- `ConfigurationLevel`/`ConfigurationContext`/`ConfigurationKeyDefinition`: a key declares which levels it
  may be overridden at (e.g. a numbering format at Company; a UI density preference at User only).
- `IConfigurationCatalog`/`InMemoryConfigurationCatalog`: the registered set of configurable items —
  same "registered contract, not ad-hoc" pattern as Security's role catalog, Workflow's definition
  catalog, and Events' event catalog.
- `IConfigurationStore`/`InMemoryConfigurationStore`: raw (key, level, scopeId) → value storage.
- `IConfigurationResolver`/`ConfigurationResolver`: the single entry point — walks User → Branch →
  Company → Tenant → System, returning the most specific override present, falling back to the key's
  registered default. Rejects `SetValue` calls at a level the key doesn't allow, or missing the
  corresponding id in context.
- `FeatureFlags/IFeatureFlagService`: a feature flag IS a configuration value (boolean-typed), resolved
  through the exact same hierarchy — not a second parallel system.
- `Rules/BusinessRule` + `IBusinessRuleEngine`: decision-table evaluation (docs #3.3) — several rules can
  share a key, each with its own condition (reusing `Platform.Core.AttributeConstraints`, the same
  Max/Min-threshold-or-exact-match logic Security's ABAC grants and Workflow's step conditions use), the
  highest-priority matching one wins. Built for things like tax determination or posting-account
  determination — declarative data, not compiled-in `if` statements.
- `Packages/ConfigurationPackage` + `IConfigurationPackageService`: export the current store to a
  versioned package, diff a package against the current store (the review step before promoting to
  Prod), and import — which validates every value against *this* environment's catalog before applying,
  so importing a package built against a different set of key definitions fails loudly rather than
  silently applying stale configuration.

Wired into Gateway.Api with two real, permanent settings: `Platform.DefaultLanguage` (System/Tenant) and
`Features.VerboseSystemStatus` (System) — the latter genuinely gates whether `/api/v1/system/status`
includes the events/audit breakdown, proving the feature-flag mechanism live, not just in tests.

## Deferred
- A real database-backed store/catalog (same in-memory-now, swap-later pattern as the rest of the kernel).
- An admin UI for maintaining configuration/rules/packages — needs `Platform.UI`'s fuller record-form
  tooling and a real deployment to administer.
- No business-module configuration keys or rules exist yet — no module (e.g. Procurement) exists yet to
  register its own approval thresholds or tax determination rules; those register into these same
  catalogs when built.
