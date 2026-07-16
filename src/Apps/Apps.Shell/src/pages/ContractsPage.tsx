import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveContract,
  createContract,
  listContracts,
  rejectContract,
  submitContract,
} from "../api/contractApi";
import type { Contract, CreateBoqLineInput } from "../api/contractApi";
import { listProjects } from "../api/projectApi";
import type { Project } from "../api/projectApi";
import { listLookupValues } from "../api/lookupApi";
import type { LookupValue } from "../api/lookupApi";

interface ContractsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

type DraftBoqLine = {
  code: string;
  description: string;
  descriptionArabic: string;
  unitOfMeasure: string;
  quantity: string;
  rate: string;
  wbsElementId: string;
};

function emptyBoqLine(): DraftBoqLine {
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

export function ContractsPage({ language }: ContractsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [contractTypes, setContractTypes] = useState<LookupValue[]>([]);
  const [unitsOfMeasure, setUnitsOfMeasure] = useState<LookupValue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectId, setProjectId] = useState("");
  const [contractType, setContractType] = useState("");
  const [paymentTerms, setPaymentTerms] = useState("");
  const [advancePaymentPercentage, setAdvancePaymentPercentage] = useState("");
  const [defectsLiabilityPeriodMonths, setDefectsLiabilityPeriodMonths] = useState("");
  const [boqLines, setBoqLines] = useState<DraftBoqLine[]>([emptyBoqLine()]);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [contractResult, projectResult, contractTypeResult, uomResult] = await Promise.all([
        listContracts(200, 0),
        listProjects(200, 0),
        listLookupValues("ContractType", false),
        listLookupValues("UnitOfMeasure", false),
      ]);
      setContracts(contractResult.items);
      setProjects(projectResult.items.filter((p) => p.status === "Approved"));
      setContractTypes(contractTypeResult);
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
  const selectedContract = selectedId ? contracts.find((c) => c.id === selectedId) ?? null : null;

  const projectLabel = (id: string) => {
    const project = projects.find((p) => p.id === id);
    return project ? `${project.documentNumber ?? ""} — ${project.projectName}` : id;
  };
  const wbsLabel = (project: Project | undefined, id: string) => {
    if (!project) return id;
    const element = project.wbsElements.find((w) => w.id === id);
    return element ? `${element.code} — ${element.name}` : id;
  };

  const selectedDraftProject = projects.find((p) => p.id === projectId);

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const updateBoqLine = (index: number, patch: Partial<DraftBoqLine>) => {
    setBoqLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const removeBoqLine = (index: number) => {
    setBoqLines((prev) => prev.filter((_, i) => i !== index));
  };

  const resetCreateForm = () => {
    setProjectId("");
    setContractType("");
    setPaymentTerms("");
    setAdvancePaymentPercentage("");
    setDefectsLiabilityPeriodMonths("");
    setBoqLines([emptyBoqLine()]);
  };

  const hasValidBoqLine = boqLines.some(
    (l) => l.code.trim() && l.description.trim() && l.unitOfMeasure && l.wbsElementId && Number(l.quantity) > 0 && Number(l.rate) > 0,
  );

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const inputs: CreateBoqLineInput[] = boqLines
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

      const created = await createContract({
        projectId,
        contractType,
        paymentTerms: paymentTerms.trim() || undefined,
        advancePaymentPercentage: advancePaymentPercentage ? Number(advancePaymentPercentage) : undefined,
        defectsLiabilityPeriodMonths: defectsLiabilityPeriodMonths ? Number(defectsLiabilityPeriodMonths) : undefined,
        boqLines: inputs,
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

  const handleAction = async (contract: Contract, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitContract(contract.id);
      else if (action === "approve") await approveContract(contract.id);
      else await rejectContract(contract.id);
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
      { key: "create", label: t("con.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !projectId || !contractType || !hasValidBoqLine },
      { key: "back", label: t("con.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("con.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("con.fieldProject", language)}
            <select style={inputStyle} value={projectId} onChange={(e) => { setProjectId(e.target.value); setBoqLines([emptyBoqLine()]); }}>
              <option value=""></option>
              {projects.map((p) => <option key={p.id} value={p.id}>{p.documentNumber} — {p.projectName}</option>)}
            </select>
          </label>
          <label>{t("con.fieldContractType", language)}
            <select style={inputStyle} value={contractType} onChange={(e) => setContractType(e.target.value)}>
              <option value=""></option>
              {contractTypes.map((v) => <option key={v.code} value={v.code}>{language === "ar" ? v.nameArabic ?? v.name : v.name}</option>)}
            </select>
          </label>
          <label>{t("con.fieldPaymentTerms", language)}
            <input style={inputStyle} value={paymentTerms} onChange={(e) => setPaymentTerms(e.target.value)} />
          </label>
          <label>{t("con.fieldAdvancePaymentPercentage", language)}
            <input type="number" min="0" max="100" style={inputStyle} value={advancePaymentPercentage} onChange={(e) => setAdvancePaymentPercentage(e.target.value)} />
          </label>
          <label>{t("con.fieldDefectsLiabilityPeriodMonths", language)}
            <input type="number" min="0" style={inputStyle} value={defectsLiabilityPeriodMonths} onChange={(e) => setDefectsLiabilityPeriodMonths(e.target.value)} />
          </label>
        </div>

        {!projectId ? (
          <p>{t("con.selectProjectFirstHint", language)}</p>
        ) : (
          <>
            <table className="pi-dense-table">
              <thead>
                <tr>
                  <th>{t("con.fieldBoqCode", language)}</th>
                  <th>{t("con.fieldBoqDescription", language)}</th>
                  <th>{t("con.fieldBoqDescriptionArabic", language)}</th>
                  <th>{t("con.fieldBoqUnitOfMeasure", language)}</th>
                  <th>{t("con.fieldBoqQuantity", language)}</th>
                  <th>{t("con.fieldBoqRate", language)}</th>
                  <th>{t("con.fieldBoqWbsElement", language)}</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {boqLines.map((line, index) => (
                  <tr key={index}>
                    <td><input style={{ inlineSize: "6rem" }} value={line.code} onChange={(e) => updateBoqLine(index, { code: e.target.value })} /></td>
                    <td><input value={line.description} onChange={(e) => updateBoqLine(index, { description: e.target.value })} /></td>
                    <td><input dir="rtl" value={line.descriptionArabic} onChange={(e) => updateBoqLine(index, { descriptionArabic: e.target.value })} /></td>
                    <td>
                      <select value={line.unitOfMeasure} onChange={(e) => updateBoqLine(index, { unitOfMeasure: e.target.value })}>
                        <option value=""></option>
                        {unitsOfMeasure.map((v) => <option key={v.code} value={v.code}>{language === "ar" ? v.nameArabic ?? v.name : v.name}</option>)}
                      </select>
                    </td>
                    <td><input type="number" min="0" step="0.001" style={{ inlineSize: "6rem" }} value={line.quantity} onChange={(e) => updateBoqLine(index, { quantity: e.target.value })} /></td>
                    <td><input type="number" min="0" step="0.01" style={{ inlineSize: "6rem" }} value={line.rate} onChange={(e) => updateBoqLine(index, { rate: e.target.value })} /></td>
                    <td>
                      <select value={line.wbsElementId} onChange={(e) => updateBoqLine(index, { wbsElementId: e.target.value })}>
                        <option value=""></option>
                        {selectedDraftProject?.wbsElements.map((w) => <option key={w.id} value={w.id}>{w.code} — {w.name}</option>)}
                      </select>
                    </td>
                    <td>
                      <button type="button" onClick={() => removeBoqLine(index)} aria-label={t("con.actionRemoveBoqLine", language)}>
                        {t("con.actionRemoveBoqLine", language)}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <button type="button" onClick={() => setBoqLines((prev) => [...prev, emptyBoqLine()])} style={{ marginBlockStart: "0.5rem" }}>
              {t("con.actionAddBoqLine", language)}
            </button>
          </>
        )}
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("con.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {contracts.length === 0 ? (
        <p>{t("con.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("con.columnDocumentNumber", language)}</th>
              <th>{t("con.columnProject", language)}</th>
              <th>{t("con.columnContractValue", language)}</th>
              <th>{t("con.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {contracts.map((contract) => (
              <tr key={contract.id} className={contract.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(contract.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(contract.id); }}>
                    <bdi dir="ltr">{contract.documentNumber}</bdi>
                  </button>
                </td>
                <td>{projectLabel(contract.projectId)}</td>
                <td><bdi dir="ltr">{contract.contractValue.toLocaleString()}</bdi></td>
                <td>{translateStatus(contract.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedContract) {
    const contract = selectedContract;
    const contractProject = projects.find((p) => p.id === contract.projectId);
    const actions: ActionItem[] = [];
    if (contract.status === "Draft")
      actions.push({ key: "submit", label: t("con.actionSubmit", language), onClick: () => handleAction(contract, "submit"), variant: "primary", isDisabled: busy });
    if (contract.status === "Submitted") {
      actions.push({ key: "approve", label: t("con.actionApprove", language), onClick: () => handleAction(contract, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("con.actionReject", language), onClick: () => handleAction(contract, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{contract.documentNumber} — {projectLabel(contract.projectId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("con.fieldProject", language)}</dt>
                <dd>{projectLabel(contract.projectId)}</dd>
                <dt>{t("con.fieldContractType", language)}</dt>
                <dd>{contract.contractType}</dd>
                <dt>{t("con.fieldPaymentTerms", language)}</dt>
                <dd>{contract.paymentTerms ?? "—"}</dd>
                <dt>{t("con.fieldAdvancePaymentPercentage", language)}</dt>
                <dd><bdi dir="ltr">{contract.advancePaymentPercentage ?? "—"}</bdi></dd>
                <dt>{t("con.fieldDefectsLiabilityPeriodMonths", language)}</dt>
                <dd><bdi dir="ltr">{contract.defectsLiabilityPeriodMonths ?? "—"}</bdi></dd>
                <dt>{t("con.columnContractValue", language)}</dt>
                <dd><bdi dir="ltr">{contract.contractValue.toLocaleString()}</bdi></dd>
                <dt>{t("con.columnStatus", language)}</dt>
                <dd>{translateStatus(contract.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "boq",
            title: t("con.tabBoqLines", language),
            content: (
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("con.fieldBoqCode", language)}</th>
                    <th>{t("con.fieldBoqDescription", language)}</th>
                    <th>{t("con.fieldBoqUnitOfMeasure", language)}</th>
                    <th>{t("con.fieldBoqQuantity", language)}</th>
                    <th>{t("con.fieldBoqRate", language)}</th>
                    <th>{t("con.fieldBoqAmount", language)}</th>
                    <th>{t("con.fieldBoqWbsElement", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {contract.boqLines.map((line) => (
                    <tr key={line.id}>
                      <td><bdi dir="ltr">{line.code}</bdi></td>
                      <td>{language === "ar" ? line.descriptionArabic ?? line.description : line.description}</td>
                      <td>{line.unitOfMeasure}</td>
                      <td><bdi dir="ltr">{line.quantity}</bdi></td>
                      <td><bdi dir="ltr">{line.rate.toLocaleString()}</bdi></td>
                      <td><bdi dir="ltr">{line.amount.toLocaleString()}</bdi></td>
                      <td>{wbsLabel(contractProject, line.wbsElementId)}</td>
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
      <h1>{t("con.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedContract && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("con.selectHint", language)}
        ariaLabel={t("con.heading", language)}
      />
    </section>
  );
}
