import type { ReactNode } from "react";
import type { DepartmentIconKey } from "./components/DepartmentIcon";

/*
 * Platform.UI's own types. Deliberately no imports from Apps.Shell — Platform.UI is a self-contained
 * design system (docs/architecture/02-business-object-model.md #4) so it can be promoted to a real npm
 * package later without rewriting consumers. The consuming app (Apps.Shell) constructs these data
 * structures — resolving translated labels via its own t()/content.ts — and passes them in as props.
 * Platform.UI components never call a translation function themselves; they render exactly what they're given.
 *
 * LanguageCode is structurally compatible with Apps.Shell's SupportedLanguageCode ("en" | "ar") but is
 * declared here independently so Platform.UI has zero upstream dependencies.
 */

/** The supported UI languages. Matches Platform.Localization's SupportedLanguage on the backend. */
export type LanguageCode = "en" | "ar";

/** One selectable language in the ShellBar switcher. `label` is the autonym (e.g. "English" / "العربية"). */
export interface LanguageOption {
  code: LanguageCode;
  label: string;
}

/** Accessible labels for Platform.UI components. The consumer resolves these via its own translation
 * system and passes them in — Platform.UI never hardcodes any string, including aria-labels. Every
 * screen-reader-facing label is treated exactly like visible display text: supplied by the app, not
 * baked into the component (same discipline enforced by the no-hardcoded-text guardrail on both sides). */
export interface AriaLabels {
  /** Label for the ShellBar's language-switcher group. */
  languageSwitchGroup: string;
  /** Label for the NavigationPane's landmark. */
  navigationLandmark: string;
  /** Label for the ActionPane's toolbar. */
  actionToolbar: string;
}

// --- Navigation Pane (docs/architecture/02-business-object-model.md #3) ---
// Modules -> Areas -> menu items, the Dynamics 365 navigation structure. Data-driven so a new business
// module adds its entry as data, not as a new component.

export interface NavItem {
  key: string;
  label: string;
  href: string;
  isActive?: boolean;
}

export interface NavArea {
  key: string;
  label: string;
  items: NavItem[];
}

export interface NavModule {
  key: string;
  label: string;
  /** The department's one consistent icon (docs/architecture/09-navigation-and-ui-standard.md) — reused
   * as-is on the landing page's matching department card, never a different icon per screen. */
  icon: DepartmentIconKey;
  /** The module's own "home" link — resolved by the app layer exactly like every NavItem.href, so
   * Platform.UI never hardcodes a routing convention. For a department with more than one item this is
   * that department's tile-grid landing page; for a department with exactly one item it's that item's own
   * href directly (no pointless one-tile landing page in between). */
  href: string;
  areas: NavArea[];
}

// --- Action Pane (docs/architecture/02-business-object-model.md #2.1) ---
// The single command bar at the top of a record form. Per the architecture doc, its buttons are "driven
// by the current FSM state + the user's security role/privilege — never hard-coded per screen." The
// ActionPane component itself is stateless; the consuming app decides which actions are available and
// passes only those. This is what makes a new BO transition surface its button everywhere automatically.

export type ActionVariant = "default" | "primary" | "danger";

export interface ActionItem {
  key: string;
  label: string;
  onClick: () => void;
  variant?: ActionVariant;
  isDisabled?: boolean;
}

// --- FastTabs (docs/architecture/02-business-object-model.md #2.1) ---
// "FastTabs, not tabs: several can be expanded simultaneously, the form scrolls vertically through them."
// Each tab is a collapsible panel; unlike a tab strip, opening one does not close the others.

export interface FastTabItem {
  key: string;
  title: string;
  content: ReactNode;
  defaultExpanded?: boolean;
}
