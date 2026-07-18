import { DepartmentIcon } from "@platform/ui";
import type { NavModule } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";

interface DepartmentLandingPageProps {
  language: SupportedLanguageCode;
  module: NavModule;
}

/**
 * One department's own tile grid — Chart of Accounts, Journal Entries, AP Invoices, ... each a tile, the
 * same "icon plus label, click to go there" gateway pattern HomePage's "Explore Departments" grid already
 * uses (same `home-department-grid`/`home-department-tile`/`home-department-tile__badge` CSS classes,
 * verbatim, not a re-implementation), just scoped one level down to a single department's own items instead
 * of every department. This is what a NavModule's multi-item sub-navigation renders as now instead of a
 * sidebar text list — see NavigationPane.tsx's own doc comment for the other half of that change.
 *
 * One shared department-colored badge for every tile here (via the `dept-landing-page` class in App.css),
 * unlike HomePage's per-department distinct colors — there's only one department's identity to carry on
 * this screen, not several.
 */
export function DepartmentLandingPage({ language: _language, module }: DepartmentLandingPageProps) {
  const items = module.areas.flatMap((area) => area.items);

  const goTo = (href: string) => {
    window.location.hash = href;
  };

  return (
    <section className="dept-landing-page">
      <div className="home-section-header">
        <h1>{module.label}</h1>
      </div>
      <div className="home-department-grid">
        {items.map((item) => (
          <div
            key={item.key}
            className="home-department-tile"
            role="link"
            tabIndex={0}
            onClick={() => goTo(item.href)}
            onKeyDown={(e) => { if (e.key === "Enter") goTo(item.href); }}
          >
            <span className="home-department-tile__badge">
              <DepartmentIcon icon={module.icon} size={26} />
            </span>
            <span className="home-department-tile__title">{item.label}</span>
          </div>
        ))}
      </div>
    </section>
  );
}
