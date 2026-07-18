import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveBudget,
  createBudget,
  listBudgets,
  rejectBudget,
  submitBudget,
} from "../api/budgetApi";
import type { Budget } from "../api/budgetApi";
import { listCostCenters } from "../api/costCenterApi";
import type { CostCenter } from "../api/costCenterApi";

interface BudgetsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; budget: Budget };

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

function currentYear(): number {
  return new Date().getFullYear();
}

export function BudgetsPage({ language }: BudgetsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [budgets, setBudgets] = useState<Budget[]>([]);
  const [costCenters, setCostCenters] = useState<CostCenter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [costCenterId, setCostCenterId] = useState("");
  const [fiscalYear, setFiscalYear] = useState(String(currentYear()));
  const [amount, setAmount] = useState("");

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [budgetResult, costCenterResult] = await Promise.all([listBudgets(200, 0), listCostCenters(200, 0)]);
      setBudgets(budgetResult.items);
      setCostCenters(costCenterResult.items);
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
    if (view.kind === "create" && costCenters.length === 0) {
      listCostCenters(200, 0).then((r) => setCostCenters(r.items)).catch(() => undefined);
    }
  }, [view.kind, costCenters.length]);

  const costCenterLabel = (id: string) => {
    const cc = costCenters.find((c) => c.id === id);
    return cc ? `${cc.costCenterCode} — ${cc.costCenterName}` : id;
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      await createBudget({ costCenterId, fiscalYear: Number(fiscalYear), amount: Number(amount) });
      setCostCenterId("");
      setFiscalYear(String(currentYear()));
      setAmount("");
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (budget: Budget, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: Budget;
      if (action === "submit") updated = await submitBudget(budget.id);
      else if (action === "approve") updated = await approveBudget(budget.id);
      else updated = await rejectBudget(budget.id);
      setView({ kind: "details", budget: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };
  const isCreateValid = costCenterId !== "" && fiscalYear !== "" && Number(amount) > 0;

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("bud.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !isCreateValid },
      { key: "back", label: t("bud.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("bud.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("bud.fieldCostCenter", language)}
            <select style={inputStyle} value={costCenterId} onChange={(e) => setCostCenterId(e.target.value)}>
              <option value=""></option>
              {costCenters.map((c) => (
                <option key={c.id} value={c.id}>{c.costCenterCode} — {c.costCenterName}</option>
              ))}
            </select>
          </label>
          <label>{t("bud.fieldFiscalYear", language)}
            <input type="number" style={inputStyle} value={fiscalYear} onChange={(e) => setFiscalYear(e.target.value)} />
          </label>
          <label>{t("bud.fieldAmount", language)}
            <input type="number" min="0" step="0.01" style={inputStyle} value={amount} onChange={(e) => setAmount(e.target.value)} />
          </label>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const budget = view.budget;
    const actions: ActionItem[] = [];
    if (budget.status === "Draft")
      actions.push({ key: "submit", label: t("bud.actionSubmit", language), onClick: () => handleAction(budget, "submit"), variant: "primary", isDisabled: busy });
    if (budget.status === "Submitted") {
      actions.push({ key: "approve", label: t("bud.actionApprove", language), onClick: () => handleAction(budget, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("bud.actionReject", language), onClick: () => handleAction(budget, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("bud.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{budget.documentNumber} — {costCenterLabel(budget.costCenterId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("bud.fieldCostCenter", language)}</dt>
              <dd>{costCenterLabel(budget.costCenterId)}</dd>
              <dt>{t("bud.fieldFiscalYear", language)}</dt>
              <dd><bdi dir="ltr">{budget.fiscalYear}</bdi></dd>
              <dt>{t("bud.fieldAmount", language)}</dt>
              <dd><bdi dir="ltr">{budget.amount.toFixed(2)}</bdi></dd>
              <dt>{t("bud.columnStatus", language)}</dt>
              <dd>{translateStatus(budget.status, language)}</dd>
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("bud.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("bud.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {budgets.length === 0 ? (
        <p>{t("bud.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("bud.columnDocumentNumber", language)}</th>
              <th>{t("bud.columnCostCenter", language)}</th>
              <th>{t("bud.columnFiscalYear", language)}</th>
              <th>{t("bud.columnAmount", language)}</th>
              <th>{t("bud.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {budgets.map((budget) => (
              <tr key={budget.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", budget })}>
                <td><bdi dir="ltr">{budget.documentNumber}</bdi></td>
                <td>{costCenterLabel(budget.costCenterId)}</td>
                <td><bdi dir="ltr">{budget.fiscalYear}</bdi></td>
                <td><bdi dir="ltr">{budget.amount.toFixed(2)}</bdi></td>
                <td>{translateStatus(budget.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
