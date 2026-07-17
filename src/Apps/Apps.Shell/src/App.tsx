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
import { JournalEntriesPage } from "./pages/JournalEntriesPage";
import { APInvoicesPage } from "./pages/APInvoicesPage";
import { BankAccountsPage } from "./pages/BankAccountsPage";
import { PaymentsPage } from "./pages/PaymentsPage";
import { VendorPrequalificationsPage } from "./pages/VendorPrequalificationsPage";
import { PurchaseRequisitionsPage } from "./pages/PurchaseRequisitionsPage";
import { RequestsForQuotationPage } from "./pages/RequestsForQuotationPage";
import { PurchaseOrdersPage } from "./pages/PurchaseOrdersPage";
import { GoodsReceiptNotesPage } from "./pages/GoodsReceiptNotesPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { ContractsPage } from "./pages/ContractsPage";
import { SubcontractsPage } from "./pages/SubcontractsPage";
import { MeasurementSheetsPage } from "./pages/MeasurementSheetsPage";
import { LookupDataPage } from "./pages/LookupDataPage";
import { UsersPage } from "./pages/UsersPage";
import { LoginPage } from "./pages/LoginPage";
import { useAuth } from "./AuthContext";
import { directionFor, type SupportedLanguageCode } from "./i18n/language";
import { t } from "./i18n/content";
import { LANGUAGE_NAMES } from "./i18n/languageNames";

// Which page a nav item's #anchor selects. No router library yet — deliberately deferred until a THIRD
// navigable screen exists (two is easy to hand-wire; see docs/architecture/02-business-object-model.md for the same
// "extract once a second/third real consumer proves the shape" philosophy applied to components).
type PageKey = "home" | "system-status" | "business-partners" | "gl-accounts" | "items" | "cost-centers" | "tax-codes" | "journal-entries" | "ap-invoices" | "bank-accounts" | "payments" | "vendor-prequalifications" | "purchase-requisitions" | "requests-for-quotation" | "purchase-orders" | "goods-receipt-notes" | "projects" | "contracts" | "subcontracts" | "measurement-sheets" | "lookup-data" | "lookup-country" | "lookup-business-role-type" | "lookup-address-type" | "lookup-unit-of-measure" | "lookup-subcontractor-trade" | "lookup-supplier-trade" | "lookup-consultant-trade" | "users";

function currentPageFromHash(): PageKey {
  if (window.location.hash === "#system-status") return "system-status";
  if (window.location.hash === "#business-partners") return "business-partners";
  if (window.location.hash === "#gl-accounts") return "gl-accounts";
  if (window.location.hash === "#items") return "items";
  if (window.location.hash === "#cost-centers") return "cost-centers";
  if (window.location.hash === "#tax-codes") return "tax-codes";
  if (window.location.hash === "#journal-entries") return "journal-entries";
  if (window.location.hash === "#ap-invoices") return "ap-invoices";
  if (window.location.hash === "#bank-accounts") return "bank-accounts";
  if (window.location.hash === "#payments") return "payments";
  if (window.location.hash === "#vendor-prequalifications") return "vendor-prequalifications";
  if (window.location.hash === "#purchase-requisitions") return "purchase-requisitions";
  if (window.location.hash === "#requests-for-quotation") return "requests-for-quotation";
  if (window.location.hash === "#purchase-orders") return "purchase-orders";
  if (window.location.hash === "#goods-receipt-notes") return "goods-receipt-notes";
  if (window.location.hash === "#projects") return "projects";
  if (window.location.hash === "#contracts") return "contracts";
  if (window.location.hash === "#subcontracts") return "subcontracts";
  if (window.location.hash === "#measurement-sheets") return "measurement-sheets";
  if (window.location.hash === "#lookup-data") return "lookup-data";
  if (window.location.hash === "#lookup-country") return "lookup-country";
  if (window.location.hash === "#lookup-business-role-type") return "lookup-business-role-type";
  if (window.location.hash === "#lookup-address-type") return "lookup-address-type";
  if (window.location.hash === "#lookup-unit-of-measure") return "lookup-unit-of-measure";
  if (window.location.hash === "#lookup-subcontractor-trade") return "lookup-subcontractor-trade";
  if (window.location.hash === "#lookup-supplier-trade") return "lookup-supplier-trade";
  if (window.location.hash === "#lookup-consultant-trade") return "lookup-consultant-trade";
  if (window.location.hash === "#users") return "users";
  return "home";
}

function App() {
  const [language, setLanguage] = useState<SupportedLanguageCode>("en");
  const [page, setPage] = useState<PageKey>(currentPageFromHash);
  const direction = directionFor(language);
  const { user, isLoading, logout } = useAuth();

  useEffect(() => {
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
  }, [direction, language]);

  useEffect(() => {
    const onHashChange = () => setPage(currentPageFromHash());
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);

  // Real authentication (MISSING-FEATURES-AUDIT.md Part 1 §1) — nothing past this point renders until a real
  // session exists. isLoading covers the brief /auth/me confirmation on first load (see AuthContext);
  // rendering nothing rather than a flash of the login form avoids a jarring flicker for an already-valid
  // session surviving a reload.
  if (isLoading) return null;
  if (!user) return <LoginPage language={language} />;

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
        {
          key: "lookup-data",
          label: t("nav.lookupDataArea", language),
          items: [
            {
              key: "all-lookup-types",
              label: t("nav.allLookupTypes", language),
              href: "#lookup-data",
              isActive: page === "lookup-data",
            },
            {
              key: "lookup-countries",
              label: t("nav.lookupCountries", language),
              href: "#lookup-country",
              isActive: page === "lookup-country",
            },
            {
              key: "lookup-business-role-types",
              label: t("nav.lookupBusinessRoleTypes", language),
              href: "#lookup-business-role-type",
              isActive: page === "lookup-business-role-type",
            },
            {
              key: "lookup-address-types",
              label: t("nav.lookupAddressTypes", language),
              href: "#lookup-address-type",
              isActive: page === "lookup-address-type",
            },
            {
              key: "lookup-units-of-measure",
              label: t("nav.lookupUnitsOfMeasure", language),
              href: "#lookup-unit-of-measure",
              isActive: page === "lookup-unit-of-measure",
            },
            {
              key: "lookup-subcontractor-trades",
              label: t("nav.lookupSubcontractorTrades", language),
              href: "#lookup-subcontractor-trade",
              isActive: page === "lookup-subcontractor-trade",
            },
            {
              key: "lookup-supplier-trades",
              label: t("nav.lookupSupplierTrades", language),
              href: "#lookup-supplier-trade",
              isActive: page === "lookup-supplier-trade",
            },
            {
              key: "lookup-consultant-trades",
              label: t("nav.lookupConsultantTrades", language),
              href: "#lookup-consultant-trade",
              isActive: page === "lookup-consultant-trade",
            },
          ],
        },
        {
          key: "users",
          label: t("nav.usersArea", language),
          items: [
            {
              key: "all-users",
              label: t("nav.allUsers", language),
              href: "#users",
              isActive: page === "users",
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
    {
      key: "finance",
      label: t("nav.financeModule", language),
      areas: [
        {
          key: "journal-entries",
          label: t("nav.journalEntriesArea", language),
          items: [
            {
              key: "all-journal-entries",
              label: t("nav.allJournalEntries", language),
              href: "#journal-entries",
              isActive: page === "journal-entries",
            },
          ],
        },
        {
          key: "ap-invoices",
          label: t("nav.apInvoicesArea", language),
          items: [
            {
              key: "all-ap-invoices",
              label: t("nav.allAPInvoices", language),
              href: "#ap-invoices",
              isActive: page === "ap-invoices",
            },
          ],
        },
        {
          key: "bank-accounts",
          label: t("nav.bankAccountsArea", language),
          items: [
            {
              key: "all-bank-accounts",
              label: t("nav.allBankAccounts", language),
              href: "#bank-accounts",
              isActive: page === "bank-accounts",
            },
          ],
        },
        {
          key: "payments",
          label: t("nav.paymentsArea", language),
          items: [
            {
              key: "all-payments",
              label: t("nav.allPayments", language),
              href: "#payments",
              isActive: page === "payments",
            },
          ],
        },
      ],
    },
    {
      key: "procurement",
      label: t("nav.procurementModule", language),
      areas: [
        {
          key: "vendor-prequalifications",
          label: t("nav.vendorPrequalificationsArea", language),
          items: [
            {
              key: "all-vendor-prequalifications",
              label: t("nav.allVendorPrequalifications", language),
              href: "#vendor-prequalifications",
              isActive: page === "vendor-prequalifications",
            },
          ],
        },
        {
          key: "purchase-requisitions",
          label: t("nav.purchaseRequisitionsArea", language),
          items: [
            {
              key: "all-purchase-requisitions",
              label: t("nav.allPurchaseRequisitions", language),
              href: "#purchase-requisitions",
              isActive: page === "purchase-requisitions",
            },
          ],
        },
        {
          key: "requests-for-quotation",
          label: t("nav.requestsForQuotationArea", language),
          items: [
            {
              key: "all-requests-for-quotation",
              label: t("nav.allRequestsForQuotation", language),
              href: "#requests-for-quotation",
              isActive: page === "requests-for-quotation",
            },
          ],
        },
        {
          key: "purchase-orders",
          label: t("nav.purchaseOrdersArea", language),
          items: [
            {
              key: "all-purchase-orders",
              label: t("nav.allPurchaseOrders", language),
              href: "#purchase-orders",
              isActive: page === "purchase-orders",
            },
          ],
        },
        {
          key: "goods-receipt-notes",
          label: t("nav.goodsReceiptNotesArea", language),
          items: [
            {
              key: "all-goods-receipt-notes",
              label: t("nav.allGoodsReceiptNotes", language),
              href: "#goods-receipt-notes",
              isActive: page === "goods-receipt-notes",
            },
          ],
        },
      ],
    },
    {
      key: "project-management",
      label: t("nav.projectManagementModule", language),
      areas: [
        {
          key: "projects",
          label: t("nav.projectsArea", language),
          items: [
            {
              key: "all-projects",
              label: t("nav.allProjects", language),
              href: "#projects",
              isActive: page === "projects",
            },
          ],
        },
      ],
    },
    {
      key: "construction",
      label: t("nav.constructionModule", language),
      areas: [
        {
          key: "contracts",
          label: t("nav.contractsArea", language),
          items: [
            {
              key: "all-contracts",
              label: t("nav.allContracts", language),
              href: "#contracts",
              isActive: page === "contracts",
            },
          ],
        },
        {
          key: "subcontracts",
          label: t("nav.subcontractsArea", language),
          items: [
            {
              key: "all-subcontracts",
              label: t("nav.allSubcontracts", language),
              href: "#subcontracts",
              isActive: page === "subcontracts",
            },
          ],
        },
        {
          key: "measurement-sheets",
          label: t("nav.measurementSheetsArea", language),
          items: [
            {
              key: "all-measurement-sheets",
              label: t("nav.allMeasurementSheets", language),
              href: "#measurement-sheets",
              isActive: page === "measurement-sheets",
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
        currentUserLabel={t("auth.loggedInAs", language).replace("{username}", user.displayName)}
        onLogout={logout}
        logoutLabel={t("auth.logoutButton", language)}
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
          ) : page === "journal-entries" ? (
            <JournalEntriesPage language={language} />
          ) : page === "ap-invoices" ? (
            <APInvoicesPage language={language} />
          ) : page === "bank-accounts" ? (
            <BankAccountsPage language={language} />
          ) : page === "payments" ? (
            <PaymentsPage language={language} />
          ) : page === "vendor-prequalifications" ? (
            <VendorPrequalificationsPage language={language} />
          ) : page === "purchase-requisitions" ? (
            <PurchaseRequisitionsPage language={language} />
          ) : page === "requests-for-quotation" ? (
            <RequestsForQuotationPage language={language} />
          ) : page === "purchase-orders" ? (
            <PurchaseOrdersPage language={language} />
          ) : page === "goods-receipt-notes" ? (
            <GoodsReceiptNotesPage language={language} />
          ) : page === "projects" ? (
            <ProjectsPage language={language} />
          ) : page === "contracts" ? (
            <ContractsPage language={language} />
          ) : page === "subcontracts" ? (
            <SubcontractsPage language={language} />
          ) : page === "measurement-sheets" ? (
            <MeasurementSheetsPage language={language} />
          ) : page === "lookup-data" ? (
            <LookupDataPage language={language} />
          ) : page === "lookup-country" ? (
            <LookupDataPage language={language} initialTypeCode="Country" />
          ) : page === "lookup-business-role-type" ? (
            <LookupDataPage language={language} initialTypeCode="BusinessRoleType" />
          ) : page === "lookup-address-type" ? (
            <LookupDataPage language={language} initialTypeCode="AddressType" />
          ) : page === "lookup-unit-of-measure" ? (
            <LookupDataPage language={language} initialTypeCode="UnitOfMeasure" />
          ) : page === "lookup-subcontractor-trade" ? (
            <LookupDataPage language={language} initialTypeCode="SubcontractorTrade" />
          ) : page === "lookup-supplier-trade" ? (
            <LookupDataPage language={language} initialTypeCode="SupplierTrade" />
          ) : page === "lookup-consultant-trade" ? (
            <LookupDataPage language={language} initialTypeCode="ConsultantTrade" />
          ) : page === "users" ? (
            <UsersPage language={language} />
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
