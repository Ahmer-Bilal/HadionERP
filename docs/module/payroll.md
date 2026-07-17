# Payroll

Not yet built. Owns the payroll run itself, WPS file export, GOSI integration, and End-of-Service Benefit
calculation — all genuinely Saudi-specific statutory requirements, not generic payroll features with a
local label attached.

## Why this stays separate from HR, and separate from project costing

Payroll calculates what an employee is actually paid. This is deliberately not the same number used to
charge a project for that employee's time — `docs/architecture/07-integrated-project-controlling.md` §3
explains why project costing uses a distinct, often burdened, labor cost rate instead of the payroll
figure directly. Payroll should be built assuming Labor Costing (wherever it ends up living) reads from a
rate table it owns, not from Payroll's own calculation output.

WPS — the Wage Protection System — is a mandatory monthly salary-file submission to the bank/government in
Saudi Arabia, not optional reporting; a payroll run that doesn't produce a compliant WPS file is a legal
compliance gap, not a missing convenience feature. GOSI (social insurance contribution calculation) is the
other statutory piece that has to be right before any real payroll run can be trusted. End-of-Service
Benefit — the gratuity calculation mandated under Saudi labor law when an employee's service ends — belongs
here as part of the payroll calculation engine rather than in a separate Legal module, per the reasoning in
`docs/architecture/07-integrated-project-controlling.md` §10: labor-law compliance is achieved by building
the calculation correctly into Payroll, not by adding a parallel legal-rules system.

## What this module explicitly does not own

Employee master data, org structure, and project mobilization belong to HR — Payroll consumes that data, it
doesn't duplicate it.
