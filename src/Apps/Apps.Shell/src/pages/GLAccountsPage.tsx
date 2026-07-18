import { useCallback, useEffect, useMemo, useState } from "react";
import { ActionPane, DonutChart, FastTabs, StatCard, StatIcon } from "@platform/ui";
import type { ActionItem, DonutChartSegment } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveGLAccount,
  createGLAccount,
  deleteGLAccount,
  listGLAccounts,
  rejectGLAccount,
  submitGLAccount,
  updateGLAccount,
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

const CATEGORY_COLORS = ["var(--pi-chart-1)", "var(--pi-chart-2)", "var(--pi-chart-3)", "var(--pi-chart-4)", "var(--pi-chart-5)"];

/** Each account's hierarchy depth (0 = top level) — same iterative parent-walk approach as
 * TrialBalanceService.ComputeLevels on the backend (bounded by a visited-set so a data-integrity cycle
 * can't hang this), ported to the client since this page already has the full flat list with
 * ParentAccountId in hand and doesn't need a server round trip just to compute a depth number. */
function computeLevels(accounts: GLAccount[]): Map<string, number> {
  const byId = new Map(accounts.map((a) => [a.id, a] as const));
  const levels = new Map<string, number>();

  for (const account of accounts) {
    if (levels.has(account.id)) continue;

    const chain: string[] = [];
    const visited = new Set<string>();
    let currentId: string | null = account.id;
    while (currentId && !levels.has(currentId) && !visited.has(currentId)) {
      visited.add(currentId);
      chain.push(currentId);
      currentId = byId.get(currentId)?.parentAccountId ?? null;
    }

    const startLevel = currentId && levels.has(currentId) ? levels.get(currentId)! : -1;
    for (let i = chain.length - 1; i >= 0; i--) {
      levels.set(chain[i], startLevel + (chain.length - i));
    }
  }

  return levels;
}

function toCsvValue(value: string): string {
  return `"${value.replace(/"/g, '""')}"`;
}

function downloadChartOfAccountsCsv(accounts: GLAccount[], levels: Map<string, number>, language: SupportedLanguageCode) {
  const header = ["Account Code", "Account Name", "Account Type", "Status", "Level"];
  const rows = accounts.map((a) => [
    a.accountCode,
    a.accountName,
    translateAccountType(a.accountType, language),
    translateStatus(a.status, language),
    String((levels.get(a.id) ?? 0) + 1),
  ]);
  const csv = [header, ...rows].map((row) => row.map(toCsvValue).join(",")).join("\n");
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "chart-of-accounts.csv";
  link.click();
  URL.revokeObjectURL(url);
}

export function GLAccountsPage({ language }: GLAccountsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [accounts, setAccounts] = useState<GLAccount[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateGLAccountInput>({
    accountCode: "", accountName: "", accountType: "Asset", accountNameArabic: "", isPostable: true, parentAccountId: undefined,
  });

  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [kindFilter, setKindFilter] = useState<"" | "Header" | "Leaf">("");
  const [page, setPage] = useState(1);
  const [rowsPerPage, setRowsPerPage] = useState(15);

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

  useEffect(() => {
    setPage(1);
  }, [search, typeFilter, statusFilter, kindFilter]);

  const levels = useMemo(() => computeLevels(accounts), [accounts]);

  const filteredAccounts = useMemo(() => {
    const query = search.trim().toLowerCase();
    return accounts
      .filter((a) => !query || a.accountCode.toLowerCase().includes(query) || a.accountName.toLowerCase().includes(query))
      .filter((a) => !typeFilter || a.accountType === typeFilter)
      .filter((a) => !statusFilter || a.status === statusFilter)
      .filter((a) => !kindFilter || (kindFilter === "Header" ? !a.isPostable : a.isPostable))
      .sort((a, b) => a.accountCode.localeCompare(b.accountCode));
  }, [accounts, search, typeFilter, statusFilter, kindFilter]);

  const totalPages = Math.max(1, Math.ceil(filteredAccounts.length / rowsPerPage));
  const pagedAccounts = filteredAccounts.slice((page - 1) * rowsPerPage, page * rowsPerPage);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const input: CreateGLAccountInput = {
        accountCode: form.accountCode.trim(),
        accountName: form.accountName.trim(),
        accountType: form.accountType,
        accountNameArabic: form.accountNameArabic?.trim() || undefined,
        parentAccountId: form.parentAccountId || undefined,
        isPostable: form.isPostable,
      };
      await createGLAccount(input);
      setForm({ accountCode: "", accountName: "", accountType: "Asset", accountNameArabic: "", isPostable: true, parentAccountId: undefined });
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

  const handleToggleActive = async (account: GLAccount) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await updateGLAccount(account.id, {
        accountName: account.accountName,
        accountNameArabic: account.accountNameArabic ?? undefined,
        parentAccountId: account.parentAccountId,
        isPostable: account.isPostable,
        isActive: !account.isActive,
      });
      setView({ kind: "details", account: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async (account: GLAccount) => {
    setBusy(true);
    setError(null);
    try {
      await deleteGLAccount(account.id);
      setView({ kind: "list" });
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
    const headerCandidates = accounts.filter((a) => !a.isPostable);
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
          <label>{t("gl.fieldParentAccount", language)}
            <select style={inputStyle} value={form.parentAccountId ?? ""} onChange={(e) => setForm({ ...form, parentAccountId: e.target.value || undefined })}>
              <option value="">{t("gl.noParent", language)}</option>
              {headerCandidates.map((a) => (
                <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>
              ))}
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
    if (account.status === "Draft") {
      actions.push({ key: "submit", label: t("gl.actionSubmit", language), onClick: () => handleStatusAction(account, "submit"), variant: "primary", isDisabled: busy });
      actions.push({ key: "delete", label: t("gl.actionDelete", language), onClick: () => handleDelete(account), variant: "danger", isDisabled: busy });
    }
    if (account.status === "Submitted") {
      actions.push({ key: "approve", label: t("gl.actionApprove", language), onClick: () => handleStatusAction(account, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("gl.actionReject", language), onClick: () => handleStatusAction(account, "reject"), isDisabled: busy });
    }
    if (account.status === "Approved") {
      actions.push({
        key: "toggle-active",
        label: t(account.isActive ? "gl.actionDeactivate" : "gl.actionActivate", language),
        onClick: () => handleToggleActive(account),
        isDisabled: busy,
      });
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
              {account.status === "Approved" && (
                <>
                  <dt>{t("gl.fieldActiveStatus", language)}</dt>
                  <dd>{account.isActive ? t("gl.statusActive", language) : t("gl.statusInactive", language)}</dd>
                </>
              )}
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const totalAccounts = accounts.length;
  const activeAccounts = accounts.filter((a) => a.isActive).length;
  const headerAccounts = accounts.filter((a) => !a.isPostable).length;
  const leafAccounts = accounts.filter((a) => a.isPostable).length;
  const inactiveAccounts = accounts.filter((a) => !a.isActive).length;

  const categoryCounts = new Map<string, number>();
  for (const a of accounts) categoryCounts.set(a.accountType, (categoryCounts.get(a.accountType) ?? 0) + 1);
  const categorySegments: DonutChartSegment[] = [...categoryCounts.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([accountType, count], index) => ({
      key: accountType,
      label: translateAccountType(accountType, language),
      value: count,
      displayValue: totalAccounts > 0 ? `${count} (${((count / totalAccounts) * 100).toFixed(0)}%)` : "0",
      color: CATEGORY_COLORS[index % CATEGORY_COLORS.length],
    }));

  const levelCounts = new Map<number, number>();
  for (const a of accounts) {
    const lvl = levels.get(a.id) ?? 0;
    levelCounts.set(lvl, (levelCounts.get(lvl) ?? 0) + 1);
  }
  const levelRows = [...levelCounts.entries()].sort((a, b) => a[0] - b[0]);

  const recentlyUpdated = [...accounts]
    .filter((a) => a.modifiedAt)
    .sort((a, b) => new Date(b.modifiedAt!).getTime() - new Date(a.modifiedAt!).getTime())
    .slice(0, 5);

  const listActions: ActionItem[] = [
    { key: "export", label: t("gl.actionExport", language), onClick: () => downloadChartOfAccountsCsv(filteredAccounts, levels, language), isDisabled: filteredAccounts.length === 0 },
    { key: "new", label: t("gl.actionNewAccount", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  return (
    <section className="fin-report-page">
      <h1>{t("gl.heading", language)} ({totalAccounts})</h1>
      <p className="fin-report__subtitle">{t("gl.subtitle", language)}</p>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

      <div className="fin-report__stats">
        <StatCard
          label={t("gl.kpiTotalAccounts", language)} value={String(totalAccounts)}
          icon={<StatIcon icon="users" />} tone="var(--pi-chart-2)"
        />
        <StatCard
          label={t("gl.kpiActiveAccounts", language)} value={String(activeAccounts)}
          icon={<StatIcon icon="checkCircle" />} tone="var(--pi-success)"
          trend={totalAccounts > 0 ? { label: `${((activeAccounts / totalAccounts) * 100).toFixed(1)}%`, direction: "up" } : undefined}
        />
        <StatCard
          label={t("gl.kpiHeaderAccounts", language)} value={String(headerAccounts)}
          icon={<StatIcon icon="layers" />} tone="var(--pi-chart-3)"
        />
        <StatCard
          label={t("gl.kpiLeafAccounts", language)} value={String(leafAccounts)}
          icon={<StatIcon icon="leaf" />} tone="var(--pi-chart-4)"
        />
        <StatCard
          label={t("gl.kpiInactiveAccounts", language)} value={String(inactiveAccounts)}
          icon={<StatIcon icon="power" />} tone="var(--pi-chart-5)"
          trend={totalAccounts > 0 ? { label: `${((inactiveAccounts / totalAccounts) * 100).toFixed(1)}%`, direction: "down" } : undefined}
        />
      </div>

      <div className="fin-report__filters">
        <label>
          {t("gl.filterSearchLabel", language)}
          <input type="search" placeholder={t("gl.filterSearchPlaceholder", language)} value={search} onChange={(e) => setSearch(e.target.value)} />
        </label>
        <label>
          {t("gl.fieldAccountType", language)}
          <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
            <option value="">{t("gl.filterAllTypes", language)}</option>
            <option value="Asset">{t("gl.accountTypeAsset", language)}</option>
            <option value="Liability">{t("gl.accountTypeLiability", language)}</option>
            <option value="Equity">{t("gl.accountTypeEquity", language)}</option>
            <option value="Revenue">{t("gl.accountTypeRevenue", language)}</option>
            <option value="Expense">{t("gl.accountTypeExpense", language)}</option>
          </select>
        </label>
        <label>
          {t("gl.columnStatus", language)}
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="">{t("gl.filterAllStatuses", language)}</option>
            <option value="Draft">{t("bp.statusDraft", language)}</option>
            <option value="Submitted">{t("bp.statusSubmitted", language)}</option>
            <option value="Approved">{t("bp.statusApproved", language)}</option>
            <option value="Rejected">{t("bp.statusRejected", language)}</option>
          </select>
        </label>
        <label>
          {t("gl.filterKindLabel", language)}
          <select value={kindFilter} onChange={(e) => setKindFilter(e.target.value as "" | "Header" | "Leaf")}>
            <option value="">{t("gl.filterAllKinds", language)}</option>
            <option value="Header">{t("gl.filterKindHeader", language)}</option>
            <option value="Leaf">{t("gl.filterKindLeaf", language)}</option>
          </select>
        </label>
      </div>

      {!busy && filteredAccounts.length === 0 ? (
        <p>{t("gl.emptyState", language)}</p>
      ) : (
        <div className="fin-report__layout">
          <div className="fin-report__main">
            <table className="pi-dense-table">
              <thead>
                <tr>
                  <th>{t("gl.columnCode", language)}</th>
                  <th>{t("gl.columnName", language)}</th>
                  <th>{t("gl.fieldAccountType", language)}</th>
                  <th>{t("gl.columnStatus", language)}</th>
                  <th>{t("gl.columnLevel", language)}</th>
                </tr>
              </thead>
              <tbody>
                {pagedAccounts.map((account) => (
                  <tr
                    key={account.id}
                    style={{ cursor: "pointer" }}
                    onClick={() => setView({ kind: "details", account })}
                  >
                    <td>
                      <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); setView({ kind: "details", account }); }}>
                        <bdi dir="ltr">{account.accountCode}</bdi>
                      </button>
                    </td>
                    <td>
                      {account.accountName}
                      {!account.isPostable && <span className="gl-badge-header">{t("gl.badgeHeader", language)}</span>}
                    </td>
                    <td>{translateAccountType(account.accountType, language)}</td>
                    <td>
                      <span className={`gl-status-pill gl-status-pill--${account.isActive ? "active" : "inactive"}`}>
                        {account.isActive ? t("gl.statusActive", language) : translateStatus(account.status, language)}
                      </span>
                    </td>
                    <td>{(levels.get(account.id) ?? 0) + 1}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="fin-report__pagination">
              <span>
                {t("gl.paginationShowing", language)
                  .replace("{from}", filteredAccounts.length === 0 ? "0" : String((page - 1) * rowsPerPage + 1))
                  .replace("{to}", String(Math.min(page * rowsPerPage, filteredAccounts.length)))
                  .replace("{total}", String(filteredAccounts.length))}
              </span>
              <div className="fin-report__pagination-controls">
                <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>‹</button>
                <span>{page} / {totalPages}</span>
                <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>›</button>
                <label className="fin-report__rows-per-page">
                  {t("gl.rowsPerPage", language)}
                  <select value={rowsPerPage} onChange={(e) => { setRowsPerPage(Number(e.target.value)); setPage(1); }}>
                    <option value={15}>15</option>
                    <option value={25}>25</option>
                    <option value={50}>50</option>
                  </select>
                </label>
              </div>
            </div>
          </div>

          <aside className="fin-report__rail">
            <div className="fin-report__panel">
              <h2>{t("gl.panelDocumentFlow", language)}</h2>
              <ol className="gl-doc-flow">
                <li><strong>{t("gl.flowStepCreate", language)}</strong><span>{t("gl.flowStepCreateDesc", language)}</span></li>
                <li><strong>{t("gl.flowStepClassify", language)}</strong><span>{t("gl.flowStepClassifyDesc", language)}</span></li>
                <li><strong>{t("gl.flowStepStructure", language)}</strong><span>{t("gl.flowStepStructureDesc", language)}</span></li>
                <li><strong>{t("gl.flowStepReview", language)}</strong><span>{t("gl.flowStepReviewDesc", language)}</span></li>
                <li className="gl-doc-flow__final"><strong>{t("gl.flowStepActive", language)}</strong><span>{t("gl.flowStepActiveDesc", language)}</span></li>
              </ol>
            </div>

            <div className="fin-report__panel">
              <h2>{t("gl.panelAccountStructure", language)}</h2>
              <ul className="gl-structure-list">
                {levelRows.map(([lvl, count], index) => (
                  <li key={lvl}>
                    <span className="gl-structure-dot" style={{ background: CATEGORY_COLORS[index % CATEGORY_COLORS.length] }} aria-hidden="true" />
                    <span>{t("gl.structureLevelLabel", language).replace("{n}", String(lvl + 1))}</span>
                    <strong>{count}</strong>
                  </li>
                ))}
              </ul>
            </div>

            <div className="fin-report__panel">
              <h2>{t("gl.panelTopCategories", language)}</h2>
              {categorySegments.length > 0 ? (
                <DonutChart segments={categorySegments} ariaLabel={t("gl.panelTopCategories", language)} />
              ) : (
                <p className="fin-report__panel-empty">{t("gl.emptyState", language)}</p>
              )}
            </div>

            <div className="fin-report__panel">
              <h2>{t("gl.panelRecentlyUpdated", language)}</h2>
              {recentlyUpdated.length > 0 ? (
                <ul className="fin-report__top-accounts">
                  {recentlyUpdated.map((a) => (
                    <li key={a.id}>
                      <span className="fin-report__top-account-name"><bdi dir="ltr">{a.accountCode}</bdi> — {a.accountName}</span>
                      <span className="fin-report__top-account-value">{new Date(a.modifiedAt!).toLocaleDateString(language === "ar" ? "ar-SA" : "en-US")}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="fin-report__panel-empty">{t("gl.recentlyUpdatedEmpty", language)}</p>
              )}
            </div>
          </aside>
        </div>
      )}
    </section>
  );
}
