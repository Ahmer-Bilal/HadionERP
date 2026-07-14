import type { LanguageCode, LanguageOption } from "../types";

/**
 * The top bar of the application shell — title + language switcher. Extracted from Apps.Shell's original
 * ShellBar and generalized: it receives the already-translated title and the language options (autonyms)
 * as props, so it has no dependency on any translation system. This is what makes Platform.UI reusable
 * across apps without each app's i18n leaking into it.
 *
 * (docs/architecture/02-business-object-model.md #4 — "built against Platform.UI".)
 */
interface ShellBarProps {
  title: string;
  /** Optional small muted line next to the title (e.g. a "by {company}" brand tagline) — already
   * translated by the consumer, same contract as `title`. */
  tagline?: string;
  languages: LanguageOption[];
  activeLanguage: LanguageCode;
  onLanguageChange: (language: LanguageCode) => void;
  /** Accessible label for the language-switcher group — resolved by the consumer, never hardcoded here. */
  languageSwitchLabel: string;
}

export function ShellBar({
  title,
  tagline,
  languages,
  activeLanguage,
  onLanguageChange,
  languageSwitchLabel,
}: ShellBarProps) {
  return (
    <header className="pi-shell-bar">
      <span className="pi-shell-bar__brand">
        <span className="pi-shell-bar__title">{title}</span>
        {tagline && <span className="pi-shell-bar__tagline">{tagline}</span>}
      </span>
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
    </header>
  );
}
