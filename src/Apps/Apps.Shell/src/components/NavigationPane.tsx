import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";

// Navigation Pane: Modules -> Areas -> menu items, per the Dynamics 365-referenced navigation model
// (docs/architecture/02-business-object-model.md #3). Only one module exists at this phase (there are
// no business modules yet, per docs/architecture/06-roadmap.md Phase 0) — Finance, Procurement, etc.
// each add their own module entry here as they're built, extending this same structure rather than
// replacing it.
interface ShellNavProps {
  language: SupportedLanguageCode;
}

export function NavigationPane({ language }: ShellNavProps) {
  return (
    <nav className="nav-pane" aria-label="Main">
      <div className="nav-pane__module">{t("nav.platformAdministration", language)}</div>
      <div className="nav-pane__area">{t("nav.system", language)}</div>
      <a className="nav-pane__item is-active" href="#system-status">
        {t("nav.systemStatus", language)}
      </a>
    </nav>
  );
}
