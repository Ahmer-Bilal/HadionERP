import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approvePurchaseOrder,
  checkThreeWayMatch,
  createPurchaseOrder,
  listPurchaseOrders,
  rejectPurchaseOrder,
  submitPurchaseOrder,
} from "../api/purchaseOrderApi";
import type { CreatePurchaseOrderLineInput, PurchaseOrder, ThreeWayMatchResult } from "../api/purchaseOrderApi";
import { listRequestsForQuotation } from "../api/requestForQuotationApi";
import type { RequestForQuotation } from "../api/requestForQuotationApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listItems } from "../api/itemApi";
import type { Item } from "../api/itemApi";
import { listCostCenters } from "../api/costCenterApi";
import type { CostCenter } from "../api/costCenterApi";
import { listAPInvoices } from "../api/apInvoiceApi";
import type { APInvoice } from "../api/apInvoiceApi";

interface PurchaseOrdersPageProps {
  language: SupportedLanguageCode;
}

// The list pane always stays on screen (see Platform.UI's SplitView doc comment for why); "browse" tracks
// which record (if any) is showing in the detail pane, "create" is a separate full-width flow — creating a
// new document is a distinct task, not a record to browse to.
type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

type DraftLine = { itemId: string; costCenterId: string; quantity: string; unitPrice: string };

// Mirrors Modules.Procurement.Application.PurchaseOrderService.VendorEligibleRoles.
const VENDOR_ELIGIBLE_ROLES = new Set([
  "Supplier", "Subcontractor", "Consultant", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
]);

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
  return { itemId: "", costCenterId: "", quantity: "", unitPrice: "" };
}

export function PurchaseOrdersPage({ language }: PurchaseOrdersPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [rfqs, setRfqs] = useState<RequestForQuotation[]>([]);
  const [vendors, setVendors] = useState<BusinessPartner[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [costCenters, setCostCenters] = useState<CostCenter[]>([]);
  const [apInvoices, setApInvoices] = useState<APInvoice[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [matchInvoiceId, setMatchInvoiceId] = useState("");
  const [matchResult, setMatchResult] = useState<ThreeWayMatchResult | null>(null);

  const [sourceMode, setSourceMode] = useState<"rfq" | "direct">("rfq");
  const [requestForQuotationId, setRequestForQuotationId] = useState("");
  const [vendorId, setVendorId] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [orderResult, rfqResult, vendorResult, itemResult, costCenterResult, apInvoiceResult] = await Promise.all([
        listPurchaseOrders(200, 0),
        listRequestsForQuotation(200, 0),
        listBusinessPartners(200, 0),
        listItems(200, 0),
        listCostCenters(200, 0),
        listAPInvoices(200, 0),
      ]);
      setOrders(orderResult.items);
      setRfqs(rfqResult.items.filter((r) => r.status === "Approved"));
      setVendors(vendorResult.items.filter((v) =>
        v.status === "Approved" && v.businessRoles.some((r) => VENDOR_ELIGIBLE_ROLES.has(r.roleType))));
      setItems(itemResult.items.filter((i) => i.isActive));
      setCostCenters(costCenterResult.items.filter((c) => c.isPostable && c.isActive));
      setApInvoices(apInvoiceResult.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  const vendorLabel = (id: string) => vendors.find((v) => v.id === id)?.name ?? id;
  const rfqLabel = (id: string | null) => (id ? rfqs.find((r) => r.id === id)?.documentNumber ?? id : "—");
  const itemLabel = (id: string) => {
    const item = items.find((i) => i.id === id);
    return item ? `${item.itemCode} — ${item.itemName}` : id;
  };
  const costCenterLabel = (id: string) => {
    const costCenter = costCenters.find((c) => c.id === id);
    return costCenter ? `${costCenter.costCenterCode} — ${costCenter.costCenterName}` : id;
  };

  const selectedRfq = rfqs.find((r) => r.id === requestForQuotationId) ?? null;
  const rfqEligibleVendorIds = selectedRfq
    ? selectedRfq.invitedVendors
        .map((v) => v.vendorId)
        .filter((vid) => selectedRfq.lines.every((l) =>
          selectedRfq.vendorQuoteLines.some((q) => q.vendorId === vid && q.rfqLineId === l.id)))
    : [];

  const updateLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const removeLine = (index: number) => {
    setLines((prev) => prev.filter((_, i) => i !== index));
  };

  const directTotal = lines.reduce((sum, l) => sum + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0);
  const hasValidDirectLine = lines.some((l) => l.itemId && l.costCenterId && Number(l.quantity) > 0 && Number(l.unitPrice) > 0);

  const resetCreateForm = () => {
    setSourceMode("rfq");
    setRequestForQuotationId("");
    setVendorId("");
    setLines([emptyLine()]);
  };

  const openDetails = (id: string) => {
    setMatchInvoiceId("");
    setMatchResult(null);
    setView({ kind: "browse", selectedId: id });
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      let created: PurchaseOrder;
      if (sourceMode === "rfq") {
        created = await createPurchaseOrder({ vendorId, requestForQuotationId });
      } else {
        const lineInputs: CreatePurchaseOrderLineInput[] = lines
          .filter((l) => l.itemId && l.costCenterId && Number(l.quantity) > 0 && Number(l.unitPrice) > 0)
          .map((l) => ({ itemId: l.itemId, costCenterId: l.costCenterId, quantity: Number(l.quantity), unitPrice: Number(l.unitPrice) }));
        created = await createPurchaseOrder({ vendorId, lines: lineInputs });
      }
      resetCreateForm();
      await load();
      openDetails(created.id);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const invoiceLabel = (invoice: APInvoice) => `${invoice.documentNumber} — ${invoice.vendorInvoiceNumber} (${invoice.netAmount.toFixed(2)})`;

  const handleCheckMatch = async (order: PurchaseOrder) => {
    setBusy(true);
    setError(null);
    try {
      setMatchResult(await checkThreeWayMatch(order.id, matchInvoiceId));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (order: PurchaseOrder, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitPurchaseOrder(order.id);
      else if (action === "approve") await approvePurchaseOrder(order.id);
      else await rejectPurchaseOrder(order.id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const canCreate = sourceMode === "rfq" ? Boolean(requestForQuotationId && vendorId) : Boolean(vendorId && hasValidDirectLine);
    const actions: ActionItem[] = [
      { key: "create", label: t("po.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !canCreate },
      { key: "back", label: t("po.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("po.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <fieldset>
            <legend>{t("po.fieldSource", language)}</legend>
            <label style={{ display: "block" }}>
              <input type="radio" checked={sourceMode === "rfq"} onChange={() => { setSourceMode("rfq"); setVendorId(""); }} />
              {" "}{t("po.sourceFromRfq", language)}
            </label>
            <label style={{ display: "block" }}>
              <input type="radio" checked={sourceMode === "direct"} onChange={() => { setSourceMode("direct"); setVendorId(""); setRequestForQuotationId(""); }} />
              {" "}{t("po.sourceDirect", language)}
            </label>
          </fieldset>

          {sourceMode === "rfq" ? (
            <>
              <label>{t("po.fieldRequestForQuotation", language)}
                <select style={inputStyle} value={requestForQuotationId} onChange={(e) => { setRequestForQuotationId(e.target.value); setVendorId(""); }}>
                  <option value=""></option>
                  {rfqs.map((r) => <option key={r.id} value={r.id}>{r.documentNumber} — {r.description}</option>)}
                </select>
              </label>
              <label>{t("po.fieldVendor", language)}
                <select style={inputStyle} value={vendorId} onChange={(e) => setVendorId(e.target.value)} disabled={!requestForQuotationId}>
                  <option value=""></option>
                  {rfqEligibleVendorIds.map((vid) => <option key={vid} value={vid}>{vendorLabel(vid)}</option>)}
                </select>
              </label>
              {requestForQuotationId && rfqEligibleVendorIds.length === 0 && (
                <p style={{ color: "var(--pi-danger)" }}>{t("po.noEligibleVendors", language)}</p>
              )}
            </>
          ) : (
            <label>{t("po.fieldVendor", language)}
              <select style={inputStyle} value={vendorId} onChange={(e) => setVendorId(e.target.value)}>
                <option value=""></option>
                {vendors.map((v) => <option key={v.id} value={v.id}>{v.name}</option>)}
              </select>
            </label>
          )}
        </div>

        {sourceMode === "direct" && (
          <>
            <table className="pi-dense-table">
              <thead>
                <tr>
                  <th>{t("po.fieldItem", language)}</th>
                  <th>{t("po.fieldCostCenter", language)}</th>
                  <th>{t("po.fieldQuantity", language)}</th>
                  <th>{t("po.fieldUnitPrice", language)}</th>
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
                    <td><input type="number" min="0" step="0.01" value={line.unitPrice} onChange={(e) => updateLine(index, { unitPrice: e.target.value })} style={{ inlineSize: "8rem" }} /></td>
                    <td>
                      <button type="button" onClick={() => removeLine(index)} aria-label={t("po.actionRemoveLine", language)}>
                        {t("po.actionRemoveLine", language)}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button type="button" onClick={() => setLines((prev) => [...prev, emptyLine()])} style={{ marginBlockStart: "0.5rem" }}>
              {t("po.actionAddLine", language)}
            </button>
            <p style={{ marginBlockStart: "1rem" }}>
              <bdi dir="ltr">{directTotal.toFixed(2)}</bdi>
            </p>
          </>
        )}
      </section>
    );
  }

  // browse view: list pane always visible, detail pane shows the selected record (Platform.UI's SplitView)
  const selectedOrder = view.selectedId ? orders.find((o) => o.id === view.selectedId) ?? null : null;

  const listActions: ActionItem[] = [
    { key: "new", label: t("po.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {orders.length === 0 ? (
        <p>{t("po.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("po.columnDocumentNumber", language)}</th>
              <th>{t("po.columnVendor", language)}</th>
              <th>{t("po.columnTotal", language)}</th>
              <th>{t("po.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr
                key={order.id}
                className={order.id === view.selectedId ? "is-selected" : undefined}
                onClick={() => openDetails(order.id)}
              >
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(order.id); }}>
                    <bdi dir="ltr">{order.documentNumber}</bdi>
                  </button>
                </td>
                <td>{vendorLabel(order.vendorId)}</td>
                <td><bdi dir="ltr">{order.total.toFixed(2)}</bdi></td>
                <td>{translateStatus(order.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedOrder) {
    const order = selectedOrder;
    const actions: ActionItem[] = [];
    if (order.status === "Draft")
      actions.push({ key: "submit", label: t("po.actionSubmit", language), onClick: () => handleAction(order, "submit"), variant: "primary", isDisabled: busy });
    if (order.status === "Submitted") {
      actions.push({ key: "approve", label: t("po.actionApprove", language), onClick: () => handleAction(order, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("po.actionReject", language), onClick: () => handleAction(order, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{order.documentNumber} — {vendorLabel(order.vendorId)}</h1>
        {order.requestForQuotationId && (
          <p className="pi-doc-chain">
            {t("po.columnSourceRfq", language)}: <bdi dir="ltr">{rfqLabel(order.requestForQuotationId)}</bdi>
          </p>
        )}
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("po.columnVendor", language)}</dt>
                <dd>{vendorLabel(order.vendorId)}</dd>
                <dt>{t("po.columnSourceRfq", language)}</dt>
                <dd><bdi dir="ltr">{rfqLabel(order.requestForQuotationId)}</bdi></dd>
                <dt>{t("po.columnTotal", language)}</dt>
                <dd><bdi dir="ltr">{order.total.toFixed(2)}</bdi></dd>
                <dt>{t("po.columnStatus", language)}</dt>
                <dd>{translateStatus(order.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "lines",
            title: t("po.tabLines", language),
            content: (
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("po.fieldItem", language)}</th>
                    <th>{t("po.fieldCostCenter", language)}</th>
                    <th>{t("po.fieldQuantity", language)}</th>
                    <th>{t("po.fieldUnitPrice", language)}</th>
                    <th>{t("po.columnLineTotal", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {order.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{itemLabel(line.itemId)}</bdi></td>
                      <td><bdi dir="ltr">{costCenterLabel(line.costCenterId)}</bdi></td>
                      <td><bdi dir="ltr">{line.quantity}</bdi></td>
                      <td><bdi dir="ltr">{line.unitPrice.toFixed(2)}</bdi></td>
                      <td><bdi dir="ltr">{line.lineTotal.toFixed(2)}</bdi></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ),
          },
          {
            key: "three-way-match",
            title: t("po.tabThreeWayMatch", language),
            content: (
              <div className="bp-form">
                <label>{t("po.fieldApInvoice", language)}
                  <select style={inputStyle} value={matchInvoiceId} onChange={(e) => { setMatchInvoiceId(e.target.value); setMatchResult(null); }}>
                    <option value=""></option>
                    {apInvoices.map((inv) => <option key={inv.id} value={inv.id}>{invoiceLabel(inv)}</option>)}
                  </select>
                </label>
                <ActionPane
                  actions={[{
                    key: "check-match",
                    label: t("po.actionCheckMatch", language),
                    onClick: () => handleCheckMatch(order),
                    variant: "primary",
                    isDisabled: busy || !matchInvoiceId,
                  }]}
                  ariaLabel={t("aria.actionToolbar", language)}
                />
                {matchResult && (
                  <>
                    <dl style={{ maxInlineSize: "32rem", marginBlockStart: "1rem" }}>
                      <dt>{t("po.matchOrdered", language)}</dt>
                      <dd><bdi dir="ltr">{matchResult.orderedTotal.toFixed(2)}</bdi></dd>
                      <dt>{t("po.matchReceived", language)}</dt>
                      <dd><bdi dir="ltr">{matchResult.receivedValue.toFixed(2)}</bdi></dd>
                      <dt>{t("po.matchInvoiced", language)}</dt>
                      <dd><bdi dir="ltr">{matchResult.invoicedNetAmount.toFixed(2)}</bdi></dd>
                      <dt>{t("po.matchResultLabel", language)}</dt>
                      <dd style={{ color: matchResult.isMatched ? "var(--pi-success)" : "var(--pi-danger)" }}>
                        {matchResult.isMatched ? t("po.matchMatched", language) : t("po.matchVariance", language)}
                      </dd>
                    </dl>
                    {matchResult.varianceNotes.length > 0 && (
                      <ul>
                        {matchResult.varianceNotes.map((note, i) => <li key={i}>{note}</li>)}
                      </ul>
                    )}
                  </>
                )}
              </div>
            ),
          },
        ]} />
      </>
    );
  }

  return (
    <section>
      <h1>{t("po.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedOrder && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={view.selectedId ?? "none"}
        emptyDetailHint={t("po.selectHint", language)}
        ariaLabel={t("po.heading", language)}
      />
    </section>
  );
}
