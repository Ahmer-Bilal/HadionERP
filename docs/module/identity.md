# Identity

Identity owns real user authentication and administration — closing what was, before this module existed,
the single biggest gap in the whole system: every action across every controller was attributed to one of
three hardcoded literals rather than a real logged-in person, which made every audit trail and every
approval decision only as trustworthy as those literals. Nothing about how any other module's services
work changed when this module was added — every service already accepted a plain `actor: string`, exactly
matching a username. What changed is what *produces* that string: a real authenticated identity instead of
a controller constant.

## How a login becomes real authorization elsewhere

A user's password is hashed with ASP.NET Core's lean password-hasher rather than the full Identity
framework's user/sign-in-manager machinery, since this system doesn't need external logins or built-in
two-factor yet — deliberately choosing the smaller tool for the actual job rather than pulling in a bigger
framework this system would leave mostly unused. Authentication never distinguishes "unknown username" from
"wrong password" in what it returns, so a login form can't be used to enumerate which usernames exist.
Successful login issues a short-lived JWT bearer token rather than a cookie, since the frontend and backend
run on different origins in development and may or may not sit behind one shared domain later — bearer
tokens sidestep that uncertainty entirely, at the cost of no refresh-token rotation yet, an accepted and
disclosed simplification for now: a token holder simply logs in again once it expires.

The part worth understanding by name is Segregation of Duties enforcement, because this module is what
finally gave that already-built engine something real to check. Every module has been registering SoD
conflict rules since Phase 1 — the classic "the same person shouldn't both create and approve a vendor"
rule — but nothing ever called the engine in a live request, because there was no real role-*assignment*
action to guard until users existed. Assigning a role now runs that check first: a genuine conflict is
rejected outright unless the caller explicitly supplies an override reason, in which case the conflict is
permanently logged as a deliberate, accepted risk rather than silently allowed or silently blocked — the
same "risk acceptance" pattern real SAP GRC systems use.

## What's still ahead

Real OIDC/SSO federation, multi-factor authentication, and refresh-token rotation are all deliberately
deferred rather than hidden — none of them block a first pilot with a small trusted user base, but all
three should be revisited before any production deployment involving external users, per the consolidated
audit's Part 1 findings.
