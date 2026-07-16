import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approvePayment,
  createPayment,
  listPayments,
  postPayment,
  rejectPayment,
  reversePayment,
  submitPayment,
} from "../api/paymentApi";
import type { CreatePaymentAllocationInput, Payment } from "../api/paymentApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listBankAccounts } from "../api/bankAccountApi";
import type { BankAccount } from "../api/bankAccountApi";
import { listAPInvoices } from "../api/apInvoiceApi";
import type { APInvoice } from "../api/apInvoiceApi";
import { listLookupValues } from "../api/lookupApi";
import type { LookupValue } from "../api/lookupApi";

interface PaymentsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; payment: Payment };

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

// Mirrors Modules.Finance.Application.PaymentService.PayableEligibleRoles.
const PAYABLE_ELIGIBLE_ROLES = new Set([
  "Supplier", "Subcontractor", "Consultant", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
]);

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function PaymentsPage({ language }: PaymentsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [payments, setPayments] = useState<Payment[]>([]);
  const [vendors, setVendors] = useState<BusinessPartner[]>([]);
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([]);
  const [invoices, setInvoices] = useState<APInvoice[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<LookupValue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [vendorId, setVendorId] = useState("");
  const [bankAccountId, setBankAccountId] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("");
  const [paymentDate, setPaymentDate] = useState(todayIsoDate());
  const [reference, setReference] = useState("");
  const [allocationAmounts, setAllocationAmounts] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [paymentResult, vendorResult, bankAccountResult, invoiceResult, paymentMethodResult] = await Promise.all([
        listPayments(200, 0),
        listBusinessPartners(200, 0),
        listBankAccounts(200, 0),
        listAPInvoices(200, 0),
        listLookupValues("PaymentMethod", false),
      ]);
      setPayments(paymentResult.items);
      setVendors(vendorResult.items.filter((v) =>
        v.status === "Approved" && v.businessRoles.some((r) => PAYABLE_ELIGIBLE_ROLES.has(r.roleType))));
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

  const vendorLabel = (id: string) => vendors.find((v) => v.id === id)?.name ?? id;
  const bankAccountLabel = (id: string) => {
    const bankAccount = bankAccounts.find((b) => b.id === id);
    return bankAccount ? `${bankAccount.accountCode} — ${bankAccount.accountName}` : id;
  };
  const invoiceLabel = (id: string) => {
    const invoice = invoices.find((i) => i.id === id);
    return invoice ? `${invoice.documentNumber} — ${invoice.vendorInvoiceNumber}` : id;
  };
  const paymentMethodLabel = (code: string) => paymentMethods.find((m) => m.code === code)?.name ?? code;

  const vendorOutstandingInvoices = invoices.filter((i) => i.vendorId === vendorId);

  const resetCreateForm = () => {
    setVendorId("");
    setBankAccountId("");
    setPaymentMethod("");
    setPaymentDate(todayIsoDate());
    setReference("");
    setAllocationAmounts({});
  };

  const allocationsForSubmit: CreatePaymentAllocationInput[] = Object.entries(allocationAmounts)
    .filter(([, amount]) => Number(amount) > 0)
    .map(([apInvoiceId, amount]) => ({ apInvoiceId, allocatedAmount: Number(amount) }));

  const canCreate = Boolean(vendorId && bankAccountId && paymentMethod && paymentDate && allocationsForSubmit.length > 0);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createPayment({
        vendorId, bankAccountId, paymentMethod, paymentDate,
        allocations: allocationsForSubmit,
        reference: reference.trim() || undefined,
      });
      resetCreateForm();
      setView({ kind: "details", payment: created });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (payment: Payment, action: "submit" | "approve" | "reject" | "post" | "reverse") => {
    setBusy(true);
    setError(null);
    try {
      let updated: Payment;
      if (action === "submit") updated = await submitPayment(payment.id);
      else if (action === "approve") updated = await approvePayment(payment.id);
      else if (action === "reject") updated = await rejectPayment(payment.id);
      else if (action === "post") updated = await postPayment(payment.id);
      else updated = await reversePayment(payment.id);
      setView({ kind: "details", payment: updated });
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
      { key: "create", label: t("pay.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !canCreate },
      { key: "back", label: t("pay.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("pay.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("pay.fieldVendor", language)}
            <select style={inputStyle} value={vendorId} onChange={(e) => { setVendorId(e.target.value); setAllocationAmounts({}); }}>
              <option value=""></option>
              {vendors.map((v) => <option key={v.id} value={v.id}>{v.name}</option>)}
            </select>
          </label>
          <label>{t("pay.fieldBankAccount", language)}
            <select style={inputStyle} value={bankAccountId} onChange={(e) => setBankAccountId(e.target.value)}>
              <option value=""></option>
              {bankAccounts.map((b) => <option key={b.id} value={b.id}>{b.accountCode} — {b.accountName}</option>)}
            </select>
          </label>
          <label>{t("pay.fieldPaymentMethod", language)}
            <select style={inputStyle} value={paymentMethod} onChange={(e) => setPaymentMethod(e.target.value)}>
              <option value=""></option>
              {paymentMethods.map((m) => <option key={m.code} value={m.code}>{m.name}</option>)}
            </select>
          </label>
          <label>{t("pay.fieldPaymentDate", language)}
            <input type="date" style={inputStyle} value={paymentDate} onChange={(e) => setPaymentDate(e.target.value)} />
          </label>
          <label>{t("pay.fieldReference", language)}
            <input style={inputStyle} value={reference} onChange={(e) => setReference(e.target.value)} />
          </label>
        </div>

        {vendorId && (
          <>
            {vendorOutstandingInvoices.length === 0 ? (
              <p>{t("pay.noOutstandingInvoices", language)}</p>
            ) : (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("pay.columnInvoice", language)}</th>
                    <th>{t("pay.columnOutstandingBalance", language)}</th>
                    <th>{t("pay.fieldAllocatedAmount", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {vendorOutstandingInvoices.map((invoice) => (
                    <tr key={invoice.id}>
                      <td><bdi dir="ltr">{invoice.documentNumber} — {invoice.vendorInvoiceNumber}</bdi></td>
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
              {t("pay.columnAmount", language)}: <bdi dir="ltr">{totalAllocated.toFixed(2)}</bdi>
            </p>
          </>
        )}
      </section>
    );
  }

  if (view.kind === "details") {
    const payment = view.payment;
    const actions: ActionItem[] = [];
    if (payment.status === "Draft")
      actions.push({ key: "submit", label: t("pay.actionSubmit", language), onClick: () => handleAction(payment, "submit"), variant: "primary", isDisabled: busy });
    if (payment.status === "Submitted") {
      actions.push({ key: "approve", label: t("pay.actionApprove", language), onClick: () => handleAction(payment, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("pay.actionReject", language), onClick: () => handleAction(payment, "reject"), isDisabled: busy });
    }
    if (payment.status === "Approved")
      actions.push({ key: "post", label: t("pay.actionPost", language), onClick: () => handleAction(payment, "post"), variant: "primary", isDisabled: busy });
    if (payment.status === "Posted")
      actions.push({ key: "reverse", label: t("pay.actionReverse", language), onClick: () => handleAction(payment, "reverse"), isDisabled: busy });
    actions.push({ key: "back", label: t("pay.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{payment.documentNumber ?? t("pay.newHeading", language)} — {vendorLabel(payment.vendorId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("pay.fieldVendor", language)}</dt>
                <dd>{vendorLabel(payment.vendorId)}</dd>
                <dt>{t("pay.fieldBankAccount", language)}</dt>
                <dd><bdi dir="ltr">{bankAccountLabel(payment.bankAccountId)}</bdi></dd>
                <dt>{t("pay.fieldPaymentMethod", language)}</dt>
                <dd>{paymentMethodLabel(payment.paymentMethod)}</dd>
                <dt>{t("pay.fieldPaymentDate", language)}</dt>
                <dd><bdi dir="ltr">{payment.paymentDate}</bdi></dd>
                {payment.reference && (
                  <>
                    <dt>{t("pay.fieldReference", language)}</dt>
                    <dd>{payment.reference}</dd>
                  </>
                )}
                <dt>{t("pay.columnAmount", language)}</dt>
                <dd><bdi dir="ltr">{payment.amount.toFixed(2)}</bdi></dd>
                <dt>{t("pay.columnStatus", language)}</dt>
                <dd>{translateStatus(payment.status, language)}</dd>
                {payment.linkedJournalEntryId && (
                  <>
                    <dt>{t("ap.linkedJournalEntry", language)}</dt>
                    <dd><bdi dir="ltr">{payment.linkedJournalEntryId}</bdi></dd>
                  </>
                )}
              </dl>
            ),
          },
          {
            key: "allocations",
            title: t("pay.tabAllocations", language),
            content: (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("pay.columnInvoice", language)}</th>
                    <th>{t("pay.fieldAllocatedAmount", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {payment.allocations.map((allocation) => (
                    <tr key={allocation.id}>
                      <td><bdi dir="ltr">{invoiceLabel(allocation.apInvoiceId)}</bdi></td>
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
    { key: "new", label: t("pay.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("pay.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {payments.length === 0 ? (
        <p>{t("pay.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("pay.columnDocumentNumber", language)}</th>
              <th>{t("pay.fieldVendor", language)}</th>
              <th>{t("pay.fieldPaymentDate", language)}</th>
              <th>{t("pay.columnAmount", language)}</th>
              <th>{t("pay.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {payments.map((payment) => (
              <tr key={payment.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", payment })}>
                <td><bdi dir="ltr">{payment.documentNumber}</bdi></td>
                <td>{vendorLabel(payment.vendorId)}</td>
                <td><bdi dir="ltr">{payment.paymentDate}</bdi></td>
                <td><bdi dir="ltr">{payment.amount.toFixed(2)}</bdi></td>
                <td>{translateStatus(payment.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
