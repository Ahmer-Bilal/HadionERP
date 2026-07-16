import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  addBackCharge,
  approveSubcontract,
  createSubcontract,
  listSubcontracts,
  rejectSubcontract,
  submitSubcontract,
} from "../api/subcontractApi";
import type { Subcontract, CreateSubcontractLineInput } from "../api/subcontractApi";
import { listProjects } from "../api/projectApi";
import type { Project } from "../api/projectApi";
import { listContracts } from "../api/contractApi";
import type { Contract } from "../api/contractApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";
import { listLookupValues } from "../api/lookupApi";
import type { LookupValue } from "../api/lookupApi";

interface SubcontractsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

type DraftLine = {
  code: string;
  description: string;
  descriptionArabic: string;
  unitOfMeasure: string;
  quantity: string;
  rate: string;
  wbsElementId: string;
};

function emptyLine(): DraftLine {
  return { code: "", description: "", descriptionArabic: "", unitOfMeasure: "", quantity: "", rate: "", wbsElementId: "" };
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

export function SubcontractsPage({ language }: SubcontractsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [subcontracts, setSubcontracts] = useState<Subcontract[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [subcontractors, setSubcontractors] = useState<BusinessPartner[]>([]);
  const [unitsOfMeasure, setUnitsOfMeasure] = useState<LookupValue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectId, setProjectId] = useState("");
  const [contractId, setContractId] = useState("");
  const [subcontractorId, setSubcontractorId] = useState("");
  const [retentionPercentage, setRetentionPercentage] = useState("");
  const [mobilizationAdvancePercentage, setMobilizationAdvancePercentage] = useState("");
  const [defectsLiabilityPeriodMonths, setDefectsLiabilityPeriodMonths] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);

  const [backChargeDescription, setBackChargeDescription] = useState("");
  const [backChargeAmount, setBackChargeAmount] = useState("");
  const [backChargeDate, setBackChargeDate] = useState("");

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [subcontractResult, projectResult, contractResult, partnerResult, uomResult] = await Promise.all([
        listSubcontracts(200, 0),
        listProjects(200, 0),
        listContracts(200, 0),
        listBusinessPartners(200, 0),
        listLookupValues("UnitOfMeasure", false),
      ]);
      setSubcontracts(subcontractResult.items);
      setProjects(projectResult.items.filter((p) => p.status === "Approved"));
      setContracts(contractResult.items.filter((c) => c.status === "Approved"));
      setSubcontractors(partnerResult.items.filter((p) =>
        p.status === "Approved" && p.businessRoles.some((r) => r.roleType === "Subcontractor")));
      setUnitsOfMeasure(uomResult);
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
  const selectedSubcontract = selectedId ? subcontracts.find((s) => s.id === selectedId) ?? null : null;

  const projectLabel = (id: string) => {
    const project = projects.find((p) => p.id === id);
    return project ? `${project.documentNumber ?? ""} — ${project.projectName}` : id;
  };
  const wbsLabel = (project: Project | undefined, id: string) => {
    if (!project) return id;
    const element = project.wbsElements.find((w) => w.id === id);
    return element ? `${element.code} — ${element.name}` : id;
  };
  const contractLabel = (id: string | null) => {
    if (!id) return "—";
    const contract = contracts.find((c) => c.id === id);
    return contract ? `${contract.documentNumber ?? ""}` : id;
  };
  const subcontractorLabel = (id: string) => subcontractors.find((p) => p.id === id)?.name ?? id;

  const selectedDraftProject = projects.find((p) => p.id === projectId);
  const contractsForProject = contracts.filter((c) => c.projectId === projectId);

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const updateLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const removeLine = (index: number) => {
    setLines((prev) => prev.filter((_, i) => i !== index));
  };

  const resetCreateForm = () => {
    setProjectId("");
    setContractId("");
    setSubcontractorId("");
    setRetentionPercentage("");
    setMobilizationAdvancePercentage("");
    setDefectsLiabilityPeriodMonths("");
    setLines([emptyLine()]);
  };

  const hasValidLine = lines.some(
    (l) => l.code.trim() && l.description.trim() && l.unitOfMeasure && l.wbsElementId && Number(l.quantity) > 0 && Number(l.rate) > 0,
  );

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const inputs: CreateSubcontractLineInput[] = lines
        .filter((l) => l.code.trim() && l.description.trim() && l.unitOfMeasure && l.wbsElementId && Number(l.quantity) > 0 && Number(l.rate) > 0)
        .map((l) => ({
          code: l.code.trim(),
          description: l.description.trim(),
          descriptionArabic: l.descriptionArabic.trim() || undefined,
          unitOfMeasure: l.unitOfMeasure,
          quantity: Number(l.quantity),
          rate: Number(l.rate),
          wbsElementId: l.wbsElementId,
        }));

      const created = await createSubcontract({
        projectId,
        contractId: contractId || undefined,
        subcontractorId,
        retentionPercentage: retentionPercentage ? Number(retentionPercentage) : undefined,
        mobilizationAdvancePercentage: mobilizationAdvancePercentage ? Number(mobilizationAdvancePercentage) : undefined,
        defectsLiabilityPeriodMonths: defectsLiabilityPeriodMonths ? Number(defectsLiabilityPeriodMonths) : undefined,
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

  const handleAction = async (subcontract: Subcontract, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitSubcontract(subcontract.id);
      else if (action === "approve") await approveSubcontract(subcontract.id);
      else await rejectSubcontract(subcontract.id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAddBackCharge = async (subcontract: Subcontract) => {
    setBusy(true);
    setError(null);
    try {
      await addBackCharge(subcontract.id, {
        description: backChargeDescription.trim(),
        amount: Number(backChargeAmount),
        dateIncurred: backChargeDate,
      });
      setBackChargeDescription("");
      setBackChargeAmount("");
      setBackChargeDate("");
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
      { key: "create", label: t("sub.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !projectId || !subcontractorId || !hasValidLine },
      { key: "back", label: t("sub.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("sub.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("sub.fieldProject", language)}
            <select style={inputStyle} value={projectId} onChange={(e) => { setProjectId(e.target.value); setContractId(""); setLines([emptyLine()]); }}>
              <option value=""></option>
              {projects.map((p) => <option key={p.id} value={p.id}>{p.documentNumber} — {p.projectName}</option>)}
            </select>
          </label>
          <label>{t("sub.fieldContract", language)}
            <select style={inputStyle} value={contractId} onChange={(e) => setContractId(e.target.value)} disabled={!projectId}>
              <option value=""></option>
              {contractsForProject.map((c) => <option key={c.id} value={c.id}>{c.documentNumber}</option>)}
            </select>
          </label>
          <label>{t("sub.fieldSubcontractor", language)}
            <select style={inputStyle} value={subcontractorId} onChange={(e) => setSubcontractorId(e.target.value)}>
              <option value=""></option>
              {subcontractors.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </label>
          <label>{t("sub.fieldRetentionPercentage", language)}
            <input type="number" min="0" max="100" style={inputStyle} value={retentionPercentage} onChange={(e) => setRetentionPercentage(e.target.value)} />
          </label>
          <label>{t("sub.fieldMobilizationAdvancePercentage", language)}
            <input type="number" min="0" max="100" style={inputStyle} value={mobilizationAdvancePercentage} onChange={(e) => setMobilizationAdvancePercentage(e.target.value)} />
          </label>
          <label>{t("sub.fieldDefectsLiabilityPeriodMonths", language)}
            <input type="number" min="0" style={inputStyle} value={defectsLiabilityPeriodMonths} onChange={(e) => setDefectsLiabilityPeriodMonths(e.target.value)} />
          </label>
        </div>

        {!projectId ? (
          <p>{t("sub.selectProjectFirstHint", language)}</p>
        ) : (
          <>
            <table className="pi-dense-table">
              <thead>
                <tr>
                  <th>{t("sub.fieldLineCode", language)}</th>
                  <th>{t("sub.fieldLineDescription", language)}</th>
                  <th>{t("sub.fieldLineDescriptionArabic", language)}</th>
                  <th>{t("sub.fieldLineUnitOfMeasure", language)}</th>
                  <th>{t("sub.fieldLineQuantity", language)}</th>
                  <th>{t("sub.fieldLineRate", language)}</th>
                  <th>{t("sub.fieldLineWbsElement", language)}</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {lines.map((line, index) => (
                  <tr key={index}>
                    <td><input style={{ inlineSize: "6rem" }} value={line.code} onChange={(e) => updateLine(index, { code: e.target.value })} /></td>
                    <td><input value={line.description} onChange={(e) => updateLine(index, { description: e.target.value })} /></td>
                    <td><input dir="rtl" value={line.descriptionArabic} onChange={(e) => updateLine(index, { descriptionArabic: e.target.value })} /></td>
                    <td>
                      <select value={line.unitOfMeasure} onChange={(e) => updateLine(index, { unitOfMeasure: e.target.value })}>
                        <option value=""></option>
                        {unitsOfMeasure.map((v) => <option key={v.code} value={v.code}>{language === "ar" ? v.nameArabic ?? v.name : v.name}</option>)}
                      </select>
                    </td>
                    <td><input type="number" min="0" step="0.001" style={{ inlineSize: "6rem" }} value={line.quantity} onChange={(e) => updateLine(index, { quantity: e.target.value })} /></td>
                    <td><input type="number" min="0" step="0.01" style={{ inlineSize: "6rem" }} value={line.rate} onChange={(e) => updateLine(index, { rate: e.target.value })} /></td>
                    <td>
                      <select value={line.wbsElementId} onChange={(e) => updateLine(index, { wbsElementId: e.target.value })}>
                        <option value=""></option>
                        {selectedDraftProject?.wbsElements.map((w) => <option key={w.id} value={w.id}>{w.code} — {w.name}</option>)}
                      </select>
                    </td>
                    <td>
                      <button type="button" onClick={() => removeLine(index)} aria-label={t("sub.actionRemoveLine", language)}>
                        {t("sub.actionRemoveLine", language)}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button type="button" onClick={() => setLines((prev) => [...prev, emptyLine()])} style={{ marginBlockStart: "0.5rem" }}>
              {t("sub.actionAddLine", language)}
            </button>
          </>
        )}
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("sub.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {subcontracts.length === 0 ? (
        <p>{t("sub.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("sub.columnDocumentNumber", language)}</th>
              <th>{t("sub.columnProject", language)}</th>
              <th>{t("sub.columnSubcontractor", language)}</th>
              <th>{t("sub.columnSubcontractValue", language)}</th>
              <th>{t("sub.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {subcontracts.map((subcontract) => (
              <tr key={subcontract.id} className={subcontract.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(subcontract.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(subcontract.id); }}>
                    <bdi dir="ltr">{subcontract.documentNumber}</bdi>
                  </button>
                </td>
                <td>{projectLabel(subcontract.projectId)}</td>
                <td>{subcontractorLabel(subcontract.subcontractorId)}</td>
                <td><bdi dir="ltr">{subcontract.subcontractValue.toLocaleString()}</bdi></td>
                <td>{translateStatus(subcontract.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedSubcontract) {
    const subcontract = selectedSubcontract;
    const subcontractProject = projects.find((p) => p.id === subcontract.projectId);
    const actions: ActionItem[] = [];
    if (subcontract.status === "Draft")
      actions.push({ key: "submit", label: t("sub.actionSubmit", language), onClick: () => handleAction(subcontract, "submit"), variant: "primary", isDisabled: busy });
    if (subcontract.status === "Submitted") {
      actions.push({ key: "approve", label: t("sub.actionApprove", language), onClick: () => handleAction(subcontract, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("sub.actionReject", language), onClick: () => handleAction(subcontract, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{subcontract.documentNumber} — {subcontractorLabel(subcontract.subcontractorId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("sub.fieldProject", language)}</dt>
                <dd>{projectLabel(subcontract.projectId)}</dd>
                <dt>{t("sub.fieldContract", language)}</dt>
                <dd><bdi dir="ltr">{contractLabel(subcontract.contractId)}</bdi></dd>
                <dt>{t("sub.fieldSubcontractor", language)}</dt>
                <dd>{subcontractorLabel(subcontract.subcontractorId)}</dd>
                <dt>{t("sub.fieldRetentionPercentage", language)}</dt>
                <dd><bdi dir="ltr">{subcontract.retentionPercentage ?? "—"}</bdi></dd>
                <dt>{t("sub.fieldMobilizationAdvancePercentage", language)}</dt>
                <dd><bdi dir="ltr">{subcontract.mobilizationAdvancePercentage ?? "—"}</bdi></dd>
                <dt>{t("sub.fieldDefectsLiabilityPeriodMonths", language)}</dt>
                <dd><bdi dir="ltr">{subcontract.defectsLiabilityPeriodMonths ?? "—"}</bdi></dd>
                <dt>{t("sub.columnSubcontractValue", language)}</dt>
                <dd><bdi dir="ltr">{subcontract.subcontractValue.toLocaleString()}</bdi></dd>
                <dt>{t("sub.fieldTotalBackCharges", language)}</dt>
                <dd><bdi dir="ltr">{subcontract.totalBackCharges.toLocaleString()}</bdi></dd>
                <dt>{t("sub.fieldNetPayableValue", language)}</dt>
                <dd><bdi dir="ltr">{subcontract.netPayableValue.toLocaleString()}</bdi></dd>
                <dt>{t("sub.columnStatus", language)}</dt>
                <dd>{translateStatus(subcontract.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "lines",
            title: t("sub.tabLines", language),
            content: (
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("sub.fieldLineCode", language)}</th>
                    <th>{t("sub.fieldLineDescription", language)}</th>
                    <th>{t("sub.fieldLineUnitOfMeasure", language)}</th>
                    <th>{t("sub.fieldLineQuantity", language)}</th>
                    <th>{t("sub.fieldLineRate", language)}</th>
                    <th>{t("sub.fieldLineAmount", language)}</th>
                    <th>{t("sub.fieldLineWbsElement", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {subcontract.lines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{line.code}</bdi></td>
                      <td>{language === "ar" ? line.descriptionArabic ?? line.description : line.description}</td>
                      <td>{line.unitOfMeasure}</td>
                      <td><bdi dir="ltr">{line.quantity}</bdi></td>
                      <td><bdi dir="ltr">{line.rate.toLocaleString()}</bdi></td>
                      <td><bdi dir="ltr">{line.amount.toLocaleString()}</bdi></td>
                      <td>{wbsLabel(subcontractProject, line.wbsElementId)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ),
          },
          {
            key: "backCharges",
            title: t("sub.tabBackCharges", language),
            content: (
              <>
                <table className="pi-dense-table">
                  <thead>
                    <tr>
                      <th>{t("sub.fieldBackChargeDescription", language)}</th>
                      <th>{t("sub.fieldBackChargeAmount", language)}</th>
                      <th>{t("sub.fieldBackChargeDate", language)}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {subcontract.backCharges.map((backCharge) => (
                      <tr key={backCharge.id}>
                        <td>{backCharge.description}</td>
                        <td><bdi dir="ltr">{backCharge.amount.toLocaleString()}</bdi></td>
                        <td><bdi dir="ltr">{backCharge.dateIncurred}</bdi></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {subcontract.status === "Approved" ? (
                  <div style={{ maxInlineSize: "28rem", marginBlockStart: "1rem" }}>
                    <label>{t("sub.fieldBackChargeDescription", language)}
                      <input style={inputStyle} value={backChargeDescription} onChange={(e) => setBackChargeDescription(e.target.value)} />
                    </label>
                    <label>{t("sub.fieldBackChargeAmount", language)}
                      <input type="number" min="0" step="0.01" style={inputStyle} value={backChargeAmount} onChange={(e) => setBackChargeAmount(e.target.value)} />
                    </label>
                    <label>{t("sub.fieldBackChargeDate", language)}
                      <input type="date" style={inputStyle} value={backChargeDate} onChange={(e) => setBackChargeDate(e.target.value)} />
                    </label>
                    <button
                      type="button"
                      disabled={busy || !backChargeDescription.trim() || !(Number(backChargeAmount) > 0) || !backChargeDate}
                      onClick={() => handleAddBackCharge(subcontract)}
                    >
                      {t("sub.actionAddBackCharge", language)}
                    </button>
                  </div>
                ) : (
                  <p>{t("sub.backChargeRequiresApprovedHint", language)}</p>
                )}
              </>
            ),
          },
        ]} />
      </>
    );
  }

  return (
    <section>
      <h1>{t("sub.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedSubcontract && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("sub.selectHint", language)}
        ariaLabel={t("sub.heading", language)}
      />
    </section>
  );
}
