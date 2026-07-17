import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  certifyMeasurementSheet,
  createMeasurementSheet,
  listMeasurementSheets,
  rejectMeasurementSheet,
  submitMeasurementSheet,
} from "../api/measurementSheetApi";
import type { MeasurementSheet, CreateMeasurementLineInput } from "../api/measurementSheetApi";
import { listProjects } from "../api/projectApi";
import type { Project } from "../api/projectApi";
import { listContracts } from "../api/contractApi";
import type { Contract } from "../api/contractApi";
import { listSubcontracts } from "../api/subcontractApi";
import type { Subcontract } from "../api/subcontractApi";

interface MeasurementSheetsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

type DocumentLine = { id: string; code: string; description: string; wbsElementId: string; quantity: number };

type DraftLine = { commercialDocumentLineId: string; quantitySubmitted: string; remarks: string };

function emptyLine(): DraftLine {
  return { commercialDocumentLineId: "", quantitySubmitted: "", remarks: "" };
}

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

export function MeasurementSheetsPage({ language }: MeasurementSheetsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [sheets, setSheets] = useState<MeasurementSheet[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [subcontracts, setSubcontracts] = useState<Subcontract[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectId, setProjectId] = useState("");
  const [documentType, setDocumentType] = useState<"Contract" | "Subcontract" | "">("");
  const [documentId, setDocumentId] = useState("");
  const [periodStart, setPeriodStart] = useState("");
  const [periodEnd, setPeriodEnd] = useState("");
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);

  const [certifiedQuantities, setCertifiedQuantities] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [sheetResult, projectResult, contractResult, subcontractResult] = await Promise.all([
        listMeasurementSheets(200, 0),
        listProjects(200, 0),
        listContracts(200, 0),
        listSubcontracts(200, 0),
      ]);
      setSheets(sheetResult.items);
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
  const selectedSheet = selectedId ? sheets.find((s) => s.id === selectedId) ?? null : null;

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

  const documentLinesFor = (type: string, docId: string): DocumentLine[] => {
    if (type === "Contract") {
      const contract = contracts.find((c) => c.id === docId);
      return contract ? contract.boqLines.map((l) => ({ id: l.id, code: l.code, description: l.description, wbsElementId: l.wbsElementId, quantity: l.quantity })) : [];
    }
    if (type === "Subcontract") {
      const subcontract = subcontracts.find((s) => s.id === docId);
      return subcontract ? subcontract.lines.map((l) => ({ id: l.id, code: l.code, description: l.description, wbsElementId: l.wbsElementId, quantity: l.quantity })) : [];
    }
    return [];
  };

  const documentLineLabel = (sheet: MeasurementSheet, commercialDocumentLineId: string) => {
    const candidates = documentLinesFor(sheet.commercialDocumentType, sheet.commercialDocumentId);
    const found = candidates.find((l) => l.id === commercialDocumentLineId);
    return found ? `${found.code} — ${found.description}` : commercialDocumentLineId;
  };

  const documentLabel = (sheet: MeasurementSheet) => {
    if (sheet.commercialDocumentType === "Contract")
      return contracts.find((c) => c.id === sheet.commercialDocumentId)?.documentNumber ?? sheet.commercialDocumentId;
    return subcontracts.find((s) => s.id === sheet.commercialDocumentId)?.documentNumber ?? sheet.commercialDocumentId;
  };

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const updateLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const removeLine = (index: number) => setLines((prev) => prev.filter((_, i) => i !== index));

  const resetCreateForm = () => {
    setProjectId("");
    setDocumentType("");
    setDocumentId("");
    setPeriodStart("");
    setPeriodEnd("");
    setNotes("");
    setLines([emptyLine()]);
  };

  const hasValidLine = lines.some((l) => l.commercialDocumentLineId && Number(l.quantitySubmitted) >= 0 && l.quantitySubmitted !== "");

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const inputs: CreateMeasurementLineInput[] = lines
        .filter((l) => l.commercialDocumentLineId && l.quantitySubmitted !== "")
        .map((l) => ({
          commercialDocumentLineId: l.commercialDocumentLineId,
          quantitySubmitted: Number(l.quantitySubmitted),
          remarks: l.remarks.trim() || undefined,
        }));

      const created = await createMeasurementSheet({
        projectId,
        commercialDocumentType: documentType,
        commercialDocumentId: documentId,
        periodStart,
        periodEnd,
        notes: notes.trim() || undefined,
        lines: inputs,
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

  const handleSubmit = async (sheet: MeasurementSheet) => {
    setBusy(true);
    setError(null);
    try {
      await submitMeasurementSheet(sheet.id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleReject = async (sheet: MeasurementSheet) => {
    setBusy(true);
    setError(null);
    try {
      await rejectMeasurementSheet(sheet.id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleCertify = async (sheet: MeasurementSheet) => {
    setBusy(true);
    setError(null);
    try {
      await certifyMeasurementSheet(sheet.id, {
        lines: sheet.lines.map((l) => ({
          lineId: l.id,
          quantityCertified: Number(certifiedQuantities[l.id] ?? l.quantitySubmitted),
        })),
      });
      setCertifiedQuantities({});
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };
  const draftDocumentLines = documentType && documentId ? documentLinesFor(documentType, documentId) : [];

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("meas.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !projectId || !documentType || !documentId || !periodStart || !periodEnd || !hasValidLine },
      { key: "back", label: t("meas.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("meas.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("meas.fieldProject", language)}
            <select style={inputStyle} value={projectId} onChange={(e) => { setProjectId(e.target.value); setDocumentType(""); setDocumentId(""); setLines([emptyLine()]); }}>
              <option value=""></option>
              {projects.map((p) => <option key={p.id} value={p.id}>{p.documentNumber} — {p.projectName}</option>)}
            </select>
          </label>
          <label>{t("meas.fieldDocumentType", language)}
            <select style={inputStyle} value={documentType} disabled={!projectId}
              onChange={(e) => { setDocumentType(e.target.value as "Contract" | "Subcontract" | ""); setDocumentId(""); setLines([emptyLine()]); }}>
              <option value=""></option>
              <option value="Contract">{t("meas.documentTypeContract", language)}</option>
              <option value="Subcontract">{t("meas.documentTypeSubcontract", language)}</option>
            </select>
          </label>
          <label>{t("meas.fieldDocument", language)}
            <select style={inputStyle} value={documentId} disabled={!documentType}
              onChange={(e) => { setDocumentId(e.target.value); setLines([emptyLine()]); }}>
              <option value=""></option>
              {documentOptions.map((d) => <option key={d.id} value={d.id}>{d.label}</option>)}
            </select>
          </label>
          <label>{t("meas.fieldPeriodStart", language)}
            <input type="date" style={inputStyle} value={periodStart} onChange={(e) => setPeriodStart(e.target.value)} />
          </label>
          <label>{t("meas.fieldPeriodEnd", language)}
            <input type="date" style={inputStyle} value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)} />
          </label>
          <label>{t("meas.fieldNotes", language)}
            <input style={inputStyle} value={notes} onChange={(e) => setNotes(e.target.value)} />
          </label>
        </div>

        {!documentId ? (
          <p>{t("meas.selectDocumentFirstHint", language)}</p>
        ) : (
          <>
            <table className="pi-dense-table">
              <thead>
                <tr>
                  <th>{t("meas.fieldLineDocumentLine", language)}</th>
                  <th>{t("meas.fieldLineQuantitySubmitted", language)}</th>
                  <th>{t("meas.fieldLineRemarks", language)}</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={index}>
                    <td>
                      <select value={line.commercialDocumentLineId} onChange={(e) => updateLine(index, { commercialDocumentLineId: e.target.value })}>
                        <option value=""></option>
                        {draftDocumentLines.map((l) => <option key={l.id} value={l.id}>{l.code} — {l.description} ({l.quantity})</option>)}
                      </select>
                    </td>
                    <td><input type="number" min="0" step="0.001" style={{ inlineSize: "6rem" }} value={line.quantitySubmitted} onChange={(e) => updateLine(index, { quantitySubmitted: e.target.value })} /></td>
                    <td><input value={line.remarks} onChange={(e) => updateLine(index, { remarks: e.target.value })} /></td>
                    <td>
                      <button type="button" onClick={() => removeLine(index)} aria-label={t("meas.actionRemoveLine", language)}>
                        {t("meas.actionRemoveLine", language)}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button type="button" onClick={() => setLines((prev) => [...prev, emptyLine()])} style={{ marginBlockStart: "0.5rem" }}>
              {t("meas.actionAddLine", language)}
            </button>
          </>
        )}
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("meas.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {sheets.length === 0 ? (
        <p>{t("meas.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("meas.columnDocumentNumber", language)}</th>
              <th>{t("meas.columnProject", language)}</th>
              <th>{t("meas.columnPeriod", language)}</th>
              <th>{t("meas.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {sheets.map((sheet) => (
              <tr key={sheet.id} className={sheet.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(sheet.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(sheet.id); }}>
                    <bdi dir="ltr">{sheet.documentNumber}</bdi>
                  </button>
                </td>
                <td>{projectLabel(sheet.projectId)}</td>
                <td><bdi dir="ltr">{sheet.periodStart} – {sheet.periodEnd}</bdi></td>
                <td>{translateStatus(sheet.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedSheet) {
    const sheet = selectedSheet;
    const actions: ActionItem[] = [];
    if (sheet.status === "Draft")
      actions.push({ key: "submit", label: t("meas.actionSubmit", language), onClick: () => handleSubmit(sheet), variant: "primary", isDisabled: busy });
    if (sheet.status === "Submitted") {
      actions.push({ key: "certify", label: t("meas.actionCertify", language), onClick: () => handleCertify(sheet), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("meas.actionReject", language), onClick: () => handleReject(sheet), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{sheet.documentNumber} — {documentLabel(sheet)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("meas.fieldProject", language)}</dt>
                <dd>{projectLabel(sheet.projectId)}</dd>
                <dt>{t("meas.fieldDocumentType", language)}</dt>
                <dd>{sheet.commercialDocumentType === "Contract" ? t("meas.documentTypeContract", language) : t("meas.documentTypeSubcontract", language)}</dd>
                <dt>{t("meas.fieldDocument", language)}</dt>
                <dd><bdi dir="ltr">{documentLabel(sheet)}</bdi></dd>
                <dt>{t("meas.fieldPeriodStart", language)}</dt>
                <dd><bdi dir="ltr">{sheet.periodStart}</bdi></dd>
                <dt>{t("meas.fieldPeriodEnd", language)}</dt>
                <dd><bdi dir="ltr">{sheet.periodEnd}</bdi></dd>
                <dt>{t("meas.fieldNotes", language)}</dt>
                <dd>{sheet.notes ?? "—"}</dd>
                <dt>{t("meas.columnStatus", language)}</dt>
                <dd>{translateStatus(sheet.status, language)}</dd>
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
                    <th>{t("meas.fieldLineQuantitySubmitted", language)}</th>
                    <th>{t("meas.fieldLineQuantityCertified", language)}</th>
                    <th>{t("meas.fieldLineRemarks", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {sheet.lines.map((line) => (
                    <tr key={line.id}>
                      <td>{documentLineLabel(sheet, line.commercialDocumentLineId)}</td>
                      <td><bdi dir="ltr">{line.quantitySubmitted}</bdi></td>
                      <td>
                        {sheet.status === "Submitted" ? (
                          <input
                            type="number" min="0" step="0.001" style={{ inlineSize: "6rem" }}
                            value={certifiedQuantities[line.id] ?? String(line.quantitySubmitted)}
                            onChange={(e) => setCertifiedQuantities((prev) => ({ ...prev, [line.id]: e.target.value }))}
                          />
                        ) : (
                          <bdi dir="ltr">{line.quantityCertified ?? "—"}</bdi>
                        )}
                      </td>
                      <td>{line.remarks ?? "—"}</td>
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
      <h1>{t("meas.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedSheet && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("meas.selectHint", language)}
        ariaLabel={t("meas.heading", language)}
      />
    </section>
  );
}
