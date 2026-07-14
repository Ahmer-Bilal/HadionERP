import { useEffect, useState } from "react";
import "./App.css";
import { ShellBar, NavigationPane } from "@platform/ui";
import type { LanguageCode, NavModule } from "@platform/ui";
import { SystemStatusPage } from "./pages/SystemStatusPage";
import { HomePage } from "./pages/HomePage";
import { BusinessPartnersPage } from "./pages/BusinessPartnersPage";
import { GLAccountsPage } from "./pages/GLAccountsPage";
import { ItemsPage } from "./pages/ItemsPage";
import { CostCentersPage } from "./pages/CostCentersPage";
import { TaxCodesPage } from "./pages/TaxCodesPage";
import { directionFor, type SupportedLanguageCode } from "./i18n/language";
import { t } from "./i18n/content";
import { LANGUAGE_NAMES } from "./i18n/languageNames";

// Which page a nav item's #anchor selects. No router library yet — deliberately deferred until a THIRD
// navigable screen exists (two is easy to hand-wire; see Platform.UI/README.md for the same
// "extract once a second/third real consumer proves the shape" philosophy applied to components).
type PageKey = "home" | "system-status" | "business-partners" | "gl-accounts" | "items" | "cost-centers" | "tax-codes";

function currentPageFromHash(): PageKey {
  if (window.location.hash === "#system-status") return "system-status";
  if (window.location.hash === "#business-partners") return "business-partners";
  if (window.location.hash === "#gl-accounts") return "gl-accounts";
  if (window.location.hash === "#items") return "items";
  if (window.location.hash === "#cost-centers") return "cost-centers";
  if (window.location.hash === "#tax-codes") return "tax-codes";
  return "home";
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
      key: "home",
      label: t("nav.homeModule", language),
      areas: [
        {
          key: "home-overview",
          label: t("nav.homeArea", language),
          items: [
            {
              key: "home",
              label: t("nav.home", language),
              href: "#home",
              isActive: page === "home",
            },
          ],
        },
      ],
    },
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
        {
          key: "chart-of-accounts",
          label: t("nav.chartOfAccountsArea", language),
          items: [
            {
              key: "all-gl-accounts",
              label: t("nav.allGLAccounts", language),
              href: "#gl-accounts",
              isActive: page === "gl-accounts",
            },
          ],
        },
        {
          key: "items",
          label: t("nav.itemsArea", language),
          items: [
            {
              key: "all-items",
              label: t("nav.allItems", language),
              href: "#items",
              isActive: page === "items",
            },
          ],
        },
        {
          key: "cost-centers",
          label: t("nav.costCentersArea", language),
          items: [
            {
              key: "all-cost-centers",
              label: t("nav.allCostCenters", language),
              href: "#cost-centers",
              isActive: page === "cost-centers",
            },
          ],
        },
        {
          key: "tax-codes",
          label: t("nav.taxCodesArea", language),
          items: [
            {
              key: "all-tax-codes",
              label: t("nav.allTaxCodes", language),
              href: "#tax-codes",
              isActive: page === "tax-codes",
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
          ) : page === "gl-accounts" ? (
            <GLAccountsPage language={language} />
          ) : page === "items" ? (
            <ItemsPage language={language} />
          ) : page === "cost-centers" ? (
            <CostCentersPage language={language} />
          ) : page === "tax-codes" ? (
            <TaxCodesPage language={language} />
          ) : page === "system-status" ? (
            <SystemStatusPage language={language} />
          ) : (
            <HomePage language={language} />
          )}
        </main>
      </div>
      <footer className="app-shell__footer">{t("shell.footer", language)}</footer>
    </div>
  );
}

export default App;
