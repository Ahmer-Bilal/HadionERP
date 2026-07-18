import { useEffect, useState } from "react";
import { DepartmentIcon, StatCard, StatIcon } from "@platform/ui";
import type { DepartmentIconKey } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { useAuth } from "../AuthContext";
import { listBusinessPartners } from "../api/businessPartnerApi";
import { listGLAccounts } from "../api/glAccountApi";
import { listItems } from "../api/itemApi";
import { listCostCenters } from "../api/costCenterApi";
import { listTaxCodes } from "../api/taxCodeApi";
import { listJournalEntries } from "../api/journalEntryApi";
import { listAPInvoices } from "../api/apInvoiceApi";
import { listARInvoices } from "../api/arInvoiceApi";
import { listBankAccounts } from "../api/bankAccountApi";
import { listPayments } from "../api/paymentApi";
import { listCustomerReceipts } from "../api/customerReceiptApi";
import { listVendorPrequalifications } from "../api/vendorPrequalificationApi";
import { listPurchaseRequisitions } from "../api/purchaseRequisitionApi";
import { listRequestsForQuotation } from "../api/requestForQuotationApi";
import { listPurchaseOrders } from "../api/purchaseOrderApi";
import { listGoodsReceiptNotes } from "../api/goodsReceiptNoteApi";
import { listProjects } from "../api/projectApi";
import { listContracts } from "../api/contractApi";
import { listSubcontracts } from "../api/subcontractApi";
import { listMeasurementSheets } from "../api/measurementSheetApi";
import { listIpcs } from "../api/ipcApi";
import { listVariationOrders } from "../api/variationOrderApi";
import { listRetentionReleases } from "../api/retentionReleaseApi";

interface HomePageProps {
  language: SupportedLanguageCode;
}

type TitleKey =
  | "nav.masterData"
  | "nav.projectManagementModule"
  | "nav.constructionModule"
  | "nav.procurementModule"
  | "nav.financeModule"
  | "nav.inventoryModule"
  | "nav.hrPayrollModule"
  | "nav.equipmentModule"
  | "nav.crmModule";

interface TileData {
  key: string;
  icon: DepartmentIconKey;
  titleKey: TitleKey;
  href: string;
  // The "crystal glow" treatment (docs/architecture/09-navigation-and-ui-standard.md's icon consistency
  // rule, extended with the same blue/glow identity established on the login screen — see LoginPage.css's
  // orbit nodes, which use these exact same colors so the two screens read as one product). Deliberately no
  // total/pending count on the tile itself — this is a gateway into the department (matching the mockup's
  // own icon-plus-label "Explore Workspaces" tiles), not a stats card; per-document-type counts belong on
  // that document type's own list screen, not summarized (and potentially misleadingly reduced to a single
  // number) on the department's front door.
  colorStart: string;
  colorEnd: string;
  glow: string;
}

interface RecordLike {
  id: string;
  documentNumber?: string | null;
  status: string;
  createdAt: string;
}

interface RecentItem {
  id: string;
  icon: DepartmentIconKey;
  label: string;
  createdAt: string;
  href: string;
}

interface QuickAction {
  key: string;
  labelKey: "home.quickActionNewJournalEntry" | "home.quickActionNewPurchaseOrder" | "home.quickActionNewContract" | "home.quickActionNewProject";
  href: string;
}

// Real navigational shortcuts into already-built create-capable pages — lands on that page's list, same
// "click New from there" mechanism the department tiles already use, just scoped to one document type
// instead of a whole department. Not a fabricated capability, just a shortcut to an existing one.
const QUICK_ACTIONS: QuickAction[] = [
  { key: "new-journal-entry", labelKey: "home.quickActionNewJournalEntry", href: "#journal-entries" },
  { key: "new-purchase-order", labelKey: "home.quickActionNewPurchaseOrder", href: "#purchase-orders" },
  { key: "new-contract", labelKey: "home.quickActionNewContract", href: "#contracts" },
  { key: "new-project", labelKey: "home.quickActionNewProject", href: "#projects" },
];

interface QuickLink {
  key: string;
  labelKey: "home.quickLinkLookupData" | "home.quickLinkUsers" | "home.quickLinkSystemStatus";
  href: string;
}

const QUICK_LINKS: QuickLink[] = [
  { key: "lookup-data", labelKey: "home.quickLinkLookupData", href: "#lookup-data" },
  { key: "users", labelKey: "home.quickLinkUsers", href: "#users" },
  { key: "system-status", labelKey: "home.quickLinkSystemStatus", href: "#system-status" },
];

function greetingKey(): "home.goodMorning" | "home.goodAfternoon" | "home.goodEvening" {
  const hour = new Date().getHours();
  if (hour < 12) return "home.goodMorning";
  if (hour < 18) return "home.goodAfternoon";
  return "home.goodEvening";
}

function toRecent(items: RecordLike[], icon: DepartmentIconKey, href: string, nameOf?: (item: RecordLike) => string): RecentItem[] {
  return items.map((i) => ({
    id: i.id,
    icon,
    label: (nameOf?.(i)) || i.documentNumber || i.id,
    createdAt: i.createdAt,
    href,
  }));
}

export function HomePage({ language }: HomePageProps) {
  const { user } = useAuth();
  const [tiles, setTiles] = useState<TileData[] | null>(null);
  const [recent, setRecent] = useState<RecentItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const [
          partners, glAccounts, items, costCenters, taxCodes,
          journalEntries, apInvoices, arInvoices, bankAccounts, payments, customerReceipts,
          vendorPrequals, purchaseRequisitions, rfqs, purchaseOrders, grns,
          projects,
          contracts, subcontracts, measurementSheets, ipcs, variationOrders, retentionReleases,
        ] = await Promise.all([
          listBusinessPartners(200, 0), listGLAccounts(200, 0), listItems(200, 0), listCostCenters(200, 0), listTaxCodes(200, 0),
          listJournalEntries(200, 0), listAPInvoices(200, 0), listARInvoices(200, 0), listBankAccounts(200, 0), listPayments(200, 0), listCustomerReceipts(200, 0),
          listVendorPrequalifications(200, 0), listPurchaseRequisitions(200, 0), listRequestsForQuotation(200, 0), listPurchaseOrders(200, 0), listGoodsReceiptNotes(200, 0),
          listProjects(200, 0),
          listContracts(200, 0), listSubcontracts(200, 0), listMeasurementSheets(200, 0), listIpcs(200, 0), listVariationOrders(200, 0), listRetentionReleases(200, 0),
        ]);
        if (cancelled) return;

        setTiles([
          {
            key: "master-data", icon: "master-data", titleKey: "nav.masterData", href: "#dept-master-data",
            colorStart: "#e0a01f", colorEnd: "#b8790f", glow: "rgba(224, 160, 31, 0.55)",
          },
          {
            key: "project-management", icon: "project-management", titleKey: "nav.projectManagementModule", href: "#projects",
            colorStart: "#e0568f", colorEnd: "#b8306a", glow: "rgba(224, 86, 143, 0.55)",
          },
          {
            key: "construction", icon: "construction", titleKey: "nav.constructionModule", href: "#dept-construction",
            colorStart: "#7b5fe0", colorEnd: "#5a3fc0", glow: "rgba(123, 95, 224, 0.55)",
          },
          {
            key: "procurement", icon: "procurement", titleKey: "nav.procurementModule", href: "#dept-procurement",
            colorStart: "#1fa97a", colorEnd: "#0f8362", glow: "rgba(31, 169, 122, 0.55)",
          },
          {
            key: "finance", icon: "finance", titleKey: "nav.financeModule", href: "#dept-finance",
            colorStart: "#2f8fe0", colorEnd: "#1a63c4", glow: "rgba(47, 143, 224, 0.55)",
          },
          // On the roadmap (ROADMAP.md's Checkpoint section and Phase 4), not built yet — each tile is a
          // real nav entry landing on the honest ComingSoonPage (see that component's own doc comment),
          // not a dead link or fabricated data.
          {
            key: "inventory", icon: "inventory", titleKey: "nav.inventoryModule", href: "#inventory",
            colorStart: "#14b8a6", colorEnd: "#0d8a7d", glow: "rgba(20, 184, 166, 0.55)",
          },
          {
            key: "hr-payroll", icon: "hr-payroll", titleKey: "nav.hrPayrollModule", href: "#hr-payroll",
            colorStart: "#dc4444", colorEnd: "#b32e2e", glow: "rgba(220, 68, 68, 0.55)",
          },
          {
            key: "equipment", icon: "equipment", titleKey: "nav.equipmentModule", href: "#equipment",
            colorStart: "#e08a2f", colorEnd: "#b86a1a", glow: "rgba(224, 138, 47, 0.55)",
          },
          {
            key: "crm", icon: "crm", titleKey: "nav.crmModule", href: "#crm",
            colorStart: "#2fb8d9", colorEnd: "#1a8fac", glow: "rgba(47, 184, 217, 0.55)",
          },
        ]);

        const recentItems = [
          ...toRecent(partners.items, "master-data", "#business-partners", (i) => (i as unknown as { name: string }).name),
          ...toRecent(glAccounts.items, "master-data", "#gl-accounts"),
          ...toRecent(items.items, "master-data", "#items"),
          ...toRecent(costCenters.items, "master-data", "#cost-centers"),
          ...toRecent(taxCodes.items, "master-data", "#tax-codes"),
          ...toRecent(journalEntries.items, "finance", "#journal-entries"),
          ...toRecent(apInvoices.items, "finance", "#ap-invoices"),
          ...toRecent(arInvoices.items, "finance", "#ar-invoices"),
          ...toRecent(bankAccounts.items, "finance", "#bank-accounts"),
          ...toRecent(payments.items, "finance", "#payments"),
          ...toRecent(customerReceipts.items, "finance", "#customer-receipts"),
          ...toRecent(vendorPrequals.items, "procurement", "#vendor-prequalifications"),
          ...toRecent(purchaseRequisitions.items, "procurement", "#purchase-requisitions"),
          ...toRecent(rfqs.items, "procurement", "#requests-for-quotation"),
          ...toRecent(purchaseOrders.items, "procurement", "#purchase-orders"),
          ...toRecent(grns.items, "procurement", "#goods-receipt-notes"),
          ...toRecent(projects.items, "project-management", "#projects", (i) => (i as unknown as { projectName: string }).projectName),
          ...toRecent(contracts.items, "construction", "#contracts"),
          ...toRecent(subcontracts.items, "construction", "#subcontracts"),
          ...toRecent(measurementSheets.items, "construction", "#measurement-sheets"),
          ...toRecent(ipcs.items, "construction", "#ipcs"),
          ...toRecent(variationOrders.items, "construction", "#variation-orders"),
          ...toRecent(retentionReleases.items, "construction", "#retention-releases"),
        ]
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          .slice(0, 8);
        setRecent(recentItems);
      } catch {
        if (!cancelled) setError(t("status.error", language));
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [language]);

  const goTo = (href: string) => {
    window.location.hash = href;
  };

  return (
    <section className="home-page">
      <div className="home-greeting">
        <h1>
          {t(greetingKey(), language)}{user ? `, ${user.displayName}` : ""} <span aria-hidden="true">👋</span>
        </h1>
        <p>{t("home.welcomeSubtitle", language)}</p>
      </div>

      <div className="home-kpi-row">
        <StatCard label={t("home.kpiToDoLabel", language)} value={t("home.kpiComingSoon", language)} icon={<StatIcon icon="checkCircle" />} tone="var(--pi-chart-2)" />
        <StatCard label={t("home.kpiApprovalsLabel", language)} value={t("home.kpiComingSoon", language)} icon={<StatIcon icon="users" />} tone="var(--pi-chart-3)" />
        <StatCard label={t("home.kpiNotificationsLabel", language)} value={t("home.kpiComingSoon", language)} icon={<StatIcon icon="layers" />} tone="var(--pi-chart-4)" />
      </div>

      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {!tiles && !error && <p>{t("status.loading", language)}</p>}

      {tiles && (
        <>
          <div className="home-main-grid">
            <div className="home-departments-column">
              <div className="home-section-header">
                <h2>{t("home.exploreDepartments", language)}</h2>
                <p>{t("home.exploreDepartmentsSubtitle", language)}</p>
              </div>

              <div className="home-department-grid">
                {tiles.map((tile) => (
                  <div
                    key={tile.key}
                    className="home-department-tile"
                    role="link"
                    tabIndex={0}
                    onClick={() => goTo(tile.href)}
                    onKeyDown={(e) => { if (e.key === "Enter") goTo(tile.href); }}
                  >
                    <span
                      className="home-department-tile__badge"
                      style={{
                        background: `linear-gradient(135deg, ${tile.colorStart}, ${tile.colorEnd})`,
                        boxShadow: `0 10px 24px -6px ${tile.glow}, 0 0 22px -4px ${tile.glow}`,
                      }}
                    >
                      <DepartmentIcon icon={tile.icon} size={26} />
                    </span>
                    <span className="home-department-tile__title">{t(tile.titleKey, language)}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="home-panel home-quick-actions-panel">
              <h2>{t("home.quickActionsHeading", language)}</h2>
              <div className="home-quick-actions-grid">
                {QUICK_ACTIONS.map((action) => (
                  <button key={action.key} type="button" className="home-quick-action" onClick={() => goTo(action.href)}>
                    {t(action.labelKey, language)}
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="home-bottom-grid">
            <div className="home-panel home-recent-panel">
              <h2>{t("home.recentActivity", language)}</h2>
              {recent.length === 0 ? (
                <p className="home-recent-empty">{t("home.recentActivityEmpty", language)}</p>
              ) : (
                <ul className="home-recent-list">
                  {recent.map((item) => (
                    <li key={`${item.icon}-${item.id}`}>
                      <button type="button" className="home-recent-item" onClick={() => goTo(item.href)}>
                        <DepartmentIcon icon={item.icon} size={16} />
                        <bdi>{item.label}</bdi>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="home-panel home-quick-links-panel">
              <h2>{t("home.quickLinksHeading", language)}</h2>
              <ul className="home-quick-links-list">
                {QUICK_LINKS.map((link) => (
                  <li key={link.key}>
                    <button type="button" onClick={() => goTo(link.href)}>{t(link.labelKey, language)}</button>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </>
      )}
    </section>
  );
}
