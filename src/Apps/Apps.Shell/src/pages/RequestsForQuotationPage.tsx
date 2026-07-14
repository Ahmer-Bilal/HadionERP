import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveRequestForQuotation,
  createRequestForQuotation,
  listRequestsForQuotation,
  recordVendorQuote,
  rejectRequestForQuotation,
  submitRequestForQuotation,
} from "../api/requestForQuotationApi";
import type { RequestForQuotation } from "../api/requestForQuotationApi";
import { listPurchaseRequisitions } from "../api/purchaseRequisitionApi";
import type { PurchaseRequisition } from "../api/purchaseRequisitionApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listItems } from "../api/itemApi";
import type { Item } from "../api/itemApi";

interface RequestsForQuotationPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; rfq: RequestForQuotation };

// Mirrors Modules.Procurement.Application.RequestForQuotationService.QuoteEligibleRoles.
const QUOTE_ELIGIBLE_ROLES = new Set([
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

export function RequestsForQuotationPage({ language }: RequestsForQuotationPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [rfqs, setRfqs] = useState<RequestForQuotation[]>([]);
  const [requisitions, setRequisitions] = useState<PurchaseRequisition[]>([]);
  const [vendors, setVendors] = useState<BusinessPartner[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [purchaseRequisitionId, setPurchaseRequisitionId] = useState("");
  const [description, setDescription] = useState("");
  const [responseDeadline, setResponseDeadline] = useState("");
  const [selectedVendorIds, setSelectedVendorIds] = useState<string[]>([]);

  const [quoteForm, setQuoteForm] = useState({ vendorId: "", rfqLineId: "", quotedUnitPrice: "" });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [rfqResult, requisitionResult, vendorResult, itemResult] = await Promise.all([
        listRequestsForQuotation(200, 0),
        listPurchaseRequisitions(200, 0),
        listBusinessPartners(200, 0),
        listItems(200, 0),
      ]);
      setRfqs(rfqResult.items);
      setRequisitions(requisitionResult.items.filter((r) => r.status === "Approved"));
      setVendors(vendorResult.items.filter((v) =>
        v.status === "Approved" && v.businessRoles.some((r) => QUOTE_ELIGIBLE_ROLES.has(r.roleType))));
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

  const requisitionLabel = (id: string) => requisitions.find((r) => r.id === id)?.documentNumber ?? id;
  const vendorLabel = (id: string) => vendors.find((v) => v.id === id)?.name ?? id;
  const itemLabel = (id: string) => {
    const item = items.find((i) => i.id === id);
    return item ? `${item.itemCode} — ${item.itemName}` : id;
  };

  const toggleVendor = (vendorId: string) => {
    setSelectedVendorIds((prev) => (prev.includes(vendorId) ? prev.filter((v) => v !== vendorId) : [...prev, vendorId]));
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createRequestForQuotation({
        purchaseRequisitionId,
        description: description.trim(),
        invitedVendorIds: selectedVendorIds,
        responseDeadline: responseDeadline || undefined,
      });
      setPurchaseRequisitionId("");
      setDescription("");
      setResponseDeadline("");
      setSelectedVendorIds([]);
      setView({ kind: "details", rfq: created });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (rfq: RequestForQuotation, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: RequestForQuotation;
      if (action === "submit") updated = await submitRequestForQuotation(rfq.id);
      else if (action === "approve") updated = await approveRequestForQuotation(rfq.id);
      else updated = await rejectRequestForQuotation(rfq.id);
      setView({ kind: "details", rfq: updated });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleRecordQuote = async (rfq: RequestForQuotation) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await recordVendorQuote(rfq.id, {
        vendorId: quoteForm.vendorId,
        rfqLineId: quoteForm.rfqLineId,
        quotedUnitPrice: Number(quoteForm.quotedUnitPrice),
      });
      setQuoteForm({ vendorId: "", rfqLineId: "", quotedUnitPrice: "" });
      setView({ kind: "details", rfq: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("rfq.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !purchaseRequisitionId || !description.trim() || selectedVendorIds.length === 0 },
      { key: "back", label: t("rfq.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("rfq.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem" }}>
          <label>{t("rfq.fieldRequisition", language)}
            <select style={inputStyle} value={purchaseRequisitionId} onChange={(e) => setPurchaseRequisitionId(e.target.value)}>
              <option value=""></option>
              {requisitions.map((r) => <option key={r.id} value={r.id}>{r.documentNumber} — {r.description}</option>)}
            </select>
          </label>
          <label>{t("rfq.fieldDescription", language)}
            <input style={inputStyle} value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          <label>{t("rfq.fieldResponseDeadline", language)}
            <input type="date" style={inputStyle} value={responseDeadline} onChange={(e) => setResponseDeadline(e.target.value)} />
          </label>
          <fieldset style={{ marginBlockStart: "0.5rem" }}>
            <legend>{t("rfq.fieldInvitedVendors", language)}</legend>
            {vendors.map((v) => (
              <label key={v.id} style={{ display: "block" }}>
                <input type="checkbox" checked={selectedVendorIds.includes(v.id)} onChange={() => toggleVendor(v.id)} />
                {" "}{v.name}
              </label>
            ))}
          </fieldset>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const rfq = view.rfq;
    const actions: ActionItem[] = [];
    if (rfq.status === "Draft")
      actions.push({ key: "submit", label: t("rfq.actionSubmit", language), onClick: () => handleAction(rfq, "submit"), variant: "primary", isDisabled: busy });
    if (rfq.status === "Submitted") {
      actions.push({ key: "approve", label: t("rfq.actionApprove", language), onClick: () => handleAction(rfq, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("rfq.actionReject", language), onClick: () => handleAction(rfq, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("rfq.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{rfq.documentNumber} — {rfq.description}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("rfq.columnRequisition", language)}</dt>
                <dd><bdi dir="ltr">{requisitionLabel(rfq.purchaseRequisitionId)}</bdi></dd>
                <dt>{t("rfq.fieldResponseDeadline", language)}</dt>
                <dd><bdi dir="ltr">{rfq.responseDeadline ?? "—"}</bdi></dd>
                <dt>{t("rfq.columnStatus", language)}</dt>
                <dd>{translateStatus(rfq.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "lines",
            title: t("rfq.tabLines", language),
            content: (
              <table className="bp-table">
                <thead>
                  <tr><th>{t("rfq.columnItem", language)}</th><th>{t("rfq.columnQuantity", language)}</th></tr>
                </thead>
                <tbody>
                  {rfq.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{itemLabel(line.itemId)}</bdi></td>
                      <td><bdi dir="ltr">{line.quantity}</bdi></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ),
          },
          {
            key: "invited-vendors",
            title: t("rfq.tabInvitedVendors", language),
            content: (
              <table className="bp-table">
                <thead><tr><th>{t("rfq.columnVendor", language)}</th></tr></thead>
                <tbody>
                  {rfq.invitedVendors.map((v) => (
                    <tr key={v.id}><td>{vendorLabel(v.vendorId)}</td></tr>
                  ))}
                </tbody>
              </table>
            ),
          },
          {
            key: "quotes",
            title: t("rfq.tabQuotes", language),
            content: (
              <div className="bp-form">
                {rfq.vendorQuoteLines.length === 0 && <p>{t("rfq.emptyQuotes", language)}</p>}
                {rfq.vendorQuoteLines.length > 0 && (
                  <table className="bp-table">
                    <thead>
                      <tr>
                        <th>{t("rfq.columnVendor", language)}</th>
                        <th>{t("rfq.columnItem", language)}</th>
                        <th>{t("rfq.columnQuotedUnitPrice", language)}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {rfq.vendorQuoteLines.map((q) => {
                        const line = rfq.lines.find((l) => l.id === q.rfqLineId);
                        return (
                          <tr key={q.id}>
                            <td>{vendorLabel(q.vendorId)}</td>
                            <td><bdi dir="ltr">{line ? itemLabel(line.itemId) : q.rfqLineId}</bdi></td>
                            <td><bdi dir="ltr">{q.quotedUnitPrice.toFixed(2)}</bdi></td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                )}

                {rfq.status === "Submitted" && (
                  <>
                    <label>{t("rfq.fieldVendor", language)}
                      <select style={inputStyle} value={quoteForm.vendorId} onChange={(e) => setQuoteForm({ ...quoteForm, vendorId: e.target.value })}>
                        <option value=""></option>
                        {rfq.invitedVendors.map((v) => <option key={v.vendorId} value={v.vendorId}>{vendorLabel(v.vendorId)}</option>)}
                      </select>
                    </label>
                    <label>{t("rfq.fieldLine", language)}
                      <select style={inputStyle} value={quoteForm.rfqLineId} onChange={(e) => setQuoteForm({ ...quoteForm, rfqLineId: e.target.value })}>
                        <option value=""></option>
                        {rfq.lines.map((l) => <option key={l.id} value={l.id}>{itemLabel(l.itemId)}</option>)}
                      </select>
                    </label>
                    <label>{t("rfq.fieldQuotedUnitPrice", language)}
                      <input type="number" min="0" step="0.01" style={inputStyle} value={quoteForm.quotedUnitPrice} onChange={(e) => setQuoteForm({ ...quoteForm, quotedUnitPrice: e.target.value })} />
                    </label>
                    <ActionPane
                      actions={[{
                        key: "record-quote",
                        label: t("rfq.actionRecordQuote", language),
                        onClick: () => handleRecordQuote(rfq),
                        variant: "primary",
                        isDisabled: busy || !quoteForm.vendorId || !quoteForm.rfqLineId || !quoteForm.quotedUnitPrice,
                      }]}
                      ariaLabel={t("aria.actionToolbar", language)}
                    />
                  </>
                )}
              </div>
            ),
          },
        ]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("rfq.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("rfq.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {rfqs.length === 0 ? (
        <p>{t("rfq.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("rfq.columnDocumentNumber", language)}</th>
              <th>{t("rfq.columnDescription", language)}</th>
              <th>{t("rfq.columnRequisition", language)}</th>
              <th>{t("rfq.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {rfqs.map((rfq) => (
              <tr key={rfq.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", rfq })}>
                <td><bdi dir="ltr">{rfq.documentNumber}</bdi></td>
                <td>{rfq.description}</td>
                <td><bdi dir="ltr">{requisitionLabel(rfq.purchaseRequisitionId)}</bdi></td>
                <td>{translateStatus(rfq.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
