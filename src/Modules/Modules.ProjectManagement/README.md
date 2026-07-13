# Modules.ProjectManagement

Owns Project Definition and **WBS elements** — the cost/revenue/Controlling backbone shared by every
project-based module (each WBS element is simultaneously a scheduling node and a full Controlling object,
flagged as planning / account-assignment / billing, per SAP Project System). Also owns Networks/Activities/
Milestones (time sequence, dependencies, resource & equipment allocation).

Construction's Contracts/BOQ/Subcontracts/Variation Orders reference this module's WBS elements rather than
inventing their own project-cost concept. See
`docs/architecture/07-project-accounting-and-financial-architecture.md` §4.
