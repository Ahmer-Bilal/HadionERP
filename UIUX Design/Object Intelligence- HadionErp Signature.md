# HadionERP Enterprise Product Bible

# HD-OBJ-003

# Universal Object Page Standard

Version: 1.0

Status: APPROVED

Priority: CRITICAL

Owner: Product Architecture

Applies To

• Business Partner
• Purchase Order
• Purchase Request
• RFQ
• Quotation
• Project
• BOQ
• WBS
• Employee
• Equipment
• Warehouse
• Asset
• Journal Entry
• AP Invoice
• AR Invoice
• Payment
• Budget
• Contract
• Variation
• Claim
• Every Future Business Object

------------------------------------------------------------------------------

# Purpose

Every Business Object shall use one identical page structure.

Users should never learn different page layouts.

When a user understands one object page,
they understand every object page.

This is one of the permanent architectural principles of HadionERP.

------------------------------------------------------------------------------

# Philosophy

An Object Page is NOT a form.

It is NOT a data entry screen.

It is NOT a report.

It is the complete business story of one object.

Users should never leave the object page
to understand the object.

------------------------------------------------------------------------------

# Enterprise Rule

Every Object Page answers:

1. What am I looking at?

2. What is happening?

3. Why does it exist?

4. Who owns it?

5. What is financially affected?

6. What is operationally affected?

7. What is connected?

8. What happened?

9. What happens next?

If any answer is missing,

the page is incomplete.

------------------------------------------------------------------------------

# Universal Layout

┌──────────────────────────────────────────────────────────────────────────────┐

Global Header

──────────────────────────────────────────────────────────────────────────────

Object Header

──────────────────────────────────────────────────────────────────────────────

Facts Strip

──────────────────────────────────────────────────────────────────────────────

Smart KPI Ribbon

──────────────────────────────────────────────────────────────────────────────

Primary Action Bar

──────────────────────────────────────────────────────────────────────────────

Quick Relationship Cards

──────────────────────────────────────────────────────────────────────────────

Tabbed Workspace

──────────────────────────────────────────────────────────────────────────────

Timeline

──────────────────────────────────────────────────────────────────────────────

Related Objects

──────────────────────────────────────────────────────────────────────────────

Activity Feed

──────────────────────────────────────────────────────────────────────────────

Attachments

──────────────────────────────────────────────────────────────────────────────

Audit Trail

└──────────────────────────────────────────────────────────────────────────────┘

No page may violate this structure.

------------------------------------------------------------------------------

# Object Header

Always Visible.

Contains

Object Number

Object Name

Status

Priority

Company

Project

Owner

Created Date

Last Updated

Workflow State

Health Score

Never hidden.

Never collapses completely.

------------------------------------------------------------------------------

Example

Purchase Order

PO-2400154

Approved

Vendor

ABC Steel

Project

Tower A

SAR 425,000

Buyer

Ahmed

Health

94%

Workflow

Completed

------------------------------------------------------------------------------

# Facts Strip

Purpose

Provide immediate understanding.

Never scroll.

Maximum eight facts.

Example

Vendor

Project

Currency

Amount

Delivery Date

Payment Terms

Budget

Department

------------------------------------------------------------------------------

# Smart KPI Ribbon

Displays live operational indicators.

Purchase Order

Committed

Received

Invoiced

Paid

Budget Used

Vendor Rating

Lead Time

Risk

Project

Progress

Cost

Revenue

Margin

Cash Flow

Forecast

Employee

Attendance

Leave

Payroll

Performance

Training

Equipment

Availability

Fuel

Utilization

Downtime

Maintenance

KPIs update in real time.

------------------------------------------------------------------------------

# Primary Action Bar

Appears directly under KPIs.

Actions depend on lifecycle.

Draft

Save

Submit

Delete

Approved

Print

Copy

Create GRN

Send

Completed

Archive

View History

Actions never remain static.

------------------------------------------------------------------------------

# Quick Relationship Cards

Immediately below the action bar.

Displays connected business objects.

Purchase Order

Vendor

Project

Budget

Contract

GRN

Invoice

Payment

Journal

Warehouse

Each card is clickable.

Hover displays summary.

Single click opens preview.

Double click opens full object.

------------------------------------------------------------------------------

# Tab Order

Every object follows exactly the same order.

Overview

Details

Financial Impact

Construction Context

Documents

Related Objects

Workflow

Timeline

Attachments

Comments

Audit

Hidden tabs are allowed.

Reordering is NOT allowed.

------------------------------------------------------------------------------

# Overview Tab

Executive summary.

No editing.

Displays

General Information

KPIs

Business Summary

Risk

Warnings

Related Objects

Recent Activity

AI Summary

Users should understand the object in under ten seconds.

------------------------------------------------------------------------------

# Details Tab

Contains editable information.

Grouped into logical sections.

Never one long form.

Supports:

Progressive Disclosure

Section collapse

Keyboard navigation

Auto validation

------------------------------------------------------------------------------

# Financial Impact

Mandatory for every financial object.

Displays

Committed Cost

Actual Cost

Budget

Revenue

Profit

Invoices

Payments

Journal Entries

Cash Flow

Forecast

Every value links to its source.

------------------------------------------------------------------------------

# Construction Context

Only appears when applicable.

Displays

Project

BOQ

WBS

Activity

Package

Engineer

Building

Floor

Zone

Inspection

Variation

Claim

Progress

Everything clickable.

------------------------------------------------------------------------------

# Documents

Displays

Contracts

Drawings

RFIs

Submittals

Specifications

Photos

Videos

PDF

Office Documents

Version History

Preview supported.

------------------------------------------------------------------------------

# Related Objects

Graph View

List View

Timeline View

Shows every relationship.

Example

Purchase Order

↓

Vendor

↓

Project

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

Users never search manually.

------------------------------------------------------------------------------

# Timeline

Chronological history.

Created

Modified

Submitted

Approved

Received

Posted

Paid

Closed

Filters supported.

------------------------------------------------------------------------------

# Activity Feed

Enterprise collaboration.

Comments

Mentions

Assignments

Approvals

Notifications

Files

Everything remains attached forever.

------------------------------------------------------------------------------

# Attachments

Universal attachment engine.

Supports

Versioning

Categories

Preview

OCR

Virus Scan

Permissions

Digital Signature

------------------------------------------------------------------------------

# Audit Trail

Read Only.

Displays

User

Action

Date

Time

Device

Old Value

New Value

Reason

Cannot be modified.

------------------------------------------------------------------------------

# Object Health Score

Every object calculates health.

Green

Healthy

Amber

Needs Attention

Red

Critical

Example

Purchase Order

Budget Overrun

Late Delivery

Workflow Delay

Invoice Delay

Vendor Risk

Combined into one score.

------------------------------------------------------------------------------

# AI Assistant

Every Object Page includes AI.

Examples

Explain delays

Summarize object

Predict risks

Find anomalies

Suggest actions

Never replaces user decisions.

Only assists.

------------------------------------------------------------------------------

# Performance Targets

Open Object

<500ms

Tab Change

<100ms

Timeline

<200ms

Relationship Graph

<300ms

No Object Page should feel slow.

------------------------------------------------------------------------------

# Current HadionERP Assessment

Business Object Engine

★★★★★

KEEP

Object Configuration

★★★★★

KEEP

Workflow

★★★★★

KEEP

Object Header

★★★★☆

ENHANCE

Facts Strip

☆☆☆☆☆

BUILD

Smart KPI Ribbon

☆☆☆☆☆

BUILD

Relationship Cards

☆☆☆☆☆

BUILD

Financial Impact

☆☆☆☆☆

BUILD

Construction Context

☆☆☆☆☆

BUILD

Activity Feed

☆☆☆☆☆

BUILD

Object Health

☆☆☆☆☆

BUILD

AI Assistant

☆☆☆☆☆

FUTURE

------------------------------------------------------------------------------

# Mandatory Before Version 2

Universal Object Header

Facts Strip

KPI Ribbon

Relationship Cards

Financial Impact

Construction Context

Timeline

Unified Attachments

Unified Comments

Object Health

Universal Object Template

------------------------------------------------------------------------------

END OF STANDARD