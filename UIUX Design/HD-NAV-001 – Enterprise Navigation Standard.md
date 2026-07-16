# HadionERP Enterprise Product Bible

# HD-NAV-001

# Enterprise Navigation Standard

Version 1.0

Status: APPROVED

Priority: CRITICAL

Owner: Product Architecture

---

# Purpose

Enterprise users should never become lost.

Navigation is not merely a menu.

Navigation is the language through which users understand the enterprise.

The navigation system shall remain identical across every module.

No module is permitted to invent its own navigation model.

---

# Product Philosophy

Navigation follows Business Objects.

Navigation never follows database tables.

Navigation never follows software modules.

Example

WRONG

Finance

↓

Vendors

↓

Open

RIGHT

Business Partner

↓

Open

↓

Financial Information

↓

Procurement

↓

Projects

↓

Documents

The object remains constant.

Only the workspace changes.

---

# Enterprise Navigation Layers

Layer 1

Platform

↓

Layer 2

Workspace

↓

Layer 3

Business Object

↓

Layer 4

Business Activity

↓

Layer 5

Related Objects

Every screen belongs to one of these layers.

---

# Navigation Hierarchy

Platform

│

├── Dashboard

├── Search

├── Notifications

├── Favorites

├── Administration

│

└── Workspaces

        │

        ├── Finance

        ├── Procurement

        ├── Construction

        ├── Inventory

        ├── HR

        ├── CRM

        ├── Equipment

        ├── Administration

Each workspace owns its business processes.

Objects remain shared.

---

# Sidebar Standard

The sidebar is permanent.

It never disappears on desktop.

Purpose:

Instant orientation.

Fast switching.

Enterprise consistency.

The sidebar shall contain only:

• Logo

• Workspace Selector

• Navigation Groups

• Favorites

• Recent Items

• Settings

• Help

No business content appears inside the sidebar.

---

ASCII Layout

┌─────────────────────────────────────────────────────────────┐

│ Sidebar │ Workspace │ Object │ Context │

└─────────────────────────────────────────────────────────────┘

This layout becomes permanent.

---

# Workspace Philosophy

Workspaces replace traditional ERP modules.

Example

Finance Workspace

contains

General Ledger

Accounts Payable

Accounts Receivable

Cash Management

Budget

Assets

Reporting

Users never leave Finance while performing Finance work.

The same philosophy applies to every department.

---

# Workspace Header

Every workspace contains exactly:

Workspace Icon

Workspace Name

Fiscal Year

Company

Search

Notifications

User Menu

Command Palette

Nothing more.

---

ASCII Example

┌─────────────────────────────────────────────────────────────┐

│ Finance │ FY2026 │ Hadion Construction │ 🔍 │ 🔔 │ User │

└─────────────────────────────────────────────────────────────┘

---

# Universal Search

Shortcut

CTRL + K

Universal Search must search:

Business Partners

Projects

Purchase Orders

Invoices

Journal Entries

Employees

Equipment

Contracts

Documents

BOQ

WBS

Payments

Users

Reports

Actions

Commands

Search never searches one module only.

Search searches the enterprise.

---

Example

User types

Ahmed

Results

Employee

Vendor

Approver

Project Team

Documents

Activities

All displayed together.

---

# Command Palette

CTRL + K

supports

Open Object

Navigate

Create

Approve

Post

Run Report

Go to Workspace

Execute Command

Example

Create Purchase Order

Open Vendor 1001

Post Journal

Open Project Alpha

Approve Invoice

Create RFQ

Everything begins from the command palette.

---

# Favorites

Users may favorite:

Objects

Reports

Dashboards

Projects

Vendors

Employees

Documents

Favorites remain visible regardless of workspace.

---

# Recent Items

Recent Items are enterprise-wide.

Not workspace-specific.

Recently opened

Projects

Invoices

Employees

Purchase Orders

Journal Entries

appear automatically.

---

# Breadcrumb Standard

Every page shall expose a breadcrumb.

Example

Finance

>

Accounts Payable

>

Vendor Invoice

>

INV-2026-00154

Breadcrumbs are clickable.

Every level supports navigation.

---

# Object Navigation

Business Objects always open in Full Object Page.

Never in popups.

Never in dialogs.

Preview Drawers are allowed only for:

Quick View

Quick Approval

Quick Reference

Double-click always opens the Full Object Page.

---

# Navigation Rules

Users shall never require more than:

Three clicks

to reach any frequently used object.

If more than three clicks are required,

navigation shall be redesigned.

---

# Context Preservation

When switching workspaces:

Filters remain.

Company remains.

Fiscal Year remains.

Project Context remains.

The user should never lose context accidentally.

---

# Multi-Company

Company selection is global.

Changing company updates every workspace.

No module maintains independent company selection.

---

# Fiscal Year

Fiscal Year remains visible permanently.

Accounting users must always know which fiscal period they are viewing.

---

# Notifications

Notifications are enterprise events.

Examples

Approval Required

Budget Exceeded

Invoice Posted

Workflow Completed

Project Delayed

Supplier Registered

Notifications always deep-link to the originating business object.

---

# Accessibility

Every navigation element is fully keyboard accessible.

Tab Order

Arrow Navigation

Shortcut Keys

Screen Readers

High Contrast

No exceptions.

---

# Performance

Navigation response time target

<150 milliseconds

Object opening

<500 milliseconds

Search suggestions

<200 milliseconds

Enterprise responsiveness is a product requirement.

---

# Current HadionERP Assessment

Sidebar

KEEP

Breadcrumbs

KEEP

Workspace concept

ENHANCE

Universal Search

ENHANCE

Command Palette

BUILD

Recent Items

BUILD

Favorites

BUILD

Workspace Templates

BUILD

Object Navigation

STANDARDIZE

Preview Drawer

STANDARDIZE

Context Preservation

BUILD

Navigation Performance Metrics

BUILD

---

# Mandatory Before Version 2

✓ Universal Command Palette

✓ Workspace Templates

✓ Navigation Standards

✓ Full Object Pages

✓ Enterprise Search

✓ Related Object Navigation

✓ Global Favorites

✓ Recent Items

✓ Context Preservation

These capabilities are mandatory before introducing additional functional modules.

---

END OF STANDARD