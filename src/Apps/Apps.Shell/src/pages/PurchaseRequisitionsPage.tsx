import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approvePurchaseRequisition,
  createPurchaseRequisition,
  listPurchaseRequisitions,
  rejectPurchaseRequisition,
  submitPurchaseRequisition,
} from "../api/purchaseRequisitionApi";
import type { CreatePurchaseRequisitionLineInput, PurchaseRequisition } from "../api/purchaseRequisitionApi";
import { listItems } from "../api/itemApi";
import type { Item } from "../api/itemApi";
import { listCostCenters } from "../api/costCenterApi";
import type { CostCenter } from "../api/costCenterApi";

interface PurchaseRequisitionsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; requisition: PurchaseRequisition };

type DraftLine = { itemId: string; costCenterId: string; quantity: string; estimatedUnitPrice: string; lineDescription: string };

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

function emptyLine(): DraftLine {
  return { itemId: "", costCenterId: "", quantity: "", estimatedUnitPrice: "", lineDescription: "" };
}

export function PurchaseRequisitionsPage({ language }: PurchaseRequisitionsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [requisitions, setRequisitions] = useState<PurchaseRequisition[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [costCenters, setCostCenters] = useState<CostCenter[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [description, setDescription] = useState("");
  const [requiredByDate, setRequiredByDate] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [requisitionResult, itemResult, costCenterResult] = await Promise.all([
        listPurchaseRequisitions(200, 0),
        listItems(200, 0),
        listCostCenters(200, 0),
      ]);
      setRequisitions(requisitionResult.items);
      setItems(itemResult.items.filter((i) => i.isActive));
      setCostCenters(costCenterResult.items.filter((c) => c.isPostable && c.isActive));
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  const itemLabel = (id: string) => {
    const item = items.find((i) => i.id === id);
    return item ? `${item.itemCode} — ${item.itemName}` : id;
  };

  const costCenterLabel = (id: string) => {
    const costCenter = costCenters.find((c) => c.id === id);
    return costCenter ? `${costCenter.costCenterCode} — ${costCenter.costCenterName}` : id;
  };

  const estimatedTotal = lines.reduce(
    (sum, l) => sum + (Number(l.quantity) || 0) * (Number(l.estimatedUnitPrice) || 0), 0);

  const updateLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const removeLine = (index: number) => {
    setLines((prev) => prev.filter((_, i) => i !== index));
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const lineInputs: CreatePurchaseRequisitionLineInput[] = lines
        .filter((l) => l.itemId && l.costCenterId && Number(l.quantity) > 0 && Number(l.estimatedUnitPrice) > 0)
        .map((l) => ({
          itemId: l.itemId,
          costCenterId: l.costCenterId,
          quantity: Number(l.quantity),
          estimatedUnitPrice: Number(l.estimatedUnitPrice),
          lineDescription: l.lineDescription.trim() || undefined,
        }));
      await createPurchaseRequisition({
        description: description.trim(),
        lines: lineInputs,
        requiredByDate: requiredByDate || undefined,
      });
      setDescription("");
      setRequiredByDate("");
      setLines([emptyLine()]);
      setView({ kind: "list" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (requisition: PurchaseRequisition, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: PurchaseRequisition;
      if (action === "submit") updated = await submitPurchaseRequisition(requisition.id);
      else if (action === "approve") updated = await approvePurchaseRequisition(requisition.id);
      else updated = await rejectPurchaseRequisition(requisition.id);
      setView({ kind: "details", requisition: updated });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };
  const hasValidLine = lines.some((l) => l.itemId && l.costCenterId && Number(l.quantity) > 0 && Number(l.estimatedUnitPrice) > 0);

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("pr.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !description.trim() || !hasValidLine },
      { key: "back", label: t("pr.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("pr.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("pr.fieldDescription", language)}
            <input style={inputStyle} value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          <label>{t("pr.fieldRequiredByDate", language)}
            <input type="date" style={inputStyle} value={requiredByDate} onChange={(e) => setRequiredByDate(e.target.value)} />
          </label>
        </div>
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("pr.fieldItem", language)}</th>
              <th>{t("pr.fieldCostCenter", language)}</th>
              <th>{t("pr.fieldQuantity", language)}</th>
              <th>{t("pr.fieldEstimatedUnitPrice", language)}</th>
              <th>{t("pr.fieldLineDescription", language)}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line, index) => (
              <tr key={index}>
                <td>
                  <select value={line.itemId} onChange={(e) => updateLine(index, { itemId: e.target.value })}>
                    <option value=""></option>
                    {items.map((i) => <option key={i.id} value={i.id}>{i.itemCode} — {i.itemName}</option>)}
                  </select>
                </td>
                <td>
                  <select value={line.costCenterId} onChange={(e) => updateLine(index, { costCenterId: e.target.value })}>
                    <option value=""></option>
                    {costCenters.map((c) => <option key={c.id} value={c.id}>{c.costCenterCode} — {c.costCenterName}</option>)}
                  </select>
                </td>
                <td><input type="number" min="0" step="0.001" value={line.quantity} onChange={(e) => updateLine(index, { quantity: e.target.value })} style={{ inlineSize: "7rem" }} /></td>
                <td><input type="number" min="0" step="0.01" value={line.estimatedUnitPrice} onChange={(e) => updateLine(index, { estimatedUnitPrice: e.target.value })} style={{ inlineSize: "8rem" }} /></td>
                <td><input value={line.lineDescription} onChange={(e) => updateLine(index, { lineDescription: e.target.value })} /></td>
                <td>
                  <button type="button" onClick={() => removeLine(index)} aria-label={t("pr.actionRemoveLine", language)}>
                    {t("pr.actionRemoveLine", language)}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <button type="button" onClick={() => setLines((prev) => [...prev, emptyLine()])} style={{ marginBlockStart: "0.5rem" }}>
          {t("pr.actionAddLine", language)}
        </button>
        <p style={{ marginBlockStart: "1rem" }}>
          <bdi dir="ltr">{estimatedTotal.toFixed(2)}</bdi>
        </p>
      </section>
    );
  }

  if (view.kind === "details") {
    const requisition = view.requisition;
    const actions: ActionItem[] = [];
    if (requisition.status === "Draft")
      actions.push({ key: "submit", label: t("pr.actionSubmit", language), onClick: () => handleAction(requisition, "submit"), variant: "primary", isDisabled: busy });
    if (requisition.status === "Submitted") {
      actions.push({ key: "approve", label: t("pr.actionApprove", language), onClick: () => handleAction(requisition, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("pr.actionReject", language), onClick: () => handleAction(requisition, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("pr.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{requisition.documentNumber} — {requisition.description}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <>
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("pr.columnRequiredByDate", language)}</dt>
                <dd><bdi dir="ltr">{requisition.requiredByDate ?? "—"}</bdi></dd>
                <dt>{t("pr.columnEstimatedTotal", language)}</dt>
                <dd><bdi dir="ltr">{requisition.estimatedTotal.toFixed(2)}</bdi></dd>
                <dt>{t("pr.columnStatus", language)}</dt>
                <dd>{translateStatus(requisition.status, language)}</dd>
              </dl>
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("pr.fieldItem", language)}</th>
                    <th>{t("pr.fieldCostCenter", language)}</th>
                    <th>{t("pr.fieldQuantity", language)}</th>
                    <th>{t("pr.fieldEstimatedUnitPrice", language)}</th>
                    <th>{t("pr.columnLineTotal", language)}</th>
                    <th>{t("pr.fieldLineDescription", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {requisition.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{itemLabel(line.itemId)}</bdi></td>
                      <td><bdi dir="ltr">{costCenterLabel(line.costCenterId)}</bdi></td>
                      <td><bdi dir="ltr">{line.quantity}</bdi></td>
                      <td><bdi dir="ltr">{line.estimatedUnitPrice.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{line.estimatedLineTotal.toFixed(2)}</bdi></td>
                      <td>{line.lineDescription}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("pr.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("pr.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {requisitions.length === 0 ? (
        <p>{t("pr.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("pr.columnDocumentNumber", language)}</th>
              <th>{t("pr.columnDescription", language)}</th>
              <th>{t("pr.columnRequiredByDate", language)}</th>
              <th>{t("pr.columnEstimatedTotal", language)}</th>
              <th>{t("pr.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {requisitions.map((requisition) => (
              <tr key={requisition.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", requisition })}>
                <td><bdi dir="ltr">{requisition.documentNumber}</bdi></td>
                <td>{requisition.description}</td>
                <td><bdi dir="ltr">{requisition.requiredByDate ?? "—"}</bdi></td>
                <td><bdi dir="ltr">{requisition.estimatedTotal.toFixed(2)}</bdi></td>
                <td>{translateStatus(requisition.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
