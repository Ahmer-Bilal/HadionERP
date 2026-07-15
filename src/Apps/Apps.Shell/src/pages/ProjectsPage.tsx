import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveProject,
  createProject,
  listProjects,
  rejectProject,
  submitProject,
} from "../api/projectApi";
import type { CreateWbsElementInput, Project } from "../api/projectApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";

interface ProjectsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

type DraftWbsElement = {
  code: string;
  name: string;
  parentIndex: string; // index into the draft array as a string, or "" for top-level
  isPlanningElement: boolean;
  isAccountAssignmentElement: boolean;
  isBillingElement: boolean;
};

function emptyWbsElement(): DraftWbsElement {
  return { code: "", name: "", parentIndex: "", isPlanningElement: false, isAccountAssignmentElement: false, isBillingElement: false };
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

export function ProjectsPage({ language }: ProjectsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [projects, setProjects] = useState<Project[]>([]);
  const [customers, setCustomers] = useState<BusinessPartner[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectName, setProjectName] = useState("");
  const [projectNameArabic, setProjectNameArabic] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [wbsElements, setWbsElements] = useState<DraftWbsElement[]>([emptyWbsElement()]);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [projectResult, partnerResult] = await Promise.all([
        listProjects(200, 0),
        listBusinessPartners(200, 0),
      ]);
      setProjects(projectResult.items);
      setCustomers(partnerResult.items.filter((p) => p.status === "Approved" && p.businessRoles.some((r) => r.roleType === "Client")));
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
  const selectedProject = selectedId ? projects.find((p) => p.id === selectedId) ?? null : null;

  const customerLabel = (id: string | null) => (id ? customers.find((c) => c.id === id)?.name ?? id : "—");
  const wbsLabel = (project: Project, id: string | null) => {
    if (!id) return "—";
    const element = project.wbsElements.find((w) => w.id === id);
    return element ? `${element.code} — ${element.name}` : id;
  };

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const updateWbsElement = (index: number, patch: Partial<DraftWbsElement>) => {
    setWbsElements((prev) => prev.map((w, i) => (i === index ? { ...w, ...patch } : w)));
  };

  const removeWbsElement = (index: number) => {
    setWbsElements((prev) => prev.filter((_, i) => i !== index));
  };

  const resetCreateForm = () => {
    setProjectName("");
    setProjectNameArabic("");
    setCustomerId("");
    setStartDate("");
    setEndDate("");
    setWbsElements([emptyWbsElement()]);
  };

  const hasValidWbsElement = wbsElements.some((w) => w.code.trim() && w.name.trim());

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      // Surviving (non-blank) rows, each remembering its original index in `wbsElements` so parent
      // references (which point at an original index via parentIndex) can be resolved to the new,
      // filtered tempId space even if a blank row above them was dropped.
      const survivors = wbsElements
        .map((w, originalIndex) => ({ w, originalIndex }))
        .filter(({ w }) => w.code.trim() && w.name.trim());
      const tempIdByOriginalIndex = new Map(survivors.map(({ originalIndex }, tempId) => [originalIndex, tempId]));

      const inputs: CreateWbsElementInput[] = survivors.map(({ w }, tempId) => {
        const parentOriginalIndex = w.parentIndex === "" ? null : Number(w.parentIndex);
        const parentTempId = parentOriginalIndex === null ? null : tempIdByOriginalIndex.get(parentOriginalIndex) ?? null;
        return {
          tempId,
          parentTempId,
          code: w.code.trim(),
          name: w.name.trim(),
          isPlanningElement: w.isPlanningElement,
          isAccountAssignmentElement: w.isAccountAssignmentElement,
          isBillingElement: w.isBillingElement,
        };
      });

      const created = await createProject({
        projectName: projectName.trim(),
        projectNameArabic: projectNameArabic.trim() || undefined,
        customerId: customerId || undefined,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        wbsElements: inputs,
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

  const handleAction = async (project: Project, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitProject(project.id);
      else if (action === "approve") await approveProject(project.id);
      else await rejectProject(project.id);
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
      { key: "create", label: t("proj.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !projectName.trim() || !hasValidWbsElement },
      { key: "back", label: t("proj.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("proj.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("proj.fieldProjectName", language)}
            <input style={inputStyle} value={projectName} onChange={(e) => setProjectName(e.target.value)} />
          </label>
          <label>{t("proj.fieldProjectNameArabic", language)}
            <input dir="rtl" style={inputStyle} value={projectNameArabic} onChange={(e) => setProjectNameArabic(e.target.value)} />
          </label>
          <label>{t("proj.fieldCustomer", language)}
            <select style={inputStyle} value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
              <option value=""></option>
              {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </label>
          <label>{t("proj.fieldStartDate", language)}
            <input type="date" style={inputStyle} value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          </label>
          <label>{t("proj.fieldEndDate", language)}
            <input type="date" style={inputStyle} value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          </label>
        </div>

        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("proj.fieldWbsCode", language)}</th>
              <th>{t("proj.fieldWbsName", language)}</th>
              <th>{t("proj.fieldWbsParent", language)}</th>
              <th>{t("proj.fieldPlanningElement", language)}</th>
              <th>{t("proj.fieldAccountAssignmentElement", language)}</th>
              <th>{t("proj.fieldBillingElement", language)}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {wbsElements.map((element, index) => (
              <tr key={index}>
                <td><input style={{ inlineSize: "6rem" }} value={element.code} onChange={(e) => updateWbsElement(index, { code: e.target.value })} /></td>
                <td><input value={element.name} onChange={(e) => updateWbsElement(index, { name: e.target.value })} /></td>
                <td>
                  <select value={element.parentIndex} onChange={(e) => updateWbsElement(index, { parentIndex: e.target.value })}>
                    <option value="">{t("proj.wbsTopLevel", language)}</option>
                    {wbsElements.map((candidate, candidateIndex) => (
                      candidateIndex !== index && candidate.code.trim() && (
                        <option key={candidateIndex} value={candidateIndex}>{candidate.code}</option>
                      )
                    ))}
                  </select>
                </td>
                <td style={{ textAlign: "center" }}><input type="checkbox" checked={element.isPlanningElement} onChange={(e) => updateWbsElement(index, { isPlanningElement: e.target.checked })} /></td>
                <td style={{ textAlign: "center" }}><input type="checkbox" checked={element.isAccountAssignmentElement} onChange={(e) => updateWbsElement(index, { isAccountAssignmentElement: e.target.checked })} /></td>
                <td style={{ textAlign: "center" }}><input type="checkbox" checked={element.isBillingElement} onChange={(e) => updateWbsElement(index, { isBillingElement: e.target.checked })} /></td>
                <td>
                  <button type="button" onClick={() => removeWbsElement(index)} aria-label={t("proj.actionRemoveWbsElement", language)}>
                    {t("proj.actionRemoveWbsElement", language)}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <button type="button" onClick={() => setWbsElements((prev) => [...prev, emptyWbsElement()])} style={{ marginBlockStart: "0.5rem" }}>
          {t("proj.actionAddWbsElement", language)}
        </button>
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("proj.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {projects.length === 0 ? (
        <p>{t("proj.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("proj.columnDocumentNumber", language)}</th>
              <th>{t("proj.columnProjectName", language)}</th>
              <th>{t("proj.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {projects.map((project) => (
              <tr key={project.id} className={project.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(project.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(project.id); }}>
                    <bdi dir="ltr">{project.documentNumber}</bdi>
                  </button>
                </td>
                <td>{project.projectName}</td>
                <td>{translateStatus(project.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedProject) {
    const project = selectedProject;
    const actions: ActionItem[] = [];
    if (project.status === "Draft")
      actions.push({ key: "submit", label: t("proj.actionSubmit", language), onClick: () => handleAction(project, "submit"), variant: "primary", isDisabled: busy });
    if (project.status === "Submitted") {
      actions.push({ key: "approve", label: t("proj.actionApprove", language), onClick: () => handleAction(project, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("proj.actionReject", language), onClick: () => handleAction(project, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{project.documentNumber} — {project.projectName}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("proj.fieldCustomer", language)}</dt>
                <dd>{customerLabel(project.customerId)}</dd>
                <dt>{t("proj.fieldStartDate", language)}</dt>
                <dd><bdi dir="ltr">{project.startDate ?? "—"}</bdi></dd>
                <dt>{t("proj.fieldEndDate", language)}</dt>
                <dd><bdi dir="ltr">{project.endDate ?? "—"}</bdi></dd>
                <dt>{t("proj.columnStatus", language)}</dt>
                <dd>{translateStatus(project.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "wbs",
            title: t("proj.tabWbsElements", language),
            content: (
              <table className="pi-dense-table">
                <thead>
                  <tr>
                    <th>{t("proj.fieldWbsCode", language)}</th>
                    <th>{t("proj.fieldWbsName", language)}</th>
                    <th>{t("proj.fieldWbsParent", language)}</th>
                    <th>{t("proj.fieldPlanningElement", language)}</th>
                    <th>{t("proj.fieldAccountAssignmentElement", language)}</th>
                    <th>{t("proj.fieldBillingElement", language)}</th>
                  </tr>
                </thead>
                <tbody>
                  {project.wbsElements.map((element) => (
                    <tr key={element.id}>
                      <td><bdi dir="ltr">{element.code}</bdi></td>
                      <td>{element.name}</td>
                      <td>{wbsLabel(project, element.parentWbsElementId)}</td>
                      <td style={{ textAlign: "center" }}>{element.isPlanningElement ? "✓" : ""}</td>
                      <td style={{ textAlign: "center" }}>{element.isAccountAssignmentElement ? "✓" : ""}</td>
                      <td style={{ textAlign: "center" }}>{element.isBillingElement ? "✓" : ""}</td>
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
      <h1>{t("proj.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedProject && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("proj.selectHint", language)}
        ariaLabel={t("proj.heading", language)}
      />
    </section>
  );
}
