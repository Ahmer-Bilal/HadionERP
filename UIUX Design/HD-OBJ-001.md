# HadionERP Enterprise Product Bible

# HD-OBJ-001

# Universal Business Object Standard

Version: 1.0

Status: APPROVED

Priority: CRITICAL

Owner: Product Architecture

Applies To:

- Business Partner
- Project
- Purchase Order
- Purchase Request
- RFQ
- Quotation
- Subcontract
- Contract
- BOQ
- WBS
- Cost Code
- Journal Entry
- Payment
- Invoice
- Employee
- Equipment
- Warehouse
- Material
- Asset
- Payroll
- Bank Account
- Budget
- Every future business object

---

# Purpose

Every business object in HadionERP follows one universal standard.

Users should never need to learn different layouts for different objects.

Every object should immediately feel familiar.

This consistency is a permanent architectural rule.

---

# Business Object Philosophy

The object is the center of the enterprise.

Modules do not own objects.

Objects participate in multiple modules.

Example

Purchase Order

belongs to

Procurement

Finance

Projects

Budget

Reporting

Workflow

AI

The object remains one.

The perspectives change.

---

# Every Business Object Must Answer

Within three seconds the user should know:

What is this?

Current Status?

Business Purpose?

Financial Impact?

Operational Impact?

Related Objects?

Next Action?

If any answer is missing,

the object page is incomplete.

---

# Universal Layout

Every object follows exactly the same structure.

┌─────────────────────────────────────────────┐

Object Header

─────────────────────────────────────────────

Facts Strip

─────────────────────────────────────────────

Action Bar

─────────────────────────────────────────────

KPI Panel

─────────────────────────────────────────────

Fast Tabs

─────────────────────────────────────────────

Workspace Content

─────────────────────────────────────────────

Timeline

─────────────────────────────────────────────

Related Objects

─────────────────────────────────────────────

Attachments

─────────────────────────────────────────────

Comments

─────────────────────────────────────────────

Workflow

─────────────────────────────────────────────

Audit Trail

└─────────────────────────────────────────────┘

No exceptions.

---

# Header Standard

Always contains

Object Icon

Object Number

Object Name

Current Status

Owner

Company

Project

Creation Date

Last Updated

Workflow Badge

Priority

Risk Indicator

Never hide these.

---

Example

Purchase Order

PO-2026-00154

Approved

Project Alpha

Vendor ABC

SAR 450,000

Budget Reserved

Workflow Complete

---

# Facts Strip

Located directly below the header.

Contains only the most important facts.

Purchase Order Example

Vendor

Project

Budget

Total

Currency

Expected Delivery

Payment Terms

Buyer

No scrolling required.

---

# Smart KPI Panel

Every object displays intelligent KPIs.

Purchase Order

Budget Used

Committed Cost

Delivered %

Invoice %

Payment %

Lead Time

Vendor Score

Risk

Project

Progress

Cost %

Schedule %

Profit %

Cash Flow

Equipment

Utilization

Downtime

Maintenance Due

Every object owns different KPIs.

The location remains identical.

---

# Action Bar

Actions are state driven.

Example

Draft

Save

Submit

Delete

Approved

Print

Create GRN

Copy

Cancel

Closed

View History

Archive

Actions never remain static.

They depend on lifecycle.

---

# Fast Tabs

Every object uses identical tab positions.

Overview

Details

Related Objects

Documents

Workflow

Financial Impact

Timeline

Attachments

Comments

Audit

Some tabs may be hidden.

Order never changes.

---

# Financial Impact

Every object capable of affecting finance exposes a Financial Impact panel.

Purchase Order

Budget

Committed

Actual

Remaining

Invoices

Payments

Journal

Variance

Project

Contract Value

Cost

Revenue

Margin

Cash Flow

Forecast

Employee

Payroll

Benefits

Loans

Cost Allocation

The panel is always available.

---

# Construction Context

Construction objects expose an additional context panel.

Project

Package

BOQ

WBS

Activity

Area

Building

Floor

Zone

Engineer

Site

Contract

Variation

Claim

Progress

Linked Procurement

Linked Finance

Linked Inventory

Everything connected.

---

# Related Objects

Every business object displays relationships.

Purchase Order

Vendor

↓

Project

↓

Budget

↓

GRN

↓

Invoice

↓

Payment

↓

Journal

↓

Cash Flow

Each item is clickable.

---

# Timeline

Timeline is mandatory.

Displays

Created

Submitted

Approved

Modified

Received

Posted

Paid

Closed

Chronological order.

---

# Workflow

Workflow remains visible.

Current Step

Approver

Completed Steps

Pending Steps

Delegation

Escalation

No hidden workflow.

---

# Attachments

Universal attachment component.

Supports

PDF

CAD

Excel

Images

Contracts

Drawings

Videos

Version history retained.

---

# Comments

Enterprise discussion.

Supports

Mentions

Attachments

Resolution

Internal

External

Audit preserved.

---

# Audit Trail

Displays

Who

What

When

Previous Value

New Value

IP

Device

Reason

Read only.

---

# Object Health Indicator

Every object displays a health score.

Green

Healthy

Yellow

Needs Attention

Red

Critical

Score calculated automatically.

Example

Purchase Order

Approval Delay

Budget Overrun

Late Delivery

Invoice Delay

Vendor Risk

Combined into one score.

---

# AI Summary

Every object contains AI Summary.

Example

Purchase Order

"Vendor delivery delayed by 12 days.

Budget utilization is 83%.

One invoice remains unpaid.

Project impact estimated at Medium."

Generated automatically.

---

# Current HadionERP Assessment

Business Object Architecture

★★★★★

KEEP

Object Configuration

★★★★★

KEEP

Workflow

★★★★★

KEEP

Header

★★★★☆

ENHANCE

Facts Strip

★★★☆☆

BUILD

Financial Impact

☆☆☆☆☆

BUILD

Construction Context

☆☆☆☆☆

BUILD

Related Objects

★★☆☆☆

BUILD

Timeline

★★★☆☆

STANDARDIZE

Attachments

★★★★☆

STANDARDIZE

Comments

★★☆☆☆

BUILD

Audit

★★★★★

KEEP

Object Health

☆☆☆☆☆

BUILD

AI Summary

☆☆☆☆☆

FUTURE

---

# Mandatory Before Version 2

Universal Object Template

Facts Strip

Financial Impact

Construction Context

Related Objects

Timeline Standard

Object Health

Workflow Panel

Unified Attachments

Unified Comments

These are mandatory.

No new business object shall be created until it complies with HD-OBJ-001.

---

END OF STANDARD