import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveBankAccount,
  createBankAccount,
  listBankAccounts,
  rejectBankAccount,
  submitBankAccount,
} from "../api/bankAccountApi";
import type { BankAccount, CreateBankAccountInput } from "../api/bankAccountApi";
import { listGLAccounts } from "../api/glAccountApi";
import type { GLAccount } from "../api/glAccountApi";

interface BankAccountsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; bankAccount: BankAccount };

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
};

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

export function BankAccountsPage({ language }: BankAccountsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([]);
  const [glAccounts, setGlAccounts] = useState<GLAccount[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateBankAccountInput>({
    accountCode: "",
    accountName: "",
    accountNameArabic: "",
    bankName: "",
    linkedGLAccountId: "",
    iban: "",
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [bankAccountResult, glAccountResult] = await Promise.all([
        listBankAccounts(200, 0),
        listGLAccounts(200, 0),
      ]);
      setBankAccounts(bankAccountResult.items);
      setGlAccounts(glAccountResult.items.filter((a) => a.isPostable && a.isActive));
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    if (view.kind === "list") load();
  }, [view.kind, load]);

  const glAccountLabel = (id: string) => {
    const account = glAccounts.find((a) => a.id === id);
    return account ? `${account.accountCode} — ${account.accountName}` : id;
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const input: CreateBankAccountInput = {
        accountCode: form.accountCode.trim(),
        accountName: form.accountName.trim(),
        bankName: form.bankName.trim(),
        linkedGLAccountId: form.linkedGLAccountId,
        accountNameArabic: form.accountNameArabic?.trim() || undefined,
        iban: form.iban?.trim() || undefined,
      };
      await createBankAccount(input);
      setForm({ accountCode: "", accountName: "", accountNameArabic: "", bankName: "", linkedGLAccountId: "", iban: "" });
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleStatusAction = async (bankAccount: BankAccount, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: BankAccount;
      if (action === "submit") updated = await submitBankAccount(bankAccount.id);
      else if (action === "approve") updated = await approveBankAccount(bankAccount.id);
      else updated = await rejectBankAccount(bankAccount.id);
      setView({ kind: "details", bankAccount: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const canCreate = Boolean(form.accountCode.trim() && form.accountName.trim() && form.bankName.trim() && form.linkedGLAccountId);
    const actions: ActionItem[] = [
      { key: "create", label: t("bank.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !canCreate },
      { key: "back", label: t("bank.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("bank.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("bank.fieldAccountCode", language)}
            <input style={inputStyle} value={form.accountCode} onChange={(e) => setForm({ ...form, accountCode: e.target.value })} />
          </label>
          <label>{t("bank.fieldAccountName", language)}
            <input style={inputStyle} value={form.accountName} onChange={(e) => setForm({ ...form, accountName: e.target.value })} />
          </label>
          <label>{t("bank.fieldAccountNameArabic", language)}
            <input dir="rtl" style={inputStyle} value={form.accountNameArabic ?? ""} onChange={(e) => setForm({ ...form, accountNameArabic: e.target.value })} />
          </label>
          <label>{t("bank.fieldBankName", language)}
            <input style={inputStyle} value={form.bankName} onChange={(e) => setForm({ ...form, bankName: e.target.value })} />
          </label>
          <label>{t("bank.fieldIban", language)}
            <input style={inputStyle} value={form.iban ?? ""} onChange={(e) => setForm({ ...form, iban: e.target.value })} />
          </label>
          <label>{t("bank.fieldLinkedGLAccount", language)}
            <select style={inputStyle} value={form.linkedGLAccountId} onChange={(e) => setForm({ ...form, linkedGLAccountId: e.target.value })}>
              <option value=""></option>
              {glAccounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
            </select>
          </label>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const bankAccount = view.bankAccount;
    const actions: ActionItem[] = [];
    if (bankAccount.status === "Draft")
      actions.push({ key: "submit", label: t("bank.actionSubmit", language), onClick: () => handleStatusAction(bankAccount, "submit"), variant: "primary", isDisabled: busy });
    if (bankAccount.status === "Submitted") {
      actions.push({ key: "approve", label: t("bank.actionApprove", language), onClick: () => handleStatusAction(bankAccount, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("bank.actionReject", language), onClick: () => handleStatusAction(bankAccount, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("bank.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{bankAccount.accountCode} — {bankAccount.accountName}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("bank.columnAccountCode", language)}</dt>
              <dd><bdi dir="ltr">{bankAccount.accountCode}</bdi></dd>
              <dt>{t("bank.columnAccountName", language)}</dt>
              <dd>{bankAccount.accountName}</dd>
              {bankAccount.accountNameArabic && (
                <>
                  <dt>{t("bank.fieldAccountNameArabic", language)}</dt>
                  <dd dir="rtl">{bankAccount.accountNameArabic}</dd>
                </>
              )}
              <dt>{t("bank.columnBankName", language)}</dt>
              <dd>{bankAccount.bankName}</dd>
              {bankAccount.iban && (
                <>
                  <dt>{t("bank.fieldIban", language)}</dt>
                  <dd><bdi dir="ltr">{bankAccount.iban}</bdi></dd>
                </>
              )}
              <dt>{t("bank.fieldLinkedGLAccount", language)}</dt>
              <dd><bdi dir="ltr">{glAccountLabel(bankAccount.linkedGLAccountId)}</bdi></dd>
              <dt>{t("bank.columnStatus", language)}</dt>
              <dd>{translateStatus(bankAccount.status, language)}</dd>
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("bank.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("bank.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {bankAccounts.length === 0 ? (
        <p>{t("bank.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("bank.columnAccountCode", language)}</th>
              <th>{t("bank.columnAccountName", language)}</th>
              <th>{t("bank.columnBankName", language)}</th>
              <th>{t("bank.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {bankAccounts.map((bankAccount) => (
              <tr key={bankAccount.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", bankAccount })}>
                <td><bdi dir="ltr">{bankAccount.accountCode}</bdi></td>
                <td>{bankAccount.accountName}</td>
                <td>{bankAccount.bankName}</td>
                <td>{translateStatus(bankAccount.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
