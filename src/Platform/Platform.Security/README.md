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
