import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveCostCenter,
  createCostCenter,
  listCostCenters,
  rejectCostCenter,
  submitCostCenter,
} from "../api/costCenterApi";
import type { CostCenter, CreateCostCenterInput } from "../api/costCenterApi";

interface CostCentersPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; costCenter: CostCenter };

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

export function CostCentersPage({ language }: CostCentersPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [costCenters, setCostCenters] = useState<CostCenter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateCostCenterInput>({
    costCenterCode: "",
    costCenterName: "",
    costCenterNameArabic: "",
    isPostable: true,
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const result = await listCostCenters(200, 0);
      setCostCenters(result.items);
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
      const input: CreateCostCenterInput = {
        costCenterCode: form.costCenterCode.trim(),
        costCenterName: form.costCenterName.trim(),
        costCenterNameArabic: form.costCenterNameArabic?.trim() || undefined,
        isPostable: form.isPostable,
      };
      await createCostCenter(input);
      setForm({ costCenterCode: "", costCenterName: "", costCenterNameArabic: "", isPostable: true });
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleStatusAction = async (costCenter: CostCenter, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: CostCenter;
      if (action === "submit") updated = await submitCostCenter(costCenter.id);
      else if (action === "approve") updated = await approveCostCenter(costCenter.id);
      else updated = await rejectCostCenter(costCenter.id);
      setView({ kind: "details", costCenter: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("cc.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("cc.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("cc.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("cc.fieldCostCenterCode", language)}
            <input style={inputStyle} value={form.costCenterCode} onChange={(e) => setForm({ ...form, costCenterCode: e.target.value })} />
          </label>
          <label>{t("cc.fieldCostCenterName", language)}
            <input style={inputStyle} value={form.costCenterName} onChange={(e) => setForm({ ...form, costCenterName: e.target.value })} />
          </label>
          <label>{t("cc.fieldCostCenterNameArabic", language)}
            <input dir="rtl" style={inputStyle} value={form.costCenterNameArabic ?? ""} onChange={(e) => setForm({ ...form, costCenterNameArabic: e.target.value })} />
          </label>
          <label>
            <input type="checkbox" checked={form.isPostable} onChange={(e) => setForm({ ...form, isPostable: e.target.checked })} />
            {" "}{t("cc.fieldIsPostable", language)}
          </label>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const costCenter = view.costCenter;
    const actions: ActionItem[] = [];
    if (costCenter.status === "Draft")
      actions.push({ key: "submit", label: t("cc.actionSubmit", language), onClick: () => handleStatusAction(costCenter, "submit"), variant: "primary", isDisabled: busy });
    if (costCenter.status === "Submitted") {
      actions.push({ key: "approve", label: t("cc.actionApprove", language), onClick: () => handleStatusAction(costCenter, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("cc.actionReject", language), onClick: () => handleStatusAction(costCenter, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("cc.actionBack", language), onClick: () => setView({ kind: "list" }) });

    const parent = costCenters.find((c) => c.id === costCenter.parentCostCenterId);

    return (
      <section>
        <h1>{costCenter.costCenterCode} — {costCenter.costCenterName}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("cc.columnCode", language)}</dt>
              <dd>{costCenter.costCenterCode}</dd>
              <dt>{t("cc.columnName", language)}</dt>
              <dd>{costCenter.costCenterName}</dd>
              {costCenter.costCenterNameArabic && (
                <>
                  <dt>{t("cc.fieldCostCenterNameArabic", language)}</dt>
                  <dd dir="rtl">{costCenter.costCenterNameArabic}</dd>
                </>
              )}
              <dt>{t("cc.fieldParentCostCenter", language)}</dt>
              <dd>{parent ? `${parent.costCenterCode} — ${parent.costCenterName}` : t("cc.noParent", language)}</dd>
              <dt>{t("cc.columnStatus", language)}</dt>
              <dd>{translateStatus(costCenter.status, language)}</dd>
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("cc.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("cc.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {costCenters.length === 0 ? (
        <p>{t("cc.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("cc.columnCode", language)}</th>
              <th>{t("cc.columnName", language)}</th>
              <th>{t("cc.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {costCenters.map((costCenter) => (
              <tr key={costCenter.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", costCenter })}>
                <td><bdi dir="ltr">{costCenter.costCenterCode}</bdi></td>
                <td>{costCenter.costCenterName}</td>
                <td>{translateStatus(costCenter.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
