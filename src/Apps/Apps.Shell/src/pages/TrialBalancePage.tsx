import { useCallback, useEffect, useState } from "react";
import { ActionPane, DonutChart, StatCard, StatIcon } from "@platform/ui";
import type { ActionItem, DonutChartSegment } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { getTrialBalance } from "../api/trialBalanceApi";
import type { TrialBalance } from "../api/trialBalanceApi";

interface TrialBalancePageProps {
  language: SupportedLanguageCode;
}

// Reuses the same GLAccount AccountType translation keys GLAccountsPage already established — one source
// of truth for "how do we say Asset/Liability/Equity/Revenue/Expense," not a second copy.
const accountTypeKeys: Record<string, "gl.accountTypeAsset" | "gl.accountTypeLiability" | "gl.accountTypeEquity" | "gl.accountTypeRevenue" | "gl.accountTypeExpense"> = {
  Asset: "gl.accountTypeAsset",
  Liability: "gl.accountTypeLiability",
  Equity: "gl.accountTypeEquity",
  Revenue: "gl.accountTypeRevenue",
  Expense: "gl.accountTypeExpense",
};

function translateAccountType(accountType: string, language: SupportedLanguageCode): string {
  const key = accountTypeKeys[accountType];
  return key ? t(key, language) : accountType;
}

function firstDayOfMonth(): string {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

const CATEGORY_COLORS = ["var(--pi-chart-1)", "var(--pi-chart-2)", "var(--pi-chart-3)", "var(--pi-chart-4)", "var(--pi-chart-5)"];

export function TrialBalancePage({ language }: TrialBalancePageProps) {
  const [periodStart, setPeriodStart] = useState(firstDayOfMonth());
  const [periodEnd, setPeriodEnd] = useState(todayIsoDate());
  const [data, setData] = useState<TrialBalance | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [showZeroBalance, setShowZeroBalance] = useState(true);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      setData(await getTrialBalance(periodStart, periodEnd));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [periodStart, periodEnd, language]);

  useEffect(() => {
    load();
  }, [load]);

  const actions: ActionItem[] = [
    { key: "refresh", label: t("tb.actionRefresh", language), onClick: load, variant: "primary", isDisabled: busy },
  ];

  const rows = (data?.accounts ?? []).filter(
    (a) => showZeroBalance || a.endingDebit !== 0 || a.endingCredit !== 0,
  );

  // Reconciled with a small epsilon, not exact equality — these are the sum of many independently rounded
  // decimal postings, so the same "compare with a tolerance" caution as any floating aggregation applies
  // even though the underlying values are decimal, not float.
  const isBalanced = data ? Math.abs(data.totalEndingDebit - data.totalEndingCredit) < 0.01 : false;

  const categoryTotals = new Map<string, number>();
  for (const a of data?.accounts ?? []) {
    if (a.isHeader) continue;
    const magnitude = Math.abs(a.endingDebit - a.endingCredit);
    categoryTotals.set(a.accountType, (categoryTotals.get(a.accountType) ?? 0) + magnitude);
  }
  const categoryTotalSum = [...categoryTotals.values()].reduce((sum, v) => sum + v, 0);
  const categorySegments: DonutChartSegment[] = [...categoryTotals.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([accountType, value], index) => ({
      key: accountType,
      label: translateAccountType(accountType, language),
      value,
      displayValue: categoryTotalSum > 0 ? `${((value / categoryTotalSum) * 100).toFixed(1)}%` : "0%",
      color: CATEGORY_COLORS[index % CATEGORY_COLORS.length],
    }));

  const topAccounts = (data?.accounts ?? [])
    .filter((a) => !a.isHeader)
    .map((a) => ({ ...a, magnitude: Math.abs(a.endingDebit - a.endingCredit) }))
    .sort((a, b) => b.magnitude - a.magnitude)
    .slice(0, 5);

  return (
    <section className="fin-report-page">
      <h1>{t("tb.heading", language)}</h1>
      <p className="fin-report__subtitle">{t("tb.subtitle", language)}</p>
      <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

      <div className="fin-report__filters">
        <label>
          {t("tb.fieldPeriodStart", language)}
          <input type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value)} />
        </label>
        <label>
          {t("tb.fieldPeriodEnd", language)}
          <input type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)} />
        </label>
        <label className="fin-report__checkbox">
          <input type="checkbox" checked={showZeroBalance} onChange={(e) => setShowZeroBalance(e.target.checked)} />
          {t("tb.fieldShowZeroBalance", language)}
        </label>
      </div>

      {!data && !error && <p>{t("status.loading", language)}</p>}

      {data && (
        <>
          <div className="fin-report__stats">
            <StatCard
              label={t("tb.statTotalAccounts", language)}
              value={String(data.accounts.filter((a) => !a.isHeader).length)}
              icon={<StatIcon icon="users" />} tone="var(--pi-chart-2)"
            />
            <StatCard
              label={t("tb.statTotalDebit", language)} value={data.totalEndingDebit.toFixed(2)}
              icon={<StatIcon icon="trendingUp" />} tone="var(--pi-chart-3)"
            />
            <StatCard
              label={t("tb.statTotalCredit", language)} value={data.totalEndingCredit.toFixed(2)}
              icon={<StatIcon icon="trendingDown" />} tone="var(--pi-chart-4)"
            />
            <StatCard
              label={t("tb.statStatus", language)}
              value={isBalanced ? t("tb.balanced", language) : t("tb.unbalanced", language)}
              icon={<StatIcon icon="scale" />} tone={isBalanced ? "var(--pi-success)" : "var(--pi-danger)"}
              trend={{
                label: isBalanced ? t("tb.balanced", language) : t("tb.unbalanced", language),
                direction: isBalanced ? "up" : "down",
              }}
            />
          </div>

          <div className="fin-report__layout">
            <div className="fin-report__main">
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("tb.columnAccountCode", language)}</th>
                    <th>{t("tb.columnAccountName", language)}</th>
                    <th>{t("tb.columnAccountType", language)}</th>
                    <th>{t("tb.columnOpeningDebit", language)}</th>
                    <th>{t("tb.columnOpeningCredit", language)}</th>
                    <th>{t("tb.columnPeriodDebit", language)}</th>
                    <th>{t("tb.columnPeriodCredit", language)}</th>
                    <th>{t("tb.columnEndingDebit", language)}</th>
                    <th>{t("tb.columnEndingCredit", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((a) => (
                    <tr key={a.accountId} className={a.isHeader ? "fin-report__row--header" : undefined}>
                      <td><bdi dir="ltr">{a.accountCode}</bdi></td>
                      <td style={{ paddingInlineStart: `${a.level * 1.25}rem` }}>{a.accountName}</td>
                      <td>{translateAccountType(a.accountType, language)}</td>
                      <td><bdi dir="ltr">{a.openingDebit.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{a.openingCredit.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{a.periodDebit.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{a.periodCredit.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{a.endingDebit.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{a.endingCredit.toFixed(2)}</bdi></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <aside className="fin-report__rail">
              <div className="fin-report__panel">
                <h2>{t("tb.panelBalanceByCategory", language)}</h2>
                {categorySegments.length > 0 ? (
                  <DonutChart segments={categorySegments} ariaLabel={t("tb.panelBalanceByCategory", language)} />
                ) : (
                  <p className="fin-report__panel-empty">{t("tb.panelEmpty", language)}</p>
                )}
              </div>
              <div className="fin-report__panel">
                <h2>{t("tb.panelTopAccounts", language)}</h2>
                {topAccounts.length > 0 ? (
                  <ul className="fin-report__top-accounts">
                    {topAccounts.map((a) => (
                      <li key={a.accountId}>
                        <span className="fin-report__top-account-name">
                          <bdi dir="ltr">{a.accountCode}</bdi> — {a.accountName}
                        </span>
                        <span className="fin-report__top-account-value"><bdi dir="ltr">{a.magnitude.toFixed(2)}</bdi></span>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="fin-report__panel-empty">{t("tb.panelEmpty", language)}</p>
                )}
              </div>
            </aside>
          </div>
        </>
      )}
    </section>
  );
}
