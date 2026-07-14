# Agent Instructions

This repository is **HadionERP**, by **hAdisHere**, created by **aHmAr**. This file is read by AI coding
agents generally (Codex and others follow the `AGENTS.md` convention; Claude Code reads `CLAUDE.md`, which
points back here). Whichever tool you are, follow this before touching the repo.

## This is a multi-session, multi-agent project

Different AI tools and humans work on this repository across different sessions over time. There is no
shared memory between them except **what is written into this repository's files.** That makes the two
files below load-bearing, not optional reading:

1. **`PROGRESS.md`** — the append-only log of what has been done, by whom, and what's next. **Read it
   first, every session, before doing anything else.** Add an entry to it after every meaningful unit of
   work, following its template. Never delete or rewrite past entries in it.
2. **`ARCHITECTURE.md`** (and `docs/architecture/*.md`) — the technical architecture this project must
   follow. Read the relevant docs before implementing anything in that area.

## Standing rule: the application must always run (added 2026-07-13)

At the end of every phase/unit of work: the solution must compile, the backend
(`src/Gateway/Gateway.Api`) must start without errors, the frontend (`src/Apps/Apps.Shell`) must start
without errors, and the user must be able to open it in a browser and see the completed work. Never leave
the project in a non-runnable state.

Build the production application from Day 1. Every phase extends the existing running application —
never build temporary UI, temporary architecture, or throwaway/demo code meant to be discarded later. An
in-memory reference implementation behind a stable interface (e.g. `InMemoryNumberRangeService`) is fine —
it gets swapped for a real one later without a rewrite — but a prototype meant to be thrown away is not.
Before reporting a phase done, actually build, start both processes, and verify (e.g. via the `run` skill)
rather than relying on the test suite alone.

## Working rules

- Do not start implementation work without first reading `PROGRESS.md`'s Phase Status Summary and the most
  recent few entries — someone (possibly a different AI) may have already started, finished, or deliberately
  reversed the thing you're about to do.
- Do not contradict a documented architecture decision (an ADR in `ARCHITECTURE.md`, or a section in
  `docs/architecture/`) without discussing it with the user first. If you believe a decision is wrong, add a
  correction the same way the 2026-07-13 entries in `PROGRESS.md` show — a new entry/section explaining what
  changed and why, never a silent rewrite.
- Follow the module boundaries, naming conventions, and Business Object lifecycle standard exactly as
  documented — consistency across contributions from different tools depends on everyone following the same
  written rules rather than each tool's own default style.
- When you finish a unit of work (a task, a phase step, a bug fix, an architecture correction), **update
  `PROGRESS.md`** with an entry before ending your session. If you're stopping mid-task, mark it `Blocked` or
  `In Progress` and say why, so the next contributor (AI or human) can pick it up without re-deriving context.
- No business modules are implemented as of the last entry in `PROGRESS.md` — check that file for the current
  actual state, since this note itself will age.
