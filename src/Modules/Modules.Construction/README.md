# Modules.Construction

The construction-industry commercial layer built on top of `Modules.ProjectManagement`'s WBS elements:
Customer Contracts, BOQ (mapped onto WBS elements), Subcontracts (procurement documents assigned to WBS
elements), Site Progress/Measurement, Variation Orders (adjust a WBS element's planned cost/revenue, which
feeds the next Results Analysis run in Finance), and Retention terms.

This module depends on ProjectManagement (consumes its WBS Contracts package) and does not define its own
project-cost structure. See `docs/architecture/07-project-accounting-and-financial-architecture.md` §4.
