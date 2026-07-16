import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveAPInvoice,
  createAPInvoice,
  listAPInvoices,
  postAPInvoice,
  rejectAPInvoice,
  reverseAPInvoice,
  submitAPInvoice,
} from "../api/apInvoiceApi";
import type { APInvoice, CreateAPInvoiceInput } from "../api/apInvoiceApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listGLAccounts } from "../api/glAccountApi";
import type { GLAccount } from "../api/glAccountApi";
import { listTaxCodes } from "../api/taxCodeApi";
import type { TaxCode } from "../api/taxCodeApi";
import { listPaymentsForInvoice } from "../api/paymentApi";
import type { Payment } from "../api/paymentApi";

interface APInvoicesPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; invoice: APInvoice };

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected" | "je.statusPosted" | "je.statusReversed"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
  Posted: "je.statusPosted",
  Reversed: "je.statusReversed",
};

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

// Mirrors Modules.Finance.Application.APInvoiceService.PayableEligibleRoles — which BusinessRoles this
// platform can raise an AP invoice against (excludes Client/JointVenturePartner/GovernmentAuthority).
const PAYABLE_ELIGIBLE_ROLES = new Set([
  "Supplier", "Subcontractor", "Consultant", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
]);

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function APInvoicesPage({ language }: APInvoicesPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [invoices, setInvoices] = useState<APInvoice[]>([]);
  const [vendors, setVendors] = useState<BusinessPartner[]>([]);
  const [accounts, setAccounts] = useState<GLAccount[]>([]);
  const [taxCodes, setTaxCodes] = useState<TaxCode[]>([]);
  const [invoicePayments, setInvoicePayments] = useState<Payment[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState({
    vendorId: "",
    vendorInvoiceNumber: "",
    invoiceDate: todayIsoDate(),
    description: "",
    expenseAccountId: "",
    payableAccountId: "",
    netAmount: "",
    taxCodeId: "",
    vatAccountId: "",
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [invoiceResult, vendorResult, accountResult, taxCodeResult] = await Promise.all([
        listAPInvoices(200, 0),
        listBusinessPartners(200, 0),
        listGLAccounts(200, 0),
        listTaxCodes(200, 0),
      ]);
      setInvoices(invoiceResult.items);
      setVendors(vendorResult.items.filter((v) => v.businessRoles.some((r) => PAYABLE_ELIGIBLE_ROLES.has(r.roleType))));
      setAccounts(accountResult.items);
      setTaxCodes(taxCodeResult.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (view.kind !== "details") { setInvoicePayments([]); return; }
    listPaymentsForInvoice(view.invoice.id).then((result) => setInvoicePayments(result.items)).catch(() => setInvoicePayments([]));
  }, [view]);

  const vendorLabel = (id: string) => {
    const vendor = vendors.find((v) => v.id === id);
    return vendor ? vendor.name : id;
  };

  const accountLabel = (id: string) => {
    const account = accounts.find((a) => a.id === id);
    return account ? `${account.accountCode} — ${account.accountName}` : id;
  };

  const selectedTaxCode = taxCodes.find((tc) => tc.id === form.taxCodeId);
  const netAmountNumber = Number(form.netAmount) || 0;
  const taxAmountPreview = selectedTaxCode ? Math.round(netAmountNumber * selectedTaxCode.rate) / 100 : 0;
  const grossAmountPreview = netAmountNumber + taxAmountPreview;

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const input: CreateAPInvoiceInput = {
        vendorId: form.vendorId,
        vendorInvoiceNumber: form.vendorInvoiceNumber.trim(),
        invoiceDate: form.invoiceDate,
        description: form.description.trim(),
        expenseAccountId: form.expenseAccountId,
        payableAccountId: form.payableAccountId,
        netAmount: netAmountNumber,
        taxCodeId: form.taxCodeId || undefined,
        vatAccountId: form.taxCodeId ? form.vatAccountId || undefined : undefined,
      };
      await createAPInvoice(input);
      setForm({
        vendorId: "", vendorInvoiceNumber: "", invoiceDate: todayIsoDate(), description: "",
        expenseAccountId: "", payableAccountId: "", netAmount: "", taxCodeId: "", vatAccountId: "",
      });
      setView({ kind: "list" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (invoice: APInvoice, action: "submit" | "approve" | "reject" | "post" | "reverse") => {
    setBusy(true);
    setError(null);
    try {
      let updated: APInvoice;
      if (action === "submit") updated = await submitAPInvoice(invoice.id);
      else if (action === "approve") updated = await approveAPInvoice(invoice.id);
      else if (action === "reject") updated = await rejectAPInvoice(invoice.id);
      else if (action === "post") updated = await postAPInvoice(invoice.id);
      else updated = await reverseAPInvoice(invoice.id);
      setView({ kind: "details", invoice: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("ap.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("ap.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("ap.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem" }}>
          <label>{t("ap.fieldVendor", language)}
            <select style={inputStyle} value={form.vendorId} onChange={(e) => setForm({ ...form, vendorId: e.target.value })}>
              <option value=""></option>
              {vendors.map((v) => <option key={v.id} value={v.id}>{v.name}</option>)}
            </select>
          </label>
          <label>{t("ap.fieldVendorInvoiceNumber", language)}
            <input style={inputStyle} value={form.vendorInvoiceNumber} onChange={(e) => setForm({ ...form, vendorInvoiceNumber: e.target.value })} />
          </label>
          <label>{t("ap.fieldInvoiceDate", language)}
            <input type="date" style={inputStyle} value={form.invoiceDate} onChange={(e) => setForm({ ...form, invoiceDate: e.target.value })} />
          </label>
          <label>{t("ap.fieldDescription", language)}
            <input style={inputStyle} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </label>
          <label>{t("ap.fieldExpenseAccount", language)}
            <select style={inputStyle} value={form.expenseAccountId} onChange={(e) => setForm({ ...form, expenseAccountId: e.target.value })}>
              <option value=""></option>
              {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
            </select>
          </label>
          <label>{t("ap.fieldPayableAccount", language)}
            <select style={inputStyle} value={form.payableAccountId} onChange={(e) => setForm({ ...form, payableAccountId: e.target.value })}>
              <option value=""></option>
              {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
            </select>
          </label>
          <label>{t("ap.fieldNetAmount", language)}
            <input type="number" min="0" step="0.01" style={inputStyle} value={form.netAmount} onChange={(e) => setForm({ ...form, netAmount: e.target.value })} />
          </label>
          <label>{t("ap.fieldTaxCode", language)}
            <select style={inputStyle} value={form.taxCodeId} onChange={(e) => setForm({ ...form, taxCodeId: e.target.value })}>
              <option value="">{t("ap.noTaxCode", language)}</option>
              {taxCodes.map((tc) => <option key={tc.id} value={tc.id}>{tc.taxCodeCode} — {tc.rate}%</option>)}
            </select>
          </label>
          {form.taxCodeId && (
            <label>{t("ap.fieldVatAccount", language)}
              <select style={inputStyle} value={form.vatAccountId} onChange={(e) => setForm({ ...form, vatAccountId: e.target.value })}>
                <option value=""></option>
                {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
              </select>
            </label>
          )}
        </div>
        <p>
          <bdi dir="ltr">{netAmountNumber.toFixed(2)}</bdi> + <bdi dir="ltr">{taxAmountPreview.toFixed(2)}</bdi> = <bdi dir="ltr">{grossAmountPreview.toFixed(2)}</bdi>
        </p>
      </section>
    );
  }

  if (view.kind === "details") {
    const invoice = view.invoice;
    const actions: ActionItem[] = [];
    if (invoice.status === "Draft")
      actions.push({ key: "submit", label: t("ap.actionSubmit", language), onClick: () => handleAction(invoice, "submit"), variant: "primary", isDisabled: busy });
    if (invoice.status === "Submitted") {
      actions.push({ key: "approve", label: t("ap.actionApprove", language), onClick: () => handleAction(invoice, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("ap.actionReject", language), onClick: () => handleAction(invoice, "reject"), isDisabled: busy });
    }
    if (invoice.status === "Approved")
      actions.push({ key: "post", label: t("ap.actionPost", language), onClick: () => handleAction(invoice, "post"), variant: "primary", isDisabled: busy });
    if (invoice.status === "Posted")
      actions.push({ key: "reverse", label: t("ap.actionReverse", language), onClick: () => handleAction(invoice, "reverse"), isDisabled: busy });
    actions.push({ key: "back", label: t("ap.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{invoice.documentNumber} — {invoice.vendorInvoiceNumber}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("ap.columnVendor", language)}</dt>
                <dd>{vendorLabel(invoice.vendorId)}</dd>
                <dt>{t("ap.columnVendorInvoiceNumber", language)}</dt>
                <dd>{invoice.vendorInvoiceNumber}</dd>
                <dt>{t("ap.columnInvoiceDate", language)}</dt>
                <dd><bdi dir="ltr">{invoice.invoiceDate}</bdi></dd>
                <dt>{t("ap.fieldExpenseAccount", language)}</dt>
                <dd><bdi dir="ltr">{accountLabel(invoice.expenseAccountId)}</bdi></dd>
                <dt>{t("ap.fieldPayableAccount", language)}</dt>
                <dd><bdi dir="ltr">{accountLabel(invoice.payableAccountId)}</bdi></dd>
                <dt>{t("ap.columnNetAmount", language)}</dt>
                <dd><bdi dir="ltr">{invoice.netAmount.toFixed(2)}</bdi></dd>
                <dt>{t("ap.columnTaxAmount", language)}</dt>
                <dd><bdi dir="ltr">{invoice.taxAmount.toFixed(2)}</bdi></dd>
                <dt>{t("ap.columnGrossAmount", language)}</dt>
                <dd><bdi dir="ltr">{invoice.grossAmount.toFixed(2)}</bdi></dd>
                {invoice.status === "Posted" && (
                  <>
                    <dt>{t("ap.columnOutstandingBalance", language)}</dt>
                    <dd><bdi dir="ltr">{invoice.outstandingBalance.toFixed(2)}</bdi></dd>
                  </>
                )}
                <dt>{t("ap.columnStatus", language)}</dt>
                <dd>{translateStatus(invoice.status, language)}</dd>
                {invoice.linkedJournalEntryId && (
                  <>
                    <dt>{t("ap.linkedJournalEntry", language)}</dt>
                    <dd><bdi dir="ltr">{invoice.linkedJournalEntryId}</bdi></dd>
                  </>
                )}
              </dl>
            ),
          },
          {
            key: "payments",
            title: t("pay.heading", language),
            content: invoicePayments.length === 0 ? (
              <p>{t("pay.emptyState", language)}</p>
            ) : (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("pay.columnDocumentNumber", language)}</th>
                    <th>{t("pay.fieldPaymentDate", language)}</th>
                    <th>{t("pay.fieldAllocatedAmount", language)}</th>
                    <th>{t("pay.columnStatus", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {invoicePayments.map((payment) => (
                    <tr key={payment.id}>
                      <td><bdi dir="ltr">{payment.documentNumber}</bdi></td>
                      <td><bdi dir="ltr">{payment.paymentDate}</bdi></td>
                      <td><bdi dir="ltr">{payment.allocations.find((a) => a.apInvoiceId === invoice.id)?.allocatedAmount.toFixed(2) ?? "—"}</bdi></td>
                      <td>{translateStatus(payment.status, language)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ),
          },
        ]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("ap.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("ap.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {invoices.length === 0 ? (
        <p>{t("ap.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("ap.columnDocumentNumber", language)}</th>
              <th>{t("ap.columnVendor", language)}</th>
              <th>{t("ap.columnVendorInvoiceNumber", language)}</th>
              <th>{t("ap.columnInvoiceDate", language)}</th>
              <th>{t("ap.columnGrossAmount", language)}</th>
              <th>{t("ap.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {invoices.map((invoice) => (
              <tr key={invoice.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", invoice })}>
                <td><bdi dir="ltr">{invoice.documentNumber}</bdi></td>
                <td>{vendorLabel(invoice.vendorId)}</td>
                <td>{invoice.vendorInvoiceNumber}</td>
                <td><bdi dir="ltr">{invoice.invoiceDate}</bdi></td>
                <td><bdi dir="ltr">{invoice.grossAmount.toFixed(2)}</bdi></td>
                <td>{translateStatus(invoice.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
