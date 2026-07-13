import type { NavModule } from "../types";

/**
 * The persistent left navigation pane — Modules -> Areas -> menu items, the Dynamics 365 navigation
 * structure (docs/architecture/02-business-object-model.md #3). Data-driven: the consuming app passes the
 * full module/area/item tree as props, so a new business module adds its entry as data rather than a new
 * component or a hardcoded block. All labels are already translated by the caller.
 */
interface NavigationPaneProps {
  modules: NavModule[];
  /** Accessible label for the navigation landmark — resolved by the consumer, never hardcoded here. */
  ariaLabel: string;
}

export function NavigationPane({ modules, ariaLabel }: NavigationPaneProps) {
  return (
    <nav className="pi-nav-pane" aria-label={ariaLabel}>
      {modules.map((module) => (
        <div key={module.key} className="pi-nav-pane__module-group">
          <div className="pi-nav-pane__module">{module.label}</div>
          {module.areas.map((area) => (
            <div key={area.key} className="pi-nav-pane__area-group">
              <div className="pi-nav-pane__area">{area.label}</div>
              {area.items.map((item) => (
                <a
                  key={item.key}
                  className={"pi-nav-pane__item" + (item.isActive ? " is-active" : "")}
                  href={item.href}
                >
                  {item.label}
                </a>
              ))}
            </div>
          ))}
        </div>
      ))}
    </nav>
  );
}
