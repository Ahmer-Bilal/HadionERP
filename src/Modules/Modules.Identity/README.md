# Modules.Identity

Real user authentication and Users administration — closes `ARCHITECTURE-AUDIT.md` Part 1 §1, the
top-priority "Blocking" finding of this session's architecture audit: before this module, zero real
authentication existed anywhere in this application, and every action across every controller was
attributed to one of three hardcoded literals (`"system/ui"`, `"system/approver"`, `"system/startup"`).

## What's built (real Authentication & Identity)

- **Domain**: `User` (Username/Email/DisplayName/PasswordHash/IsActive) and a child `UserRole` collection
  (one row per assigned `Platform.Security.Role` key). Deliberately not a `Platform.Core.BusinessObject` —
  no Draft/Submit/Approve lifecycle, same reasoning as `Modules.MasterData.Domain.LookupType`: real user
  administration in SAP/Dynamics is immediate-effect, gated by a security role, not a workflow.
  `Username` is the exact same `actor: string` value every Application-layer service across every module
  already accepted before this module existed — no Application-layer service anywhere changed when this
  module was added, only what *produces* that string changed (a real authenticated identity instead of a
  controller constant).
- **`UserService`**: `CreateAsync` (hashes the password via `Microsoft.AspNetCore.Identity.PasswordHasher<User>`
  — the lean password-hashing package, not the full ASP.NET Core Identity framework's `UserManager`/
  `SignInManager`/external-login/2FA machinery this system doesn't need yet), `AuthenticateAsync` (verifies
  the hash, never distinguishes "unknown username" from "wrong password" in its return value — a login form
  can't be used to enumerate valid usernames), `AssignRoleAsync`/`RemoveRoleAsync`, `SetActiveAsync`,
  `ResetPasswordAsync`, `UpdateProfileAsync`.
- **Real Segregation of Duties enforcement — the first time this already-built engine has ever run in a
  live request path.** `Platform.Security.Sod.ISodEngine`/`SodConflictRule` and every module's registered
  conflict rules (e.g. `BusinessPartnerSecurity.MaintainerApproverConflict`) were real and unit-tested since
  Phase 1, but nothing ever called `ISodEngine.FindConflicts` — it's meant to run at role-*assignment* time,
  and there was no assignment action to guard until this module existed. `UserService.AssignRoleAsync` now
  resolves the user's proposed role set through the same `ISecurityCatalog.ResolveDutyKeys` every
  authorization check already uses, then calls `ISodEngine.FindUnresolvedConflicts`. A conflict throws
  `SodConflictException` (mapped to a 409 by `UsersController`, with the conflicting Duty pairs and reasons
  structured for the frontend) unless the caller supplies `AssignRoleRequest.OverrideReason`, in which case
  the conflict is logged as an explicit, permanent exception via the already-existing
  `ISodExceptionLog.Grant` — the same "risk acceptance" pattern real SAP GRC uses. Never silently allowed,
  never silently blocked. Live-verified: assigning `MasterData.BusinessPartner.Maintainer` then
  `MasterData.ApproveBusinessPartner` to the same user correctly 409s with the registered conflict's own
  reason text; supplying an override reason on retry correctly succeeds.
- **`IdentitySecurity`**: one `Identity.User.Administer` privilege/Duty/Role — no Maintainer/Approver split,
  same shape as `Modules.MasterData.Application.LookupSecurity` (immediate-effect administration, not a
  two-person approval).
- **JWT bearer tokens**, not cookies — the frontend (Vite dev server) and backend (Gateway.Api) are
  different origins in development, and a self-hosted deployment may or may not sit behind one reverse-proxy
  domain later; bearer tokens sidestep CORS-credential/SameSite complexity entirely.
  `Platform.Security.ITokenService` is the storage/module-agnostic port (same "kernel defines the port, a
  module implements it" pattern as `IActorRoleAssignmentStore`/`Platform.Core.NumberRanges.INumberRangeService`);
  `Modules.Identity.Infrastructure.JwtTokenService` is the real implementation. Tokens are short-lived
  (12 hours) — no refresh-token rotation in this pass, an accepted, disclosed simplification; a token holder
  simply re-authenticates after it expires.
- **`EfActorRoleAssignmentStore : Platform.Security.IActorRoleAssignmentStore`** replaces
  `InMemoryActorRoleAssignmentStore`'s hardcoded dictionary — resolves a real username's assigned roles from
  the database. A deactivated or unknown username resolves to zero roles ("denied by default"), the same
  convention the in-memory reference implementation it replaces already established. Registered `Scoped`
  (it depends on a scoped `DbContext`), where the store it replaces was `Singleton` — confirmed safe: no
  other singleton service in `Program.cs` captured `IActorRoleAssignmentStore` as a constructor dependency.
- **Bootstrap seeding** (`IdentitySeeder`, mirrors `Modules.MasterData.Infrastructure.LookupSeeder`'s
  idempotent pattern): if the `users` table is completely empty on startup, creates one administrator
  (`admin`, password from `Identity:BootstrapAdminPassword` — .NET User Secrets in Development, never a
  committed value, same handling as `ConnectionStrings:Default`) granted every role key any module currently
  registers — so the system is immediately fully usable after a fresh deploy with no separate manual
  bootstrap step. Never re-runs once any user exists, so it never overrides an operator's own later changes
  to that account.
- **Global default-deny**: `Gateway.Api/Program.cs` registers a global `AuthorizeFilter` — every controller
  action across every module now requires a valid bearer token by default, not opt-in per controller. Only
  `AuthController.Login` is `[AllowAnonymous]` (it's what produces a token in the first place).
  `PlatformApiController.CurrentActor` (in `Platform.Api`, not this module) is the one shared place every
  one of the 14 pre-existing controllers now reads the real logged-in username from — replacing what used to
  be `MaintainerActor`/`ApproverActor`/`AdministratorActor` hardcoded constants in each controller file.
- **Api**: `AuthController` (`POST api/v1/identity/auth/login`, `GET api/v1/identity/auth/me`),
  `UsersController` (`api/v1/identity/users` — CRUD, activate/deactivate, reset-password, role assignment
  with the SoD-conflict 409 described above).
- **Frontend**: `authApi.ts` (login/me, token persisted in `localStorage` — a disclosed simplification, not
  httpOnly-cookie-grade XSS hardening), `AuthContext`/`useAuth()` wrapping the whole app (`main.tsx`),
  `LoginPage.tsx`, and every one of the 15 existing `api/*.ts` files now attaches
  `Authorization: Bearer <token>` via a shared `authHeaders()` helper instead of a bare `fetch`.
  `Platform.UI.ShellBar` gained optional `currentUserLabel`/`onLogout`/`logoutLabel` props (only rendered
  when provided, same optional-prop pattern as `tagline` — keeps `ShellBar` usable by any consumer with no
  concept of a logged-in user). New `UsersPage.tsx` (list/create/deactivate/reset-password, a Roles FastTab
  that surfaces an SoD conflict inline with a required override-reason field before retrying) under a new
  "Users" nav area alongside "Lookup Data" in Platform Administration.
- Verified end-to-end: 13 new unit tests (`UserServiceTests` — password hash/verify round-trip, duplicate-
  username rejection, authorization denial, deactivation blocking authentication, SoD conflict blocking a
  role assignment, override-reason granting an exception and letting the assignment through, role removal,
  password reset) + 4 new integration tests against real PostgreSQL (user+roles round-trip, username
  uniqueness enforced at the DB level, deactivation persists, cascade delete removes all roles). 22 test
  projects pass solution-wide, zero regressions — confirmed by design: every pre-existing Application-service
  test builds its own fake `IActorRoleAssignmentStore` directly, bypassing HTTP/controllers/JWT entirely.
  Live `curl` exercise: unauthenticated request to a protected endpoint correctly 401s; bootstrap admin login
  returns a real JWT carrying every registered role as claims; the same token against a protected endpoint
  returns 200; creating a Business Partner with the real token attributes it to `"admin"`, not `"system/ui"`
  (`createdBy` in the response); the full SoD block → override → succeed sequence described above. Live
  Playwright pass (screenshots, zero console errors) in both English and Arabic: login page, authenticated
  shell showing "Logged in as System Administrator · Logout" (RTL-correct in Arabic), an existing page
  (Business Partners) still working end-to-end with a real session, the Users admin page (list, create,
  detail FastTabs), logout correctly returning to the login page.

## Deferred (disclosed, not hidden)

- Real OIDC/SSO federation (Entra ID/Auth0/Keycloak) — `Platform.Security.IActorRoleAssignmentStore`'s
  interface already supports swapping this in later without touching any Application-layer code, per its
  own doc comment; this pass is username/password only, self-hosted.
- MFA.
- Password-reset-by-email — no notification system exists yet anywhere in this application
  (`ARCHITECTURE-AUDIT.md` Part 1 §7); an administrator resets another user's password directly through the
  Users admin UI as the interim mechanism.
- Refresh-token rotation/revocation list — the accepted short-lived-token simplification described above.
- Delegation UI (`ARCHITECTURE-AUDIT.md` Part 1 §6) — that finding noted this "closes with #1," meaning it
  *becomes buildable* now that real users exist, not that this module builds it. Separate future work.
- Row-level/multi-company scoping, Field-level security (`ARCHITECTURE-AUDIT.md` Part 1 §2/§4) — separate,
  later phases; real users existing doesn't retroactively give this application a second company to scope
  against.
- No role-picker dropdown in the Users admin UI — `AssignRoleRequest.RoleKey` is a plain text field an
  administrator types the exact role key into (e.g. `"MasterData.BusinessPartner.Maintainer"`), not a
  dropdown reading from `ISecurityCatalog`. A real convenience gap, not a design gap — revisit once a
  `GET`-all-registered-roles endpoint is worth building.
- `EfActorRoleAssignmentStore.ResolveRoleKeys` is a synchronous EF Core query (the interface it implements
  is synchronous by design, called inline from inside every Application service's existing
  `BuildPrincipal(actor)` helper across every module) — one blocking DB round trip per authorization check.
  A small, disclosed inefficiency, not a correctness issue; revisit only if `IActorRoleAssignmentStore` ever
  grows an async twin solution-wide.
- `ISodExceptionLog` itself stays the pre-existing in-memory singleton — exceptions granted through the SoD
  override flow above don't survive an application restart. The point of this module is that the *check*
  finally runs in a live request path for the first time; persisting the exception log itself is separate,
  smaller follow-up work.
