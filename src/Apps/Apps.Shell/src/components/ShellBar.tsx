import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { LANGUAGE_NAMES } from "../i18n/languageNames";

interface ShellBarProps {
  language: SupportedLanguageCode;
  onLanguageChange: (language: SupportedLanguageCode) => void;
}

export function ShellBar({ language, onLanguageChange }: ShellBarProps) {
  return (
    <header className="shell-bar">
      <span className="shell-bar__title">{t("shell.title", language)}</span>
      <div className="shell-bar__language-switch" role="group" aria-label="Language">
        <button
          type="button"
          className={language === "en" ? "is-active" : ""}
          onClick={() => onLanguageChange("en")}
        >
          {LANGUAGE_NAMES.en}
        </button>
        <button
          type="button"
          className={language === "ar" ? "is-active" : ""}
          onClick={() => onLanguageChange("ar")}
        >
          {LANGUAGE_NAMES.ar}
        </button>
      </div>
    </header>
  );
}
