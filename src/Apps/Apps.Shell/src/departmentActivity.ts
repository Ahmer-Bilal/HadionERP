import type { TranslationKey } from "./i18n/content";
import { listBusinessPartners } from "./api/businessPartnerApi";
import { listItems } from "./api/itemApi";
import { listGLAccounts } from "./api/glAccountApi";
import { listCostCenters } from "./api/costCenterApi";
import { listTaxCodes } from "./api/taxCodeApi";
import { listJournalEntries } from "./api/journalEntryApi";
import { listAPInvoices } from "./api/apInvoiceApi";
import { listARInvoices } from "./api/arInvoiceApi";
import { listBankAccounts } from "./api/bankAccountApi";
import { listPayments } from "./api/paymentApi";
import { listCustomerReceipts } from "./api/customerReceiptApi";
import { listPurchaseRequisitions } from "./api/purchaseRequisitionApi";
import { listRequestsForQuotation } from "./api/requestForQuotationApi";
import { listPurchaseOrders } from "./api/purchaseOrderApi";
import { listGoodsReceiptNotes } from "./api/goodsReceiptNoteApi";
import { listContracts } from "./api/contractApi";
import { listSubcontracts } from "./api/subcontractApi";
import { listMeasurementSheets } from "./api/measurementSheetApi";
import { listIpcs } from "./api/ipcApi";
import { listVariationOrders } from "./api/variationOrderApi";
import { listRetentionReleases } from "./api/retentionReleaseApi";
import { listProjects } from "./api/projectApi";

/** The one shape every document type's list endpoint already returns (BusinessObject-derived DTOs are
 * uniform across every module — verified against all 21 types registered below) — a structural type, not
 * tied to any one api/*.ts file's own copy of PagedResult<T>. */
interface ActivityRecord {
  id: string;
  documentNumber: string | null;
  status: string;
  createdAt: string;
  createdBy: string;
}

interface ActivityPage {
  items: ActivityRecord[];
}

/**
 * One approvable document type within a department — reuses the exact `list*` function every existing page
 * already calls (no new API surface) plus the real `*Workflow.ApproverRoleKey` literal each module's own
 * backend uses to gate its Approve action (copied from each `*Workflow.cs`, since the frontend has no
 * access to the C# constant itself — the same "literal string matching a backend contract" precedent
 * `initialTypeCode` props already use for Lookup types). `useDepartmentActivity` is what actually turns this
 * into real Approvals/Submitted lists — see that hook's own doc comment.
 */
export interface ActivityDocType {
  labelKey: TranslationKey;
  href: string;
  approverRoleKey: string;
  list: (top: number, skip: number) => Promise<ActivityPage>;
}

// Cost Center/Tax Code/Chart of Accounts moved from Master Data into Finance and Accounting's own nav last
// pass — their activity entries live under "finance" below for the same reason, not duplicated here.
export const DEPARTMENT_ACTIVITY: Record<string, ActivityDocType[]> = {
  "master-data": [
    { labelKey: "nav.businessPartnersArea", href: "#business-partners", approverRoleKey: "MasterData.ApproveBusinessPartner", list: listBusinessPartners },
    { labelKey: "nav.itemsArea", href: "#items", approverRoleKey: "MasterData.ApproveItem", list: listItems },
  ],
  finance: [
    { labelKey: "nav.chartOfAccountsArea", href: "#gl-accounts", approverRoleKey: "MasterData.ApproveGLAccount", list: listGLAccounts },
    { labelKey: "nav.costCentersArea", href: "#cost-centers", approverRoleKey: "MasterData.ApproveCostCenter", list: listCostCenters },
    { labelKey: "nav.taxCodesArea", href: "#tax-codes", approverRoleKey: "MasterData.ApproveTaxCode", list: listTaxCodes },
    { labelKey: "nav.journalEntriesArea", href: "#journal-entries", approverRoleKey: "Finance.ApproveJournalEntry", list: listJournalEntries },
    { labelKey: "nav.apInvoicesArea", href: "#ap-invoices", approverRoleKey: "Finance.ApproveAPInvoice", list: listAPInvoices },
    { labelKey: "nav.arInvoicesArea", href: "#ar-invoices", approverRoleKey: "Finance.ApproveARInvoice", list: listARInvoices },
    { labelKey: "nav.bankAccountsArea", href: "#bank-accounts", approverRoleKey: "Finance.ApproveBankAccount", list: listBankAccounts },
    { labelKey: "nav.paymentsArea", href: "#payments", approverRoleKey: "Finance.ApprovePayment", list: listPayments },
    { labelKey: "nav.customerReceiptsArea", href: "#customer-receipts", approverRoleKey: "Finance.ApproveCustomerReceipt", list: listCustomerReceipts },
  ],
  // Vendor Prequalification deliberately excluded — its approval model is multi-reviewer (Commercial/Legal/
  // Technical/HSE/Quality), not the single-Approve-role shape this registry assumes; needs its own design
  // pass, not a forced fit here.
  procurement: [
    { labelKey: "nav.purchaseRequisitionsArea", href: "#purchase-requisitions", approverRoleKey: "Procurement.ApprovePurchaseRequisition", list: listPurchaseRequisitions },
    { labelKey: "nav.requestsForQuotationArea", href: "#requests-for-quotation", approverRoleKey: "Procurement.ApproveRequestForQuotation", list: listRequestsForQuotation },
    { labelKey: "nav.purchaseOrdersArea", href: "#purchase-orders", approverRoleKey: "Procurement.ApprovePurchaseOrder", list: listPurchaseOrders },
    { labelKey: "nav.goodsReceiptNotesArea", href: "#goods-receipt-notes", approverRoleKey: "Procurement.ApproveGoodsReceiptNote", list: listGoodsReceiptNotes },
  ],
  construction: [
    { labelKey: "nav.contractsArea", href: "#contracts", approverRoleKey: "Construction.ApproveContract", list: listContracts },
    { labelKey: "nav.subcontractsArea", href: "#subcontracts", approverRoleKey: "Construction.ApproveSubcontract", list: listSubcontracts },
    { labelKey: "nav.measurementSheetsArea", href: "#measurement-sheets", approverRoleKey: "Construction.CertifyMeasurementSheet", list: listMeasurementSheets },
    { labelKey: "nav.ipcsArea", href: "#ipcs", approverRoleKey: "Construction.CertifyIpc", list: listIpcs },
    { labelKey: "vo.heading", href: "#variation-orders", approverRoleKey: "Construction.ApproveVariationOrder", list: listVariationOrders },
    { labelKey: "nav.retentionReleasesArea", href: "#retention-releases", approverRoleKey: "Construction.ApproveRetentionRelease", list: listRetentionReleases },
  ],
  "project-management": [
    { labelKey: "nav.projectsArea", href: "#projects", approverRoleKey: "ProjectManagement.ApproveProject", list: listProjects },
  ],
};
