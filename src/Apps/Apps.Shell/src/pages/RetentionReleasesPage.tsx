import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveRetentionRelease,
  createRetentionRelease,
  getRetentionReleaseBalance,
  listRetentionReleases,
  rejectRetentionRelease,
  submitRetentionRelease,
} from "../api/retentionReleaseApi";
import type { RetentionBalance, RetentionRelease } from "../api/retentionReleaseApi";
import { listProjects } from "../api/projectApi";
import type { Project } from "../api/projectApi";
import { listContracts } from "../api/contractApi";
import type { Contract } from "../api/contractApi";
import { listSubcontracts } from "../api/subcontractApi";
import type { Subcontract } from "../api/subcontractApi";
import { listGLAccounts } from "../api/glAccountApi";
import type { GLAccount } from "../api/glAccountApi";
import { listTaxCodes } from "../api/taxCodeApi";
import type { TaxCode } from "../api/taxCodeApi";

interface RetentionReleasesPageProps {
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

const triggerEventKeys: Record<string, "retrel.triggerTakingOver" | "retrel.triggerDefectsLiabilityExpiry" | "retrel.triggerManual"> = {
  TakingOver: "retrel.triggerTakingOver",
  DefectsLiabilityExpiry: "retrel.triggerDefectsLiabilityExpiry",
  Manual: "retrel.triggerManual",
};

function translateTrigger(trigger: string, language: SupportedLanguageCode): string {
  const key = triggerEventKeys[trigger];
  return key ? t(key, language) : trigger;
}

export function RetentionReleasesPage({ language }: RetentionReleasesPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [releases, setReleases] = useState<RetentionRelease[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [subcontracts, setSubcontracts] = useState<Subcontract[]>([]);
  const [accounts, setAccounts] = useState<GLAccount[]>([]);
  const [taxCodes, setTaxCodes] = useState<TaxCode[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [projectId, setProjectId] = useState("");
  const [documentType, setDocumentType] = useState<"Contract" | "Subcontract" | "">("");
  const [documentId, setDocumentId] = useState("");
  const [releaseDate, setReleaseDate] = useState("");
  const [triggerEvent, setTriggerEvent] = useState<"TakingOver" | "DefectsLiabilityExpiry" | "Manual" | "">("");
  const [amountReleased, setAmountReleased] = useState("");
  const [revenueAccountId, setRevenueAccountId] = useState("");
  const [receivableAccountId, setReceivableAccountId] = useState("");
  const [expenseAccountId, setExpenseAccountId] = useState("");
  const [payableAccountId, setPayableAccountId] = useState("");
  const [taxCodeId, setTaxCodeId] = useState("");
  const [vatAccountId, setVatAccountId] = useState("");
  const [balance, setBalance] = useState<RetentionBalance | null>(null);
  const [balanceError, setBalanceError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [releaseResult, projectResult, contractResult, subcontractResult, accountResult, taxCodeResult] = await Promise.all([
        listRetentionReleases(200, 0),
        listProjects(200, 0),
        listContracts(200, 0),
        listSubcontracts(200, 0),
        listGLAccounts(200, 0),
        listTaxCodes(200, 0),
      ]);
      setReleases(releaseResult.items);
      setProjects(projectResult.items.filter((p) => p.status === "Approved"));
      setContracts(contractResult.items.filter((c) => c.status === "Approved"));
      setSubcontracts(subcontractResult.items.filter((s) => s.status === "Approved"));
      setAccounts(accountResult.items);
      setTaxCodes(taxCodeResult.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!documentType || !documentId) {
      setBalance(null);
      setBalanceError(null);
      return;
    }
    let cancelled = false;
    getRetentionReleaseBalance(documentType, documentId)
      .then((b) => { if (!cancelled) { setBalance(b); setBalanceError(null); } })
      .catch((e) => { if (!cancelled) { setBalance(null); setBalanceError(e instanceof Error ? e.message : String(e)); } });
    return () => { cancelled = true; };
  }, [documentType, documentId]);

  const selectedId = view.kind === "browse" ? view.selectedId : null;
  const selectedRelease = selectedId ? releases.find((r) => r.id === selectedId) ?? null : null;

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

  const documentLabel = (release: RetentionRelease) => {
    if (release.commercialDocumentType === "Contract")
      return contracts.find((c) => c.id === release.commercialDocumentId)?.documentNumber ?? release.commercialDocumentId;
    return subcontracts.find((s) => s.id === release.commercialDocumentId)?.documentNumber ?? release.commercialDocumentId;
  };

  const accountLabel = (id: string) => {
    const account = accounts.find((a) => a.id === id);
    return account ? `${account.accountCode} — ${account.accountName}` : id;
  };

  const openDetails = (id: string) => setView({ kind: "browse", selectedId: id });

  const resetCreateForm = () => {
    setProjectId("");
    setDocumentType("");
    setDocumentId("");
    setReleaseDate("");
    setTriggerEvent("");
    setAmountReleased("");
    setRevenueAccountId("");
    setReceivableAccountId("");
    setExpenseAccountId("");
    setPayableAccountId("");
    setTaxCodeId("");
    setVatAccountId("");
    setBalance(null);
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createRetentionRelease({
        projectId,
        commercialDocumentType: documentType,
        commercialDocumentId: documentId,
        releaseDate,
        amountReleased: Number(amountReleased) || 0,
        triggerEvent,
        revenueAccountId: documentType === "Contract" ? revenueAccountId || undefined : undefined,
        receivableAccountId: documentType === "Contract" ? receivableAccountId || undefined : undefined,
        expenseAccountId: documentType === "Subcontract" ? expenseAccountId || undefined : undefined,
        payableAccountId: documentType === "Subcontract" ? payableAccountId || undefined : undefined,
        taxCodeId: taxCodeId || undefined,
        vatAccountId: taxCodeId ? vatAccountId || undefined : undefined,
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

  const handleAction = async (release: RetentionRelease, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      if (action === "submit") await submitRetentionRelease(release.id);
      else if (action === "approve") await approveRetentionRelease(release.id);
      else await rejectRetentionRelease(release.id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const missingBillingAccounts =
      (documentType === "Contract" && (!revenueAccountId || !receivableAccountId)) ||
      (documentType === "Subcontract" && (!expenseAccountId || !payableAccountId));
    const exceedsBalance = balance !== null && Number(amountReleased) > balance.outstandingBalance;
    const actions: ActionItem[] = [
      {
        key: "create", label: t("retrel.actionCreate", language), onClick: handleCreate, variant: "primary",
        isDisabled: busy || !projectId || !documentType || !documentId || !releaseDate || !triggerEvent || !amountReleased || missingBillingAccounts || exceedsBalance,
      },
      { key: "back", label: t("retrel.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("retrel.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem" }}>
          <label>{t("retrel.fieldProject", language)}
            <select style={inputStyle} value={projectId} onChange={(e) => { setProjectId(e.target.value); setDocumentType(""); setDocumentId(""); }}>
              <option value=""></option>
              {projects.map((p) => <option key={p.id} value={p.id}>{p.documentNumber} — {p.projectName}</option>)}
            </select>
          </label>
          <label>{t("retrel.fieldDocumentType", language)}
            <select style={inputStyle} value={documentType} disabled={!projectId}
              onChange={(e) => { setDocumentType(e.target.value as "Contract" | "Subcontract" | ""); setDocumentId(""); }}>
              <option value=""></option>
              <option value="Contract">{t("meas.documentTypeContract", language)}</option>
              <option value="Subcontract">{t("meas.documentTypeSubcontract", language)}</option>
            </select>
          </label>
          <label>{t("retrel.fieldDocument", language)}
            <select style={inputStyle} value={documentId} disabled={!documentType} onChange={(e) => setDocumentId(e.target.value)}>
              <option value=""></option>
              {documentOptions.map((d) => <option key={d.id} value={d.id}>{d.label}</option>)}
            </select>
          </label>
          {documentId && balanceError && <p style={{ color: "var(--pi-danger)" }}>{balanceError}</p>}
          {documentId && balance && (
            <dl style={{ maxInlineSize: "28rem", border: "1px solid var(--pi-border)", borderRadius: "0.5rem", padding: "0.75rem 1rem" }}>
              <dt>{t("retrel.fieldTotalWithheld", language)}</dt>
              <dd><bdi dir="ltr">{balance.totalWithheldToDate.toLocaleString()}</bdi></dd>
              <dt>{t("retrel.fieldTotalReleased", language)}</dt>
              <dd><bdi dir="ltr">{balance.totalReleasedToDate.toLocaleString()}</bdi></dd>
              <dt><strong>{t("retrel.fieldOutstandingBalance", language)}</strong></dt>
              <dd><strong><bdi dir="ltr">{balance.outstandingBalance.toLocaleString()}</bdi></strong></dd>
            </dl>
          )}
          <label>{t("retrel.fieldReleaseDate", language)}
            <input type="date" style={inputStyle} value={releaseDate} onChange={(e) => setReleaseDate(e.target.value)} />
          </label>
          <label>{t("retrel.fieldTriggerEvent", language)}
            <select style={inputStyle} value={triggerEvent} onChange={(e) => setTriggerEvent(e.target.value as typeof triggerEvent)}>
              <option value=""></option>
              <option value="TakingOver">{t("retrel.triggerTakingOver", language)}</option>
              <option value="DefectsLiabilityExpiry">{t("retrel.triggerDefectsLiabilityExpiry", language)}</option>
              <option value="Manual">{t("retrel.triggerManual", language)}</option>
            </select>
          </label>
          <label>{t("retrel.fieldAmountReleased", language)}
            <input type="number" min="0" step="0.01" style={inputStyle} value={amountReleased} onChange={(e) => setAmountReleased(e.target.value)} />
          </label>
          {exceedsBalance && <p style={{ color: "var(--pi-danger)" }}>{t("retrel.exceedsBalanceHint", language)}</p>}
          {documentType === "Contract" && (
            <>
              <p>{t("ipc.billingAccountsHint", language)}</p>
              <label>{t("ipc.fieldRevenueAccount", language)}
                <select style={inputStyle} value={revenueAccountId} onChange={(e) => setRevenueAccountId(e.target.value)}>
                  <option value=""></option>
                  {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
                </select>
              </label>
              <label>{t("ipc.fieldReceivableAccount", language)}
                <select style={inputStyle} value={receivableAccountId} onChange={(e) => setReceivableAccountId(e.target.value)}>
                  <option value=""></option>
                  {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
                </select>
              </label>
            </>
          )}
          {documentType === "Subcontract" && (
            <>
              <p>{t("ipc.apBillingAccountsHint", language)}</p>
              <label>{t("ipc.fieldExpenseAccount", language)}
                <select style={inputStyle} value={expenseAccountId} onChange={(e) => setExpenseAccountId(e.target.value)}>
                  <option value=""></option>
                  {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
                </select>
              </label>
              <label>{t("ipc.fieldPayableAccount", language)}
                <select style={inputStyle} value={payableAccountId} onChange={(e) => setPayableAccountId(e.target.value)}>
                  <option value=""></option>
                  {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
                </select>
              </label>
            </>
          )}
          {documentType && (
            <>
              <label>{t("ar.fieldTaxCode", language)}
                <select style={inputStyle} value={taxCodeId} onChange={(e) => setTaxCodeId(e.target.value)}>
                  <option value="">{t("ar.noTaxCode", language)}</option>
                  {taxCodes.map((tc) => <option key={tc.id} value={tc.id}>{tc.taxCodeCode} — {tc.rate}%</option>)}
                </select>
              </label>
              {taxCodeId && (
                <label>{t("ar.fieldVatAccount", language)}
                  <select style={inputStyle} value={vatAccountId} onChange={(e) => setVatAccountId(e.target.value)}>
                    <option value=""></option>
                    {accounts.map((a) => <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>)}
                  </select>
                </label>
              )}
            </>
          )}
        </div>
      </section>
    );
  }

  const listActions: ActionItem[] = [
    { key: "new", label: t("retrel.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  const listPane = (
    <>
      {releases.length === 0 ? (
        <p>{t("retrel.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("retrel.columnDocumentNumber", language)}</th>
              <th>{t("retrel.columnProject", language)}</th>
              <th>{t("retrel.columnAmountReleased", language)}</th>
              <th>{t("retrel.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {releases.map((release) => (
              <tr key={release.id} className={release.id === selectedId ? "is-selected" : undefined} onClick={() => openDetails(release.id)}>
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(release.id); }}>
                    <bdi dir="ltr">{release.documentNumber}</bdi>
                  </button>
                </td>
                <td>{projectLabel(release.projectId)}</td>
                <td><bdi dir="ltr">{release.amountReleased.toLocaleString()}</bdi></td>
                <td>{translateStatus(release.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedRelease) {
    const release = selectedRelease;
    const actions: ActionItem[] = [];
    if (release.status === "Draft")
      actions.push({ key: "submit", label: t("retrel.actionSubmit", language), onClick: () => handleAction(release, "submit"), variant: "primary", isDisabled: busy });
    if (release.status === "Submitted") {
      actions.push({ key: "approve", label: t("retrel.actionApprove", language), onClick: () => handleAction(release, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("retrel.actionReject", language), onClick: () => handleAction(release, "reject"), isDisabled: busy });
    }

    detailPane = (
      <>
        <h1>{release.documentNumber} — {documentLabel(release)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("retrel.fieldProject", language)}</dt>
                <dd>{projectLabel(release.projectId)}</dd>
                <dt>{t("retrel.fieldDocument", language)}</dt>
                <dd><bdi dir="ltr">{documentLabel(release)}</bdi></dd>
                <dt>{t("retrel.fieldReleaseDate", language)}</dt>
                <dd><bdi dir="ltr">{release.releaseDate}</bdi></dd>
                <dt>{t("retrel.fieldTriggerEvent", language)}</dt>
                <dd>{translateTrigger(release.triggerEvent, language)}</dd>
                <dt>{t("retrel.fieldAmountReleased", language)}</dt>
                <dd><bdi dir="ltr">{release.amountReleased.toLocaleString()}</bdi></dd>
                <dt>{t("retrel.columnStatus", language)}</dt>
                <dd>{translateStatus(release.status, language)}</dd>
                {release.revenueAccountId && (
                  <>
                    <dt>{t("ipc.fieldRevenueAccount", language)}</dt>
                    <dd><bdi dir="ltr">{accountLabel(release.revenueAccountId)}</bdi></dd>
                  </>
                )}
                {release.receivableAccountId && (
                  <>
                    <dt>{t("ipc.fieldReceivableAccount", language)}</dt>
                    <dd><bdi dir="ltr">{accountLabel(release.receivableAccountId)}</bdi></dd>
                  </>
                )}
                {release.linkedArInvoiceId && (
                  <>
                    <dt>{t("ipc.linkedArInvoice", language)}</dt>
                    <dd><bdi dir="ltr">{release.linkedArInvoiceId}</bdi></dd>
                  </>
                )}
                {release.expenseAccountId && (
                  <>
                    <dt>{t("ipc.fieldExpenseAccount", language)}</dt>
                    <dd><bdi dir="ltr">{accountLabel(release.expenseAccountId)}</bdi></dd>
                  </>
                )}
                {release.payableAccountId && (
                  <>
                    <dt>{t("ipc.fieldPayableAccount", language)}</dt>
                    <dd><bdi dir="ltr">{accountLabel(release.payableAccountId)}</bdi></dd>
                  </>
                )}
                {release.linkedApInvoiceId && (
                  <>
                    <dt>{t("ipc.linkedApInvoice", language)}</dt>
                    <dd><bdi dir="ltr">{release.linkedApInvoiceId}</bdi></dd>
                  </>
                )}
              </dl>
            ),
          },
        ]} />
      </>
    );
  }

  return (
    <section>
      <h1>{t("retrel.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedRelease && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("retrel.selectHint", language)}
        ariaLabel={t("retrel.heading", language)}
      />
    </section>
  );
}
