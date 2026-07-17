import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveVariationOrder,
  createVariationOrder,
  listVariationOrders,
  rejectVariationOrder,
  submitVariationOrder,
} from "../api/variationOrderApi";
import type { CreateVariationOrderLineInput, VariationOrder } from "../api/variationOrderApi";
import { listProjects } from "../api/projectApi";
import type { Project } from "../api/projectApi";
import { listContracts } from "../api/contractApi";
import type { Contract } from "../api/contractApi";
import { listSubcontracts } from "../api/subcontractApi";
import type { Subcontract } from "../api/subcontractApi";

interface VariationOrdersPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

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

export function VariationOrdersPage({ language }: VariationOrdersPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [orders, setOrders] = useState<VariationOrder[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [subcontracts, setSubcontracts] = useState<Subcontract[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectId, setProjectId] = useState("");
  const [documentType, setDocumentType] = useState<"Contract" | "Subcontract" | "">("");
  const [documentId, setDocumentId] = useState("");
  const [reason, setReason] = useState("");
  const [lines, setLines] = useState<CreateVariationOrderLineInput[]>([]);

  const [lineMode, setLineMode] = useState<"adjust" | "new">("adjust");
  const [adjustLineId, setAdjustLineId] = useState("");
  const [adjustDelta, setAdjustDelta] = useState("");
  const [newCode, setNewCode] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newUnitOfMeasure, setNewUnitOfMeasure] = useState("");
  const [newWbsElementId, setNewWbsElementId] = useState("");
  const [newQuantity, setNewQuantity] = useState("");
  const [newRate, setNewRate] = useState("");

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [orderResult, projectResult, contractResult, subcontractResult] = await Promise.all([
        listVariationOrders(200, 0),
        listProjects(200, 0),
        listContracts(200, 0),
        listSubcontracts(200, 0),
      ]);
      setOrders(orderResult.items);
      setProjects(projectResult.items.filter((p) => p.status === "Approved"));
      setContracts(contractResult.items.filter((c) => c.status === "Approved"));
      setSubcontracts(subcontractResult.items.filter((s) => s.status === "Approved"));
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  const selectedId = view.kind === "browse" ? view.selectedId : null;
  const selectedOrder = selectedId ? orders.find((o) => o.id === selectedId) ?? null : null;

  const selectedProject = projects.find((p) => p.id === projectId) ?? null;

  const documentOptions: { id: string; label: string }[] =
    documentType === "Contract"
      ? contracts.filter((c) => c.projectId === projectId).map((c) => ({ id: c.id, label: c.documentNumber ?? c.id }))
      : documentType === "Subcontract"
        ? subcontracts.filter((s) => s.projectId === projectId).map((s) => ({ id: s.id, label: s.documentNumber ?? s.id }))
        : [];

  const documentLines: { id: string; label: string }[] =
    documentType === "Contract"
      ? (contracts.find((c) => c.id === documentId)?.boqLines ?? []).map((l) => ({ id: l.id, label: `${l.code} — ${l.description}` }))
      : (subcontracts.find((s) => s.id === documentId)?.lines ?? []).map((l) => ({ id: l.id, label: `${l.code} — ${l.description}` }));

  const projectLabel = (id: string) => {
    const project = projects.find((p) => p.id === id);
    return project ? `${project.documentNumber ?? ""} — ${project.projectName}` : id;
  };

  const documentLabel = (order: VariationOrder) => {
    if (order.commercialDocumentType === "Contract")
      return contracts.find((c) => c.id === order.commercialDocumentId)?.documentNumber ?? order.commercialDocumentId;
    return subcontracts.find((s) => s.id === order.commercialDocumentId)?.documentNumber ?? order.commercialDocumentId;
  };

  const lineDescription = (line: CreateVariationOrderLineInput) =>
    line.commercialDocumentLineId
      ? documentLines.find((l) => l.id === line.commercialDocumentLineId)?.label ?? line.commercialDocumentLineId
      : `${line.code} — ${line.description}`;

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const resetCreateForm = () => {
    setProjectId("");
    setDocumentType("");
    setDocumentId("");
    setReason("");
    setLines([]);
    resetLineForm();
  };

  const resetLineForm = () => {
    setAdjustLineId("");
    setAdjustDelta("");
    setNewCode("");
    setNewDescription("");
    setNewUnitOfMeasure("");
    setNewWbsElementId("");
    setNewQuantity("");
    setNewRate("");
  };

  const handleAddLine = () => {
    if (lineMode === "adjust") {
      if (!adjustLineId || !Number(adjustDelta)) return;
      setLines([...lines, { commercialDocumentLineId: adjustLineId, quantityDelta: Number(adjustDelta) }]);
    } else {
      if (!newCode || !newDescription || !newUnitOfMeasure || !newWbsElementId || !Number(newQuantity) || !Number(newRate)) return;
      setLines([...lines, {
        code: newCode, description: newDescription, unitOfMeasure: newUnitOfMeasure,
        wbsElementId: newWbsElementId, quantityDelta: Number(newQuantity), rate: Number(newRate),
      }]);
    }
    resetLineForm();
  };

  const handleRemoveLine = (index: number) => setLines(lines.filter((_, i) => i !== index));

  const canCreate = Boolean(projectId && documentType && documentId && reason.trim() && lines.length > 0);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createVariationOrder({
        projectId, commercialDocumentType: documentType, commercialDocumentId: documentId,
        reason: reason.trim(), lines,
      });
      resetCreateForm();
      await load();
      openDetails(created.id);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (order: VariationOrder, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitVariationOrder(order.id);
      else if (action === "approve") await approveVariationOrder(order.id);
      else await rejectVariationOrder(order.id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("vo.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !canCreate },
      { key: "back", label: t("vo.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("vo.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem" }}>
          <label>{t("vo.fieldProject", language)}
            <select style={inputStyle} value={projectId} onChange={(e) => { setProjectId(e.target.value); setDocumentType(""); setDocumentId(""); setLines([]); }}>
              <option value=""></option>
              {projects.map((p) => <option key={p.id} value={p.id}>{p.documentNumber} — {p.projectName}</option>)}
            </select>
          </label>
          <label>{t("vo.fieldDocumentType", language)}
            <select style={inputStyle} value={documentType} disabled={!projectId}
              onChange={(e) => { setDocumentType(e.target.value as "Contract" | "Subcontract" | ""); setDocumentId(""); setLines([]); }}>
              <option value=""></option>
              <option value="Contract">{t("meas.documentTypeContract", language)}</option>
              <option value="Subcontract">{t("meas.documentTypeSubcontract", language)}</option>
            </select>
          </label>
          <label>{t("vo.fieldDocument", language)}
            <select style={inputStyle} value={documentId} disabled={!documentType}
              onChange={(e) => { setDocumentId(e.target.value); setLines([]); }}>
              <option value=""></option>
              {documentOptions.map((d) => <option key={d.id} value={d.id}>{d.label}</option>)}
            </select>
          </label>
          <label>{t("vo.fieldReason", language)}
            <input style={inputStyle} value={reason} onChange={(e) => setReason(e.target.value)} />
          </label>
        </div>

        {documentId && (
          <>
            <h2>{t("vo.tabLines", language)}</h2>
            {lines.length > 0 && (
              <table className="bp-table" style={{ marginBlockEnd: "1rem" }}>
                <thead>
                  <tr>
                    <th>{t("vo.columnLine", language)}</th>
                    <th>{t("vo.fieldQuantityDelta", language)}</th>
                    <th>{t("vo.fieldRate", language)}</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line, index) => (
                    <tr key={index}>
                      <td><bdi dir="ltr">{lineDescription(line)}</bdi></td>
                      <td><bdi dir="ltr">{line.quantityDelta}</bdi></td>
                      <td><bdi dir="ltr">{line.rate !== undefined ? line.rate : t("vo.rateSnapshotted", language)}</bdi></td>
                      <td><button type="button" onClick={() => handleRemoveLine(index)}>{t("vo.actionRemoveLine", language)}</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <div style={{ maxInlineSize: "36rem" }}>
              <label>{t("vo.fieldLineMode", language)}
                <select style={inputStyle} value={lineMode} onChange={(e) => setLineMode(e.target.value as "adjust" | "new")}>
                  <option value="adjust">{t("vo.lineModeAdjust", language)}</option>
                  <option value="new">{t("vo.lineModeNew", language)}</option>
                </select>
              </label>
              {lineMode === "adjust" ? (
                <>
                  <label>{t("vo.columnLine", language)}
                    <select style={inputStyle} value={adjustLineId} onChange={(e) => setAdjustLineId(e.target.value)}>
                      <option value=""></option>
                      {documentLines.map((l) => <option key={l.id} value={l.id}>{l.label}</option>)}
                    </select>
                  </label>
                  <label>{t("vo.fieldQuantityDelta", language)}
                    <input type="number" step="0.001" style={inputStyle} value={adjustDelta} onChange={(e) => setAdjustDelta(e.target.value)} />
                  </label>
                </>
              ) : (
                <>
                  <label>{t("vo.fieldNewLineCode", language)}
                    <input style={inputStyle} value={newCode} onChange={(e) => setNewCode(e.target.value)} />
                  </label>
                  <label>{t("vo.fieldNewLineDescription", language)}
                    <input style={inputStyle} value={newDescription} onChange={(e) => setNewDescription(e.target.value)} />
                  </label>
                  <label>{t("vo.fieldNewLineUnitOfMeasure", language)}
                    <input style={inputStyle} value={newUnitOfMeasure} onChange={(e) => setNewUnitOfMeasure(e.target.value)} />
                  </label>
                  <label>{t("vo.fieldNewLineWbsElement", language)}
                    <select style={inputStyle} value={newWbsElementId} onChange={(e) => setNewWbsElementId(e.target.value)}>
                      <option value=""></option>
                      {(selectedProject?.wbsElements ?? []).map((w) => <option key={w.id} value={w.id}>{w.code} — {w.name}</option>)}
                    </select>
                  </label>
                  <label>{t("vo.fieldQuantityDelta", language)}
                    <input type="number" step="0.001" style={inputStyle} value={newQuantity} onChange={(e) => setNewQuantity(e.target.value)} />
                  </label>
                  <label>{t("vo.fieldRate", language)}
                    <input type="number" step="0.01" style={inputStyle} value={newRate} onChange={(e) => setNewRate(e.target.value)} />
                  </label>
                </>
              )}
              <button type="button" onClick={handleAddLine}>{t("vo.actionAddLine", language)}</button>
            </div>
          </>
        )}
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("vo.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {orders.length === 0 ? (
        <p>{t("vo.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("vo.columnDocumentNumber", language)}</th>
              <th>{t("vo.fieldProject", language)}</th>
              <th>{t("vo.columnTotalValue", language)}</th>
              <th>{t("vo.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => (
              <tr key={order.id} className={order.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(order.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(order.id); }}>
                    <bdi dir="ltr">{order.documentNumber}</bdi>
                  </button>
                </td>
                <td>{projectLabel(order.projectId)}</td>
                <td><bdi dir="ltr">{order.totalValue.toLocaleString()}</bdi></td>
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
      actions.push({ key: "submit", label: t("vo.actionSubmit", language), onClick: () => handleAction(order, "submit"), variant: "primary", isDisabled: busy });
    if (order.status === "Submitted") {
      actions.push({ key: "approve", label: t("vo.actionApprove", language), onClick: () => handleAction(order, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("vo.actionReject", language), onClick: () => handleAction(order, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{order.documentNumber} — {documentLabel(order)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("vo.fieldProject", language)}</dt>
                <dd>{projectLabel(order.projectId)}</dd>
                <dt>{t("vo.fieldDocument", language)}</dt>
                <dd><bdi dir="ltr">{documentLabel(order)}</bdi></dd>
                <dt>{t("vo.fieldReason", language)}</dt>
                <dd>{order.reason}</dd>
                <dt>{t("vo.columnTotalValue", language)}</dt>
                <dd><bdi dir="ltr">{order.totalValue.toLocaleString()}</bdi></dd>
                <dt>{t("vo.columnStatus", language)}</dt>
                <dd>{translateStatus(order.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "lines",
            title: t("vo.tabLines", language),
            content: (
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("vo.columnLine", language)}</th>
                    <th>{t("vo.fieldQuantityDelta", language)}</th>
                    <th>{t("vo.fieldRate", language)}</th>
                    <th>{t("vo.columnAmount", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {order.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{line.code ?? line.commercialDocumentLineId}</bdi></td>
                      <td><bdi dir="ltr">{line.quantityDelta}</bdi></td>
                      <td><bdi dir="ltr">{line.rate.toLocaleString()}</bdi></td>
                      <td><bdi dir="ltr">{line.amount.toLocaleString()}</bdi></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ),
          },
        ]} />
      </>
    );
  }

  return (
    <section>
      <h1>{t("vo.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedOrder && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("vo.selectHint", language)}
        ariaLabel={t("vo.heading", language)}
      />
    </section>
  );
}
