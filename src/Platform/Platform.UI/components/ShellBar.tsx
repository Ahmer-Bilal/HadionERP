import type { LanguageCode, LanguageOption } from "../types";

type ChromeIconKey = "menu" | "search" | "bell" | "help";

/** A small icon set for ShellBar's own chrome only — same "not the shared DepartmentIcon vocabulary"
 * reasoning LoginPage.tsx's local Icon component already established for page-specific decorative icons
 * that aren't one of the real department icons. */
function ChromeIcon({ icon, size = 18 }: { icon: ChromeIconKey; size?: number }) {
  const common = {
    width: size, height: size, viewBox: "0 0 20 20", fill: "none", stroke: "currentColor",
    strokeWidth: 1.5, strokeLinecap: "round" as const, strokeLinejoin: "round" as const, "aria-hidden": true,
  };
  switch (icon) {
    case "menu":
      return <svg {...common}><path d="M3 6h14M3 10h14M3 14h14" /></svg>;
    case "search":
      return <svg {...common}><circle cx="8.5" cy="8.5" r="5.5" /><path d="M16.5 16.5 13 13" /></svg>;
    case "bell":
      return (
        <svg {...common}>
          <path d="M5 8a5 5 0 0 1 10 0c0 3.5 1.2 4.8 1.5 5.5H3.5C3.8 12.8 5 11.5 5 8Z" />
          <path d="M8.2 16.5a1.8 1.8 0 0 0 3.6 0" />
        </svg>
      );
    case "help":
      return (
        <svg {...common}>
          <circle cx="10" cy="10" r="7.3" />
          <path d="M7.8 7.8a2.2 2.2 0 1 1 3.1 2c-.7.5-1.1.9-1.1 1.9" />
          <path d="M9.85 14.6h.01" strokeWidth="2" />
        </svg>
      );
  }
}

/** Initials for the avatar badge (e.g. "Ahmer Bilal" -> "AB") — computed here rather than asked of the
 * caller since it's pure display derivation off the one name string ShellBar already receives, the same
 * "derive display concerns locally, don't ask the app layer to precompute them" precedent
 * DepartmentIcon's icon-per-key mapping already sets. */
function initialsOf(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

export interface ShellBarBreadcrumbSegment {
  label: string;
  href?: string;
}

interface ShellBarProps {
  /** Used only for the "H"-style logo badge's first letter — the app name itself lives in the footer
   * (App.css's .app-shell__footer), not repeated here, matching the mockups' top bar. */
  title: string;
  isNavCollapsed: boolean;
  onToggleNav: () => void;
  toggleNavLabel: string;
  /** The current page's location, e.g. [{label: "Finance and Accounting", href: "#..."}, {label: "Trial
   * Balance"}] — every segment but the last is a link back up the hierarchy. */
  breadcrumb: ShellBarBreadcrumbSegment[];
  /** Chrome only — no command palette exists behind this yet (see ShellBar's own doc comment). */
  searchPlaceholder: string;
  languages: LanguageOption[];
  activeLanguage: LanguageCode;
  onLanguageChange: (language: LanguageCode) => void;
  languageSwitchLabel: string;
  notificationsLabel: string;
  helpLabel: string;
  /** The logged-in user's display name — shown next to the avatar and used to derive its initials. Only
   * `displayName` exists on the real user model today (api/authApi.ts's AuthenticatedUser), so unlike the
   * mockups' avatar block, there's no job-title subtitle here — showing one would be fabricated data. */
  currentUserLabel?: string;
  onLogout?: () => void;
  logoutLabel?: string;
}

/**
 * The top bar of the application shell — hamburger + logo + breadcrumb, a (currently decorative) search
 * box, then language switch + notification/help icons + user avatar. Replaces the earlier plain
 * title/tagline bar to match UI/Finance's mockups (see project_visual_identity_decisions memory for the
 * color-identity reversal this was part of). Every string still arrives pre-translated from the consumer —
 * this component has no dependency on any translation system, same contract as before this pass.
 */
export function ShellBar({
  title,
  isNavCollapsed,
  onToggleNav,
  toggleNavLabel,
  breadcrumb,
  searchPlaceholder,
  languages,
  activeLanguage,
  onLanguageChange,
  languageSwitchLabel,
  notificationsLabel,
  helpLabel,
  currentUserLabel,
  onLogout,
  logoutLabel,
}: ShellBarProps) {
  return (
    <header className="pi-shell-bar">
      <div className="pi-shell-bar__leading">
        <button
          type="button"
          className="pi-shell-bar__icon-button"
          onClick={onToggleNav}
          aria-label={toggleNavLabel}
          aria-pressed={isNavCollapsed}
        >
          <ChromeIcon icon="menu" />
        </button>
        <span className="pi-shell-bar__logo" aria-hidden="true">{title.charAt(0)}</span>
        <nav className="pi-shell-bar__breadcrumb" aria-label={title}>
          {breadcrumb.map((segment, index) => (
            <span key={index} className="pi-shell-bar__breadcrumb-segment">
              {index > 0 && <span className="pi-shell-bar__breadcrumb-sep" aria-hidden="true">/</span>}
              {segment.href && index < breadcrumb.length - 1 ? (
                <a href={segment.href}>{segment.label}</a>
              ) : (
                <span className={index === breadcrumb.length - 1 ? "is-current" : undefined}>{segment.label}</span>
              )}
            </span>
          ))}
        </nav>
      </div>

      <div className="pi-shell-bar__search">
        <ChromeIcon icon="search" size={16} />
        <input type="search" placeholder={searchPlaceholder} aria-label={searchPlaceholder} />
      </div>

      <div className="pi-shell-bar__trailing">
        <div className="pi-shell-bar__language-switch" role="group" aria-label={languageSwitchLabel}>
          {languages.map((language) => (
            <button
              key={language.code}
              type="button"
              className={language.code === activeLanguage ? "is-active" : ""}
              onClick={() => onLanguageChange(language.code)}
            >
              {language.label}
            </button>
          ))}
        </div>
        <button type="button" className="pi-shell-bar__icon-button" aria-label={notificationsLabel}>
          <ChromeIcon icon="bell" />
        </button>
        <button type="button" className="pi-shell-bar__icon-button" aria-label={helpLabel}>
          <ChromeIcon icon="help" />
        </button>
        {currentUserLabel && onLogout && (
          <div className="pi-shell-bar__user">
            <span className="pi-shell-bar__avatar" aria-hidden="true">{initialsOf(currentUserLabel)}</span>
            <span className="pi-shell-bar__user-label">{currentUserLabel}</span>
            <button type="button" onClick={onLogout}>{logoutLabel ?? "Logout"}</button>
          </div>
        )}
      </div>
    </header>
  );
}
