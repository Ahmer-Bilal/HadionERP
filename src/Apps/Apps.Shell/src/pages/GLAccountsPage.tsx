import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveGLAccount,
  createGLAccount,
  listGLAccounts,
  rejectGLAccount,
  submitGLAccount,
} from "../api/glAccountApi";
import type { CreateGLAccountInput, GLAccount } from "../api/glAccountApi";

interface GLAccountsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; account: GLAccount };

const accountTypeKeys: Record<string, "gl.accountTypeAsset" | "gl.accountTypeLiability" | "gl.accountTypeEquity" | "gl.accountTypeRevenue" | "gl.accountTypeExpense"> = {
  Asset: "gl.accountTypeAsset",
  Liability: "gl.accountTypeLiability",
  Equity: "gl.accountTypeEquity",
  Revenue: "gl.accountTypeRevenue",
  Expense: "gl.accountTypeExpense",
};

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
};

function translateAccountType(accountType: string, language: SupportedLanguageCode): string {
  const key = accountTypeKeys[accountType];
  return key ? t(key, language) : accountType;
}

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

export function GLAccountsPage({ language }: GLAccountsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [accounts, setAccounts] = useState<GLAccount[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateGLAccountInput>({
    accountCode: "",
    accountName: "",
    accountType: "Asset",
    accountNameArabic: "",
    isPostable: true,
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const result = await listGLAccounts(200, 0);
      setAccounts(result.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    if (view.kind === "list") load();
  }, [view.kind, load]);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const input: CreateGLAccountInput = {
        accountCode: form.accountCode.trim(),
        accountName: form.accountName.trim(),
        accountType: form.accountType,
        accountNameArabic: form.accountNameArabic?.trim() || undefined,
        isPostable: form.isPostable,
      };
      await createGLAccount(input);
      setForm({ accountCode: "", accountName: "", accountType: "Asset", accountNameArabic: "", isPostable: true });
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleStatusAction = async (account: GLAccount, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: GLAccount;
      if (action === "submit") updated = await submitGLAccount(account.id);
      else if (action === "approve") updated = await approveGLAccount(account.id);
      else updated = await rejectGLAccount(account.id);
      setView({ kind: "details", account: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("gl.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("gl.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("gl.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("gl.fieldAccountCode", language)}
            <input style={inputStyle} value={form.accountCode} onChange={(e) => setForm({ ...form, accountCode: e.target.value })} />
          </label>
          <label>{t("gl.fieldAccountName", language)}
            <input style={inputStyle} value={form.accountName} onChange={(e) => setForm({ ...form, accountName: e.target.value })} />
          </label>
          <label>{t("gl.fieldAccountNameArabic", language)}
            <input dir="rtl" style={inputStyle} value={form.accountNameArabic ?? ""} onChange={(e) => setForm({ ...form, accountNameArabic: e.target.value })} />
          </label>
          <label>{t("gl.fieldAccountType", language)}
            <select style={inputStyle} value={form.accountType} onChange={(e) => setForm({ ...form, accountType: e.target.value })}>
              <option value="Asset">{t("gl.accountTypeAsset", language)}</option>
              <option value="Liability">{t("gl.accountTypeLiability", language)}</option>
              <option value="Equity">{t("gl.accountTypeEquity", language)}</option>
              <option value="Revenue">{t("gl.accountTypeRevenue", language)}</option>
              <option value="Expense">{t("gl.accountTypeExpense", language)}</option>
            </select>
          </label>
          <label>
            <input type="checkbox" checked={form.isPostable} onChange={(e) => setForm({ ...form, isPostable: e.target.checked })} />
            {" "}{t("gl.fieldIsPostable", language)}
          </label>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const account = view.account;
    const actions: ActionItem[] = [];
    if (account.status === "Draft")
      actions.push({ key: "submit", label: t("gl.actionSubmit", language), onClick: () => handleStatusAction(account, "submit"), variant: "primary", isDisabled: busy });
    if (account.status === "Submitted") {
      actions.push({ key: "approve", label: t("gl.actionApprove", language), onClick: () => handleStatusAction(account, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("gl.actionReject", language), onClick: () => handleStatusAction(account, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("gl.actionBack", language), onClick: () => setView({ kind: "list" }) });

    const parentAccount = accounts.find((a) => a.id === account.parentAccountId);

    return (
      <section>
        <h1>{account.accountCode} — {account.accountName}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("gl.columnCode", language)}</dt>
              <dd>{account.accountCode}</dd>
              <dt>{t("gl.columnName", language)}</dt>
              <dd>{account.accountName}</dd>
              {account.accountNameArabic && (
                <>
                  <dt>{t("gl.fieldAccountNameArabic", language)}</dt>
                  <dd dir="rtl">{account.accountNameArabic}</dd>
                </>
              )}
              <dt>{t("gl.columnType", language)}</dt>
              <dd>{translateAccountType(account.accountType, language)}</dd>
              <dt>{t("gl.columnNormalBalance", language)}</dt>
              <dd><bdi dir="ltr">{account.normalBalance}</bdi></dd>
              <dt>{t("gl.fieldParentAccount", language)}</dt>
              <dd>{parentAccount ? `${parentAccount.accountCode} — ${parentAccount.accountName}` : t("gl.noParent", language)}</dd>
              <dt>{t("gl.columnStatus", language)}</dt>
              <dd>{translateStatus(account.status, language)}</dd>
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("gl.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("gl.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {accounts.length === 0 ? (
        <p>{t("gl.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("gl.columnCode", language)}</th>
              <th>{t("gl.columnName", language)}</th>
              <th>{t("gl.columnType", language)}</th>
              <th>{t("gl.columnNormalBalance", language)}</th>
              <th>{t("gl.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {accounts.map((account) => (
              <tr key={account.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", account })}>
                <td><bdi dir="ltr">{account.accountCode}</bdi></td>
                <td>{account.accountName}</td>
                <td>{translateAccountType(account.accountType, language)}</td>
                <td><bdi dir="ltr">{account.normalBalance}</bdi></td>
                <td>{translateStatus(account.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
