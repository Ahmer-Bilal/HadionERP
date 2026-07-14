import { useEffect, useState } from "react";
import "./App.css";
import { ShellBar, NavigationPane } from "@platform/ui";
import type { LanguageCode, NavModule } from "@platform/ui";
import { SystemStatusPage } from "./pages/SystemStatusPage";
import { BusinessPartnersPage } from "./pages/BusinessPartnersPage";
import { directionFor, type SupportedLanguageCode } from "./i18n/language";
import { t } from "./i18n/content";
import { LANGUAGE_NAMES } from "./i18n/languageNames";

// Which page a nav item's #anchor selects. No router library yet — deliberately deferred until a THIRD
// navigable screen exists (two is easy to hand-wire; see Platform.UI/README.md for the same
// "extract once a second/third real consumer proves the shape" philosophy applied to components).
type PageKey = "system-status" | "business-partners";

function currentPageFromHash(): PageKey {
  return window.location.hash === "#business-partners" ? "business-partners" : "system-status";
}

function App() {
  const [language, setLanguage] = useState<SupportedLanguageCode>("en");
  const [page, setPage] = useState<PageKey>(currentPageFromHash);
  const direction = directionFor(language);

  useEffect(() => {
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
  }, [direction, language]);

  useEffect(() => {
    const onHashChange = () => setPage(currentPageFromHash());
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);

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
            {
              key: "system-status",
              label: t("nav.systemStatus", language),
              href: "#system-status",
              isActive: page === "system-status",
            },
          ],
        },
      ],
    },
    {
      key: "master-data",
      label: t("nav.masterData", language),
      areas: [
        {
          key: "business-partners",
          label: t("nav.businessPartnersArea", language),
          items: [
            {
              key: "all-business-partners",
              label: t("nav.allBusinessPartners", language),
              href: "#business-partners",
              isActive: page === "business-partners",
            },
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
        tagline={t("shell.tagline", language)}
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
          {page === "business-partners" ? (
            <BusinessPartnersPage language={language} />
          ) : (
            <SystemStatusPage language={language} />
          )}
        </main>
      </div>
      <footer className="app-shell__footer">{t("shell.footer", language)}</footer>
    </div>
  );
}

export default App;
