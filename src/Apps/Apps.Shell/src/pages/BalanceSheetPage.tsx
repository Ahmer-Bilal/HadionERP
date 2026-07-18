import { useCallback, useEffect, useState } from "react";
import { ActionPane, DonutChart, StatCard, StatIcon } from "@platform/ui";
import type { ActionItem, DonutChartSegment } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { getBalanceSheet } from "../api/balanceSheetApi";
import type { BalanceSheet } from "../api/balanceSheetApi";
import type { StatementLine } from "../api/incomeStatementApi";

interface BalanceSheetPageProps {
  language: SupportedLanguageCode;
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function oneYearAgoIsoDate(): string {
  const now = new Date();
  return new Date(now.getFullYear() - 1, now.getMonth(), now.getDate()).toISOString().slice(0, 10);
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

function StatementRows({ lines, showCompare, language }: { lines: StatementLine[]; showCompare: boolean; language: SupportedLanguageCode }) {
  return (
    <>
      {lines.map((l) => (
        <tr key={l.accountId ?? "retained-earnings"}>
          <td><bdi dir="ltr">{l.accountId ? l.accountCode : "—"}</bdi></td>
          <td>{l.accountId ? l.accountName : t("bs.retainedEarnings", language)}</td>
          <td><bdi dir="ltr">{l.amount.toFixed(2)}</bdi></td>
          {showCompare && <td><bdi dir="ltr">{(l.compareAmount ?? 0).toFixed(2)}</bdi></td>}
          {showCompare && <VarianceCell variance={l.variance} variancePercent={l.variancePercent} />}
        </tr>
      ))}
    </>
  );
}

export function BalanceSheetPage({ language }: BalanceSheetPageProps) {
  const [asOfDate, setAsOfDate] = useState(todayIsoDate());
  const [compareEnabled, setCompareEnabled] = useState(false);
  const [compareAsOfDate, setCompareAsOfDate] = useState(oneYearAgoIsoDate());
  const [data, setData] = useState<BalanceSheet | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      setData(await getBalanceSheet(asOfDate, compareEnabled ? compareAsOfDate : undefined));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [asOfDate, compareEnabled, compareAsOfDate, language]);

  useEffect(() => {
    load();
  }, [load]);

  const actions: ActionItem[] = [
    { key: "refresh", label: t("bs.actionRefresh", language), onClick: load, variant: "primary", isDisabled: busy },
  ];

  const showCompare = Boolean(data?.compareAsOfDate);
  const isBalanced = data ? Math.abs(data.totalAssets - data.totalLiabilitiesAndEquity) < 0.01 : false;

  // Magnitude-based, not signed-sum — matches how DonutChart itself sizes arcs (see its own doc comment),
  // so a negative total (e.g. an accumulated deficit) doesn't produce percentages that don't sum to 100%.
  const compositionTotal = data ? Math.abs(data.totalAssets) + Math.abs(data.totalLiabilities) + Math.abs(data.totalEquity) : 0;
  const compositionSegments: DonutChartSegment[] = data
    ? [
        {
          key: "assets",
          label: t("bs.totalAssets", language),
          value: data.totalAssets,
          displayValue: compositionTotal > 0 ? `${((Math.abs(data.totalAssets) / compositionTotal) * 100).toFixed(1)}%` : "0%",
          color: "var(--pi-chart-1)",
        },
        {
          key: "liabilities",
          label: t("bs.totalLiabilities", language),
          value: data.totalLiabilities,
          displayValue: compositionTotal > 0 ? `${((Math.abs(data.totalLiabilities) / compositionTotal) * 100).toFixed(1)}%` : "0%",
          color: "var(--pi-chart-4)",
        },
        {
          key: "equity",
          label: t("bs.totalEquity", language),
          value: data.totalEquity,
          displayValue: compositionTotal > 0 ? `${((Math.abs(data.totalEquity) / compositionTotal) * 100).toFixed(1)}%` : "0%",
          color: "var(--pi-chart-2)",
        },
      ]
    : [];

  return (
    <section className="fin-report-page">
      <h1>{t("bs.heading", language)}</h1>
      <p className="fin-report__subtitle">{t("bs.subtitle", language)}</p>
      <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

      <div className="fin-report__filters">
        <label>
          {t("bs.fieldAsOfDate", language)}
          <input type="date" value={asOfDate} onChange={(e) => setAsOfDate(e.target.value)} />
        </label>
        <label className="fin-report__checkbox">
          <input type="checkbox" checked={compareEnabled} onChange={(e) => setCompareEnabled(e.target.checked)} />
          {t("bs.fieldCompareEnabled", language)}
        </label>
        {compareEnabled && (
          <label>
            {t("bs.fieldCompareAsOfDate", language)}
            <input type="date" value={compareAsOfDate} onChange={(e) => setCompareAsOfDate(e.target.value)} />
          </label>
        )}
      </div>

      {!data && !error && <p>{t("status.loading", language)}</p>}

      {data && (
        <>
          <div className="fin-report__stats">
            <StatCard
              label={t("bs.totalAssets", language)} value={data.totalAssets.toFixed(2)}
              icon={<StatIcon icon="coins" />} tone="var(--pi-chart-2)"
            />
            <StatCard
              label={t("bs.totalLiabilities", language)} value={data.totalLiabilities.toFixed(2)}
              icon={<StatIcon icon="receipt" />} tone="var(--pi-chart-4)"
            />
            <StatCard
              label={t("bs.totalEquity", language)} value={data.totalEquity.toFixed(2)}
              icon={<StatIcon icon="layers" />} tone="var(--pi-chart-3)"
            />
            <StatCard
              label={t("bs.statStatus", language)}
              value={isBalanced ? t("bs.balanced", language) : t("bs.unbalanced", language)}
              icon={<StatIcon icon="scale" />} tone={isBalanced ? "var(--pi-success)" : "var(--pi-danger)"}
              trend={{
                label: isBalanced ? t("bs.balanced", language) : t("bs.unbalanced", language),
                direction: isBalanced ? "up" : "down",
              }}
            />
          </div>

          <div className="fin-report__layout">
            <div className="fin-report__main">
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("bs.columnAccountCode", language)}</th>
                    <th>{t("bs.columnAccountName", language)}</th>
                    <th>{t("bs.columnAmount", language)}</th>
                    {showCompare && <th>{t("bs.columnCompareAmount", language)}</th>}
                    {showCompare && <th>{t("bs.columnVariance", language)}</th>}
                  </tr>
                </thead>
                <tbody>
                  <tr className="fin-report__row--section">
                    <td colSpan={showCompare ? 5 : 3}>{t("bs.sectionAssets", language)}</td>
                  </tr>
                  <StatementRows lines={data.assetLines} showCompare={showCompare} language={language} />
                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("bs.totalAssets", language)}</td>
                    <td><bdi dir="ltr">{data.totalAssets.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareTotalAssets ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>

                  <tr className="fin-report__row--section">
                    <td colSpan={showCompare ? 5 : 3}>{t("bs.sectionLiabilities", language)}</td>
                  </tr>
                  <StatementRows lines={data.liabilityLines} showCompare={showCompare} language={language} />
                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("bs.totalLiabilities", language)}</td>
                    <td><bdi dir="ltr">{data.totalLiabilities.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareTotalLiabilities ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>

                  <tr className="fin-report__row--section">
                    <td colSpan={showCompare ? 5 : 3}>{t("bs.sectionEquity", language)}</td>
                  </tr>
                  <StatementRows lines={data.equityLines} showCompare={showCompare} language={language} />
                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("bs.totalEquity", language)}</td>
                    <td><bdi dir="ltr">{data.totalEquity.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareTotalEquity ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>

                  <tr className="fin-report__row--total">
                    <td></td>
                    <td>{t("bs.totalLiabilitiesAndEquity", language)}</td>
                    <td><bdi dir="ltr">{data.totalLiabilitiesAndEquity.toFixed(2)}</bdi></td>
                    {showCompare && <td><bdi dir="ltr">{(data.compareTotalLiabilitiesAndEquity ?? 0).toFixed(2)}</bdi></td>}
                    {showCompare && <td></td>}
                  </tr>
                </tbody>
              </table>
            </div>

            <aside className="fin-report__rail">
              <div className="fin-report__panel">
                <h2>{t("bs.panelComposition", language)}</h2>
                {compositionSegments.some((s) => s.value !== 0) ? (
                  <DonutChart segments={compositionSegments} ariaLabel={t("bs.panelComposition", language)} />
                ) : (
                  <p className="fin-report__panel-empty">{t("bs.panelEmpty", language)}</p>
                )}
              </div>
            </aside>
          </div>
        </>
      )}
    </section>
  );
}
