import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveGoodsReceiptNote,
  createGoodsReceiptNote,
  listGoodsReceiptNotes,
  rejectGoodsReceiptNote,
  submitGoodsReceiptNote,
} from "../api/goodsReceiptNoteApi";
import type { CreateGoodsReceiptNoteLineInput, GoodsReceiptNote } from "../api/goodsReceiptNoteApi";
import { listPurchaseOrders } from "../api/purchaseOrderApi";
import type { PurchaseOrder } from "../api/purchaseOrderApi";
import { listItems } from "../api/itemApi";
import type { Item } from "../api/itemApi";

interface GoodsReceiptNotesPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; grn: GoodsReceiptNote };

type DraftLine = { purchaseOrderLineId: string; quantityReceived: string };

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
  return { purchaseOrderLineId: "", quantityReceived: "" };
}

export function GoodsReceiptNotesPage({ language }: GoodsReceiptNotesPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [grns, setGrns] = useState<GoodsReceiptNote[]>([]);
  const [purchaseOrders, setPurchaseOrders] = useState<PurchaseOrder[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [purchaseOrderId, setPurchaseOrderId] = useState("");
  const [receivedDate, setReceivedDate] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [grnResult, poResult, itemResult] = await Promise.all([
        listGoodsReceiptNotes(200, 0),
        listPurchaseOrders(200, 0),
        listItems(200, 0),
      ]);
      setGrns(grnResult.items);
      setPurchaseOrders(poResult.items.filter((p) => p.status === "Approved"));
      setItems(itemResult.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  const poLabel = (id: string) => purchaseOrders.find((p) => p.id === id)?.documentNumber ?? id;
  const itemLabel = (id: string) => {
    const item = items.find((i) => i.id === id);
    return item ? `${item.itemCode} — ${item.itemName}` : id;
  };

  const selectedPo = purchaseOrders.find((p) => p.id === purchaseOrderId) ?? null;

  const updateLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const removeLine = (index: number) => {
    setLines((prev) => prev.filter((_, i) => i !== index));
  };

  const resetCreateForm = () => {
    setPurchaseOrderId("");
    setReceivedDate("");
    setLines([emptyLine()]);
  };

  const hasValidLine = lines.some((l) => l.purchaseOrderLineId && Number(l.quantityReceived) > 0);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const lineInputs: CreateGoodsReceiptNoteLineInput[] = lines
        .filter((l) => l.purchaseOrderLineId && Number(l.quantityReceived) > 0)
        .map((l) => ({ purchaseOrderLineId: l.purchaseOrderLineId, quantityReceived: Number(l.quantityReceived) }));
      const created = await createGoodsReceiptNote({ purchaseOrderId, receivedDate, lines: lineInputs });
      resetCreateForm();
      setView({ kind: "details", grn: created });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (grn: GoodsReceiptNote, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: GoodsReceiptNote;
      if (action === "submit") updated = await submitGoodsReceiptNote(grn.id);
      else if (action === "approve") updated = await approveGoodsReceiptNote(grn.id);
      else updated = await rejectGoodsReceiptNote(grn.id);
      setView({ kind: "details", grn: updated });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("grn.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !purchaseOrderId || !receivedDate || !hasValidLine },
      { key: "back", label: t("grn.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("grn.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("grn.fieldPurchaseOrder", language)}
            <select style={inputStyle} value={purchaseOrderId} onChange={(e) => { setPurchaseOrderId(e.target.value); setLines([emptyLine()]); }}>
              <option value=""></option>
              {purchaseOrders.map((p) => <option key={p.id} value={p.id}>{p.documentNumber}</option>)}
            </select>
          </label>
          <label>{t("grn.fieldReceivedDate", language)}
            <input type="date" style={inputStyle} value={receivedDate} onChange={(e) => setReceivedDate(e.target.value)} />
          </label>
        </div>

        {selectedPo && (
          <>
            <table className="bp-table">
              <thead>
                <tr>
                  <th>{t("grn.fieldPurchaseOrderLine", language)}</th>
                  <th>{t("grn.fieldQuantityReceived", language)}</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={index}>
                    <td>
                      <select value={line.purchaseOrderLineId} onChange={(e) => updateLine(index, { purchaseOrderLineId: e.target.value })}>
                        <option value=""></option>
                        {selectedPo.lines.map((l) => (
                          <option key={l.id} value={l.id}>{itemLabel(l.itemId)} ({l.quantity})</option>
                        ))}
                      </select>
                    </td>
                    <td><input type="number" min="0" step="0.001" value={line.quantityReceived} onChange={(e) => updateLine(index, { quantityReceived: e.target.value })} style={{ inlineSize: "7rem" }} /></td>
                    <td>
                      <button type="button" onClick={() => removeLine(index)} aria-label={t("grn.actionRemoveLine", language)}>
                        {t("grn.actionRemoveLine", language)}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button type="button" onClick={() => setLines((prev) => [...prev, emptyLine()])} style={{ marginBlockStart: "0.5rem" }}>
              {t("grn.actionAddLine", language)}
            </button>
          </>
        )}
      </section>
    );
  }

  if (view.kind === "details") {
    const grn = view.grn;
    const actions: ActionItem[] = [];
    if (grn.status === "Draft")
      actions.push({ key: "submit", label: t("grn.actionSubmit", language), onClick: () => handleAction(grn, "submit"), variant: "primary", isDisabled: busy });
    if (grn.status === "Submitted") {
      actions.push({ key: "approve", label: t("grn.actionApprove", language), onClick: () => handleAction(grn, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("grn.actionReject", language), onClick: () => handleAction(grn, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("grn.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{grn.documentNumber} — {poLabel(grn.purchaseOrderId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("grn.columnPurchaseOrder", language)}</dt>
                <dd><bdi dir="ltr">{poLabel(grn.purchaseOrderId)}</bdi></dd>
                <dt>{t("grn.fieldReceivedDate", language)}</dt>
                <dd><bdi dir="ltr">{grn.receivedDate}</bdi></dd>
                <dt>{t("grn.columnReceivedValue", language)}</dt>
                <dd><bdi dir="ltr">{grn.receivedValue.toFixed(2)}</bdi></dd>
                <dt>{t("grn.columnStatus", language)}</dt>
                <dd>{translateStatus(grn.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "lines",
            title: t("grn.tabLines", language),
            content: (
              <table className="bp-table">
                <thead>
                  <tr>
                    <th>{t("grn.columnItem", language)}</th>
                    <th>{t("grn.fieldQuantityReceived", language)}</th>
                    <th>{t("grn.columnUnitPrice", language)}</th>
                    <th>{t("grn.columnLineValue", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {grn.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{itemLabel(line.itemId)}</bdi></td>
                      <td><bdi dir="ltr">{line.quantityReceived}</bdi></td>
                      <td><bdi dir="ltr">{line.unitPrice.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{line.lineValue.toFixed(2)}</bdi></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ),
          },
        ]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("grn.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("grn.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {grns.length === 0 ? (
        <p>{t("grn.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("grn.columnDocumentNumber", language)}</th>
              <th>{t("grn.columnPurchaseOrder", language)}</th>
              <th>{t("grn.fieldReceivedDate", language)}</th>
              <th>{t("grn.columnReceivedValue", language)}</th>
              <th>{t("grn.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {grns.map((grn) => (
              <tr key={grn.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", grn })}>
                <td><bdi dir="ltr">{grn.documentNumber}</bdi></td>
                <td><bdi dir="ltr">{poLabel(grn.purchaseOrderId)}</bdi></td>
                <td><bdi dir="ltr">{grn.receivedDate}</bdi></td>
                <td><bdi dir="ltr">{grn.receivedValue.toFixed(2)}</bdi></td>
                <td>{translateStatus(grn.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
