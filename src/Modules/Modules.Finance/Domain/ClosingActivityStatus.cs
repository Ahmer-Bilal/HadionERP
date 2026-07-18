namespace Modules.Finance.Domain;

/// <summary>The four states `UI/Finance/d1f20165-...png`'s checklist shows per row. <see cref="NotStarted"/>/
/// <see cref="InProgress"/>/<see cref="Completed"/> are auto-derived from <see cref="ClosingActivity"/>'s own
/// step completion (0%, 1–99%, 100%); <see cref="Blocked"/> is the one state that's never derived — only the
/// assigned person (or a Finance Manager) sets or clears it explicitly, since "blocked" is a real-world fact
/// about an external dependency, not something step completion alone can express.</summary>
public enum ClosingActivityStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Blocked = 3,
}
