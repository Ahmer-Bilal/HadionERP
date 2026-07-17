# HR

Not yet built. This document exists to record what this module is intended to own before any code is
written against it, so its first slice is built against a real design rather than a generic office-HR
assumption that would need reworking later.

## Why construction HR isn't generic HR

A generic HR module tracks who works in which department. A construction company's HR additionally needs
to track which *project* an employee is currently mobilized to, because site labor is routinely hired for
a specific project and demobilized when it ends — this mobilization/demobilization relationship should be
a first-class link between an Employee and a Project or WBS element, not an afterthought, because it's what
a Timesheet (owned by Labor Costing, see `docs/architecture/07-integrated-project-controlling.md` §3)
defaults against. Employee lifecycle and org structure are the generic foundation this module still needs
to own first — an Employee master, reporting lines, job grades — but that foundation should be built with
the project-mobilization link in mind from the start rather than bolted on afterward.

## What's scoped, beyond the generic foundation

Leave management is standard HR scope and not construction-specific. Saudization/Nitaqat tracking is
specific to this system's Saudi localization target and belongs here rather than in Payroll, since it's a
workforce-composition reporting concern, not a pay-calculation one. Labor accommodation/camp management is
common on large GCC and EPC projects but genuinely optional depending on whether this company houses its
own site labor — worth confirming as a real requirement before building it, rather than assuming every
construction company needs it. Iqama/work-permit expiry tracking for expatriate labor is a real compliance
need flagged in `docs/architecture/07-integrated-project-controlling.md` §5 but not yet placed in a
specific roadmap phase.

## What this module explicitly does not own

Labor cost rates used to charge a project (see the same integration doc, §3) are deliberately a *separate*
concept from an employee's actual pay rate, and the Timesheet/costing-rate mechanism itself belongs to a
future Labor Costing capability, not to HR directly — HR's job is who the person is and which project
they're assigned to, not what that time costs the project.
