import type { NavItem, NavModule } from "../types";
import { DepartmentIcon } from "./DepartmentIcon";

/** A WORKSPACE row with a real count badge (Approvals/Submitted) — same NavItem shape every other row
 * already uses, plus the count. */
interface ActivityNavItem extends NavItem {
  count: number;
}

interface NavigationPaneProps {
  isCollapsed: boolean;
  workspaceLabel: string;
  /** The one fixed WORKSPACE entry — "Dashboard," always present regardless of which department is
   * current. Not part of `modules` (see this component's own doc comment for why Home is handled
   * separately rather than as just another department). */
  dashboardItem: NavItem;
  /** Real per-department Approvals/Submitted rows (see useDepartmentActivity.ts) — undefined outside any
   * department (e.g. on the Dashboard), where neither concept applies yet. */
  activity?: { approvals: ActivityNavItem; submitted: ActivityNavItem };
  modulesLabel: string;
  modules: NavModule[];
  /** Which module (by key) is current — resolved by the consumer, not derived here from `item.isActive`.
   * A department's own landing page (`#dept-<key>`) has no "active item" underneath it to scan for (the
   * landing page itself is the destination), so item-flag scanning alone can't tell this pane which
   * department that page belongs to; the consumer already has to do this same "which page am I on" logic
   * for routing, so it's the single source of truth here too rather than a second, potentially-diverging
   * copy of it. Null on the Dashboard, where no department is current. */
  currentModuleKey: string | null;
  /** Accessible label for the navigation landmark — resolved by the consumer, never hardcoded here. */
  ariaLabel: string;
}

/**
 * The persistent left navigation pane — deliberately minimal now, not a text list of every department's
 * document types. A department's own sub-items (Chart of Accounts, Journal Entries, ...) render as a tile
 * grid on that department's own landing page (`DepartmentLandingPage.tsx`) instead; this pane just shows
 * WORKSPACE (Dashboard — reserved space for real future personal-workflow items like Approvals/My Work,
 * not filled with placeholders today) and, when a department is current, one clickable heading back to its
 * tile grid. No "OTHER MODULES" text list in the expanded pane either — Home's own tile grid is the one
 * place to switch departments now; a compact icon-only version of that switcher still lives in the
 * collapsed (hamburger-toggled) rail below, since that's a small utility dock rather than a growing list.
 */
export function NavigationPane({
  isCollapsed,
  workspaceLabel,
  dashboardItem,
  activity,
  modulesLabel,
  modules,
  currentModuleKey,
  ariaLabel,
}: NavigationPaneProps) {
  // "home" is represented once, by dashboardItem in the WORKSPACE section — never duplicated as a
  // switchable department.
  const realModules = modules.filter((module) => module.key !== "home");
  const currentModule = realModules.find((module) => module.key === currentModuleKey) ?? null;
  const otherModules = realModules.filter((module) => module.key !== currentModule?.key);

  if (isCollapsed) {
    return (
      <nav className="pi-nav-pane pi-nav-pane--collapsed" aria-label={ariaLabel}>
        <a
          className={"pi-nav-pane__icon-link" + (dashboardItem.isActive ? " is-active" : "")}
          href={dashboardItem.href}
          title={dashboardItem.label}
        >
          <DepartmentIcon icon="home" />
        </a>
        {currentModule && (
          <a className="pi-nav-pane__icon-link is-active" href={currentModule.href} title={currentModule.label}>
            <DepartmentIcon icon={currentModule.icon} />
          </a>
        )}
        {otherModules.map((module) => (
          <a key={module.key} className="pi-nav-pane__icon-link" href={module.href} title={module.label}>
            <DepartmentIcon icon={module.icon} />
          </a>
        ))}
      </nav>
    );
  }

  return (
    <nav className="pi-nav-pane" aria-label={ariaLabel}>
      <div className="pi-nav-pane__section">
        <div className="pi-nav-pane__section-label">{workspaceLabel}</div>
        <a
          className={"pi-nav-pane__item" + (dashboardItem.isActive ? " is-active" : "")}
          href={dashboardItem.href}
        >
          {dashboardItem.label}
        </a>
        {activity && (
          <>
            <a
              className={"pi-nav-pane__item" + (activity.approvals.isActive ? " is-active" : "")}
              href={activity.approvals.href}
            >
              <span>{activity.approvals.label}</span>
              {activity.approvals.count > 0 && <span className="pi-nav-pane__badge">{activity.approvals.count}</span>}
            </a>
            <a
              className={"pi-nav-pane__item" + (activity.submitted.isActive ? " is-active" : "")}
              href={activity.submitted.href}
            >
              <span>{activity.submitted.label}</span>
              {activity.submitted.count > 0 && <span className="pi-nav-pane__badge">{activity.submitted.count}</span>}
            </a>
          </>
        )}
      </div>

      {currentModule && (
        <div className="pi-nav-pane__section">
          <div className="pi-nav-pane__section-label">{modulesLabel}</div>
          <a className="pi-nav-pane__module-heading" href={currentModule.href}>
            <DepartmentIcon icon={currentModule.icon} />
            <span>{currentModule.label}</span>
          </a>
        </div>
      )}
    </nav>
  );
}
