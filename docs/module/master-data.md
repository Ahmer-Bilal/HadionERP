# Master Data

Master Data is the first business module in the system, and the first place real persisted data replaced
the in-memory kernel scaffolding everything before it ran on. It owns the shared reference data every other
module depends on rather than duplicating: Business Partners (customers, vendors, subcontractors, all one
entity type distinguished by role, not separate tables), the Chart of Accounts, Items, Cost Centers, Tax
Codes, and Number Range definitions. Other modules never reach into this module's own Domain or
Infrastructure directly — every cross-module reference goes through this module's published Contracts
package (`IBusinessPartnerLookup`, `IGLAccountLookup`, `ICostCenterLookup`, and so on), the same boundary
rule Project Management's `IProjectLookup` and Construction's use of it already follow.

## Business Partner — one entity, many roles

A Business Partner isn't typed as "customer" or "vendor" at creation — it holds a collection of Business
Roles, and the same company can legitimately hold several: a firm can be both a Customer on one project and
a Subcontractor on another, and the system needs to represent that as one real-world entity with two roles,
not two disconnected records that happen to share a name. This is also why a company can hold the same
role type more than once distinguished by trade — Subcontractor–Electrical and Subcontractor–Concrete are
two separate qualifications on the same partner, matching how Vendor Prequalification in Procurement
already needs to treat them.

A partner also owns multiple addresses and multiple contacts, deliberately unrestricted in count per type —
several active site-office addresses for different concurrent projects, several named contacts each with
their own phone and email rather than one shared pair for the whole company, because that's genuinely the
shape a real construction-industry counterparty has. Creating or approving a partner runs through the full
lifecycle, since onboarding a new counterparty is a real compliance and fraud-prevention control point — but
adding an address or a contact to an already-approved partner deliberately isn't gated by that same
lifecycle, because correcting or extending a partner's contact details isn't the kind of change that needs
a formal reversal to undo.

## What's still ahead

`PaymentTerms` as a real structured field on the Business Partner master — referenced as a gap by
Construction's Contract, which currently can't default its terms from the customer record because this
field doesn't exist yet. Beyond that, this module's remaining scope is mostly depth rather than new
document types: richer Item master data (units of measure, material groups) to properly back the Materials/
Warehouse work described in `docs/architecture/07-integrated-project-controlling.md` §2 once that begins.
