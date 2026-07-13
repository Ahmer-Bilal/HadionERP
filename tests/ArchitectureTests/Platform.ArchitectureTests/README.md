# Platform.ArchitectureTests

Automated "fitness function" tests that enforce architecture rules at build time instead of relying on
convention or memory across sessions — see docs/architecture/05-engineering-standards.md #1.

Currently enforces: no hardcoded translatable text (Arabic script) in `src/` outside an explicit,
justified allow-list (`NoHardcodedTranslatableTextTests.cs`). Added 2026-07-13 after the rule was violated
once during development of `Platform.Localization` — see PROGRESS.md for the story. Extend this project
with further statically-checkable architecture rules as they come up (e.g. module dependency direction
from docs/architecture/01-architecture-foundation.md #3.2) rather than leaving them as documentation only.
