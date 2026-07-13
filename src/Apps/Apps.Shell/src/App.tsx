import { useEffect, useState } from "react";
import "./App.css";
import { ShellBar, NavigationPane } from "@platform/ui";
import type { LanguageCode, NavModule } from "@platform/ui";
import { SystemStatusPage } from "./pages/SystemStatusPage";
import { directionFor, type SupportedLanguageCode } from "./i18n/language";
import { t } from "./i18n/content";
import { LANGUAGE_NAMES } from "./i18n/languageNames";

function App() {
  const [language, setLanguage] = useState<SupportedLanguageCode>("en");
  const direction = directionFor(language);

  useEffect(() => {
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
  }, [direction, language]);

  // The navigation tree, data-driven per docs/architecture/02-business-object-model.md #3. A new business
  // module adds its own entry here as data — Platform.UI's NavigationPane renders whatever structure it's
  // given. Labels are resolved through t() so they localize with the rest of the shell.
  const navModules: NavModule[] = [
    {
      key: "platform-administration",
      label: t("nav.platformAdministration", language),
      areas: [
        {
          key: "system",
          label: t("nav.system", language),
          items: [
            { key: "system-status", label: t("nav.systemStatus", language), href: "#system-status", isActive: true },
          ],
        },
      ],
    },
  ];

  const languageOptions = [
    { code: "en" as LanguageCode, label: LANGUAGE_NAMES.en },
    { code: "ar" as LanguageCode, label: LANGUAGE_NAMES.ar },
  ];

  return (
    <div className="app-shell">
      <ShellBar
        title={t("shell.title", language)}
        languages={languageOptions}
        activeLanguage={language}
        onLanguageChange={setLanguage}
        languageSwitchLabel={t("aria.languageSwitchGroup", language)}
      />
      <div className="app-shell__body">
        <NavigationPane
          modules={navModules}
          ariaLabel={t("aria.navigationLandmark", language)}
        />
        <main className="app-shell__content">
          <SystemStatusPage language={language} />
        </main>
      </div>
    </div>
  );
}

export default App;
