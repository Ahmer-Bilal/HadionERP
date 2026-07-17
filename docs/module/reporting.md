# Reporting

Not yet built. Owns cross-module statutory and management reports, built on Platform.Reporting rather than
each module rolling its own reporting logic.

## Why this waits until other modules mature

A reporting module is only as good as the data it reads, and most of what a construction ERP's management
actually wants to see — project cost-to-date versus budget, WIP/unbilled-revenue position, retention
balances outstanding, subcontractor payment status — depends on modules that either don't exist yet
(Materials/Warehouse, Labor Costing, Fixed Assets) or exist but haven't reached the slices that produce
real numbers yet (Construction's IPC/Retention, Finance's Results Analysis). Building Reporting ahead of
those would mean building against data that doesn't exist yet, or building reports that need to be
substantially reworked once the underlying modules catch up. This module's own real build should start once
enough of `docs/architecture/07-integrated-project-controlling.md`'s cross-module flow is in place to
report on something genuinely meaningful — cost-to-date-versus-budget per WBS element is the natural first
report, since almost every department eventually feeds it.

## What's expected here, once it starts

Statutory reports (VAT/ZATCA-related, GOSI/WPS submissions surfaced for review before filing) sit alongside
management reports (project profitability, cash position, aging AP/AR) as two genuinely different
categories with different audiences and different accuracy requirements — statutory reports need to be
exactly right and auditable; management reports can reasonably show near-real-time estimates. Worth keeping
that distinction explicit in how this module is eventually structured, rather than treating every report as
the same kind of artifact.
