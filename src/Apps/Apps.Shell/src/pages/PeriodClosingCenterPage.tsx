import { useCallback, useEffect, useMemo, useState } from "react";
import { ActionPane, DonutChart, StatCard, StatIcon } from "@platform/ui";
import type { ActionItem, DonutChartSegment } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  assignClosingActivity,
  closePeriod,
  createFiscalYear,
  getActivityLog,
  getClosingChecklist,
  getCompletionTrend,
  getInsights,
  listAssignableUsers,
  listFiscalYears,
  reopenPeriod,
  setClosingActivityBlocked,
  setTargetCloseDate,
  toggleClosingActivityStep,
} from "../api/fiscalYearApi";
import type {
  ActiveUser,
  ClosingActivity,
  ClosingActivityLogEntry,
  ClosingInsight,
  CompletionTrendPoint,
  FiscalYear,
} from "../api/fiscalYearApi";

interface PeriodClosingCenterPageProps {
  language: SupportedLanguageCode;
}

type TabKey = "checklist" | "posting" | "reconciliation" | "journal" | "history";

const STATUS_COLORS: Record<string, string> = {
  Completed: "var(--pi-success)",
  InProgress: "var(--pi-chart-2)",
  NotStarted: "var(--pi-border)",
  Blocked: "var(--pi-danger)",
};

const statusKey: Record<string, "pcc.statusCompleted" | "pcc.statusInProgress" | "pcc.statusNotStarted" | "pcc.statusBlocked"> = {
  Completed: "pcc.statusCompleted",
  InProgress: "pcc.statusInProgress",
  NotStarted: "pcc.statusNotStarted",
  Blocked: "pcc.statusBlocked",
};

// Derived grouping of the ten real checklist activities into the mockup's own five-phase timeline —
// presentation only, no separate backend concept (see docs/module/finance.md for why).
const TIMELINE_PHASES: { key: string; labelKey: "pcc.phaseStart" | "pcc.phaseReconciliations" | "pcc.phaseSubLedger" | "pcc.phaseFinalReview" | "pcc.phaseClose"; activityKeys: string[] }[] = [
  { key: "start", labelKey: "pcc.phaseStart", activityKeys: [] },
  { key: "reconciliations", labelKey: "pcc.phaseReconciliations", activityKeys: ["BankReconciliation"] },
  { key: "subledger", labelKey: "pcc.phaseSubLedger", activityKeys: ["AccountsPayable", "AccountsReceivable", "InventoryClosing", "PayrollPosting", "FixedAssets", "TaxValidation", "CostAllocation"] },
  { key: "finalreview", labelKey: "pcc.phaseFinalReview", activityKeys: ["JournalReview"] },
  { key: "close", labelKey: "pcc.phaseClose", activityKeys: ["ManagementReview"] },
];

function activityTitleKey(activityKey: string): string {
  const map: Record<string, string> = {
    BankReconciliation: "pcc.activityBankReconciliation",
    AccountsPayable: "pcc.activityAccountsPayable",
    AccountsReceivable: "pcc.activityAccountsReceivable",
    InventoryClosing: "pcc.activityInventoryClosing",
    PayrollPosting: "pcc.activityPayrollPosting",
    FixedAssets: "pcc.activityFixedAssets",
    TaxValidation: "pcc.activityTaxValidation",
    CostAllocation: "pcc.activityCostAllocation",
    JournalReview: "pcc.activityJournalReview",
    ManagementReview: "pcc.activityManagementReview",
  };
  return map[activityKey] ?? activityKey;
}

function activityDescriptionKey(activityKey: string): string {
  const map: Record<string, string> = {
    BankReconciliation: "pcc.activityDescBankReconciliation",
    AccountsPayable: "pcc.activityDescAccountsPayable",
    AccountsReceivable: "pcc.activityDescAccountsReceivable",
    InventoryClosing: "pcc.activityDescInventoryClosing",
    PayrollPosting: "pcc.activityDescPayrollPosting",
    FixedAssets: "pcc.activityDescFixedAssets",
    TaxValidation: "pcc.activityDescTaxValidation",
    CostAllocation: "pcc.activityDescCostAllocation",
    JournalReview: "pcc.activityDescJournalReview",
    ManagementReview: "pcc.activityDescManagementReview",
  };
  return map[activityKey] ?? activityKey;
}

export function PeriodClosingCenterPage({ language }: PeriodClosingCenterPageProps) {
  const [fiscalYears, setFiscalYears] = useState<FiscalYear[]>([]);
  const [selectedYearId, setSelectedYearId] = useState<string>("");
  const [selectedPeriodNumber, setSelectedPeriodNumber] = useState<number>(0);
  const [activeTab, setActiveTab] = useState<TabKey>("checklist");

  const [activities, setActivities] = useState<ClosingActivity[]>([]);
  const [insights, setInsights] = useState<ClosingInsight[]>([]);
  const [activityLog, setActivityLog] = useState<ClosingActivityLogEntry[]>([]);
  const [trend, setTrend] = useState<CompletionTrendPoint[]>([]);
  const [assignableUsers, setAssignableUsers] = useState<ActiveUser[]>([]);
  const [assigningActivityId, setAssigningActivityId] = useState<string | null>(null);

  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [newYearInput, setNewYearInput] = useState(String(new Date().getFullYear()));

  const loadYears = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const years = await listFiscalYears();
      setFiscalYears(years);
      if (years.length > 0 && !selectedYearId) {
        setSelectedYearId(years[0].id);
        const currentMonth = new Date().getMonth() + 1;
        const defaultPeriod = years[0].periods.find((p) => p.periodNumber === currentMonth) ?? years[0].periods[0];
        setSelectedPeriodNumber(defaultPeriod.periodNumber);
      }
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [language]);

  useEffect(() => { loadYears(); }, [loadYears]);
  useEffect(() => { listAssignableUsers().then(setAssignableUsers).catch(() => undefined); }, []);

  const selectedYear = fiscalYears.find((y) => y.id === selectedYearId);
  const selectedPeriod = selectedYear?.periods.find((p) => p.periodNumber === selectedPeriodNumber);

  const loadPeriodData = useCallback(async () => {
    if (!selectedYearId || !selectedPeriodNumber) return;
    setBusy(true);
    setError(null);
    try {
      const [checklist, insightList, log, trendPoints] = await Promise.all([
        getClosingChecklist(selectedYearId, selectedPeriodNumber),
        getInsights(selectedYearId, selectedPeriodNumber),
        getActivityLog(selectedYearId, selectedPeriodNumber),
        getCompletionTrend(selectedYearId, selectedPeriodNumber),
      ]);
      setActivities(checklist);
      setInsights(insightList);
      setActivityLog(log);
      setTrend(trendPoints);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedYearId, selectedPeriodNumber, language]);

  useEffect(() => { loadPeriodData(); }, [loadPeriodData]);

  const refreshFiscalYearsAndPeriod = async () => {
    const years = await listFiscalYears();
    setFiscalYears(years);
  };

  const handleCreateYear = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createFiscalYear(Number(newYearInput));
      await refreshFiscalYearsAndPeriod();
      setSelectedYearId(created.id);
      setSelectedPeriodNumber(created.periods[0].periodNumber);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleToggleLock = async () => {
    if (!selectedYearId || !selectedPeriod) return;
    setBusy(true);
    setError(null);
    try {
      if (selectedPeriod.isOpen) await closePeriod(selectedYearId, selectedPeriodNumber);
      else await reopenPeriod(selectedYearId, selectedPeriodNumber);
      await refreshFiscalYearsAndPeriod();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAssign = async (activityId: string, userId: string) => {
    setBusy(true);
    setError(null);
    try {
      await assignClosingActivity(activityId, userId, null);
      setAssigningActivityId(null);
      await loadPeriodData();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleToggleStep = async (activityId: string, stepId: string, isCompleted: boolean) => {
    setBusy(true);
    setError(null);
    try {
      await toggleClosingActivityStep(activityId, stepId, isCompleted);
      await loadPeriodData();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleToggleBlocked = async (activityId: string, isBlocked: boolean) => {
    setBusy(true);
    setError(null);
    try {
      await setClosingActivityBlocked(activityId, isBlocked);
      await loadPeriodData();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleEditTargetCloseDate = async (newDate: string) => {
    if (!selectedYearId) return;
    setBusy(true);
    setError(null);
    try {
      await setTargetCloseDate(selectedYearId, selectedPeriodNumber, newDate);
      await refreshFiscalYearsAndPeriod();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const totalSteps = activities.reduce((sum, a) => sum + a.totalSteps, 0);
  const completedSteps = activities.reduce((sum, a) => sum + a.completedSteps, 0);
  const overallPercent = totalSteps > 0 ? Math.round((completedSteps / totalSteps) * 100) : (activities.every((a) => a.status === "Completed") ? 100 : 0);

  const statusCounts = useMemo(() => {
    const counts: Record<string, number> = { Completed: 0, InProgress: 0, NotStarted: 0, Blocked: 0 };
    for (const a of activities) counts[a.status] = (counts[a.status] ?? 0) + 1;
    return counts;
  }, [activities]);

  const progressSegments: DonutChartSegment[] = (["Completed", "InProgress", "NotStarted", "Blocked"] as const)
    .filter((s) => statusCounts[s] > 0)
    .map((s) => ({
      key: s, label: t(statusKey[s], language), value: statusCounts[s],
      displayValue: String(statusCounts[s]), color: STATUS_COLORS[s],
    }));

  const lastClosedPeriod = selectedYear?.periods
    .filter((p) => !p.isOpen && p.periodNumber < selectedPeriodNumber)
    .sort((a, b) => b.periodNumber - a.periodNumber)[0];

  const daysInPeriod = selectedPeriod
    ? Math.round((new Date(selectedPeriod.endDate).getTime() - new Date(selectedPeriod.startDate).getTime()) / 86400000) + 1
    : 0;

  const responsibleTeam = useMemo(() => {
    const map = new Map<string, { name: string; role: string | null }>();
    for (const a of activities) {
      if (a.assignedToUserId && a.assignedToDisplayName) {
        map.set(a.assignedToUserId, { name: a.assignedToDisplayName, role: a.assignedToRoleKey });
      }
    }
    return [...map.values()];
  }, [activities]);

  const userLabel = (id: string) => assignableUsers.find((u) => u.id === id)?.displayName ?? id;

  if (fiscalYears.length === 0 && !busy) {
    return (
      <section>
        <h1>{t("pcc.heading", language)}</h1>
        <p>{t("pcc.noFiscalYears", language)}</p>
        <div style={{ display: "flex", gap: "0.5rem", alignItems: "end", maxInlineSize: "24rem" }}>
          <label style={{ flex: 1 }}>{t("pcc.fieldYear", language)}
            <input type="number" value={newYearInput} onChange={(e) => setNewYearInput(e.target.value)} style={{ display: "block", inlineSize: "100%", padding: "0.3rem" }} />
          </label>
          <button type="button" onClick={handleCreateYear} disabled={busy}>
            {t("pcc.actionCreateFiscalYear", language)}
          </button>
        </div>
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      </section>
    );
  }

  const actions: ActionItem[] = [];

  return (
    <section className="fin-report-page pcc-page">
      <h1>{t("pcc.heading", language)}</h1>
      <p className="fin-report__subtitle">{t("pcc.subtitle", language)}</p>
      <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

      <div className="pcc-selectors">
        <label>{t("pcc.fieldFiscalYear", language)}
          <select value={selectedYearId} onChange={(e) => {
            setSelectedYearId(e.target.value);
            const year = fiscalYears.find((y) => y.id === e.target.value);
            if (year) setSelectedPeriodNumber(year.periods[0].periodNumber);
          }}>
            {fiscalYears.map((y) => <option key={y.id} value={y.id}>{y.year}</option>)}
          </select>
        </label>
        <label>{t("pcc.fieldPeriod", language)}
          <select value={selectedPeriodNumber} onChange={(e) => setSelectedPeriodNumber(Number(e.target.value))}>
            {selectedYear?.periods.map((p) => (
              <option key={p.id} value={p.periodNumber}>
                {new Date(p.startDate).toLocaleDateString(language === "ar" ? "ar-SA" : "en-US", { month: "long", year: "numeric" })}
              </option>
            ))}
          </select>
        </label>
        <div style={{ display: "flex", gap: "0.4rem", alignItems: "end" }}>
          <input type="number" value={newYearInput} onChange={(e) => setNewYearInput(e.target.value)} style={{ inlineSize: "6rem", padding: "0.3rem" }} />
          <button type="button" onClick={handleCreateYear} disabled={busy}>{t("pcc.actionCreateFiscalYear", language)}</button>
        </div>
      </div>

      {selectedPeriod && (
        <>
          <div className="fin-report__stats pcc-overview-stats">
            <div className="pi-stat-card pcc-progress-card">
              <DonutChart
                segments={progressSegments.length > 0 ? progressSegments : [{ key: "empty", label: "", displayValue: "", value: 1, color: "var(--pi-border)" }]}
                ariaLabel={t("pcc.overallProgress", language)}
                size={90} thickness={12}
                centerValue={`${overallPercent}%`}
                centerLabel={t("pcc.complete", language)}
              />
              <ul className="pcc-progress-legend">
                {(["Completed", "InProgress", "NotStarted", "Blocked"] as const).map((s) => (
                  <li key={s}>
                    <span className="gl-structure-dot" style={{ background: STATUS_COLORS[s] }} aria-hidden="true" />
                    {t(statusKey[s], language)}: <strong>{statusCounts[s] ?? 0}</strong>
                  </li>
                ))}
              </ul>
            </div>
            <StatCard
              label={t("pcc.periodStatus", language)}
              value={selectedPeriod.isOpen ? t("pcc.statusOpen", language) : t("pcc.statusClosed", language)}
              icon={<StatIcon icon="checkCircle" />}
              tone={selectedPeriod.isOpen ? "var(--pi-success)" : "var(--pi-danger)"}
            />
            <StatCard
              label={t("pcc.targetCloseDate", language)}
              value={new Date(selectedPeriod.targetCloseDate).toLocaleDateString(language === "ar" ? "ar-SA" : "en-US", { day: "2-digit", month: "short", year: "numeric" })}
              icon={<StatIcon icon="trendingUp" />}
              tone="var(--pi-chart-2)"
            />
            <StatCard
              label={t("pcc.daysInPeriod", language)}
              value={String(daysInPeriod)}
              icon={<StatIcon icon="layers" />}
              tone="var(--pi-chart-3)"
            />
            <StatCard
              label={t("pcc.lastClosing", language)}
              value={lastClosedPeriod
                ? new Date(lastClosedPeriod.startDate).toLocaleDateString(language === "ar" ? "ar-SA" : "en-US", { month: "long", year: "numeric" })
                : t("pcc.none", language)}
              icon={<StatIcon icon="receipt" />}
              tone="var(--pi-chart-4)"
            />
          </div>

          <div className="pcc-tabs">
            {(["checklist", "posting", "reconciliation", "journal", "history"] as TabKey[]).map((tab) => (
              <button
                key={tab}
                type="button"
                className={`pcc-tab ${activeTab === tab ? "pcc-tab--active" : ""}`}
                onClick={() => setActiveTab(tab)}
              >
                {t(`pcc.tab${tab.charAt(0).toUpperCase()}${tab.slice(1)}` as "pcc.tabChecklist", language)}
              </button>
            ))}
          </div>

          {activeTab !== "checklist" ? (
            <p className="fin-report__panel-empty">{t("pcc.tabNotBuiltYet", language)}</p>
          ) : (
            <div className="fin-report__layout">
              <div className="fin-report__main">
                <table className="pi-dense-table">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>{t("pcc.columnActivity", language)}</th>
                      <th>{t("pcc.columnResponsible", language)}</th>
                      <th>{t("pcc.columnStatus", language)}</th>
                      <th>{t("pcc.columnCompletion", language)}</th>
                      <th>{t("pcc.columnDueDate", language)}</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {activities.map((a) => (
                      <tr key={a.id}>
                        <td>{a.sequenceNumber}</td>
                        <td>
                          <strong>{t(activityTitleKey(a.activityKey) as "pcc.activityBankReconciliation", language)}</strong>
                          <div className="fin-report__panel-empty" style={{ fontSize: "0.8em" }}>
                            {t(activityDescriptionKey(a.activityKey) as "pcc.activityDescBankReconciliation", language)}
                          </div>
                        </td>
                        <td>
                          {assigningActivityId === a.id ? (
                            <select autoFocus onBlur={() => setAssigningActivityId(null)} onChange={(e) => e.target.value && handleAssign(a.id, e.target.value)} defaultValue="">
                              <option value="" disabled>{t("pcc.pickAssignee", language)}</option>
                              {assignableUsers.map((u) => <option key={u.id} value={u.id}>{u.displayName}</option>)}
                            </select>
                          ) : (
                            <button type="button" className="pi-link" onClick={() => setAssigningActivityId(a.id)}>
                              {a.assignedToDisplayName ?? t("pcc.unassigned", language)}
                            </button>
                          )}
                        </td>
                        <td>
                          <span className={`gl-status-pill gl-status-pill--${a.status === "Completed" ? "active" : "inactive"}`}>
                            {t(statusKey[a.status], language)}
                          </span>
                        </td>
                        <td>
                          <bdi dir="ltr">{a.completedSteps} / {a.totalSteps}</bdi>
                        </td>
                        <td><bdi dir="ltr">{a.dueDate ?? "—"}</bdi></td>
                        <td>
                          <button type="button" onClick={() => handleToggleBlocked(a.id, a.status !== "Blocked")} disabled={busy}>
                            {a.status === "Blocked" ? t("pcc.actionUnblock", language) : t("pcc.actionBlock", language)}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {activities.map((a) => a.steps.length > 0 && (
                  <details key={a.id} className="pcc-steps-detail">
                    <summary>{t(activityTitleKey(a.activityKey) as "pcc.activityBankReconciliation", language)} — {t("pcc.stepsLabel", language)}</summary>
                    <ul>
                      {a.steps.map((s) => (
                        <li key={s.id}>
                          <label>
                            <input
                              type="checkbox"
                              checked={s.isCompleted}
                              disabled={s.isAutoTracked || busy}
                              onChange={(e) => handleToggleStep(a.id, s.id, e.target.checked)}
                            />
                            {" "}{s.description}
                            {s.isAutoTracked && <span className="fin-report__panel-empty"> ({t("pcc.autoTracked", language)})</span>}
                          </label>
                        </li>
                      ))}
                    </ul>
                  </details>
                ))}

                <div className="pcc-timeline">
                  <h2>{t("pcc.closingTimeline", language)}</h2>
                  <ol className="pcc-timeline-steps">
                    {TIMELINE_PHASES.map((phase) => {
                      const relevant = activities.filter((a) => phase.activityKeys.includes(a.activityKey));
                      const done = phase.key === "start" || (relevant.length > 0 && relevant.every((a) => a.status === "Completed"));
                      const inProgress = relevant.length > 0 && relevant.some((a) => a.status === "InProgress" || a.status === "Blocked") && !done;
                      return (
                        <li key={phase.key} className={done ? "pcc-timeline-step--done" : inProgress ? "pcc-timeline-step--active" : ""}>
                          <span className="pcc-timeline-dot">{done ? "✓" : ""}</span>
                          <span>{t(phase.labelKey, language)}</span>
                        </li>
                      );
                    })}
                  </ol>
                </div>

                {trend.length > 0 && (
                  <div className="pcc-trend">
                    <h2>{t("pcc.completionTrend", language)}</h2>
                    <svg viewBox="0 0 400 120" className="pcc-trend-svg" role="img" aria-label={t("pcc.completionTrend", language)}>
                      <polyline
                        fill="none" stroke="var(--pi-success)" strokeWidth="2"
                        points={trend.map((p, i) => `${(i / Math.max(1, trend.length - 1)) * 380 + 10},${110 - (p.percentComplete / 100) * 100}`).join(" ")}
                      />
                      {trend.map((p, i) => (
                        <circle key={p.date} cx={(i / Math.max(1, trend.length - 1)) * 380 + 10} cy={110 - (p.percentComplete / 100) * 100} r="3" fill="var(--pi-success)" />
                      ))}
                    </svg>
                  </div>
                )}
              </div>

              <aside className="fin-report__rail">
                <div className="fin-report__panel">
                  <h2>{t("pcc.closingInsights", language)}</h2>
                  {insights.map((insight, i) => (
                    <p key={i} className={`pcc-insight pcc-insight--${insight.severity}`}>
                      <strong>{insight.title === "On Track" ? t("pcc.insightOnTrack", language)
                        : insight.title === "Attention Required" ? t("pcc.insightAttentionRequired", language)
                        : t("pcc.insightBestPractice", language)}</strong>
                      <br />{insight.message}
                    </p>
                  ))}
                </div>

                <div className="fin-report__panel">
                  <h2>{t("pcc.periodControls", language)}</h2>
                  <p>{t("pcc.periodStatus", language)}: <strong>{selectedPeriod.isOpen ? t("pcc.statusOpen", language) : t("pcc.statusClosed", language)}</strong></p>
                  <button type="button" onClick={handleToggleLock} disabled={busy}>
                    {selectedPeriod.isOpen ? t("pcc.actionLockPeriod", language) : t("pcc.actionReopenPeriod", language)}
                  </button>
                  <div style={{ marginBlockStart: "0.75rem" }}>
                    <label>{t("pcc.targetCloseDate", language)}
                      <input
                        type="date"
                        defaultValue={selectedPeriod.targetCloseDate}
                        onBlur={(e) => e.target.value !== selectedPeriod.targetCloseDate && handleEditTargetCloseDate(e.target.value)}
                        style={{ display: "block", padding: "0.3rem" }}
                      />
                    </label>
                  </div>
                </div>

                <div className="fin-report__panel">
                  <h2>{t("pcc.responsibleTeam", language)}</h2>
                  {responsibleTeam.length > 0 ? (
                    <ul className="fin-report__top-accounts">
                      {responsibleTeam.map((person) => (
                        <li key={person.name}>
                          <span className="fin-report__top-account-name">{person.name}</span>
                          <span className="fin-report__top-account-value">{person.role ?? ""}</span>
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="fin-report__panel-empty">{t("pcc.unassigned", language)}</p>
                  )}
                </div>

                <div className="fin-report__panel">
                  <h2>{t("pcc.activityLog", language)}</h2>
                  {activityLog.length > 0 ? (
                    <ul className="fin-report__top-accounts">
                      {activityLog.map((entry, i) => (
                        <li key={i}>
                          <span className="fin-report__top-account-name">{userLabel(entry.actor) !== entry.actor ? userLabel(entry.actor) : entry.actor}</span>
                          <span className="fin-report__top-account-value">{new Date(entry.at).toLocaleString(language === "ar" ? "ar-SA" : "en-US")}</span>
                          <div className="fin-report__panel-empty">{entry.message}</div>
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="fin-report__panel-empty">{t("pcc.noActivity", language)}</p>
                  )}
                </div>
              </aside>
            </div>
          )}
        </>
      )}
    </section>
  );
}
