import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveCustomerReceipt,
  createCustomerReceipt,
  listCustomerReceipts,
  postCustomerReceipt,
  rejectCustomerReceipt,
  reverseCustomerReceipt,
  submitCustomerReceipt,
} from "../api/customerReceiptApi";
import type { CreateCustomerReceiptAllocationInput, CustomerReceipt } from "../api/customerReceiptApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listBankAccounts } from "../api/bankAccountApi";
import type { BankAccount } from "../api/bankAccountApi";
import { listARInvoices } from "../api/arInvoiceApi";
import type { ARInvoice } from "../api/arInvoiceApi";
import { listLookupValues } from "../api/lookupApi";
import type { LookupValue } from "../api/lookupApi";

interface CustomerReceiptsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; receipt: CustomerReceipt };

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

// Mirrors Modules.Finance.Application.CustomerReceiptService.ReceivableEligibleRoles.
const RECEIVABLE_ELIGIBLE_ROLES = new Set(["Client"]);

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function CustomerReceiptsPage({ language }: CustomerReceiptsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [receipts, setReceipts] = useState<CustomerReceipt[]>([]);
  const [customers, setCustomers] = useState<BusinessPartner[]>([]);
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([]);
  const [invoices, setInvoices] = useState<ARInvoice[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<LookupValue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [customerId, setCustomerId] = useState("");
  const [bankAccountId, setBankAccountId] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("");
  const [receiptDate, setReceiptDate] = useState(todayIsoDate());
  const [reference, setReference] = useState("");
  const [allocationAmounts, setAllocationAmounts] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [receiptResult, customerResult, bankAccountResult, invoiceResult, paymentMethodResult] = await Promise.all([
        listCustomerReceipts(200, 0),
        listBusinessPartners(200, 0),
        listBankAccounts(200, 0),
        listARInvoices(200, 0),
        listLookupValues("PaymentMethod", false),
      ]);
      setReceipts(receiptResult.items);
      setCustomers(customerResult.items.filter((c) =>
        c.status === "Approved" && c.businessRoles.some((r) => RECEIVABLE_ELIGIBLE_ROLES.has(r.roleType))));
      setBankAccounts(bankAccountResult.items.filter((b) => b.status === "Approved" && b.isActive));
      setInvoices(invoiceResult.items.filter((i) => i.status === "Posted" && i.outstandingBalance > 0));
      setPaymentMethods(paymentMethodResult);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    if (view.kind === "list") load();
  }, [view.kind, load]);

  const customerLabel = (id: string) => customers.find((c) => c.id === id)?.name ?? id;
  const bankAccountLabel = (id: string) => {
    const bankAccount = bankAccounts.find((b) => b.id === id);
    return bankAccount ? `${bankAccount.accountCode} — ${bankAccount.accountName}` : id;
  };
  const invoiceLabel = (id: string) => {
    const invoice = invoices.find((i) => i.id === id);
    return invoice ? `${invoice.documentNumber} — ${invoice.customerReference ?? ""}` : id;
  };
  const paymentMethodLabel = (code: string) => paymentMethods.find((m) => m.code === code)?.name ?? code;

  const customerOutstandingInvoices = invoices.filter((i) => i.customerId === customerId);

  const resetCreateForm = () => {
    setCustomerId("");
    setBankAccountId("");
    setPaymentMethod("");
    setReceiptDate(todayIsoDate());
    setReference("");
    setAllocationAmounts({});
  };

  const allocationsForSubmit: CreateCustomerReceiptAllocationInput[] = Object.entries(allocationAmounts)
    .filter(([, amount]) => Number(amount) > 0)
    .map(([arInvoiceId, amount]) => ({ arInvoiceId, allocatedAmount: Number(amount) }));

  const canCreate = Boolean(customerId && bankAccountId && paymentMethod && receiptDate && allocationsForSubmit.length > 0);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createCustomerReceipt({
        customerId, bankAccountId, paymentMethod, receiptDate,
        allocations: allocationsForSubmit,
        reference: reference.trim() || undefined,
      });
      resetCreateForm();
      setView({ kind: "details", receipt: created });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (receipt: CustomerReceipt, action: "submit" | "approve" | "reject" | "post" | "reverse") => {
    setBusy(true);
    setError(null);
    try {
      let updated: CustomerReceipt;
      if (action === "submit") updated = await submitCustomerReceipt(receipt.id);
      else if (action === "approve") updated = await approveCustomerReceipt(receipt.id);
      else if (action === "reject") updated = await rejectCustomerReceipt(receipt.id);
      else if (action === "post") updated = await postCustomerReceipt(receipt.id);
      else updated = await reverseCustomerReceipt(receipt.id);
      setView({ kind: "details", receipt: updated });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };
  const totalAllocated = allocationsForSubmit.reduce((sum, a) => sum + a.allocatedAmount, 0);

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("cr.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !canCreate },
      { key: "back", label: t("cr.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("cr.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("cr.fieldCustomer", language)}
            <select style={inputStyle} value={customerId} onChange={(e) => { setCustomerId(e.target.value); setAllocationAmounts({}); }}>
              <option value=""></option>
              {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </label>
          <label>{t("cr.fieldBankAccount", language)}
            <select style={inputStyle} value={bankAccountId} onChange={(e) => setBankAccountId(e.target.value)}>
              <option value=""></option>
              {bankAccounts.map((b) => <option key={b.id} value={b.id}>{b.accountCode} — {b.accountName}</option>)}
            </select>
          </label>
          <label>{t("cr.fieldPaymentMethod", language)}
            <select style={inputStyle} value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)}>
              <option value=""></option>
              {paymentMethods.map((m) => <option key={m.code} value={m.code}>{m.name}</option>)}
            </select>
          </label>
          <label>{t("cr.fieldReceiptDate", language)}
            <input type="date" style={inputStyle} value={receiptDate} onChange={(e) => setReceiptDate(e.target.value)} />
          </label>
          <label>{t("cr.fieldReference", language)}
            <input style={inputStyle} value={reference} onChange={(e) => setReference(e.target.value)} />
          </label>
        </div>

        {customerId && (
          <>
            {customerOutstandingInvoices.length === 0 ? (
              <p>{t("cr.noOutstandingInvoices", language)}</p>
            ) : (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("cr.columnInvoice", language)}</th>
                    <th>{t("cr.columnOutstandingBalance", language)}</th>
                    <th>{t("cr.fieldAllocatedAmount", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {customerOutstandingInvoices.map((invoice) => (
                    <tr key={invoice.id}>
                      <td><bdi dir="ltr">{invoice.documentNumber} — {invoice.customerReference}</bdi></td>
                      <td><bdi dir="ltr">{invoice.outstandingBalance.toFixed(2)}</bdi></td>
                      <td>
                        <input
                          type="number" min="0" max={invoice.outstandingBalance} step="0.01"
                          style={{ inlineSize: "8rem" }}
                          value={allocationAmounts[invoice.id] ?? ""}
                          onChange={(e) => setAllocationAmounts({ ...allocationAmounts, [invoice.id]: e.target.value })}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            <p style={{ marginBlockStart: "1rem" }}>
              {t("cr.columnAmount", language)}: <bdi dir="ltr">{totalAllocated.toFixed(2)}</bdi>
            </p>
          </>
        )}
      </section>
    );
  }

  if (view.kind === "details") {
    const receipt = view.receipt;
    const actions: ActionItem[] = [];
    if (receipt.status === "Draft")
      actions.push({ key: "submit", label: t("cr.actionSubmit", language), onClick: () => handleAction(receipt, "submit"), variant: "primary", isDisabled: busy });
    if (receipt.status === "Submitted") {
      actions.push({ key: "approve", label: t("cr.actionApprove", language), onClick: () => handleAction(receipt, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("cr.actionReject", language), onClick: () => handleAction(receipt, "reject"), isDisabled: busy });
    }
    if (receipt.status === "Approved")
      actions.push({ key: "post", label: t("cr.actionPost", language), onClick: () => handleAction(receipt, "post"), variant: "primary", isDisabled: busy });
    if (receipt.status === "Posted")
      actions.push({ key: "reverse", label: t("cr.actionReverse", language), onClick: () => handleAction(receipt, "reverse"), isDisabled: busy });
    actions.push({ key: "back", label: t("cr.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{receipt.documentNumber ?? t("cr.newHeading", language)} — {customerLabel(receipt.customerId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("cr.fieldCustomer", language)}</dt>
                <dd>{customerLabel(receipt.customerId)}</dd>
                <dt>{t("cr.fieldBankAccount", language)}</dt>
                <dd><bdi dir="ltr">{bankAccountLabel(receipt.bankAccountId)}</bdi></dd>
                <dt>{t("cr.fieldPaymentMethod", language)}</dt>
                <dd>{paymentMethodLabel(receipt.paymentMethod)}</dd>
                <dt>{t("cr.fieldReceiptDate", language)}</dt>
                <dd><bdi dir="ltr">{receipt.receiptDate}</bdi></dd>
                {receipt.reference && (
                  <>
                    <dt>{t("cr.fieldReference", language)}</dt>
                    <dd>{receipt.reference}</dd>
                  </>
                )}
                <dt>{t("cr.columnAmount", language)}</dt>
                <dd><bdi dir="ltr">{receipt.amount.toFixed(2)}</bdi></dd>
                <dt>{t("cr.columnStatus", language)}</dt>
                <dd>{translateStatus(receipt.status, language)}</dd>
                {receipt.linkedJournalEntryId && (
                  <>
                    <dt>{t("ar.linkedJournalEntry", language)}</dt>
                    <dd><bdi dir="ltr">{receipt.linkedJournalEntryId}</bdi></dd>
                  </>
                )}
              </dl>
            ),
          },
          {
            key: "allocations",
            title: t("cr.tabAllocations", language),
            content: (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("cr.columnInvoice", language)}</th>
                    <th>{t("cr.fieldAllocatedAmount", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {receipt.allocations.map((allocation) => (
                    <tr key={allocation.id}>
                      <td><bdi dir="ltr">{invoiceLabel(allocation.arInvoiceId)}</bdi></td>
                      <td><bdi dir="ltr">{allocation.allocatedAmount.toFixed(2)}</bdi></td>
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
    { key: "new", label: t("cr.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("cr.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {receipts.length === 0 ? (
        <p>{t("cr.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("cr.columnDocumentNumber", language)}</th>
              <th>{t("cr.fieldCustomer", language)}</th>
              <th>{t("cr.fieldReceiptDate", language)}</th>
              <th>{t("cr.columnAmount", language)}</th>
              <th>{t("cr.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {receipts.map((receipt) => (
              <tr key={receipt.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", receipt })}>
                <td><bdi dir="ltr">{receipt.documentNumber}</bdi></td>
                <td>{customerLabel(receipt.customerId)}</td>
                <td><bdi dir="ltr">{receipt.receiptDate}</bdi></td>
                <td><bdi dir="ltr">{receipt.amount.toFixed(2)}</bdi></td>
                <td>{translateStatus(receipt.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
