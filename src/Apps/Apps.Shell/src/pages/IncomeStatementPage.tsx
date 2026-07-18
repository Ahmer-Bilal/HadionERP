import { useCallback, useEffect, useState } from "react";
import { ActionPane, DonutChart, StatCard, StatIcon } from "@platform/ui";
import type { ActionItem, DonutChartSegment } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { getIncomeStatement } from "../api/incomeStatementApi";
import type { IncomeStatement, StatementLine } from "../api/incomeStatementApi";

interface IncomeStatementPageProps {
  language: SupportedLanguageCode;
}

function firstDayOfMonth(): string {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function firstDayOfPreviousMonth(): string {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString().slice(0, 10);
}

function lastDayOfPreviousMonth(): string {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), 0).toISOString().slice(0, 10);
}

function VarianceCell({ variance, variancePercent }: { variance: number | null; variancePercent: number | null }) {
  if (variance === null) return <td>—</td>;
  const negative = variance < 0;
  return (
    <td className={negative ? "fin-report__negative" : undefined}>
      <bdi dir="ltr">{variance.toFixed(2)}{variancePercent !== null ? ` (${variancePercent.toFixed(1)}%)` : ""}</bdi>
    </td>
  );
}

function StatementRows({ lines, showCompare }: { lines: StatementLine[]; showCompare: boolean }) {
  return (
    <>
      {lines.map((l) => (
        <tr key={l.accountId ?? l.accountCode}>
          <td><bdi dir="ltr">{l.accountCode}</bdi></td>
          <td>{l.accountName}</td>
          <td><bdi dir="ltr">{l.amount.toFixed(2)}</bdi></td>
          {showCompare && <td><bdi dir="ltr">{(l.compareAmount ?? 0).toFixed(2)}</bdi></td>}
          {showCompare && <VarianceCell variance={l.variance} variancePercent={l.variancePercent} />}
        </tr>
      ))}
    </>
  );
}

export function IncomeStatementPage({ language }: IncomeStatementPageProps) {
  const [periodStart, setPeriodStart] = useState(firstDayOfMonth());
  const [periodEnd, setPeriodEnd] = useState(todayIsoDate());
  const [compareEnabled, setCompareEnabled] = useState(false);
  const [comparePeriodStart, setComparePeriodStart] = useState(firstDayOfPreviousMonth());
  const [comparePeriodEnd, setComparePeriodEnd] = useState(lastDayOfPreviousMonth());
  const [data, setData] = useState<IncomeStatement | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      setData(await getIncomeStatement(
        periodStart, periodEnd,
        compareEnabled ? comparePeriodStart : undefined,
        compareEnabled ? comparePeriodEnd : undefined,
      ));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [periodStart, periodEnd, compareEnabled, comparePeriodStart, comparePeriodEnd, language]);

  useEffect(() => {
    load();
  }, [load]);

  const actions: ActionItem[] = [
    { key: "refresh", label: t("is.actionRefresh", language), onClick: load, variant: "primary", isDisabled: busy },
  ];

  const showCompare = Boolean(data?.compareTotalRevenue !== undefined && data?.comparePeriodStart);
  const netMargin = data && data.totalRevenue !== 0 ? (data.netProfit / data.totalRevenue) * 100 : null;

  // Magnitude-based, not signed-sum — matches how DonutChart itself sizes arcs (see its own doc comment),
  // so a negative total (e.g. a contra-posted account) doesn't produce percentages that don't sum to 100%.
  const compositionTotal = data ? Math.abs(data.totalRevenue) + Math.abs(data.totalExpenses) : 0;
  const compositionSegments: DonutChartSegment[] = data
    ? [
        {
          key: "revenue",
          label: t("is.totalRevenue", language),
          value: data.totalRevenue,
          displayValue: compositionTotal > 0 ? `${((Math.abs(data.totalRevenue) / compositionTotal) * 100).toFixed(1)}%` : "0%",
          color: "var(--pi-chart-1)",
        },
        {
          key: "expenses",
          label: t("is.totalExpenses", language),
          value: data.totalExpenses,
          displayValue: compositionTotal > 0 ? `${((Math.abs(data.totalExpenses) / compositionTotal) * 100).toFixed(1)}%` : "0%",
          color: "var(--pi-chart-4)",
        },
      ]
    : [];

  return (
    <section className="fin-report-page">
      <h1>{t("is.heading", language)}</h1>
      <p className="fin-report__subtitle">{t("is.subtitle", language)}</p>
      <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

      <div className="fin-report__filters">
        <label>
          {t("is.fieldPeriodStart", language)}
          <input type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value)} />
        </label>
        <label>
          {t("is.fieldPeriodEnd", language)}
          <input type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)} />
        </label>
        <label className="fin-report__checkbox">
          <input type="checkbox" checked={compareEnabled} onChange={(e) => setCompareEnabled(e.target.checked)} />
          {t("is.fieldCompareEnabled", language)}
        </label>
        {compareEnabled && (
          <>
            <label>
              {t("is.fieldComparePeriodStart", language)}
              <input type="date" value={comparePeriodStart} onChange={(e) => setComparePeriodStart(e.target.value)} />
            </label>
            <label>
              {t("is.fieldComparePeriodEnd", language)}
              <input type="date" value={comparePeriodEnd} onChange={(e) => setComparePeriodEnd(e.target.value)} />
            </label>
          </>
        )}
      </div>

      {!data && !error && <p>{t("status.loading", language)}</p>}

      {data && (
        <>
          <div className="fin-report__stats">
            <StatCard
              label={t("is.totalRevenue", language)} value={data.totalRevenue.toFixed(2)}
              icon={<StatIcon icon="trendingUp" />} tone="var(--pi-chart-2)"
            />
            <StatCard
              label={t("is.totalExpenses", language)} value={data.totalExpenses.toFixed(2)}
              icon={<StatIcon icon="trendingDown" />} tone="var(--pi-chart-4)"
            />
            <StatCard
              label={t("is.netProfit", language)}
              value={data.netProfit.toFixed(2)}
              icon={<StatIcon icon="coins" />} tone={data.netProfit >= 0 ? "var(--pi-success)" : "var(--pi-danger)"}
              trend={netMargin !== null ? { label: `${netMargin.toFixed(1)}%`, direction: data.netProfit >= 0 ? "up" : "down" } : undefined}
            />
            {showCompare && (
              <StatCard
                label={t("is.compareNetProfit", language)} value={(data.compareNetProfit ?? 0).toFixed(2)}
                icon={<StatIcon icon="coins" />} tone="var(--pi-chart-3)"
              />
            )}
          </div>

          <div className="fin-report__layout">
            <div className="fin-report__main">
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("is.columnAccountCode", language)}</th>
                    <th>{t("is.columnAccountName", language)}</th>
                    <th>{t("is.columnAmount", language)}</th>
                    {showCompare && <th>{t("is.columnCompareAmount", language)}</th>}
                    {showCompare && <th>{t("is.columnVariance", language)}</th>}
                  </tr>
                </thead>
                <tbody>
                  <tr className="fin-report__row--section">
                    <td colSpan={showCompare ? 5 : 3}>{t("is.sectionRevenue", language)}</td>
                  </tr>
                  <StatementRows lines={data.revenueLines} showCompare={showCompare} />
                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("is.totalRevenue", language)}</td>
                    <td><bdi dir="ltr">{data.totalRevenue.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareTotalRevenue ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>

                  <tr className="fin-report__row--section">
                    <td colSpan={showCompare ? 5 : 3}>{t("is.sectionExpenses", language)}</td>
                  </tr>
                  <StatementRows lines={data.expenseLines} showCompare={showCompare} />
                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("is.totalExpenses", language)}</td>
                    <td><bdi dir="ltr">{data.totalExpenses.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareTotalExpenses ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>

                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("is.netProfit", language)}</td>
                    <td><bdi dir="ltr">{data.netProfit.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareNetProfit ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>
                </tbody>
              </table>
            </div>

            <aside className="fin-report__rail">
              <div className="fin-report__panel">
                <h2>{t("is.panelComposition", language)}</h2>
                {compositionSegments.some((s) => s.value !== 0) ? (
                  <DonutChart segments={compositionSegments} ariaLabel={t("is.panelComposition", language)} />
                ) : (
                  <p className="fin-report__panel-empty">{t("is.panelEmpty", language)}</p>
                )}
              </div>
            </aside>
          </div>
        </>
      )}
    </section>
  );
}
