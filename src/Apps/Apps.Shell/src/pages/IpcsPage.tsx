import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import { approveIpc, createIpc, listIpcs, rejectIpc, submitIpc } from "../api/ipcApi";
import type { Ipc } from "../api/ipcApi";
import { listProjects } from "../api/projectApi";
import type { Project } from "../api/projectApi";
import { listContracts } from "../api/contractApi";
import type { Contract } from "../api/contractApi";
import { listSubcontracts } from "../api/subcontractApi";
import type { Subcontract } from "../api/subcontractApi";
import { listMeasurementSheets } from "../api/measurementSheetApi";
import type { MeasurementSheet } from "../api/measurementSheetApi";

interface IpcsPageProps {
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

export function IpcsPage({ language }: IpcsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [ipcs, setIpcs] = useState<Ipc[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [subcontracts, setSubcontracts] = useState<Subcontract[]>([]);
  const [measurementSheets, setMeasurementSheets] = useState<MeasurementSheet[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectId, setProjectId] = useState("");
  const [documentType, setDocumentType] = useState<"Contract" | "Subcontract" | "">("");
  const [documentId, setDocumentId] = useState("");
  const [measurementSheetId, setMeasurementSheetId] = useState("");
  const [otherDeductions, setOtherDeductions] = useState("0");

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [ipcResult, projectResult, contractResult, subcontractResult, sheetResult] = await Promise.all([
        listIpcs(200, 0),
        listProjects(200, 0),
        listContracts(200, 0),
        listSubcontracts(200, 0),
        listMeasurementSheets(200, 0),
      ]);
      setIpcs(ipcResult.items);
      setProjects(projectResult.items.filter((p) => p.status === "Approved"));
      setContracts(contractResult.items.filter((c) => c.status === "Approved"));
      setSubcontracts(subcontractResult.items.filter((s) => s.status === "Approved"));
      setMeasurementSheets(sheetResult.items.filter((s) => s.status === "Approved"));
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
  const selectedIpc = selectedId ? ipcs.find((i) => i.id === selectedId) ?? null : null;

  const projectLabel = (id: string) => {
    const project = projects.find((p) => p.id === id);
    return project ? `${project.documentNumber ?? ""} — ${project.projectName}` : id;
  };

  const documentOptions: { id: string; label: string }[] =
    documentType === "Contract"
      ? contracts.filter((c) => c.projectId === projectId).map((c) => ({ id: c.id, label: c.documentNumber ?? c.id }))
      : documentType === "Subcontract"
        ? subcontracts.filter((s) => s.projectId === projectId).map((s) => ({ id: s.id, label: s.documentNumber ?? s.id }))
        : [];

  const alreadyBilledSheetIds = new Set(ipcs.map((i) => i.measurementSheetId));
  const eligibleSheets = measurementSheets.filter(
    (s) => s.projectId === projectId && s.commercialDocumentType === documentType && s.commercialDocumentId === documentId && !alreadyBilledSheetIds.has(s.id),
  );

  const documentLabel = (ipc: Ipc) => {
    if (ipc.commercialDocumentType === "Contract")
      return contracts.find((c) => c.id === ipc.commercialDocumentId)?.documentNumber ?? ipc.commercialDocumentId;
    return subcontracts.find((s) => s.id === ipc.commercialDocumentId)?.documentNumber ?? ipc.commercialDocumentId;
  };

  const sheetLabel = (id: string) => measurementSheets.find((s) => s.id === id)?.documentNumber ?? id;

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const resetCreateForm = () => {
    setProjectId("");
    setDocumentType("");
    setDocumentId("");
    setMeasurementSheetId("");
    setOtherDeductions("0");
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createIpc({
        projectId,
        commercialDocumentType: documentType,
        commercialDocumentId: documentId,
        measurementSheetId,
        otherDeductions: Number(otherDeductions) || 0,
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

  const handleAction = async (ipc: Ipc, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitIpc(ipc.id);
      else if (action === "approve") await approveIpc(ipc.id);
      else await rejectIpc(ipc.id);
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
      { key: "create", label: t("ipc.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !projectId || !documentType || !documentId || !measurementSheetId },
      { key: "back", label: t("ipc.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("ipc.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem" }}>
          <label>{t("ipc.fieldProject", language)}
            <select style={inputStyle} value={projectId} onChange={(e) => { setProjectId(e.target.value); setDocumentType(""); setDocumentId(""); setMeasurementSheetId(""); }}>
              <option value=""></option>
              {projects.map((p) => <option key={p.id} value={p.id}>{p.documentNumber} — {p.projectName}</option>)}
            </select>
          </label>
          <label>{t("ipc.fieldDocumentType", language)}
            <select style={inputStyle} value={documentType} disabled={!projectId}
              onChange={(e) => { setDocumentType(e.target.value as "Contract" | "Subcontract" | ""); setDocumentId(""); setMeasurementSheetId(""); }}>
              <option value=""></option>
              <option value="Contract">{t("meas.documentTypeContract", language)}</option>
              <option value="Subcontract">{t("meas.documentTypeSubcontract", language)}</option>
            </select>
          </label>
          <label>{t("ipc.fieldDocument", language)}
            <select style={inputStyle} value={documentId} disabled={!documentType}
              onChange={(e) => { setDocumentId(e.target.value); setMeasurementSheetId(""); }}>
              <option value=""></option>
              {documentOptions.map((d) => <option key={d.id} value={d.id}>{d.label}</option>)}
            </select>
          </label>
          <label>{t("ipc.fieldMeasurementSheet", language)}
            <select style={inputStyle} value={measurementSheetId} disabled={!documentId} onChange={(e) => setMeasurementSheetId(e.target.value)}>
              <option value=""></option>
              {eligibleSheets.map((s) => <option key={s.id} value={s.id}>{s.documentNumber} ({s.periodStart} – {s.periodEnd})</option>)}
            </select>
          </label>
          {documentId && eligibleSheets.length === 0 && <p>{t("ipc.noEligibleSheetsHint", language)}</p>}
          <label>{t("ipc.fieldOtherDeductions", language)}
            <input type="number" min="0" step="0.01" style={inputStyle} value={otherDeductions} onChange={(e) => setOtherDeductions(e.target.value)} />
          </label>
        </div>
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("ipc.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {ipcs.length === 0 ? (
        <p>{t("ipc.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("ipc.columnDocumentNumber", language)}</th>
              <th>{t("ipc.columnProject", language)}</th>
              <th>{t("ipc.columnNetPayable", language)}</th>
              <th>{t("ipc.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {ipcs.map((ipc) => (
              <tr key={ipc.id} className={ipc.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(ipc.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(ipc.id); }}>
                    <bdi dir="ltr">{ipc.documentNumber}</bdi>
                  </button>
                </td>
                <td>{projectLabel(ipc.projectId)}</td>
                <td><bdi dir="ltr">{ipc.netPayable.toLocaleString()}</bdi></td>
                <td>{translateStatus(ipc.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedIpc) {
    const ipc = selectedIpc;
    const actions: ActionItem[] = [];
    if (ipc.status === "Draft")
      actions.push({ key: "submit", label: t("ipc.actionSubmit", language), onClick: () => handleAction(ipc, "submit"), variant: "primary", isDisabled: busy });
    if (ipc.status === "Submitted") {
      actions.push({ key: "approve", label: t("ipc.actionCertify", language), onClick: () => handleAction(ipc, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("ipc.actionReject", language), onClick: () => handleAction(ipc, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{ipc.documentNumber} — {documentLabel(ipc)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("ipc.fieldProject", language)}</dt>
                <dd>{projectLabel(ipc.projectId)}</dd>
                <dt>{t("ipc.fieldDocument", language)}</dt>
                <dd><bdi dir="ltr">{documentLabel(ipc)}</bdi></dd>
                <dt>{t("ipc.fieldMeasurementSheet", language)}</dt>
                <dd><bdi dir="ltr">{sheetLabel(ipc.measurementSheetId)}</bdi></dd>
                <dt>{t("meas.fieldPeriodStart", language)}</dt>
                <dd><bdi dir="ltr">{ipc.periodStart}</bdi></dd>
                <dt>{t("meas.fieldPeriodEnd", language)}</dt>
                <dd><bdi dir="ltr">{ipc.periodEnd}</bdi></dd>
                <dt>{t("ipc.columnStatus", language)}</dt>
                <dd>{translateStatus(ipc.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "waterfall",
            title: t("ipc.tabWaterfall", language),
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("ipc.fieldGrossValueToDate", language)}</dt>
                <dd><bdi dir="ltr">{ipc.grossValueToDate.toLocaleString()}</bdi></dd>
                <dt>{t("ipc.fieldGrossValuePreviousIpc", language)}</dt>
                <dd><bdi dir="ltr">{ipc.grossValuePreviousIpc.toLocaleString()}</bdi></dd>
                <dt>{t("ipc.fieldGrossValueThisPeriod", language)}</dt>
                <dd><bdi dir="ltr">{ipc.grossValueThisPeriod.toLocaleString()}</bdi></dd>
                <dt>{t("ipc.fieldRetentionAmount", language)} ({ipc.retentionPercentageApplied ?? 0}%)</dt>
                <dd><bdi dir="ltr">-{ipc.retentionAmount.toLocaleString()}</bdi></dd>
                <dt>{t("ipc.fieldAdvanceRecoveryAmount", language)} ({ipc.advancePaymentPercentageApplied ?? 0}%)</dt>
                <dd><bdi dir="ltr">-{ipc.advanceRecoveryAmount.toLocaleString()}</bdi></dd>
                <dt>{t("ipc.fieldOtherDeductions", language)}</dt>
                <dd><bdi dir="ltr">-{ipc.otherDeductions.toLocaleString()}</bdi></dd>
                <dt><strong>{t("ipc.fieldNetPayable", language)}</strong></dt>
                <dd><strong><bdi dir="ltr">{ipc.netPayable.toLocaleString()}</bdi></strong></dd>
              </dl>
            ),
          },
          {
            key: "lines",
            title: t("meas.tabLines", language),
            content: (
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("meas.fieldLineDocumentLine", language)}</th>
                    <th>{t("ipc.fieldLineRate", language)}</th>
                    <th>{t("ipc.fieldLineQuantityThisPeriod", language)}</th>
                    <th>{t("ipc.fieldLineValueThisPeriod", language)}</th>
                    <th>{t("ipc.fieldLineQuantityToDate", language)}</th>
                    <th>{t("ipc.fieldLineValueToDate", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {ipc.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{line.commercialDocumentLineId}</bdi></td>
                      <td><bdi dir="ltr">{line.rate.toLocaleString()}</bdi></td>
                      <td><bdi dir="ltr">{line.quantityThisPeriod}</bdi></td>
                      <td><bdi dir="ltr">{line.valueThisPeriod.toLocaleString()}</bdi></td>
                      <td><bdi dir="ltr">{line.quantityToDate}</bdi></td>
                      <td><bdi dir="ltr">{line.valueToDate.toLocaleString()}</bdi></td>
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
      <h1>{t("ipc.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedIpc && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("ipc.selectHint", language)}
        ariaLabel={t("ipc.heading", language)}
      />
    </section>
  );
}
