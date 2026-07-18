# HadionERP — Master UI Architecture Specification
### For Construction ERP — Whole System

**Version:** 2.0  
**Authority:** This document is the single source of truth for every UI decision in HadionERP.  
**How to use:** Give this to Claude Code before building any screen. It replaces mockups as the design contract.  
**Reference systems:** SAP S/4HANA Fiori, Microsoft Dynamics 365 Business Central  

---

## PART 0 — THE GOLDEN RULES

Before reading anything else, understand these five rules. Every screen in HadionERP must follow all five.

**Rule 1 — No screen invents its own pattern.**  
Every screen is one of six screen types (defined in Part 3). Pick the type, follow the spec. Period.

**Rule 2 — The right rail tells the story.**  
Every Object Page has a right rail showing Document Flow, Related Documents, and Activity Feed. This is non-negotiable. A user must always be able to answer "where did this come from?" and "who touched it?" without navigating away.

**Rule 3 — The action toolbar is driven by status, not hardcoded.**  
What buttons appear depends on the document's current lifecycle state and the user's role. Never add a button because "it would be useful." Only actions valid for the current state + role are shown.

**Rule 4 — Role controls visibility, not just access.**  
A Procurement Officer does not see Finance menu items. A Site Engineer does not see Approve buttons. The UI hides what the user cannot do — it does not show it grayed out. (Exception: explain why something is locked, e.g. "Posted — cannot edit.")

**Rule 5 — Numbers, dates, and IDs are always LTR inside RTL.**  
Wrap every number, date, document reference, and currency amount in `<bdi dir="ltr">`. No exceptions.

---

## PART 1 — SYSTEM ROLES AND WHAT THEY SEE

### 1.1 Role Definitions

HadionERP has the following system roles. Each role maps to a set of modules, a set of actions, and a navigation profile.

| Role | Department | Module Access | Approval Authority |
|---|---|---|---|
| **System Administrator** | IT | All modules | All |
| **CFO / Finance Director** | Finance | Finance (full), Reports (all) | Final approval on payments, closing |
| **Finance Manager** | Finance | Finance (full) | Approve JE, post, approve payments |
| **Finance Officer / Accountant** | Finance | Finance (create/submit only) | Submit only |
| **AP Accountant** | Finance | AP, Payments, Bank | Submit AP invoices, payment vouchers |
| **AR Accountant** | Finance | AR, Receipts, Bank | Submit AR invoices, customer receipts |
| **Procurement Manager** | Procurement | Procurement (full), Finance (view PO-related) | Approve PO, RFQ, contracts |
| **Procurement Officer** | Procurement | Procurement (create/submit), Finance (view AP invoices they raised) | Submit only |
| **Site Engineer** | Construction | Construction (full), Procurement (view/raise PR) | Submit PRs, approve GRNs on site |
| **Project Manager** | Construction | Construction (full), Procurement (view), Finance (project cost view) | Approve subcontractor invoices, BOQ variations |
| **Contract Manager** | Construction | Construction (contracts), Procurement (view) | Approve subcontracts, variations |
| **HR Manager** | HR & Payroll | HR (full) | Approve leave, payroll run |
| **HR Officer** | HR & Payroll | HR (create/submit) | Submit only |
| **Warehouse / Store Manager** | Inventory | Inventory (full) | Approve GRN, issue materials |
| **Equipment Manager** | Equipment | Equipment (full) | Approve equipment allocation |
| **CRM Manager** | CRM | CRM (full) | Approve quotes, contracts |
| **Executive / Management** | — | Dashboard (all modules, read-only KPIs) | None (view only) |

### 1.2 How Role Affects the UI — The Three Rules

**Rule A — Module visibility in sidebar:**  
The NavigationPane only shows modules the user's role can access. A Procurement Officer never sees the Finance module group. A Site Engineer sees Construction and a limited Procurement group (raise PR only).

**Rule B — Action availability in toolbar:**  
`ActionPane` buttons are computed from two inputs: the document's current FSM state AND the user's role. If the role cannot perform the action, the button is not rendered — not disabled, not grayed, simply absent.

Example: A Finance Officer can see a Journal Entry but cannot see the "Approve" button. Only a Finance Manager or CFO sees it.

**Rule C — Field-level visibility:**  
Some fields are hidden entirely based on role. Cost codes and budget line items are visible to Finance roles but hidden from a Site Engineer viewing a Purchase Request. Vendor bank details are visible only to AP Accountant and Finance Manager.

### 1.3 Role-to-Module Access Matrix

| Module | CFO | Fin Mgr | Fin Officer | Proc Mgr | Proc Officer | Site Eng | Project Mgr | HR Mgr | WH Mgr | Exec |
|---|---|---|---|---|---|---|---|---|---|---|
| Finance — full | ✓ | ✓ | ✓ | — | — | — | view | — | — | view |
| Finance — AP only | — | — | — | view | raise | — | — | — | — | — |
| Procurement | view | view | — | ✓ | ✓ | raise PR | view | — | view | view |
| Construction | view | view | — | view | — | ✓ | ✓ | — | — | view |
| Inventory | view | view | — | view | view | view | view | — | ✓ | view |
| HR & Payroll | view | — | — | — | — | — | — | ✓ | — | view |
| Equipment | view | — | — | view | — | view | view | — | — | view |
| CRM | — | — | — | — | — | — | — | — | — | view |
| Reports | ✓ | ✓ | limited | — | — | — | project | — | — | ✓ |

`✓` = full access, `view` = read-only, `raise` = create and submit only, `—` = not visible

---

## PART 2 — DESIGN TOKENS (SINGLE SOURCE OF TRUTH)

All values live in `src/Platform/Platform.UI/tokens/design-tokens.css`. Use `--pi-*` variables everywhere. Never hardcode.

### 2.1 Color Tokens

**Semantic colors (light mode → dark mode):**

| Token | Light | Dark | Role |
|---|---|---|---|
| `--pi-text` | `#111827` | `#e5e7eb` | Primary text |
| `--pi-text-muted` | `#64748b` | `#94a3b8` | Labels, captions, secondary |
| `--pi-bg` | `#f8fafc` | `#0f172a` | Page background |
| `--pi-surface` | `#eef2f7` | `#1e293b` | Cards, hover, input backgrounds |
| `--pi-border` | `#e2e8f0` | `#334155` | All borders and dividers |
| `--pi-accent` | `#2563eb` | `#60a5fa` | Primary actions, active nav, links |
| `--pi-accent-contrast` | `#ffffff` | `#ffffff` | Text on accent backgrounds |
| `--pi-danger` | `#b3261e` | `#f2867e` | Errors, destructive, negative amounts |
| `--pi-success` | `#1b7a43` | `#4fd189` | Confirmed, balanced, posted |
| `--pi-warning` | `#a15c07` | `#e3a548` | Caution, pending approval |

**⚠ FLAG — Missing tokens to add:**

| Token to create | Value suggestion | Why needed |
|---|---|---|
| `--pi-accent-surface` | `rgba(37,99,235,0.08)` | Tinted highlight areas, selected rows |
| `--pi-warning-surface` | `rgba(161,92,7,0.10)` | Amber caution panels |
| `--pi-success-surface` | `rgba(27,122,67,0.08)` | Green confirmation panels |
| `--pi-danger-surface` | `rgba(179,38,30,0.08)` | Red error panels |
| `--pi-font-size-xl` | `1.35rem` | Section titles inside panels |
| `--pi-font-size-2xl` | `1.75rem` | Page h1 |
| `--pi-focus-ring` | `0 0 0 3px rgba(37,99,235,0.35)` | Keyboard accessibility |

**Sidebar (always dark navy — never theme-toggled):**

| Token | Value | Role |
|---|---|---|
| `--pi-sidebar-bg` | Dark navy gradient | Sidebar background |
| `--pi-sidebar-surface` | `#16213a` | Hover / active item background |
| `--pi-sidebar-text` | `#e2e8f0` | Nav item text |
| `--pi-sidebar-text-muted` | `#8b95ab` | Section labels, inactive items |
| `--pi-sidebar-accent` | `#2f6fe0` | Active item fill |
| `--pi-sidebar-border` | `#1f2a44` | Dividers inside sidebar |

**Status badge colors — document lifecycle:**

| Status | Background token | Text token |
|---|---|---|
| Draft | `--pi-surface` | `--pi-text-muted` |
| Submitted | `--pi-warning-surface` | `--pi-warning` |
| Approved | `--pi-success-surface` | `--pi-success` |
| Posted | `--pi-success-surface` | `--pi-success` |
| Rejected | `--pi-danger-surface` | `--pi-danger` |
| Reversed | `--pi-surface` | `--pi-text-muted` |
| Pending | `--pi-warning-surface` | `--pi-warning` |
| Closed | `--pi-surface` | `--pi-text-muted` |
| Active | `--pi-success-surface` | `--pi-success` |
| Cancelled | `--pi-danger-surface` | `--pi-danger` |
| On Hold | `--pi-warning-surface` | `--pi-warning` |

### 2.2 Typography

| Token | Value | Used for |
|---|---|---|
| `--pi-font-family` | Inter / Noto Sans Arabic / system-ui | All text |
| `--pi-font-size-sm` | `0.85rem` | Table cells, labels, captions |
| `--pi-font-size-base` | `1rem` | Body, form fields |
| `--pi-font-size-lg` | `1.1rem` | Info bar values, emphasized body |
| `--pi-font-size-xl` | `1.35rem` | ⚠ ADD THIS — panel headings h2 |
| `--pi-font-size-2xl` | `1.75rem` | ⚠ ADD THIS — page title h1 |
| `--pi-font-weight-normal` | `400` | Body text |
| `--pi-font-weight-semibold` | `600` | Headings, column headers, active nav |
| `--pi-line-height` | `1.5` | All text |

Fonts: **Inter** (Latin/English), **Noto Sans Arabic** (Arabic). Both self-hosted as variable woff2 files covering weight 400–600.

### 2.3 Spacing Scale

| Token | Value | Typical use |
|---|---|---|
| `--pi-space-1` | `4px` | Tight gaps, badge padding, icon margins |
| `--pi-space-2` | `8px` | Icon-to-label, small button padding |
| `--pi-space-3` | `12px` | Button padding, form field spacing |
| `--pi-space-4` | `16px` | Card padding, intra-section gap |
| `--pi-space-5` | `24px` | Page content padding, major section gap |
| `--pi-space-6` | `32px` | Empty state padding, hero sections |

### 2.4 Radius, Shadow, Motion

| Token | Value | Use |
|---|---|---|
| `--pi-radius-sm` | `6px` | Inputs, pills, small buttons |
| `--pi-radius-md` | `8px` | Cards, nav items, most buttons |
| `--pi-radius-lg` | `12px` | Panels, modals, large cards |
| `--pi-shadow-panel` | Two-layer subtle box-shadow | SplitView detail, floating panels |
| `--pi-motion-duration` | `220ms` | All transitions |
| `--pi-motion-easing` | `cubic-bezier(0.22, 1, 0.36, 1)` | All easing |

---

## PART 3 — SHELL LAYOUT (EVERY PAGE)

```
┌──────────────────────────────────────────────────────────────────────────┐
│ ShellBar — 100% width, 56px height, border-bottom: 1px var(--pi-border)  │
│  [☰] [H Logo] [Breadcrumb path]    [Search Ctrl+K]    [🔔][✓][?][User]  │
├───────────┬──────────────────────────────────────────────────────────────┤
│           │                                                               │
│ Nav Pane  │  .app-shell__content                                         │
│ 216px exp │  padding: --pi-space-5 --pi-space-6                          │
│  64px col │  overflow-y: auto                                            │
│           │                                                               │
│ (dark     │                                                               │
│  navy)    │                                                               │
│           │                                                               │
├───────────┴──────────────────────────────────────────────────────────────┤
│ Footer — border-top, centered muted text, version                        │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.1 ShellBar — Left to right

1. **Hamburger** `☰` — toggles NavigationPane between 216px and 64px
2. **Logo badge** — 32px square, accent background, "H" initial, `--pi-radius-md`
3. **Breadcrumb** — `Module › Sub-section › Document ID`. Ancestors are muted links. Current item is bold, not a link. Separator is `›` in muted color.
4. **Command search** — centered, max-width 480px, `--pi-surface` bg, `--pi-radius-sm`, placeholder: "Search or type a command (Ctrl+K)"
5. **Notification icon** — bell, badge with count when unread > 0
6. **Tasks icon** — checkmark, badge with count
7. **Help icon** — question mark
8. **User area** — avatar circle (accent bg, 2-letter initials), name, role title, left border separator

### 3.2 NavigationPane

Always dark navy. Never affected by light/dark theme toggle.

**Expanded (216px):**

```
WORKSPACE
  🏠 Dashboard
  ✓  My Tasks           [8]
  ✓  Approvals         [12]
  🕐 Recent Items
  🔔 Notifications

MODULES
  💰 Finance           ▾     ← visible only to Finance roles
     Chart of Accounts
     Journal Entries         ← active item shown with accent bg
     Bank Reconciliation
     Budget Control
     Accounts Payable
     Accounts Receivable
     Fixed Assets
     Reports

  🛒 Procurement       ▾     ← visible only to Procurement + related roles
     Purchase Requests
     Request for Quotation
     Purchase Orders
     Goods Receipts
     Vendor Management
     Contracts

  🏗  Construction     ▾     ← visible only to Construction roles
     Projects
     BOQ Management
     Subcontracts
     Site Progress
     Variations
     Drawing Register

  📦 Inventory         ▾
  👥 HR & Payroll      ▾
  🚜 Equipment         ▾
  📞 CRM               ▾
  📊 Reports           ▾

FAVORITES             ← user-pinned items
  Journal Entries
  Purchase Orders
  Projects

──────────────────
  ⚙  Settings
  ◀  Collapse
```

**Collapsed (64px icon rail):**
Only module icons. Active module gets accent background. All items have tooltip on hover showing full label.

**Nav item states:**
- Default: `--pi-sidebar-text-muted`
- Hover: `--pi-sidebar-surface` background
- Active (current page): `--pi-sidebar-accent` background, `--pi-sidebar-text` bold
- Badge: pill, accent bg, white text, right-aligned, only when count > 0

**⚠ FLAG — Critical gap:** When inside a module, the sub-items (Chart of Accounts, Journal Entries, etc.) must be visible in the sidebar. Currently the sidebar shows only the module heading when inside a module. Sub-items must expand in the sidebar when a module is active.

---

## PART 4 — SIX SCREEN TYPES

Every screen is one of these six. Identify the type first, then build from its spec.

| Type | Description | Examples |
|---|---|---|
| **T1 — Object Page** | Single document, full detail | Journal Entry, Purchase Order, Project |
| **T2 — List Page** | Searchable grid of records | All JEs, All POs, All Projects |
| **T3 — Dashboard** | KPI overview for a module or role | Finance Dashboard, Procurement Dashboard |
| **T4 — Report / Statement** | Financial or analytical output | Trial Balance, Balance Sheet, BOQ Report |
| **T5 — Workflow Center** | Multi-step process driver | Period Closing, Payroll Run |
| **T6 — Department Landing** | Module entry tile grid | Finance Home, Procurement Home |

---

## PART 5 — T1: OBJECT PAGE (THE CORE PATTERN)

Used for: Journal Entry, AP Invoice, AR Invoice, Payment Voucher, Customer Receipt, Purchase Request, RFQ, Purchase Order, Goods Receipt, Subcontract, Variation Order, Project, BOQ, Site Progress Entry, Equipment Allocation, Leave Request, Employee Record, Asset.

### 5.1 Full Layout

```
┌─────────────────────────────────────────────────────┬──────────────────────┐
│ MAIN AREA                                           │ RIGHT RAIL (20rem)   │
│                                                     │                      │
│ [Record Type label — small muted]                   │ ┌──────────────────┐ │
│ [Document Number]  [Status Badge]                   │ │ Document Flow    │ │
│                      [Actions driven by FSM+Role]   │ │ (vertical chain) │ │
│                                                     │ └──────────────────┘ │
│ [Info Bar — 5-7 key fields horizontal]              │                      │
│                                                     │ ┌──────────────────┐ │
│ [Tab Bar]                                           │ │ Related Docs     │ │
│  Overview | Lines | Attachments | Notes | History   │ └──────────────────┘ │
│                                                     │                      │
│ [Tab Content]                                       │ ┌──────────────────┐ │
│  └ Overview: 3-column panel grid + lines + notes    │ │ Activity Feed    │ │
│  └ Lines: full dense table                          │ └──────────────────┘ │
│  └ Attachments: upload + file table                 │                      │
│  └ Notes: add note + note list                      │                      │
│  └ History: audit timeline                          │                      │
│                                                     │                      │
└─────────────────────────────────────────────────────┴──────────────────────┘
```

### 5.2 Document Header Region

```
Journal Entry                          ← type label: --pi-text-muted, --pi-font-size-sm, uppercase
JE-2026-00521   [Posted ✓]             ← number: 1.75rem semibold | badge: pill with status color
                                       ← navigation arrows [‹] [›] top-right
         [Edit]  [Copy]  [Print]  [Actions ▾]  [‹]  [›]
```

- Document number: `--pi-font-size-2xl` (1.75rem), `--pi-font-weight-semibold`
- Status badge: `border-radius: 999px`, padding `2px 10px`, color from status table in Part 2
- Action buttons: always top-right, computed by FSM state + user role (see Part 7)
- `[‹] [›]`: navigate to previous/next record in the current filtered list

### 5.3 Info Bar

Horizontal `<dl>` showing 5–7 critical fields at a glance. Always visible regardless of tab.

```
Entry Date          Reference        Source              Created By        Total Debit      Total Credit
31-May-2026         INV-2026-0456    Accounts Payable    Ahmer Khan        125,000.00 SAR   125,000.00 SAR
──────────          ─────────────    ─────────────────   ──────────        ────────────     ────────────
--pi-text-muted     values in        --pi-font-size-lg   (--pi-font-       bold for         bold for
--pi-font-size-sm   --pi-font-       --pi-text           size-base)        amounts          amounts
```

Numbers and dates: always `<bdi dir="ltr">`. Currency amounts: right-aligned, 2 decimal places.

**Info bar fields by document type:**

| Document | Field 1 | Field 2 | Field 3 | Field 4 | Field 5 | Field 6 |
|---|---|---|---|---|---|---|
| Journal Entry | Entry Date | Reference | Source | Created By | Total Debit | Total Credit |
| AP Invoice | Invoice Date | Vendor | Net Amount | VAT | Gross Amount | Status |
| AR Invoice | Invoice Date | Customer | Net Amount | VAT | Gross Amount | Status |
| Purchase Order | PO Date | Vendor | Delivery Date | Total Amount | Status | Approved By |
| Purchase Request | PR Date | Requested By | Required Date | Total Estimated | Priority | Status |
| Goods Receipt | GR Date | PO Reference | Vendor | Received By | Total Value | Status |
| Project | Start Date | End Date | Contract Value | % Complete | Budget | Status |
| Subcontract | Contract Date | Subcontractor | Contract Value | Paid To Date | Retention | Status |

### 5.4 Tab Bar

```
Overview   Line Items (3)   Attachments (1)   Notes (2)   History   Related ▾
─────────
(active: --pi-accent underline, semibold)
```

- Active: `--pi-accent` color text, 2px solid `--pi-accent` bottom border
- Inactive: `--pi-text-muted`
- Count in parentheses only when > 0
- "Related ▾" is a dropdown for sub-documents when there are multiple relation types

**Standard tabs — all Object Pages have these:**

| Tab | Content |
|---|---|
| Overview | Summary panels (3-col grid) + line items preview + first note + first attachment |
| Line Items | Full `pi-dense-table` of all transaction lines |
| Attachments | File upload area + attached files table |
| Notes | Add-note text area + chronological note list |
| History | Audit trail — every status change, every field edit |
| Related | Cross-references to related documents not in Document Flow |

### 5.5 Overview Tab — The Three-Panel Grid

Three `<div class="fin-report__panel">` in a `repeat(3, 1fr)` grid, gap `--pi-space-4`.

**Panel 1 — Document Identity** (what this document is)
Core fields: Document number, description, reference, transaction date, accounting date, fiscal period, company, currency, source document.

**Panel 2 — Processing / Workflow State** (what happened to it)
Fields: Who submitted, when submitted, who approved, when approved, who posted, when posted, posting type (auto/manual), approval status badge, posting status badge.

**Panel 3 — Totals / Financials** (the money)
Fields: Total debit, total credit, variance, net amount, VAT, gross amount. Plus the balance indicator:

```
┌──────────────────────────────────────────────┐
│ ✓ The journal entry is balanced.             │  ← --pi-success-surface bg, --pi-success border-left
│   Total Debit equals Total Credit.           │
└──────────────────────────────────────────────┘
```
Or red danger box if unbalanced.

Below the three panels: line items table preview (first 5 rows), then notes preview, then attachments preview.

### 5.6 Right Rail — Always Visible

Three stacked panels. Never hidden, never tabbed. Fixed 20rem width.

**Panel A — Document Flow (most important panel in the system)**

Shows the complete procurement/approval chain this document belongs to, as a vertical timeline.

```
Document Flow
─────────────────────────────────────
[✓] Purchase Request        Approved
    PR-1021
[✓] Request for Quotation   Completed  
    RFQ-1006
[✓] Quotation               Selected
    QTN-10062
[✓] Purchase Order          Approved
    PO-1045
[✓] Goods Receipt           Completed
    GR-1087
[→] Supplier Invoice        ← This Document
    INV-2026-0456
[ ] Payment                 Pending
```

- Completed/Approved nodes: green icon `✓`, `--pi-success` color
- Current document: highlighted with `--pi-accent` left border, "This Document" label
- Pending nodes: gray icon `○`
- Each node: document type label + document number (link) + status

Document flow chains by module:

| Chain | Nodes |
|---|---|
| Procurement | PR → RFQ → Quotation → PO → GRN → AP Invoice → Payment |
| Construction subcontract | Project → BOQ → Subcontract → Progress Claim → Payment Certificate → Payment |
| Construction variation | Project → Variation Request → Variation Order → Additional Invoice |
| AR / Sales | Quote → Customer Order → Delivery → AR Invoice → Customer Receipt |

**Panel B — Related Documents**

Lateral relationships (not the main chain). Quick links with external-link icon.

```
Related Documents
─────────────────────────────────────
[📄] Purchase Order    PO-1045  ↗
[📄] Goods Receipt     GR-1087  ↗
[📄] Payment (Pending) PAY-2001 ↗
```

**Panel C — Activity Feed**

Reverse-chronological. Most recent action first.

```
Activity Feed                     View All
─────────────────────────────────────────
[AK] Ahmer Khan posted this entry
     31-May-2026 04:25 PM

[SA] Sana Ali approved this entry
     31-May-2026 03:58 PM

[MS] Mustafa Saleem submitted this entry
     31-May-2026 02:10 PM
```

- Avatar: 32px circle, `--pi-accent` background, 2-letter initials, `--pi-font-size-sm`
- Actor name: semibold
- Action description: regular
- Timestamp: `--pi-text-muted`, `--pi-font-size-sm`
- "View All" link top-right opens full history

---

## PART 6 — T2: LIST PAGE

Used for: All Journal Entries, Chart of Accounts, All POs, All PRs, All Projects, All Subcontracts, All GRNs, All AP Invoices, All AR Invoices, All Assets, etc.

### 6.1 Layout

```
[Page Title — h1]
[ActionPane — primary actions only: New, Import]

[Filter Bar]
  [🔍 Search...]  [Date From]  [Date To]  [Status ▾]  [Type ▾]  [More Filters]
                                                              [Saved Views ▾] [Columns] [Export]

[Status Quick-Filter Tab Rail]
  All (145)  |  Draft (12)  |  Submitted (8)  |  Approved (3)  |  Posted (118)  |  Reversed (4)

[pi-dense-table]
  [Column headers — sortable]
  [Data rows — click row → opens Object Page]
  [Empty state when no results]

[Pagination Bar]
  Showing 1–25 of 145    [‹ Prev]  [1] [2] [3] ... [6]  [Next ›]    Rows per page [25 ▾]
```

### 6.2 Filter Bar Rules

- Filters are above the table, never in the ActionPane
- Always: text search input (left), Export button (right), Saved Views (right)
- Common: Date range (From / To), Status dropdown, Type/Source dropdown
- Contextual: Vendor dropdown (AP), Customer dropdown (AR), Project dropdown (Construction), Account type (COA)
- Filters apply immediately (no "Apply" button needed for single-select dropdowns)
- "More Filters" expands a secondary filter row for less common filters
- Saved Views: user can save any filter combination with a name. Views are per-user, not shared by default.

### 6.3 Status Quick-Filter Rail

Horizontal tab row below the filter bar. Each tab shows the status name and count of matching records.

- "All" tab always first, always shown
- Only statuses with count > 0 are shown (except "All")
- Active tab: `--pi-accent` text, 2px bottom border in accent
- Selecting a tab filters the table instantly

### 6.4 Table — pi-dense-table

```
font-size: --pi-font-size-sm (0.85rem) for all cells
Column headers: --pi-text-muted, semibold, sortable (up/down arrow icon)
Row height: 40px (compact), consistent
Row hover: --pi-surface background
Row selected: --pi-accent-surface bg + 3px inset accent border on first cell (inline-start)
Document number/ID cell: --pi-accent color, semibold, cursor pointer (acts as link)
Status column: always a badge pill (Part 2 status colors)
Amount columns: <bdi dir="ltr">, right-aligned, 2 decimal places, monospace
Date columns: <bdi dir="ltr">
Row click: opens the Object Page for that record
Checkbox column (leftmost): for bulk actions — select one, multiple, or all
```

### 6.5 Standard Columns by Document Type

**Journal Entries:** `☐` | `#` | Document No | Description | Posting Date | Source | Status | Total Debits | Total Credits | Created By

**Chart of Accounts:** `#` | Account Code | Account Name | Account Type | Normal Balance | Parent Account | Status | Actions

**Purchase Orders:** `☐` | `#` | PO Number | Vendor | PO Date | Delivery Date | Description | Total Amount | Status | Approved By

**Purchase Requests:** `☐` | `#` | PR Number | Description | Requested By | Required Date | Project | Priority | Total Est. | Status

**Goods Receipts:** `#` | GRN Number | PO Reference | Vendor | Receipt Date | Received By | Total Value | Status

**Projects:** `#` | Project Code | Project Name | Client | Start Date | End Date | Contract Value | % Complete | Status

**Subcontracts:** `#` | Contract No | Subcontractor | Project | Scope | Contract Value | Paid | Retention | Status

**AP Invoices:** `☐` | `#` | Invoice No | Vendor | Invoice Date | Net | VAT | Gross | PO Reference | Status

**AR Invoices:** `☐` | `#` | Invoice No | Customer | Invoice Date | Net | VAT | Gross | Status

### 6.6 Bulk Actions

When one or more rows are selected, a bulk action bar appears above the table:

```
[✓ 3 selected]  [Bulk Approve]  [Bulk Export]  [Bulk Print]  [Clear Selection]
```

Only actions valid for the selection are shown (e.g. Bulk Approve only appears if all selected records are in Submitted status and the user has Approve role).

### 6.7 Pagination

- Default 25 rows per page
- "Showing X–Y of Z records"
- Page number buttons: show up to 5 pages, then ellipsis
- Rows-per-page selector: 10, 25, 50, 100

---

## PART 7 — ACTION TOOLBAR (FSM + ROLE RULES)

### 7.1 Universal Rules

The `ActionPane` component renders zero or more buttons. The consumer page computes which buttons to show based on:
1. The document's current `status` (FSM state)
2. The current user's `role`
3. Whether `busy === true` (all buttons disabled)

Never hardcode buttons. Never show buttons that are invalid for the current state or role.

### 7.2 FSM State Machines by Document Type

**Journal Entry:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Approved ──[Post]──→ Posted ──[Reverse]──→ Reversed
                         └──[Reject]──→ Rejected
```

**AP / AR Invoice:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Approved ──[Post]──→ Posted ──[Reverse]──→ Reversed
                         └──[Reject]──→ Rejected
```

**Purchase Request:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Approved ──[Convert to RFQ / PO]──→ (closes PR)
                         └──[Reject]──→ Rejected
```

**Purchase Order:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Approved ──[Confirm]──→ Confirmed
                         └──[Reject]──→ Rejected                └──[Close]──→ Closed
                                                                └──[Cancel]──→ Cancelled
```

**Goods Receipt:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Approved ──[Post]──→ Posted
                         └──[Reject]──→ Rejected
```

**Payment Voucher / Customer Receipt:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Approved ──[Post]──→ Posted ──[Reverse]──→ Reversed
                         └──[Reject]──→ Rejected
```

**Project:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Active ──[Complete]──→ Completed
                                                   └──[Suspend]──→ On Hold
                                                   └──[Cancel]──→ Cancelled
```

**Subcontract:**
```
Draft ──[Submit]──→ Submitted ──[Approve]──→ Active ──[Close]──→ Closed
                         └──[Reject]──→ Rejected    └──[Terminate]──→ Terminated
```

### 7.3 Role × Action Matrix (who can do what)

| Action | CFO | Fin Mgr | Fin Officer | Proc Mgr | Proc Officer | Site Eng | Project Mgr |
|---|---|---|---|---|---|---|---|
| Submit (any) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ (PR/GRN) | ✓ |
| Approve JE/Invoice | ✓ | ✓ | — | — | — | — | — |
| Post JE/Invoice | ✓ | ✓ | — | — | — | — | — |
| Reverse Posted | ✓ | ✓ | — | — | — | — | — |
| Approve PO/PR | ✓ | — | — | ✓ | — | — | — |
| Approve GRN | ✓ | — | — | ✓ | — | ✓ | ✓ |
| Approve Subcontract | ✓ | — | — | ✓ | — | — | ✓ |
| Approve Project | ✓ | — | — | — | — | — | ✓ |
| Approve Payment | ✓ | ✓ | — | — | — | — | — |
| Edit (Draft only) | ✓ | ✓ | ✓ (own) | ✓ | ✓ (own) | ✓ (own) | ✓ (own) |
| Delete (Draft only) | ✓ | ✓ | — | ✓ | — | — | — |

**"own" = only records created by that user.**

### 7.4 Button Placement and Visual Hierarchy

All buttons appear in the ActionPane at top of the main content area, right-aligned:

```
[Primary Action]  [Secondary Action]  [Secondary Action]  [Destructive]  [Actions ▾]
```

- **Primary** (one only per state): filled `--pi-accent` bg, white text — Submit, Approve, Post, Confirm
- **Secondary** (any number): outline border, `--pi-text` — Edit, Copy, Print, Back
- **Destructive** (rare): `--pi-danger` border and text — Reject, Reverse, Cancel, Terminate
- **Actions ▾** dropdown: catches less-common actions — Duplicate, Export PDF, Send by Email

**Always present:** `Back` button (returns to list), `Print` button (PDF generation), `Export` button

---

## PART 8 — T3: DASHBOARD

Used for: Finance Dashboard, Procurement Dashboard, Construction Dashboard, HR Dashboard, Executive Dashboard.

### 8.1 Layout

```
[Page Title]  [Period Selector — This Month / Q1 / YTD / Custom]        [Refresh]

[KPI Stat Card Row — 4-6 cards, equal width, horizontal scroll on small screens]

[Main Grid — 12-column, responsive]
  [Chart area — 8 cols]    [Summary panel — 4 cols]
  [Chart area — 8 cols]    [Summary panel — 4 cols]
  [Full-width table or alert section — 12 cols]
```

### 8.2 KPI Stat Card

```
┌────────────────────────────┐
│ [Icon]  Label              │
│         Large Value        │
│         ↑ +12% vs last mo │
└────────────────────────────┘
```

- Icon: 40px circle, tinted with `tone` color (configurable per card)
- Label: `--pi-text-muted`, `--pi-font-size-sm`
- Value: `1.5rem`, semibold, `--pi-text`
- Trend: `--pi-success` with ↑ for positive, `--pi-danger` with ↓ for negative, `--pi-text-muted` for neutral

### 8.3 Finance Dashboard — KPI Cards and Panels

**KPI Cards (top row):**
- Total Revenue (YTD)
- Total Expenses (YTD)
- Net Income (YTD)
- Accounts Receivable (outstanding)
- Accounts Payable (outstanding)
- Cash Position

**Panels (below KPIs):**
- Revenue vs Expenses chart (bar chart, monthly)
- AR Aging breakdown (donut chart: 0-30 / 31-60 / 61-90 / 90+ days)
- AP Due This Week (list panel)
- Unposted Journal Entries (alert list — requires action)
- Budget vs Actual by cost center (bar chart)
- Recent Transactions (mini table, last 10)

### 8.4 Procurement Dashboard — KPIs and Panels

**KPI Cards:**
- Open Purchase Requests
- POs Pending Approval
- Total PO Value (this month)
- Overdue Deliveries
- Vendor Performance (avg rating)
- Savings vs Budget

**Panels:**
- PR to PO cycle time (trend line)
- PO by status (donut)
- Top vendors by spend (bar)
- Pending approvals queue (table — direct action from dashboard)
- Overdue deliveries (alert list)

### 8.5 Construction Dashboard — KPIs and Panels

**KPI Cards:**
- Active Projects
- Total Contract Value
- % Milestones On Track
- Subcontractor Claims Pending
- Material Requests Open
- Equipment Utilization %

**Panels:**
- Project progress overview (horizontal progress bars per project)
- Budget vs Actual by project (grouped bar)
- Upcoming milestones (timeline)
- Subcontractor payment status (table)
- Site daily logs (recent activity)

### 8.6 Executive Dashboard — Role-specific

The Executive role sees a cross-module overview: Finance KPIs + Procurement spend + Construction project health + HR headcount in one page. All data is read-only. No action buttons except "Drill down" links that open the relevant module's dashboard.

---

## PART 9 — T4: REPORT / STATEMENT PAGE

Used for: Trial Balance, Balance Sheet, Income Statement, Cash Flow, Budget vs Actual, BOQ Report, Cost Report.

### 9.1 Layout

```
[Page Title]           ← e.g. "Trial Balance"
[Subtitle]             ← e.g. "Al Hadion Construction — As of 31 May 2026"

[Filter / Parameter Bar]
  [Period selector]  [Account filter]  [Project filter]  [Show zeros ☐]  [Run Report]  [Export ▾]

[KPI Summary Row — 3-4 stat cards]

[Two-column layout: 1fr main + 20rem right rail]
LEFT: Statement Table
RIGHT: Charts + Top accounts + Summary panels
```

### 9.2 Statement Table Row Types

```css
.fin-report__row--header    /* section label: "Assets", "Revenue". Surface bg, semibold */
.fin-report__row--section   /* sub-section: "Current Assets". Accent color, semibold */
.fin-report__row--data      /* normal account rows */
.fin-report__row--total     /* totals: double border-top, semibold, larger font */
.fin-report__negative       /* negative numbers: --pi-danger color */
```

### 9.3 Report Export Options

Every Report Page has an Export button with these options:
- Export to Excel (.xlsx)
- Export to PDF
- Print (browser print)
- Send by Email

---

## PART 10 — T5: WORKFLOW CENTER

Used for: Period Closing Center, Payroll Run Center, Year-End Closing.

### 10.1 Layout

```
[Page Title]  [Period / Year selector]  [Company selector]

[KPI Row]
  [Progress donut + %]  [Steps Complete: X/Y]  [Pending: N]  [Overdue: N]  [Blocked: N]

[Stage Tabs]
  General Ledger | Accounts Receivable | Accounts Payable | Fixed Assets | Final Closing

[Timeline Steps Row — horizontal]
  ●────●────○────○────○
  Done Done Act  Pend Pend

[Step Detail Area]
  Step name, owner, deadline, status, sub-tasks checklist
  [Mark Complete]  [Assign]  [Add Note]

[Right panel: Issues / Blockers / AI Insights]
```

### 10.2 Step States

| State | Icon | Color |
|---|---|---|
| Complete | ● filled | `--pi-success` |
| Active / In Progress | ● accent | `--pi-accent` |
| Pending | ○ empty | `--pi-border` |
| Overdue | ⚠ | `--pi-danger` |
| Blocked | ✕ | `--pi-danger` |
| Skipped | — | `--pi-text-muted` |

---

## PART 11 — T6: DEPARTMENT LANDING PAGE

Used for: Finance home, Procurement home, Construction home, etc.

### 11.1 Target Layout (current implementation needs updating)

```
[Module Name — h1]

[KPI Alert Row — 3 cards showing items requiring attention]
  [Unposted JEs: 3 ⚠]  [AR Overdue: SAR 45,000]  [AP Due Today: SAR 28,000]

[Section: General Ledger]
  [Chart of Accounts] [Journal Entries] [Fiscal Periods] [Posting Setup]

[Section: Accounts Receivable]
  [Customers] [AR Invoices] [Customer Receipts]

[Section: Accounts Payable]
  [Vendors] [AP Invoices] [Payment Vouchers]

[Section: Financial Statements]
  [Trial Balance] [Balance Sheet] [Income Statement]
```

### 11.2 Tile Design

Each tile: 160px × 120px card, `--pi-radius-lg`, `--pi-surface` background, hover lifts with shadow.

```
┌──────────────────┐
│    [Distinct     │
│      Icon]       │
│                  │
│  Journal Entries │
│  [badge: 12 ●]   │   ← badge only when there are pending items
└──────────────────┘
```

- Icon: 44px, distinct per item type (not the same icon for everything)
- Label: `--pi-font-size-sm`, centered, max 2 lines
- Badge: pending items count, `--pi-warning` or `--pi-danger` depending on urgency

**⚠ FLAG:** Current implementation uses the same icon for every tile. Each NavItem needs its own `icon` property.

---

## PART 12 — SHARED COMPONENT LIBRARY (Platform.UI)

### 12.1 Components that exist

| Component | Class/Name | Status |
|---|---|---|
| ShellBar | `ShellBar` | ✓ Built |
| NavigationPane | `NavigationPane` | ✓ Built — missing sub-items in module context |
| ActionPane | `ActionPane` | ✓ Built |
| FastTabs (accordion) | `FastTabs` | ✓ Built |
| SplitView (master-detail) | `SplitView` | ✓ Built |
| StatCard (KPI tile) | `StatCard` | ✓ Built |
| Dense table | `.pi-dense-table` | ✓ Built |
| Status pill | `.gl-status-pill` | ✓ Built |
| Link style | `.pi-link` | ✓ Built |

### 12.2 Components that must be created

| Component | Priority | What it does |
|---|---|---|
| `LoadingSkeleton` | Critical | Shimmer placeholder matching shape of content being loaded |
| `ErrorBanner` | Critical | Top-of-content error strip with message + retry button |
| `EmptyState` | High | Centered dashed-border area with label and optional CTA button |
| `ConfirmDialog` | High | Modal for destructive actions: "Are you sure? This cannot be undone." |
| `ToastNotification` | High | Bottom-right transient message: success / error / info |
| `DocumentFlowPanel` | High | Right-rail document chain timeline (currently inline in each page) |
| `ActivityFeedPanel` | High | Right-rail activity feed (currently inline in each page) |
| `RelatedDocsPanel` | High | Right-rail related documents (currently inline in each page) |
| `FilterBar` | Medium | Standardized filter row with search, dropdowns, saved views |
| `BulkActionBar` | Medium | Appears above table when rows are selected |
| `StatusQuickFilter` | Medium | Horizontal tab rail with status counts |
| `RightRail` | Medium | Wrapper for the three right-rail panels on Object Pages |

### 12.3 SplitView — How it works

Master-detail pattern: list stays visible on the left while detail slides in from the right.

```tsx
<SplitView
  list={<RecordList onSelect={setSelected} />}
  detail={selected ? <RecordDetail record={selected} /> : null}
  detailKey={selected?.id ?? ''}
  emptyDetailHint="Select a record to view its details"
  ariaLabel="Records list and detail"
/>
```

Detail panel: frosted glass effect (`backdrop-filter: blur(20px)`), translucent background, shadow, slides in with `--pi-motion-duration` easing.

### 12.4 ActionPane — Interface

```tsx
interface ActionItem {
  key: string
  label: string
  onClick: () => void
  variant?: 'primary' | 'danger'   // default = secondary (outline)
  isDisabled?: boolean
  icon?: ReactNode
}

<ActionPane
  actions={computedActions}   // computed from FSM state + user role
  ariaLabel="Document actions toolbar"
/>
```

Returns `null` (renders nothing) when `actions` array is empty.

---

## PART 13 — SCREEN STATES (ALL PAGES)

### 13.1 Loading State

**Initial load (no data yet):**
Use `LoadingSkeleton` component. Show shimmer blocks matching the shape of the content.
- List page: 8–10 skeleton rows
- Object page: skeleton for info bar + three skeleton panels
- Dashboard: skeleton stat cards + skeleton chart areas
- Do NOT show a spinner in the center of the screen

**Action in progress (data already visible):**
- Disable all ActionPane buttons (`isDisabled: true`)
- Show a subtle spinner inside the clicked button (not replacing it)
- Do not replace the visible content

### 13.2 Error State

**API call failed:**
Use `ErrorBanner` component. Appears at the top of the main content area.

```
┌─────────────────────────────────────────────────────────────────────────┐
│ ✕  Failed to load journal entries. The server returned an error.        │
│    [Try Again]                                                  [×]     │
└─────────────────────────────────────────────────────────────────────────┘
```

- Left border: 3px `--pi-danger`
- Background: `--pi-danger-surface`
- Message: human-readable, not a raw exception or HTTP status code
- "Try Again" button triggers the same API call

**Validation error (form):**
- Highlight the invalid field with `--pi-danger` border
- Show error message below the field in `--pi-danger` color, `--pi-font-size-sm`
- Do NOT use an alert dialog for field validation

### 13.3 Empty State

When a list has no records matching current filters:

```
           ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐
           
           │         [Icon]                │
                  No journal entries
           │   matching your filters.      │
                  
           │   [Clear Filters]  [Create New] │
           └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘
```

- Dashed border, `--pi-radius-lg`, centered in content area
- Label names the specific thing that's missing (not generic "No data")
- Primary CTA when creation is possible, secondary CTA to clear filters

### 13.4 Locked / Read-Only State

When a document is in a terminal state (Posted, Reversed, Closed, Cancelled):
- ActionPane shows only safe actions: Print, Copy, Back (and Reverse if role allows)
- Form fields render as `<dl>` key-value pairs, not `<input>` elements
- A subtle banner: "This document is Posted and cannot be edited."
- Status badge makes this clear without additional locks or icons

### 13.5 Confirmation Dialog (Destructive Actions)

Before executing Reject, Reverse, Cancel, Terminate, Delete:

```
┌────────────────────────────────────────────┐
│  Reverse Journal Entry?                    │
│                                            │
│  This will create a reversal entry and     │
│  cannot be undone. Are you sure?           │
│                                            │
│  [Cancel]              [Yes, Reverse]      │
│                         (--pi-danger btn)  │
└────────────────────────────────────────────┘
```

Use `ConfirmDialog` component. Never execute destructive actions without confirmation.

---

## PART 14 — CONSTRUCTION-SPECIFIC PATTERNS

Construction is the core business of HadionERP. These patterns appear only in the Construction module but must be consistent within it.

### 14.1 Project Object Page

A Project is the master object. Everything else hangs off it.

**Info Bar:** Project Code | Project Name | Client | Start Date | End Date | Contract Value | % Complete | Status

**Tab Bar:** Overview | BOQ | Subcontracts | Variations | Progress | Drawings | Costs | Milestones | Attachments | Notes | History

**Overview tab panels:**
- Panel 1: Project Identity (code, name, description, client, location, project manager, site engineer)
- Panel 2: Contract Details (contract value, start/end, contract type, retention %, advance payment %)
- Panel 3: Financial Summary (billed to date, collected, outstanding, cost to date, margin)

**Right Rail:**
- Document Flow: not applicable (Project is the root, not a child document)
- Related Procurement: open PRs, open POs linked to this project (count + link)
- Milestones: next 3 upcoming milestones with due dates
- Activity Feed: standard

### 14.2 BOQ (Bill of Quantities)

Hierarchical table. Parent rows are work sections, children are line items.

```
#   | Description              | Unit | Quantity | Unit Rate | Amount    | % Complete | Billed
────┼──────────────────────────┼──────┼──────────┼───────────┼───────────┼────────────┼───────
    | CIVIL WORKS              |      |          |           | 2,450,000 |    68%     |
  1 | Earthworks               | m³   | 5,000    | 85.00     | 425,000   |    100%    | ✓
  2 | Foundation concrete      | m³   | 800      | 650.00    | 520,000   |    75%     | partial
  3 | Block masonry            | m²   | 3,200    | 95.00     | 304,000   |    45%     |
    | Total Civil Works        |      |          |           | 1,249,000 |            |
```

### 14.3 Subcontract Object Page

**Info Bar:** Contract No | Subcontractor | Project | Scope | Contract Value | Paid to Date | Retention | Status

**Tab Bar:** Overview | Scope of Work | Progress Claims | Variations | Retention | Payments | Documents | Notes | History

**Right Rail Document Flow:** Project → Subcontract → Progress Claim → Payment Certificate → Payment

### 14.4 Variation Order

A Variation is always a child of a Project or Subcontract.

**Info Bar:** VO Number | Project/Subcontract | Type (Addition/Omission/Substitution) | Value | Status | Approved By

**Right Rail Document Flow:** Project → [Original Contract] → Variation Request → Variation Order → Invoice

### 14.5 Site Progress Entry

Daily or weekly site progress log.

**Fields:** Date | Project | Work Package | Today's Quantity | Cumulative Quantity | Weather | Crew Size | Equipment | Remarks | Photos

Progress entries feed into % completion on BOQ line items and Project dashboard.

---

## PART 15 — PROCUREMENT-SPECIFIC PATTERNS

### 15.1 Full Procurement Document Flow

```
Purchase Request (PR)
  └── Request for Quotation (RFQ)
        └── Quotation (from vendor)
              └── Purchase Order (PO)
                    └── Goods Receipt Note (GRN)
                          └── AP Invoice
                                └── Payment Voucher
```

Every document in this chain shows the full chain in its Document Flow panel.

### 15.2 Purchase Order Object Page

**Info Bar:** PO Number | Vendor | PO Date | Delivery Date | Project | Total Amount | Status

**Tab Bar:** Overview | Line Items | Delivery Schedule | Goods Receipts | Invoices | Attachments | Notes | History

**Overview panels:**
- Panel 1: PO Identity (PO number, vendor, description, buyer, PR reference, project)
- Panel 2: Terms (delivery date, delivery address, payment terms, incoterms, currency)
- Panel 3: Financial Summary (subtotal, VAT, total, invoiced to date, received to date, outstanding)

**Line Items table columns:** `#` | Item Code | Description | Unit | Quantity | Unit Price | Discount % | Net Amount | Received Qty | Invoiced Qty | Status

**Right Rail Document Flow:** PR → RFQ → Quotation → PO (current) → GRN (pending) → AP Invoice (pending)

### 15.3 RFQ and Quotation Comparison

The RFQ object page has a special "Quotation Comparison" tab (does not exist on other document types).

This tab shows a comparison matrix:

```
Item Description    | Unit | Qty | Vendor A    | Vendor B    | Vendor C    | Recommended
────────────────────┼──────┼─────┼─────────────┼─────────────┼─────────────┼────────────
Steel rebar D12     | ton  | 50  | SAR 2,850   | SAR 2,750 ✓ | SAR 2,900   | Vendor B
Concrete C30        | m³   | 200 | SAR 285     | SAR 310     | SAR 275 ✓   | Vendor C
```

Selected vendor cells highlighted in `--pi-success-surface`.

---

## PART 16 — RTL (ARABIC LANGUAGE) RULES

These rules are mandatory in every component. The codebase already uses logical CSS — maintain this.

**CSS logical properties — always use, never physical:**

| Never | Always |
|---|---|
| `margin-left` / `margin-right` | `margin-inline-start` / `margin-inline-end` |
| `padding-left` / `padding-right` | `padding-inline-start` / `padding-inline-end` |
| `border-left` / `border-right` | `border-inline-start` / `border-inline-end` |
| `left` / `right` (position) | `inset-inline-start` / `inset-inline-end` |
| `text-align: left` | `text-align: start` |
| `width` / `height` | `inline-size` / `block-size` |
| `float: left` | Never use float |

**Numbers, dates, amounts — always LTR inside RTL:**
```html
<bdi dir="ltr">125,000.00</bdi>
<bdi dir="ltr">31-May-2026</bdi>
<bdi dir="ltr">JE-2026-00521</bdi>
<bdi dir="ltr">PO-1045</bdi>
```

**Selected row accent border (flips automatically with dir):**
```css
/* LTR */
.pi-dense-table tbody tr.is-selected td:first-child {
  box-shadow: inset 3px 0 0 var(--pi-accent);
}
/* RTL */
:root[dir="rtl"] .pi-dense-table tbody tr.is-selected td:first-child {
  box-shadow: inset -3px 0 0 var(--pi-accent);
}
```

**SplitView slide direction flips with dir automatically** using `transform: translateX(100%)` vs `translateX(-100%)` based on `dir` attribute.

---

## PART 17 — NAMING CONVENTIONS AND FILE STRUCTURE

### 17.1 File naming
`{Module}{Object}Page.tsx` — e.g. `FinanceJournalEntriesPage.tsx`, `ProcurementPurchaseOrdersPage.tsx`

### 17.2 ViewState pattern (every List+Object page)
```tsx
type ViewState =
  | { kind: 'list' }
  | { kind: 'create' }
  | { kind: 'details'; record: RecordType }
```

### 17.3 API function naming
```
list{Objects}(limit, offset)   → { items: T[], total: number }
get{Object}(id)                → T
create{Object}(input)          → T
submit{Object}(id)             → T
approve{Object}(id)            → T
reject{Object}(id)             → T
post{Object}(id)               → T
reverse{Object}(id)            → T
close{Object}(id)              → T
cancel{Object}(id)             → T
```

### 17.4 CSS class prefix system
| Prefix | Scope |
|---|---|
| `pi-` | Platform.UI shared — never modify from a page file |
| `fin-` | Finance module shared |
| `proc-` | Procurement module shared |
| `con-` | Construction module shared |
| `hr-` | HR & Payroll module shared |
| `inv-` | Inventory module shared |
| `eq-` | Equipment module shared |
| `crm-` | CRM module shared |
| `je-` | Journal Entry page specific |
| `ap-` | AP Invoice page specific |
| `po-` | Purchase Order page specific |
| `proj-` | Project page specific |
| `home-` | Landing page specific |

---

## PART 18 — FLAGS: WHAT DOES NOT EXIST YET

Items marked ⚠ must be built. They are not optional.

| # | What | Priority | Impact if missing |
|---|---|---|---|
| 1 | NavigationPane shows sub-items when inside a module | **Critical** | Users cannot navigate between screens |
| 2 | Per-item distinct icons on NavItem and landing tiles | **Critical** | Every tile looks identical |
| 3 | `LoadingSkeleton` shared component | **Critical** | Loading states are inconsistent across pages |
| 4 | `ErrorBanner` shared component | **Critical** | Errors display inconsistently |
| 5 | `ConfirmDialog` for destructive actions | **Critical** | Reverse/Reject/Delete have no confirmation |
| 6 | `ToastNotification` component | **High** | No feedback after successful actions |
| 7 | `DocumentFlowPanel` extracted as shared component | **High** | Duplicated inline in every Object Page |
| 8 | `ActivityFeedPanel` extracted as shared component | **High** | Duplicated inline in every Object Page |
| 9 | `RightRail` wrapper component | **High** | Right rail layout duplicated in every Object Page |
| 10 | `FilterBar` shared component | **High** | Filter UI will diverge across modules |
| 11 | `StatusQuickFilter` shared component | **High** | Status tab rails duplicated in every list page |
| 12 | `BulkActionBar` component | **Medium** | No bulk operations in any list page |
| 13 | `--pi-accent-surface` token | **Medium** | Needed for selected row tints, highlighted sections |
| 14 | `--pi-warning-surface` token | **Medium** | Needed for caution panels |
| 15 | `--pi-success-surface` token | **Medium** | Needed for confirmation/balanced panels |
| 16 | `--pi-danger-surface` token | **Medium** | Needed for error panels |
| 17 | `--pi-font-size-xl` and `--pi-font-size-2xl` tokens | **Medium** | Page title sizes hardcoded in App.css |
| 18 | `--pi-focus-ring` token + global `:focus-visible` style | **Medium** | Keyboard accessibility incomplete |
| 19 | Department Landing KPI alert row | **Medium** | Landing pages show no actionable information |
| 20 | Tile grouping by area on Department Landing | **Medium** | All tiles in one flat undifferentiated grid |
| 21 | Role-based sidebar filtering (hide modules by role) | **High** | All modules visible to all users |
| 22 | Role-based ActionPane filtering | **High** | All action buttons visible to all roles |
| 23 | Quotation comparison tab on RFQ | **Medium** | Procurement officers cannot compare vendor quotes |
| 24 | BOQ hierarchical table component | **High** | Construction BOQ cannot be displayed |
| 25 | Progress entry form for site engineers | **High** | Site progress cannot be recorded |

---

*HadionERP Master UI Architecture Specification v2.0*  
*Covers: Finance, Procurement, Construction, Inventory, HR & Payroll, Equipment, CRM*  
*Reference: SAP S/4HANA Fiori patterns, Microsoft Dynamics 365 Business Central*  
*This document is the single source of truth. Individual mockups are reference only — this spec governs.*
