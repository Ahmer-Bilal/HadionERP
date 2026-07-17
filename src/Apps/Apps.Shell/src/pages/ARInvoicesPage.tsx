import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveARInvoice,
  createARInvoice,
  listARInvoices,
  postARInvoice,
  rejectARInvoice,
  reverseARInvoice,
  submitARInvoice,
} from "../api/arInvoiceApi";
import type { ARInvoice, CreateARInvoiceInput } from "../api/arInvoiceApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listGLAccounts } from "../api/glAccountApi";
import type { GLAccount } from "../api/glAccountApi";
import { listTaxCodes } from "../api/taxCodeApi";
import type { TaxCode } from "../api/taxCodeApi";
import { listCustomerReceiptsForInvoice } from "../api/customerReceiptApi";
import type { CustomerReceipt } from "../api/customerReceiptApi";

interface ARInvoicesPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; invoice: ARInvoice };

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

// Mirrors Modules.Finance.Application.ARInvoiceService.ReceivableEligibleRoles.
const RECEIVABLE_ELIGIBLE_ROLES = new Set(["Client"]);

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ARInvoicesPage({ language }: ARInvoicesPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [invoices, setInvoices] = useState<ARInvoice[]>([]);
  const [customers, setCustomers] = useState<BusinessPartner[]>([]);
  const [accounts, setAccounts] = useState<GLAccount[]>([]);
  const [taxCodes, setTaxCodes] = useState<TaxCode[]>([]);
  const [invoiceReceipts, setInvoiceReceipts] = useState<CustomerReceipt[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState({
    customerId: "",
    customerReference: "",
    invoiceDate: todayIsoDate(),
    description: "",
    revenueAccountId: "",
    receivableAccountId: "",
    netAmount: "",
    taxCodeId: "",
    vatAccountId: "",
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [invoiceResult, customerResult, accountResult, taxCodeResult] = await Promise.all([
        listARInvoices(200, 0),
        listBusinessPartners(200, 0),
        listGLAccounts(200, 0),
        listTaxCodes(200, 0),
      ]);
      setInvoices(invoiceResult.items);
      setCustomers(customerResult.items.filter((c) => c.businessRoles.some((r) => RECEIVABLE_ELIGIBLE_ROLES.has(r.roleType))));
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
    if (view.kind !== "details") { setInvoiceReceipts([]); return; }
    listCustomerReceiptsForInvoice(view.invoice.id).then((result) => setInvoiceReceipts(result.items)).catch(() => setInvoiceReceipts([]));
  }, [view]);

  const customerLabel = (id: string) => customers.find((c) => c.id === id)?.name ?? id;

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
      const input: CreateARInvoiceInput = {
        customerId: form.customerId,
        customerReference: form.customerReference.trim() || undefined,
        invoiceDate: form.invoiceDate,
        description: form.description.trim(),
        revenueAccountId: form.revenueAccountId,
        receivableAccountId: form.receivableAccountId,
        netAmount: netAmountNumber,
        taxCodeId: form.taxCodeId || undefined,
        vatAccountId: form.taxCodeId ? form.vatAccountId || undefined : undefined,
      };
      await createARInvoice(input);
      setForm({
        customerId: "", customerReference: "", invoiceDate: todayIsoDate(), description: "",
        revenueAccountId: "", receivableAccountId: "", netAmount: "", taxCodeId: "", vatAccountId: "",
      });
      setView({ kind: "list" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (invoice: ARInvoice, action: "submit" | "approve" | "reject" | "post" | "reverse") => {
    setBusy(true);
    setError(null);
    try {
      let updated: ARInvoice;
      if (action === "submit") updated = await submitARInvoice(invoice.id);
      else if (action === "approve") updated = await approveARInvoice(invoice.id);
      else if (action === "reject") updated = await rejectARInvoice(invoice.id);
      else if (action === "post") updated = await postARInvoice(invoice.id);
      else updated = await reverseARInvoice(invoice.id);
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
      { key: "create", label: t("ar.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("ar.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("ar.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem" }}>
          <label>{t("ar.fieldCustomer", language)}
            <select style={inputStyle} value={form.customerId} onChange={(e) => setForm({ ...form, customerId: e.target.value })}>
              <option value=""></option>
              {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </label>
          <label>{t("ar.fieldCustomerReference", language)}
            <input style={inputStyle} value={form.customerReference} onChange={(e) => setForm({ ...form, customerReference: e.target.value })} />
          </label>
          <label>{t("ar.fieldInvoiceDate", language)}
            <input type="date" style={inputStyle} value={form.invoiceDate} onChange={(e) => setForm({ ...form, invoiceDate: e.target.value })} />
          </label>
          <label>{t("ar.fieldDescription", language)}
            <input style={inputStyle} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </label>
          <label>{t("ar.fieldRevenueAccount", language)}
            <select style={inputStyle} value={form.revenueAccountId} onChange={(e) => setForm({ ...form, revenueAccountId: e.target.value })}>
              <option value=""></option>
              {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
            </select>
          </label>
          <label>{t("ar.fieldReceivableAccount", language)}
            <select style={inputStyle} value={form.receivableAccountId} onChange={(e) => setForm({ ...form, receivableAccountId: e.target.value })}>
              <option value=""></option>
              {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
            </select>
          </label>
          <label>{t("ar.fieldNetAmount", language)}
            <input type="number" min="0" step="0.01" style={inputStyle} value={form.netAmount} onChange={(e) => setForm({ ...form, netAmount: e.target.value })} />
          </label>
          <label>{t("ar.fieldTaxCode", language)}
            <select style={inputStyle} value={form.taxCodeId} onChange={(e) => setForm({ ...form, taxCodeId: e.target.value })}>
              <option value="">{t("ar.noTaxCode", language)}</option>
              {taxCodes.map((tc) => <option key={tc.id} value={tc.id}>{tc.taxCodeCode} — {tc.rate}%</option>)}
            </select>
          </label>
          {form.taxCodeId && (
            <label>{t("ar.fieldVatAccount", language)}
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
      actions.push({ key: "submit", label: t("ar.actionSubmit", language), onClick: () => handleAction(invoice, "submit"), variant: "primary", isDisabled: busy });
    if (invoice.status === "Submitted") {
      actions.push({ key: "approve", label: t("ar.actionApprove", language), onClick: () => handleAction(invoice, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("ar.actionReject", language), onClick: () => handleAction(invoice, "reject"), isDisabled: busy });
    }
    if (invoice.status === "Approved")
      actions.push({ key: "post", label: t("ar.actionPost", language), onClick: () => handleAction(invoice, "post"), variant: "primary", isDisabled: busy });
    if (invoice.status === "Posted")
      actions.push({ key: "reverse", label: t("ar.actionReverse", language), onClick: () => handleAction(invoice, "reverse"), isDisabled: busy });
    actions.push({ key: "back", label: t("ar.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{invoice.documentNumber} — {customerLabel(invoice.customerId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("ar.columnCustomer", language)}</dt>
                <dd>{customerLabel(invoice.customerId)}</dd>
                <dt>{t("ar.fieldCustomerReference", language)}</dt>
                <dd>{invoice.customerReference ?? "—"}</dd>
                <dt>{t("ar.columnInvoiceDate", language)}</dt>
                <dd><bdi dir="ltr">{invoice.invoiceDate}</bdi></dd>
                <dt>{t("ar.fieldRevenueAccount", language)}</dt>
                <dd><bdi dir="ltr">{accountLabel(invoice.revenueAccountId)}</bdi></dd>
                <dt>{t("ar.fieldReceivableAccount", language)}</dt>
                <dd><bdi dir="ltr">{accountLabel(invoice.receivableAccountId)}</bdi></dd>
                <dt>{t("ar.columnNetAmount", language)}</dt>
                <dd><bdi dir="ltr">{invoice.netAmount.toFixed(2)}</bdi></dd>
                <dt>{t("ar.columnTaxAmount", language)}</dt>
                <dd><bdi dir="ltr">{invoice.taxAmount.toFixed(2)}</bdi></dd>
                <dt>{t("ar.columnGrossAmount", language)}</dt>
                <dd><bdi dir="ltr">{invoice.grossAmount.toFixed(2)}</bdi></dd>
                {invoice.status === "Posted" && (
                  <>
                    <dt>{t("ar.columnOutstandingBalance", language)}</dt>
                    <dd><bdi dir="ltr">{invoice.outstandingBalance.toFixed(2)}</bdi></dd>
                  </>
                )}
                <dt>{t("ar.columnStatus", language)}</dt>
                <dd>{translateStatus(invoice.status, language)}</dd>
                {invoice.linkedJournalEntryId && (
                  <>
                    <dt>{t("ar.linkedJournalEntry", language)}</dt>
                    <dd><bdi dir="ltr">{invoice.linkedJournalEntryId}</bdi></dd>
                  </>
                )}
              </dl>
            ),
          },
          {
            key: "receipts",
            title: t("cr.heading", language),
            content: invoiceReceipts.length === 0 ? (
              <p>{t("cr.emptyState", language)}</p>
            ) : (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("cr.columnDocumentNumber", language)}</th>
                    <th>{t("cr.fieldReceiptDate", language)}</th>
                    <th>{t("cr.fieldAllocatedAmount", language)}</th>
                    <th>{t("cr.columnStatus", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {invoiceReceipts.map((receipt) => (
                    <tr key={receipt.id}>
                      <td><bdi dir="ltr">{receipt.documentNumber}</bdi></td>
                      <td><bdi dir="ltr">{receipt.receiptDate}</bdi></td>
                      <td><bdi dir="ltr">{receipt.allocations.find((a) => a.arInvoiceId === invoice.id)?.allocatedAmount.toFixed(2) ?? "—"}</bdi></td>
                      <td>{translateStatus(receipt.status, language)}</td>
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

  const listActions: ActionItem[] = [
    { key: "new", label: t("ar.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("ar.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {invoices.length === 0 ? (
        <p>{t("ar.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("ar.columnDocumentNumber", language)}</th>
              <th>{t("ar.columnCustomer", language)}</th>
              <th>{t("ar.columnInvoiceDate", language)}</th>
              <th>{t("ar.columnGrossAmount", language)}</th>
              <th>{t("ar.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {invoices.map((invoice) => (
              <tr key={invoice.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", invoice })}>
                <td><bdi dir="ltr">{invoice.documentNumber}</bdi></td>
                <td>{customerLabel(invoice.customerId)}</td>
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
