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
import { ARInvoicesPage } from "./pages/ARInvoicesPage";
import { BankAccountsPage } from "./pages/BankAccountsPage";
import { PaymentsPage } from "./pages/PaymentsPage";
import { CustomerReceiptsPage } from "./pages/CustomerReceiptsPage";
import { TrialBalancePage } from "./pages/TrialBalancePage";
import { IncomeStatementPage } from "./pages/IncomeStatementPage";
import { BalanceSheetPage } from "./pages/BalanceSheetPage";
import { BudgetsPage } from "./pages/BudgetsPage";
import { PeriodClosingCenterPage } from "./pages/PeriodClosingCenterPage";
import { VariationOrdersPage } from "./pages/VariationOrdersPage";
import { RetentionReleasesPage } from "./pages/RetentionReleasesPage";
import { VendorPrequalificationsPage } from "./pages/VendorPrequalificationsPage";
import { PurchaseRequisitionsPage } from "./pages/PurchaseRequisitionsPage";
import { RequestsForQuotationPage } from "./pages/RequestsForQuotationPage";
import { PurchaseOrdersPage } from "./pages/PurchaseOrdersPage";
import { GoodsReceiptNotesPage } from "./pages/GoodsReceiptNotesPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { ContractsPage } from "./pages/ContractsPage";
import { SubcontractsPage } from "./pages/SubcontractsPage";
import { MeasurementSheetsPage } from "./pages/MeasurementSheetsPage";
import { IpcsPage } from "./pages/IpcsPage";
import { LookupDataPage } from "./pages/LookupDataPage";
import { UsersPage } from "./pages/UsersPage";
import { ComingSoonPage } from "./pages/ComingSoonPage";
import { DepartmentLandingPage } from "./pages/DepartmentLandingPage";
import { DepartmentActivityPage } from "./pages/DepartmentActivityPage";
import { useDepartmentActivity } from "./useDepartmentActivity";
import { LoginPage } from "./pages/LoginPage";
import { useAuth } from "./AuthContext";
import { directionFor, type SupportedLanguageCode } from "./i18n/language";
import { t } from "./i18n/content";
import { LANGUAGE_NAMES } from "./i18n/languageNames";

// Which page a nav item's #anchor selects. No router library yet — deliberately deferred until a THIRD
// navigable screen exists (two is easy to hand-wire; see docs/architecture/02-business-object-model.md for the same
// "extract once a second/third real consumer proves the shape" philosophy applied to components).
type PageKey = "home" | "system-status" | "business-partners" | "gl-accounts" | "items" | "cost-centers" | "tax-codes" | "journal-entries" | "ap-invoices" | "ar-invoices" | "bank-accounts" | "payments" | "customer-receipts" | "trial-balance" | "income-statement" | "balance-sheet" | "budgets" | "period-closing-center" | "vendor-prequalifications" | "purchase-requisitions" | "requests-for-quotation" | "purchase-orders" | "goods-receipt-notes" | "projects" | "contracts" | "subcontracts" | "measurement-sheets" | "ipcs" | "variation-orders" | "retention-releases" | "lookup-data" | "lookup-country" | "lookup-business-role-type" | "lookup-address-type" | "lookup-unit-of-measure" | "lookup-subcontractor-trade" | "lookup-supplier-trade" | "lookup-consultant-trade" | "users" | "inventory" | "hr-payroll" | "equipment" | "crm" | "dept-platform-administration" | "dept-master-data" | "dept-finance" | "dept-procurement" | "dept-construction" | "activity-master-data" | "activity-finance" | "activity-procurement" | "activity-construction" | "activity-project-management";

function currentPageFromHash(): PageKey {
  if (window.location.hash === "#system-status") return "system-status";
  if (window.location.hash === "#business-partners") return "business-partners";
  if (window.location.hash === "#gl-accounts") return "gl-accounts";
  if (window.location.hash === "#items") return "items";
  if (window.location.hash === "#cost-centers") return "cost-centers";
  if (window.location.hash === "#tax-codes") return "tax-codes";
  if (window.location.hash === "#journal-entries") return "journal-entries";
  if (window.location.hash === "#ap-invoices") return "ap-invoices";
  if (window.location.hash === "#ar-invoices") return "ar-invoices";
  if (window.location.hash === "#bank-accounts") return "bank-accounts";
  if (window.location.hash === "#payments") return "payments";
  if (window.location.hash === "#customer-receipts") return "customer-receipts";
  if (window.location.hash === "#trial-balance") return "trial-balance";
  if (window.location.hash === "#income-statement") return "income-statement";
  if (window.location.hash === "#balance-sheet") return "balance-sheet";
  if (window.location.hash === "#budgets") return "budgets";
  if (window.location.hash === "#period-closing-center") return "period-closing-center";
  if (window.location.hash === "#vendor-prequalifications") return "vendor-prequalifications";
  if (window.location.hash === "#purchase-requisitions") return "purchase-requisitions";
  if (window.location.hash === "#requests-for-quotation") return "requests-for-quotation";
  if (window.location.hash === "#purchase-orders") return "purchase-orders";
  if (window.location.hash === "#goods-receipt-notes") return "goods-receipt-notes";
  if (window.location.hash === "#projects") return "projects";
  if (window.location.hash === "#contracts") return "contracts";
  if (window.location.hash === "#subcontracts") return "subcontracts";
  if (window.location.hash === "#measurement-sheets") return "measurement-sheets";
  if (window.location.hash === "#ipcs") return "ipcs";
  if (window.location.hash === "#variation-orders") return "variation-orders";
  if (window.location.hash === "#retention-releases") return "retention-releases";
  if (window.location.hash === "#lookup-data") return "lookup-data";
  if (window.location.hash === "#lookup-country") return "lookup-country";
  if (window.location.hash === "#lookup-business-role-type") return "lookup-business-role-type";
  if (window.location.hash === "#lookup-address-type") return "lookup-address-type";
  if (window.location.hash === "#lookup-unit-of-measure") return "lookup-unit-of-measure";
  if (window.location.hash === "#lookup-subcontractor-trade") return "lookup-subcontractor-trade";
  if (window.location.hash === "#lookup-supplier-trade") return "lookup-supplier-trade";
  if (window.location.hash === "#lookup-consultant-trade") return "lookup-consultant-trade";
  if (window.location.hash === "#users") return "users";
  if (window.location.hash === "#inventory") return "inventory";
  if (window.location.hash === "#hr-payroll") return "hr-payroll";
  if (window.location.hash === "#equipment") return "equipment";
  if (window.location.hash === "#crm") return "crm";
  if (window.location.hash === "#dept-platform-administration") return "dept-platform-administration";
  if (window.location.hash === "#dept-master-data") return "dept-master-data";
  if (window.location.hash === "#dept-finance") return "dept-finance";
  if (window.location.hash === "#dept-procurement") return "dept-procurement";
  if (window.location.hash === "#dept-construction") return "dept-construction";
  if (window.location.hash === "#activity-master-data") return "activity-master-data";
  if (window.location.hash === "#activity-finance") return "activity-finance";
  if (window.location.hash === "#activity-procurement") return "activity-procurement";
  if (window.location.hash === "#activity-construction") return "activity-construction";
  if (window.location.hash === "#activity-project-management") return "activity-project-management";
  return "home";
}

/** Which module (by key) the current page belongs to — null on the Dashboard, where no department is
 * current. Single source of truth for NavigationPane's `currentModuleKey` prop and (via the loop below)
 * findBreadcrumb's own module lookup — matches on a `#dept-<key>` landing page, a `#activity-<key>`
 * approvals/submitted page, or an active item nested under that module's areas. */
function findCurrentModuleKey(modules: NavModule[], page: PageKey): string | null {
  for (const module of modules) {
    if (module.key === "home") continue;
    if (page === `dept-${module.key}` || page === `activity-${module.key}`) return module.key;
    if (module.areas.some((area) => area.items.some((item) => item.isActive))) return module.key;
  }
  return null;
}

/** The current page's location for ShellBar's breadcrumb — walks the same `navModules` tree the
 * NavigationPane renders (built fresh every render already) to find whichever item is `isActive`, rather
 * than maintaining a second, parallel page->label lookup that could drift out of sync with it. */
function findBreadcrumb(
  modules: NavModule[],
  page: PageKey,
  homeLabel: string,
): { label: string; href?: string }[] {
  if (page === "home") return [{ label: homeLabel }];
  for (const module of modules) {
    if (module.key === "home") continue;
    // A dept-* landing page or an activity-* page IS the destination — no "active item" underneath it to
    // find, same one-segment breadcrumb shape as the "home" case above.
    if (page === `dept-${module.key}` || page === `activity-${module.key}`) return [{ label: module.label }];
    for (const area of module.areas) {
      for (const item of area.items) {
        if (item.isActive) {
          return [{ label: module.label, href: module.href }, { label: item.label }];
        }
      }
    }
  }
  return [{ label: homeLabel }];
}

function App() {
  const [language, setLanguage] = useState<SupportedLanguageCode>("en");
  const [page, setPage] = useState<PageKey>(currentPageFromHash);
  const [isNavCollapsed, setIsNavCollapsed] = useState(false);
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

  const languageOptions = [
    { code: "en" as LanguageCode, label: LANGUAGE_NAMES.en },
    { code: "ar" as LanguageCode, label: LANGUAGE_NAMES.ar },
  ];

  // The navigation tree, data-driven per docs/architecture/02-business-object-model.md #3. A new business
  // module adds its own entry here as data — Platform.UI's NavigationPane renders whatever structure it's
  // given. Labels are resolved through t() so they localize with the rest of the shell. Built (and the
  // useDepartmentActivity hook below called) unconditionally, ahead of the auth early-return further down —
  // neither depends on `user` being non-null (useDepartmentActivity itself tolerates a null user), and
  // React's rules of hooks require every hook to run in the same order on every render, which an early
  // return placed before this call would violate the moment isLoading/user ever changes.
  const navModules: NavModule[] = [
    {
      key: "home",
      label: t("nav.homeModule", language),
      icon: "home",
      href: "#home",
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
      icon: "platform-administration",
      // A module's own href: more than one item below it → its #dept-<key> tile-grid landing page
      // (DepartmentLandingPage.tsx); exactly one item → that item's own href directly, no pointless
      // one-tile landing page in between. Applied the same way to every module below.
      href: "#dept-platform-administration",
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
      icon: "master-data",
      href: "#dept-master-data",
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
      ],
    },
    {
      key: "finance",
      label: t("nav.financeModule", language),
      icon: "finance",
      href: "#dept-finance",
      areas: [
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
          key: "ar-invoices",
          label: t("nav.arInvoicesArea", language),
          items: [
            {
              key: "all-ar-invoices",
              label: t("nav.allARInvoices", language),
              href: "#ar-invoices",
              isActive: page === "ar-invoices",
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
        {
          key: "customer-receipts",
          label: t("nav.customerReceiptsArea", language),
          items: [
            {
              key: "all-customer-receipts",
              label: t("nav.allCustomerReceipts", language),
              href: "#customer-receipts",
              isActive: page === "customer-receipts",
            },
          ],
        },
        {
          key: "financial-statements",
          label: t("nav.financialStatementsArea", language),
          items: [
            {
              key: "trial-balance",
              label: t("nav.trialBalance", language),
              href: "#trial-balance",
              isActive: page === "trial-balance",
            },
            {
              key: "income-statement",
              label: t("nav.incomeStatement", language),
              href: "#income-statement",
              isActive: page === "income-statement",
            },
            {
              key: "balance-sheet",
              label: t("nav.balanceSheet", language),
              href: "#balance-sheet",
              isActive: page === "balance-sheet",
            },
          ],
        },
        {
          key: "budgets",
          label: t("nav.budgetsArea", language),
          items: [
            {
              key: "all-budgets",
              label: t("nav.allBudgets", language),
              href: "#budgets",
              isActive: page === "budgets",
            },
          ],
        },
        {
          key: "period-closing",
          label: t("nav.periodClosingArea", language),
          items: [
            {
              key: "period-closing-center",
              label: t("nav.periodClosingCenter", language),
              href: "#period-closing-center",
              isActive: page === "period-closing-center",
            },
          ],
        },
      ],
    },
    {
      key: "procurement",
      label: t("nav.procurementModule", language),
      icon: "procurement",
      href: "#dept-procurement",
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
      icon: "project-management",
      href: "#projects", // single-item module — links straight to its one real page
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
      icon: "construction",
      href: "#dept-construction",
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
        {
          key: "ipcs",
          label: t("nav.ipcsArea", language),
          items: [
            {
              key: "all-ipcs",
              label: t("nav.allIpcs", language),
              href: "#ipcs",
              isActive: page === "ipcs",
            },
          ],
        },
        {
          key: "variation-orders",
          label: t("vo.heading", language),
          items: [
            {
              key: "all-variation-orders",
              label: t("vo.heading", language),
              href: "#variation-orders",
              isActive: page === "variation-orders",
            },
          ],
        },
        {
          key: "retention-releases",
          label: t("nav.retentionReleasesArea", language),
          items: [
            {
              key: "all-retention-releases",
              label: t("nav.allRetentionReleases", language),
              href: "#retention-releases",
              isActive: page === "retention-releases",
            },
          ],
        },
      ],
    },
    // Inventory/HR & Payroll/Equipment/CRM: on the roadmap (ROADMAP.md's Checkpoint section and Phase 4)
    // and already shown as sibling departments in every UI/Finance mockup's own sidebar, but not built yet
    // — each is a real nav entry landing on ComingSoonPage (see that component's own doc comment) rather
    // than a dead link or a page with fabricated data.
    {
      key: "inventory",
      label: t("nav.inventoryModule", language),
      icon: "inventory",
      href: "#inventory",
      areas: [
        {
          key: "inventory-overview",
          label: t("nav.inventoryModule", language),
          items: [
            {
              key: "inventory-coming-soon",
              label: t("nav.overviewItem", language),
              href: "#inventory",
              isActive: page === "inventory",
            },
          ],
        },
      ],
    },
    {
      key: "hr-payroll",
      label: t("nav.hrPayrollModule", language),
      icon: "hr-payroll",
      href: "#hr-payroll",
      areas: [
        {
          key: "hr-payroll-overview",
          label: t("nav.hrPayrollModule", language),
          items: [
            {
              key: "hr-payroll-coming-soon",
              label: t("nav.overviewItem", language),
              href: "#hr-payroll",
              isActive: page === "hr-payroll",
            },
          ],
        },
      ],
    },
    {
      key: "equipment",
      label: t("nav.equipmentModule", language),
      icon: "equipment",
      href: "#equipment",
      areas: [
        {
          key: "equipment-overview",
          label: t("nav.equipmentModule", language),
          items: [
            {
              key: "equipment-coming-soon",
              label: t("nav.overviewItem", language),
              href: "#equipment",
              isActive: page === "equipment",
            },
          ],
        },
      ],
    },
    {
      key: "crm",
      label: t("nav.crmModule", language),
      icon: "crm",
      href: "#crm",
      areas: [
        {
          key: "crm-overview",
          label: t("nav.crmModule", language),
          items: [
            {
              key: "crm-coming-soon",
              label: t("nav.overviewItem", language),
              href: "#crm",
              isActive: page === "crm",
            },
          ],
        },
      ],
    },
  ];

  const currentModuleKey = findCurrentModuleKey(navModules, page);
  const { approvals, submitted } = useDepartmentActivity(currentModuleKey, user, language);

  // Real authentication (MISSING-FEATURES-AUDIT.md Part 1 §1) — nothing past this point renders until a real
  // session exists. isLoading covers the brief /auth/me confirmation on first load (see AuthContext);
  // rendering nothing rather than a flash of the login form avoids a jarring flicker for an already-valid
  // session surviving a reload.
  if (isLoading) return null;
  if (!user) return <LoginPage language={language} languages={languageOptions} onLanguageChange={setLanguage} />;

  const activity = currentModuleKey
    ? {
        approvals: {
          key: "approvals",
          label: t("nav.approvals", language),
          href: `#activity-${currentModuleKey}`,
          isActive: page === `activity-${currentModuleKey}`,
          count: approvals.length,
        },
        submitted: {
          key: "submitted",
          label: t("nav.submitted", language),
          href: `#activity-${currentModuleKey}`,
          isActive: page === `activity-${currentModuleKey}`,
          count: submitted.length,
        },
      }
    : undefined;

  return (
    <div className="app-shell">
      <ShellBar
        title={t("shell.title", language)}
        isNavCollapsed={isNavCollapsed}
        onToggleNav={() => setIsNavCollapsed((collapsed) => !collapsed)}
        toggleNavLabel={t("aria.toggleNavigation", language)}
        breadcrumb={findBreadcrumb(navModules, page, t("nav.home", language))}
        searchPlaceholder={t("shell.searchPlaceholder", language)}
        languages={languageOptions}
        activeLanguage={language}
        onLanguageChange={setLanguage}
        languageSwitchLabel={t("aria.languageSwitchGroup", language)}
        notificationsLabel={t("aria.notifications", language)}
        helpLabel={t("aria.help", language)}
        currentUserLabel={user.displayName}
        onLogout={logout}
        logoutLabel={t("auth.logoutButton", language)}
      />
      <div className="app-shell__body">
        {/* dashboardItem.isActive is always false below: NavigationPane never renders while page === "home". */}
        {page !== "home" && (
          <NavigationPane
            isCollapsed={isNavCollapsed}
            workspaceLabel={t("nav.workspaceSection", language)}
            dashboardItem={{ key: "dashboard", label: t("nav.home", language), href: "#home", isActive: false }}
            activity={activity}
            modulesLabel={t("nav.modulesSection", language)}
            modules={navModules}
            currentModuleKey={findCurrentModuleKey(navModules, page)}
            ariaLabel={t("aria.navigationLandmark", language)}
          />
        )}
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
          ) : page === "ar-invoices" ? (
            <ARInvoicesPage language={language} />
          ) : page === "bank-accounts" ? (
            <BankAccountsPage language={language} />
          ) : page === "payments" ? (
            <PaymentsPage language={language} />
          ) : page === "customer-receipts" ? (
            <CustomerReceiptsPage language={language} />
          ) : page === "trial-balance" ? (
            <TrialBalancePage language={language} />
          ) : page === "income-statement" ? (
            <IncomeStatementPage language={language} />
          ) : page === "balance-sheet" ? (
            <BalanceSheetPage language={language} />
          ) : page === "budgets" ? (
            <BudgetsPage language={language} />
          ) : page === "period-closing-center" ? (
            <PeriodClosingCenterPage language={language} />
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
          ) : page === "ipcs" ? (
            <IpcsPage language={language} />
          ) : page === "variation-orders" ? (
            <VariationOrdersPage language={language} />
          ) : page === "retention-releases" ? (
            <RetentionReleasesPage language={language} />
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
          ) : page === "inventory" ? (
            <ComingSoonPage language={language} icon="inventory" title={t("nav.inventoryModule", language)} />
          ) : page === "hr-payroll" ? (
            <ComingSoonPage language={language} icon="hr-payroll" title={t("nav.hrPayrollModule", language)} />
          ) : page === "equipment" ? (
            <ComingSoonPage language={language} icon="equipment" title={t("nav.equipmentModule", language)} />
          ) : page === "crm" ? (
            <ComingSoonPage language={language} icon="crm" title={t("nav.crmModule", language)} />
          ) : page === "dept-platform-administration" ? (
            <DepartmentLandingPage language={language} module={navModules.find((m) => m.key === "platform-administration")!} />
          ) : page === "dept-master-data" ? (
            <DepartmentLandingPage language={language} module={navModules.find((m) => m.key === "master-data")!} />
          ) : page === "dept-finance" ? (
            <DepartmentLandingPage language={language} module={navModules.find((m) => m.key === "finance")!} />
          ) : page === "dept-procurement" ? (
            <DepartmentLandingPage language={language} module={navModules.find((m) => m.key === "procurement")!} />
          ) : page === "dept-construction" ? (
            <DepartmentLandingPage language={language} module={navModules.find((m) => m.key === "construction")!} />
          ) : page === "activity-master-data" ? (
            <DepartmentActivityPage language={language} module={navModules.find((m) => m.key === "master-data")!} user={user} />
          ) : page === "activity-finance" ? (
            <DepartmentActivityPage language={language} module={navModules.find((m) => m.key === "finance")!} user={user} />
          ) : page === "activity-procurement" ? (
            <DepartmentActivityPage language={language} module={navModules.find((m) => m.key === "procurement")!} user={user} />
          ) : page === "activity-construction" ? (
            <DepartmentActivityPage language={language} module={navModules.find((m) => m.key === "construction")!} user={user} />
          ) : page === "activity-project-management" ? (
            <DepartmentActivityPage language={language} module={navModules.find((m) => m.key === "project-management")!} user={user} />
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
