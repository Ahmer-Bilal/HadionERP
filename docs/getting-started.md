# Getting Started with HadionERP

This guide is written for the people who will actually use this system day to day — project managers,
site engineers, commercial/QS staff, accountants, and administrators — not for developers. If you're
looking for technical documentation, that lives in `docs/architecture/` and `docs/module/` instead.

## What this system is for

HadionERP is your company's system of record for running construction projects — from the moment a project
is agreed with a customer, through pricing the work, managing subcontractors, tracking progress on site,
billing the customer, and closing the project out. Instead of separate spreadsheets for contracts, BOQs,
progress, and invoicing, everything lives in one connected system, so a number entered once (like a
contract's pricing) is automatically used everywhere else it's needed (like a progress bill), rather than
copied and retyped.

## Logging in

You'll be given a username and password by your system administrator. Enter these on the login screen.
Your access is role-based — what you can see and do depends on the role(s) your administrator has assigned
you. If something you expect to see or do isn't available, that's usually a role/permission question for
your administrator, not a bug.

## Finding your way around

The left-hand navigation is organized by department — Project Management, Construction, Procurement,
Finance, and so on — matching how your company is actually organized, not how the software happens to be
built underneath. Within each department, you'll find the documents relevant to that area (Projects,
Contracts, Purchase Orders, and so on).

Most screens follow the same basic shape: a list on one side showing existing records, and a details view
on the other showing whatever you've selected. Creating something new is always a clearly-labeled "Create"
or "+" action from the list.

## How documents move through the system

Almost everything you create in this system — a Contract, a Purchase Order, a Project — follows the same
basic journey: you start it as a **Draft** (you can still change your mind freely at this stage), you
**Submit** it once you're confident it's ready, and someone with the right authority **Approves** it (or
sends it back to you with a reason, if something needs fixing). Once approved, some documents (like
payments) go a step further and get **Posted**, meaning the transaction is now final and reflected in the
company's real financial records. If a posted document ever needs correcting, it's never simply edited or
deleted — it's **reversed**, and if needed, a corrected version is created — this is what keeps a complete,
honest history of everything that's actually happened, which matters both for your own record-keeping and
for audits.

## Arabic and English

Every screen in this system is available in both English and Arabic, including full right-to-left layout
in Arabic. You can switch languages from the settings menu; your choice is remembered for future sessions.

## Getting help

If something isn't working the way you expect, or a screen looks wrong, contact your system administrator
or the development team — screenshots of exactly what you're seeing are always the fastest way to get a
problem understood and fixed.

## Where to go next

Each department has its own guide under `docs/user-guide/modules/` covering the specific screens and steps
for that area — for example, how to create a Contract and its Bill of Quantities, or how to raise a
Purchase Order. Those guides are added as each area is actually built and ready to use, so check back as
new capabilities come online. The `glossary.md` in this same folder explains the business terms you'll see
throughout the system (BOQ, IPC, Retention, and so on) in plain language.
