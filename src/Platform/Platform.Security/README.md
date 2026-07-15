# Platform.Security

Implements the authorization side of docs/architecture/03-platform-services.md #2: Roles → Duties →
Privileges (RBAC), attribute-constrained grants such as "approve up to 50,000 SAR" (ABAC), Segregation of
Duties conflict detection with logged exceptions, row-level scoping (company/branch/project), and
field-level masking (e.g. salary, IBAN).

**Deferred to later, once a real deployment target exists** (this is authorization — deciding what an
already-identified user can do — not authentication, which needs an actual identity provider and hosting
environment to mean anything):
- SSO/OIDC login and MFA — needs a real identity provider (Duende IdentityServer / Azure AD B2C, per ADR-8).
- Postgres Row Level Security policies — the database-layer backstop to `RowLevelSecurityService`; needs an
  actual database.
- Encryption at rest/in transit, secrets management — needs an actual deployment target.

All in-memory reference implementations here (`InMemorySecurityCatalog`, `InMemorySodExceptionLog`) are
swappable for database-backed ones later without any module code changing, the same pattern used in
`Platform.Core`'s `InMemoryNumberRangeService`.

**`FieldLevel/`/`RowLevel/` are built but not yet consumed anywhere** (found by `ARCHITECTURE-AUDIT.md`'s
2026-07-15 audit §4 — noted here so the intro paragraph above doesn't read as more complete than it is): the
types exist and are real, but no module calls into them today — no record is currently scoped by company/
branch/project (there's only ever one implicit company so far, see `ARCHITECTURE-AUDIT.md` §2), and no
field is masked (nothing salary/IBAN-sensitive is built yet — that's Phase 4's HR/Payroll). This is expected
sequencing, not a defect: both become real the moment a module has data that actually needs either.

## First real consumer: Modules.MasterData's Business Partner

`BusinessPartnerSecurity` (in `Modules.MasterData.Application`) is the first real Roles/Duties/Privileges
registration and the first real SoD conflict rule — a deliberately split Maintainer/Approver Duty pair,
the exact "Create Vendor vs. Approve Vendor Payment" example from docs/architecture/03-platform-services.md
#2.2. `BusinessPartnerService` calls `IAuthorizationService.Authorize(...)` before every action and throws
`UnauthorizedAccessException` on denial. See `Modules.MasterData/README.md` for the full story, including
what SoD enforcement is still missing (a role-*assignment* admin surface to check the rule against).

### `IActorRoleAssignmentStore` — a temporary stand-in for real SSO

Added alongside Business Partner's wiring: resolves a bare actor-id string (what every module currently
passes around as "actor") to the Role keys assigned to them, so `SecurityPrincipal` objects built from that
actor have real data to check against instead of an unconditional grant. `InMemoryActorRoleAssignmentStore`
is a fixed lookup table seeded at startup — an actor with no entry resolves to zero Roles (denied by
default, not granted by default). A real deployment replaces this entirely with role assignment resolved
from the authenticated identity, behind this same interface — no calling code changes.
