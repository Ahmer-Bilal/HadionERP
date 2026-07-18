import { useCallback, useEffect, useMemo, useState } from "react";
import { ActionPane } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveJournalEntry,
  createJournalEntry,
  getJournalEntryDocumentFlow,
  listJournalEntries,
  postJournalEntry,
  rejectJournalEntry,
  reverseJournalEntry,
  submitJournalEntry,
} from "../api/journalEntryApi";
import type { CreateJournalLineInput, JournalEntry, JournalEntryDocumentFlowNode } from "../api/journalEntryApi";
import { listGLAccounts } from "../api/glAccountApi";
import type { GLAccount } from "../api/glAccountApi";
import { listAttachments, uploadAttachment, downloadAttachment, deleteAttachment } from "../api/attachmentApi";
import type { AttachmentMetadata } from "../api/attachmentApi";
import { listNotes, addNote, deleteNote } from "../api/noteApi";
import type { Note } from "../api/noteApi";
import { listAuditHistory } from "../api/auditHistoryApi";
import type { AuditHistoryEntry } from "../api/auditHistoryApi";
import { getIpc } from "../api/ipcApi";
import { getRetentionRelease } from "../api/retentionReleaseApi";
import { getPurchaseOrder } from "../api/purchaseOrderApi";
import { getRequestForQuotation } from "../api/requestForQuotationApi";
import { getPurchaseRequisition } from "../api/purchaseRequisitionApi";
import { listGoodsReceiptNotes } from "../api/goodsReceiptNoteApi";

interface JournalEntriesPageProps {
  language: SupportedLanguageCode;
}

const NOTE_TARGET_TYPE = "JournalEntry";

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; entry: JournalEntry };
type DetailTab = "overview" | "lineItems" | "attachments" | "notes" | "history" | "related";
type DraftLine = { glAccountId: string; debitAmount: string; creditAmount: string; lineDescription: string };

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected" | "je.statusPosted" | "je.statusReversed"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
  Posted: "je.statusPosted",
  Reversed: "je.statusReversed",
};

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

const sourceDocumentTypeKeys: Record<string, "je.sourceManual" | "je.sourceAPInvoice" | "je.sourceARInvoice" | "je.sourcePayment" | "je.sourceCustomerReceipt"> = {
  Manual: "je.sourceManual",
  APInvoice: "je.sourceAPInvoice",
  ARInvoice: "je.sourceARInvoice",
  Payment: "je.sourcePayment",
  CustomerReceipt: "je.sourceCustomerReceipt",
};

/** Human label for JournalEntry.sourceDocumentType — "—" for entries created before this field existed
 * (see that field's own doc comment on the Domain entity), the literal type string for a future source
 * type this page doesn't know how to translate yet, never a blank cell. */
function translateSourceDocumentType(sourceDocumentType: string | null, language: SupportedLanguageCode): string {
  if (!sourceDocumentType) return "—";
  const key = sourceDocumentTypeKeys[sourceDocumentType];
  return key ? t(key, language) : sourceDocumentType;
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function emptyLine(): DraftLine {
  return { glAccountId: "", debitAmount: "", creditAmount: "", lineDescription: "" };
}

/** Resolves the one extra hop the backend deliberately leaves to the client — an AP/AR Invoice's own
 * origin (Ipc/RetentionRelease/PurchaseOrder) — by calling Construction's/Procurement's own already-
 * published APIs directly, never through Finance's backend (see JournalEntryDocumentFlowService's own doc
 * comment for why). Best-effort: a resolution failure just leaves that one node's label as its raw kind. */
async function resolveNodeLabel(node: JournalEntryDocumentFlowNode): Promise<{ label: string; documentNumber: string | null }> {
  if (!node.documentId) return { label: node.label, documentNumber: node.documentNumber };
  try {
    if (node.kind === "Ipc") {
      const ipc = await getIpc(node.documentId);
      return { label: "Construction Billing (IPC)", documentNumber: ipc.documentNumber };
    }
    if (node.kind === "RetentionRelease") {
      const release = await getRetentionRelease(node.documentId);
      return { label: "Retention Release", documentNumber: release.documentNumber };
    }
    if (node.kind === "PurchaseOrder") {
      const po = await getPurchaseOrder(node.documentId);
      return { label: "Purchase Order", documentNumber: po.documentNumber };
    }
  } catch {
    // Cross-module lookup can legitimately fail (deleted, no permission) — the raw node still renders.
  }
  return { label: node.label, documentNumber: node.documentNumber };
}

/** Walks the real Purchase Order → RFQ → Purchase Requisition chain, and separately finds any Goods
 * Receipt Notes against that PO — all client-side, all real existing Procurement APIs, never a Finance
 * backend call (see this file's own JournalEntryDocumentFlowService import comment). Silently returns an
 * empty upstream/downstream when the PO has no RFQ (a direct PO) or no GRNs yet. */
async function resolveProcurementChain(purchaseOrderId: string): Promise<JournalEntryDocumentFlowNode[]> {
  const extra: JournalEntryDocumentFlowNode[] = [];
  try {
    const po = await getPurchaseOrder(purchaseOrderId);
    if (po.requestForQuotationId) {
      try {
        const rfq = await getRequestForQuotation(po.requestForQuotationId);
        try {
          const pr = await getPurchaseRequisition(rfq.purchaseRequisitionId);
          extra.push({ kind: "PurchaseRequisition", label: "Purchase Requisition", documentNumber: pr.documentNumber, documentId: pr.id, status: pr.status, isCurrent: false });
        } catch { /* PR lookup best-effort */ }
        extra.push({ kind: "RequestForQuotation", label: "Request for Quotation", documentNumber: rfq.documentNumber, documentId: rfq.id, status: rfq.status, isCurrent: false });
      } catch { /* RFQ lookup best-effort */ }
    }
    const grns = await listGoodsReceiptNotes(200, 0);
    const matching = grns.items.find((g) => g.purchaseOrderId === purchaseOrderId);
    if (matching) {
      extra.push({ kind: "GoodsReceiptNote", label: "Goods Receipt", documentNumber: matching.documentNumber, documentId: matching.id, status: matching.status, isCurrent: false });
    }
  } catch {
    // Best-effort — the PO node itself still renders even if this deeper chain can't be resolved.
  }
  return extra;
}

export function JournalEntriesPage({ language }: JournalEntriesPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [entries, setEntries] = useState<JournalEntry[]>([]);
  const [accounts, setAccounts] = useState<GLAccount[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [postingDate, setPostingDate] = useState(todayIsoDate());
  const [description, setDescription] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([emptyLine(), emptyLine()]);

  // List filters
  const [search, setSearch] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [sourceFilter, setSourceFilter] = useState("");
  const [createdByFilter, setCreatedByFilter] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [page, setPage] = useState(1);
  const rowsPerPage = 15;

  // Detail tabs
  const [detailTab, setDetailTab] = useState<DetailTab>("overview");
  const [documentFlow, setDocumentFlow] = useState<JournalEntryDocumentFlowNode[]>([]);
  const [attachments, setAttachments] = useState<AttachmentMetadata[]>([]);
  const [notes, setNotes] = useState<Note[]>([]);
  const [history, setHistory] = useState<AuditHistoryEntry[]>([]);
  const [newNoteText, setNewNoteText] = useState("");

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [entryResult, accountResult] = await Promise.all([listJournalEntries(500, 0), listGLAccounts(200, 0)]);
      setEntries(entryResult.items);
      setAccounts(accountResult.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    if (view.kind === "list") load();
  }, [view.kind, load]);

  useEffect(() => {
    if (view.kind === "create" && accounts.length === 0) {
      listGLAccounts(200, 0).then((r) => setAccounts(r.items)).catch(() => undefined);
    }
  }, [view.kind, accounts.length]);

  const loadDetailData = useCallback(async (entry: JournalEntry) => {
    try {
      const [flowNodes, atts, notesResult, hist] = await Promise.all([
        getJournalEntryDocumentFlow(entry.id),
        listAttachments(NOTE_TARGET_TYPE, entry.id),
        listNotes(NOTE_TARGET_TYPE, entry.id),
        listAuditHistory(NOTE_TARGET_TYPE, entry.id),
      ]);
      setAttachments(atts);
      setNotes(notesResult);
      setHistory(hist);

      // Resolve the one extra cross-module hop (Ipc/RetentionRelease/PurchaseOrder) client-side, then —
      // for a real Purchase Order — the further PO -> RFQ -> PR and PO -> GRN legs of the same chain.
      const resolved = await Promise.all(flowNodes.map(async (node) => {
        const { label, documentNumber } = await resolveNodeLabel(node);
        return { ...node, label, documentNumber };
      }));
      const poNode = flowNodes.find((n) => n.kind === "PurchaseOrder");
      const extra = poNode?.documentId ? await resolveProcurementChain(poNode.documentId) : [];
      const poIndex = resolved.findIndex((n) => n.kind === "PurchaseOrder");
      const withChain = poIndex >= 0 ? [...resolved.slice(0, poIndex), ...extra, ...resolved.slice(poIndex)] : resolved;
      setDocumentFlow(withChain);
    } catch {
      setDocumentFlow([]);
    }
  }, []);

  useEffect(() => {
    if (view.kind === "details") {
      setDetailTab("overview");
      loadDetailData(view.entry);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [view.kind === "details" ? view.entry.id : null]);

  // Computed unconditionally (Rules of Hooks) even though only the list view below renders it — a hook
  // can never live after this component's own early "create"/"details" returns.
  const filteredEntries = useMemo(() => {
    const query = search.trim().toLowerCase();
    return entries
      .filter((e) => !query || e.description.toLowerCase().includes(query) || (e.documentNumber ?? "").toLowerCase().includes(query))
      .filter((e) => !dateFrom || e.postingDate >= dateFrom)
      .filter((e) => !dateTo || e.postingDate <= dateTo)
      .filter((e) => !statusFilter || e.status === statusFilter)
      .filter((e) => !sourceFilter || e.sourceDocumentType === sourceFilter)
      .filter((e) => !createdByFilter || e.createdBy === createdByFilter)
      .sort((a, b) => b.postingDate.localeCompare(a.postingDate));
  }, [entries, search, dateFrom, dateTo, statusFilter, sourceFilter, createdByFilter]);

  const accountLabel = (id: string) => {
    const account = accounts.find((a) => a.id === id);
    return account ? `${account.accountCode} — ${account.accountName}` : id;
  };

  const totalDebits = lines.reduce((sum, l) => sum + (Number(l.debitAmount) || 0), 0);
  const totalCredits = lines.reduce((sum, l) => sum + (Number(l.creditAmount) || 0), 0);
  const isBalanced = lines.some((l) => l.glAccountId) && totalDebits > 0 && totalDebits === totalCredits;

  const updateLine = (index: number, patch: Partial<DraftLine>) => {
    setLines((prev) => prev.map((l, i) => (i === index ? { ...l, ...patch } : l)));
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const lineInputs: CreateJournalLineInput[] = lines
        .filter((l) => l.glAccountId && (Number(l.debitAmount) > 0 || Number(l.creditAmount) > 0))
        .map((l) => ({
          glAccountId: l.glAccountId,
          debitAmount: Number(l.debitAmount) || 0,
          creditAmount: Number(l.creditAmount) || 0,
          lineDescription: l.lineDescription.trim() || undefined,
        }));
      await createJournalEntry({ postingDate, description: description.trim(), lines: lineInputs });
      setPostingDate(todayIsoDate());
      setDescription("");
      setLines([emptyLine(), emptyLine()]);
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (entry: JournalEntry, action: "submit" | "approve" | "reject" | "post" | "reverse") => {
    setBusy(true);
    setError(null);
    try {
      let updated: JournalEntry;
      if (action === "submit") updated = await submitJournalEntry(entry.id);
      else if (action === "approve") updated = await approveJournalEntry(entry.id);
      else if (action === "reject") updated = await rejectJournalEntry(entry.id);
      else if (action === "post") updated = await postJournalEntry(entry.id);
      else updated = await reverseJournalEntry(entry.id);
      setView({ kind: "details", entry: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleBulkAction = async (action: "approve" | "reverse") => {
    setBusy(true);
    setError(null);
    try {
      for (const id of selectedIds) {
        const entry = entries.find((e) => e.id === id);
        if (!entry) continue;
        if (action === "approve" && entry.status === "Submitted") await approveJournalEntry(id);
        if (action === "reverse" && entry.status === "Posted") await reverseJournalEntry(id);
      }
      setSelectedIds(new Set());
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleUploadFile = async (entry: JournalEntry, file: File) => {
    setBusy(true);
    setError(null);
    try {
      await uploadAttachment(NOTE_TARGET_TYPE, entry.id, file);
      const atts = await listAttachments(NOTE_TARGET_TYPE, entry.id);
      setAttachments(atts);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAddNote = async (entry: JournalEntry) => {
    if (!newNoteText.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await addNote(NOTE_TARGET_TYPE, entry.id, newNoteText.trim());
      setNewNoteText("");
      setNotes(await listNotes(NOTE_TARGET_TYPE, entry.id));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  // ---------- CREATE VIEW ----------
  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("je.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !isBalanced },
      { key: "back", label: t("je.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("je.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "36rem", marginBlockEnd: "1rem" }}>
          <label>{t("je.fieldPostingDate", language)}
            <input type="date" style={inputStyle} value={postingDate} onChange={(e) => setPostingDate(e.target.value)} />
          </label>
          <label>{t("je.fieldDescription", language)}
            <input style={inputStyle} value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
        </div>
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("je.fieldGLAccount", language)}</th>
              <th>{t("je.fieldDebit", language)}</th>
              <th>{t("je.fieldCredit", language)}</th>
              <th>{t("je.fieldLineDescription", language)}</th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line, index) => (
              <tr key={index}>
                <td>
                  <select value={line.glAccountId} onChange={(e) => updateLine(index, { glAccountId: e.target.value })}>
                    <option value=""></option>
                    {accounts.map((a) => (
                      <option key={a.id} value={a.id}>{a.accountCode} — {a.accountName}</option>
                    ))}
                  </select>
                </td>
                <td><input type="number" min="0" step="0.01" value={line.debitAmount} onChange={(e) => updateLine(index, { debitAmount: e.target.value, creditAmount: e.target.value ? "0" : line.creditAmount })} style={{ inlineSize: "8rem" }} /></td>
                <td><input type="number" min="0" step="0.01" value={line.creditAmount} onChange={(e) => updateLine(index, { creditAmount: e.target.value, debitAmount: e.target.value ? "0" : line.debitAmount })} style={{ inlineSize: "8rem" }} /></td>
                <td><input value={line.lineDescription} onChange={(e) => updateLine(index, { lineDescription: e.target.value })} /></td>
              </tr>
            ))}
          </tbody>
        </table>
        <button type="button" onClick={() => setLines((prev) => [...prev, emptyLine()])} style={{ marginBlockStart: "0.5rem" }}>
          {t("je.actionAddLine", language)}
        </button>
        <p style={{ marginBlockStart: "1rem" }}>
          <bdi dir="ltr">{totalDebits.toFixed(2)}</bdi> / <bdi dir="ltr">{totalCredits.toFixed(2)}</bdi> —{" "}
          <strong style={{ color: isBalanced ? "var(--pi-success, green)" : "var(--pi-danger)" }}>
            {isBalanced ? t("je.balanced", language) : t("je.unbalanced", language)}
          </strong>
        </p>
      </section>
    );
  }

  // ---------- DETAIL VIEW ----------
  if (view.kind === "details") {
    const entry = view.entry;
    const actions: ActionItem[] = [];
    if (entry.status === "Draft")
      actions.push({ key: "submit", label: t("je.actionSubmit", language), onClick: () => handleAction(entry, "submit"), variant: "primary", isDisabled: busy });
    if (entry.status === "Submitted") {
      actions.push({ key: "approve", label: t("je.actionApprove", language), onClick: () => handleAction(entry, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("je.actionReject", language), onClick: () => handleAction(entry, "reject"), isDisabled: busy });
    }
    if (entry.status === "Approved")
      actions.push({ key: "post", label: t("je.actionPost", language), onClick: () => handleAction(entry, "post"), variant: "primary", isDisabled: busy });
    if (entry.status === "Posted")
      actions.push({ key: "reverse", label: t("je.actionReverse", language), onClick: () => handleAction(entry, "reverse"), isDisabled: busy });
    actions.push({ key: "back", label: t("je.actionBack", language), onClick: () => setView({ kind: "list" }) });

    const tabs: { key: DetailTab; labelKey: "je.tabOverview" | "je.tabLineItems" | "je.tabAttachments" | "je.tabNotes" | "je.tabHistory" | "je.tabRelated"; count?: number }[] = [
      { key: "overview", labelKey: "je.tabOverview" },
      { key: "lineItems", labelKey: "je.tabLineItems", count: entry.lines.length },
      { key: "attachments", labelKey: "je.tabAttachments", count: attachments.length },
      { key: "notes", labelKey: "je.tabNotes", count: notes.length },
      { key: "history", labelKey: "je.tabHistory" },
      { key: "related", labelKey: "je.tabRelated" },
    ];

    const lineItemsTable = (
      <table className="pi-dense-table">
        <thead>
          <tr>
            <th>{t("je.fieldGLAccount", language)}</th>
            <th>{t("je.fieldDebit", language)}</th>
            <th>{t("je.fieldCredit", language)}</th>
            <th>{t("je.fieldLineDescription", language)}</th>
          </tr>
        </thead>
        <tbody>
          {entry.lines.map((line) => (
            <tr key={line.id}>
              <td><bdi dir="ltr">{accountLabel(line.glAccountId)}</bdi></td>
              <td><bdi dir="ltr">{line.debitAmount.toFixed(2)}</bdi></td>
              <td><bdi dir="ltr">{line.creditAmount.toFixed(2)}</bdi></td>
              <td>{line.lineDescription}</td>
            </tr>
          ))}
        </tbody>
      </table>
    );

    return (
      <section className="fin-report-page">
        <h1>{entry.documentNumber} <span className="gl-status-pill gl-status-pill--active">{translateStatus(entry.status, language)}</span></h1>
        <p className="fin-report__subtitle">{entry.description}</p>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

        <dl className="je-info-bar">
          <div><dt>{t("je.columnPostingDate", language)}</dt><dd><bdi dir="ltr">{entry.postingDate}</bdi></dd></div>
          <div><dt>{t("je.fieldSourceDocument", language)}</dt><dd>{translateSourceDocumentType(entry.sourceDocumentType, language)}</dd></div>
          <div><dt>{t("je.columnCreatedBy", language)}</dt><dd>{entry.createdBy}</dd></div>
          <div><dt>{t("je.columnTotalDebits", language)}</dt><dd><bdi dir="ltr">{entry.totalDebits.toFixed(2)}</bdi></dd></div>
          <div><dt>{t("je.columnTotalCredits", language)}</dt><dd><bdi dir="ltr">{entry.totalCredits.toFixed(2)}</bdi></dd></div>
        </dl>

        <div className="pcc-tabs">
          {tabs.map((tab) => (
            <button key={tab.key} type="button" className={`pcc-tab ${detailTab === tab.key ? "pcc-tab--active" : ""}`} onClick={() => setDetailTab(tab.key)}>
              {t(tab.labelKey, language)}{tab.count !== undefined ? ` (${tab.count})` : ""}
            </button>
          ))}
        </div>

        <div className="fin-report__layout">
          <div className="fin-report__main">
            {detailTab === "overview" && (
              <>
                <div className="fin-report__stats" style={{ gridTemplateColumns: "repeat(3, 1fr)" }}>
                  <div className="fin-report__panel">
                    <h2>{t("je.panelEntryInfo", language)}</h2>
                    <dl>
                      <dt>{t("je.columnDocumentNumber", language)}</dt><dd><bdi dir="ltr">{entry.documentNumber}</bdi></dd>
                      <dt>{t("je.columnDescription", language)}</dt><dd>{entry.description}</dd>
                      <dt>{t("je.columnPostingDate", language)}</dt><dd><bdi dir="ltr">{entry.postingDate}</bdi></dd>
                      <dt>{t("je.fieldSourceDocument", language)}</dt><dd>{translateSourceDocumentType(entry.sourceDocumentType, language)}</dd>
                    </dl>
                  </div>
                  <div className="fin-report__panel">
                    <h2>{t("je.panelPostingInfo", language)}</h2>
                    <dl>
                      <dt>{t("je.columnStatus", language)}</dt><dd>{translateStatus(entry.status, language)}</dd>
                      <dt>{t("je.columnCreatedBy", language)}</dt><dd>{entry.createdBy}</dd>
                      <dt>{t("je.postingType", language)}</dt>
                      <dd>{entry.sourceDocumentType && entry.sourceDocumentType !== "Manual" ? t("je.postingTypeAutomatic", language) : t("je.postingTypeManual", language)}</dd>
                    </dl>
                  </div>
                  <div className="fin-report__panel">
                    <h2>{t("je.panelTotals", language)}</h2>
                    <dl>
                      <dt>{t("je.columnTotalDebits", language)}</dt><dd><bdi dir="ltr">{entry.totalDebits.toFixed(2)}</bdi></dd>
                      <dt>{t("je.columnTotalCredits", language)}</dt><dd><bdi dir="ltr">{entry.totalCredits.toFixed(2)}</bdi></dd>
                    </dl>
                    <p className={entry.isBalanced ? "pcc-insight pcc-insight--OnTrack" : "pcc-insight pcc-insight--AttentionRequired"}>
                      {entry.isBalanced ? t("je.balanced", language) : t("je.unbalanced", language)}
                    </p>
                  </div>
                </div>
                <h2>{t("je.tabLineItems", language)}</h2>
                {lineItemsTable}
                <h2 style={{ marginBlockStart: "1rem" }}>{t("je.tabNotes", language)}</h2>
                {notes.length > 0 ? (
                  <p>{notes[0].text} <span className="fin-report__panel-empty">— {notes[0].createdBy}, {new Date(notes[0].createdAt).toLocaleString()}</span></p>
                ) : <p className="fin-report__panel-empty">{t("je.noNotes", language)}</p>}
                <h2>{t("je.tabAttachments", language)}</h2>
                {attachments.length > 0 ? (
                  <p>📄 {attachments[0].fileName} <span className="fin-report__panel-empty">({Math.round(attachments[0].sizeBytes / 1024)} KB)</span></p>
                ) : <p className="fin-report__panel-empty">{t("je.noAttachments", language)}</p>}
              </>
            )}

            {detailTab === "lineItems" && lineItemsTable}

            {detailTab === "attachments" && (
              <div>
                <input type="file" onChange={(e) => e.target.files?.[0] && handleUploadFile(entry, e.target.files[0])} disabled={busy} />
                <table className="pi-dense-table" style={{ marginBlockStart: "0.5rem" }}>
                  <thead><tr><th>{t("je.columnFileName", language)}</th><th>{t("je.columnSize", language)}</th><th>{t("je.columnUploadedBy", language)}</th><th></th></tr></thead>
                  <tbody>
                    {attachments.map((a) => (
                      <tr key={a.id}>
                        <td>{a.fileName}</td>
                        <td><bdi dir="ltr">{Math.round(a.sizeBytes / 1024)} KB</bdi></td>
                        <td>{a.uploadedBy}</td>
                        <td>
                          <button type="button" onClick={() => downloadAttachment(a.id, a.fileName)}>{t("je.actionDownload", language)}</button>{" "}
                          <button type="button" onClick={() => deleteAttachment(a.id).then(() => listAttachments(NOTE_TARGET_TYPE, entry.id)).then(setAttachments)}>{t("je.actionDelete", language)}</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {attachments.length === 0 && <p className="fin-report__panel-empty">{t("je.noAttachments", language)}</p>}
              </div>
            )}

            {detailTab === "notes" && (
              <div>
                <textarea value={newNoteText} onChange={(e) => setNewNoteText(e.target.value)} rows={2} style={{ inlineSize: "100%" }} />
                <button type="button" onClick={() => handleAddNote(entry)} disabled={busy || !newNoteText.trim()}>{t("je.actionAddNote", language)}</button>
                <ul className="fin-report__top-accounts" style={{ marginBlockStart: "0.75rem" }}>
                  {notes.map((n) => (
                    <li key={n.id}>
                      <span className="fin-report__top-account-name">{n.createdBy}</span>
                      <span className="fin-report__top-account-value">{new Date(n.createdAt).toLocaleString()}</span>
                      <div>{n.text}</div>
                      <button type="button" onClick={() => deleteNote(n.id).then(() => listNotes(NOTE_TARGET_TYPE, entry.id)).then(setNotes)}>{t("je.actionDelete", language)}</button>
                    </li>
                  ))}
                </ul>
                {notes.length === 0 && <p className="fin-report__panel-empty">{t("je.noNotes", language)}</p>}
              </div>
            )}

            {detailTab === "history" && (
              <ul className="fin-report__top-accounts">
                {history.map((h, i) => (
                  <li key={i}>
                    <span className="fin-report__top-account-name">{h.actor}</span>
                    <span className="fin-report__top-account-value">{new Date(h.occurredAt).toLocaleString()}</span>
                    <div>{h.summary}</div>
                  </li>
                ))}
                {history.length === 0 && <p className="fin-report__panel-empty">{t("je.noHistory", language)}</p>}
              </ul>
            )}

            {detailTab === "related" && (
              <ul className="fin-report__top-accounts">
                {documentFlow.filter((n) => !n.isCurrent).map((n, i) => (
                  <li key={i}>
                    <span className="fin-report__top-account-name">{n.label}</span>
                    <span className="fin-report__top-account-value"><bdi dir="ltr">{n.documentNumber ?? "—"}</bdi> — {n.status}</span>
                  </li>
                ))}
                {documentFlow.filter((n) => !n.isCurrent).length === 0 && <p className="fin-report__panel-empty">{t("je.noRelated", language)}</p>}
              </ul>
            )}
          </div>

          <aside className="fin-report__rail">
            <div className="fin-report__panel">
              <h2>{t("je.panelDocumentFlow", language)}</h2>
              <ol className="pcc-timeline-steps" style={{ flexDirection: "column", alignItems: "stretch" }}>
                {documentFlow.map((node, i) => (
                  <li key={i} className={node.isCurrent ? "pcc-timeline-step--active" : node.status === "Posted" || node.status === "Approved" || node.status === "Completed" ? "pcc-timeline-step--done" : ""} style={{ flexDirection: "row", justifyContent: "flex-start", gap: "0.5rem", textAlign: "start" }}>
                    <span className="pcc-timeline-dot" style={{ flexShrink: 0 }}>{node.isCurrent ? "●" : ""}</span>
                    <span>
                      <strong>{node.label}</strong>
                      {node.documentNumber && <><br /><bdi dir="ltr">{node.documentNumber}</bdi></>}
                      <br /><span className="fin-report__panel-empty">{node.status}</span>
                    </span>
                  </li>
                ))}
              </ol>
            </div>
            <div className="fin-report__panel">
              <h2>{t("je.panelActivityFeed", language)}</h2>
              {history.slice(0, 3).map((h, i) => (
                <p key={i} style={{ fontSize: "0.85em" }}>
                  <strong>{h.actor}</strong> — {h.summary}<br />
                  <span className="fin-report__panel-empty">{new Date(h.occurredAt).toLocaleString()}</span>
                </p>
              ))}
              {history.length === 0 && <p className="fin-report__panel-empty">{t("je.noHistory", language)}</p>}
            </div>
          </aside>
        </div>
      </section>
    );
  }

  // ---------- LIST VIEW ----------
  const statusCounts = {
    All: entries.length,
    Draft: entries.filter((e) => e.status === "Draft").length,
    Submitted: entries.filter((e) => e.status === "Submitted").length,
    Posted: entries.filter((e) => e.status === "Posted").length,
    Reversed: entries.filter((e) => e.status === "Reversed").length,
  };
  const sources = Array.from(new Set(entries.map((e) => e.sourceDocumentType).filter((s): s is string => !!s)));
  const creators = Array.from(new Set(entries.map((e) => e.createdBy)));
  const totalPages = Math.max(1, Math.ceil(filteredEntries.length / rowsPerPage));
  const pagedEntries = filteredEntries.slice((page - 1) * rowsPerPage, page * rowsPerPage);

  const listActions: ActionItem[] = [
    { key: "new", label: t("je.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];

  return (
    <section className="fin-report-page">
      <h1>{t("je.heading", language)} ({entries.length})</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}

      <div className="pcc-tabs">
        {(["All", "Draft", "Submitted", "Posted", "Reversed"] as const).map((s) => (
          <button key={s} type="button" className={`pcc-tab ${statusFilter === (s === "All" ? "" : s) ? "pcc-tab--active" : ""}`}
            onClick={() => { setStatusFilter(s === "All" ? "" : s); setPage(1); }}>
            {t(s === "All" ? "je.filterAllStatuses" : statusKeys[s], language)} ({statusCounts[s]})
          </button>
        ))}
      </div>

      <div className="fin-report__filters">
        <label>{t("je.filterSearchLabel", language)}
          <input type="search" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder={t("je.filterSearchPlaceholder", language)} />
        </label>
        <label>{t("je.filterDateFrom", language)}<input type="date" value={dateFrom} onChange={(e) => { setDateFrom(e.target.value); setPage(1); }} /></label>
        <label>{t("je.filterDateTo", language)}<input type="date" value={dateTo} onChange={(e) => { setDateTo(e.target.value); setPage(1); }} /></label>
        <label>{t("je.filterSource", language)}
          <select value={sourceFilter} onChange={(e) => { setSourceFilter(e.target.value); setPage(1); }}>
            <option value="">{t("je.filterAllSources", language)}</option>
            {sources.map((s) => <option key={s} value={s}>{translateSourceDocumentType(s, language)}</option>)}
          </select>
        </label>
        <label>{t("je.filterCreatedBy", language)}
          <select value={createdByFilter} onChange={(e) => { setCreatedByFilter(e.target.value); setPage(1); }}>
            <option value="">{t("je.filterAllCreators", language)}</option>
            {creators.map((c) => <option key={c} value={c}>{c}</option>)}
          </select>
        </label>
      </div>

      {selectedIds.size > 0 && (
        <div className="fin-report__filters" style={{ alignItems: "center" }}>
          <span>{selectedIds.size} {t("je.selectedLabel", language)}</span>
          <button type="button" onClick={() => handleBulkAction("approve")} disabled={busy}>{t("je.actionApprove", language)}</button>
          <button type="button" onClick={() => handleBulkAction("reverse")} disabled={busy}>{t("je.actionReverse", language)}</button>
        </div>
      )}

      {!busy && filteredEntries.length === 0 ? (
        <p>{t("je.emptyState", language)}</p>
      ) : (
        <div className="fin-report__layout">
          <div className="fin-report__main">
            <table className="pi-dense-table">
              <thead>
                <tr>
                  <th><input type="checkbox" checked={pagedEntries.length > 0 && pagedEntries.every((e) => selectedIds.has(e.id))}
                    onChange={(e) => {
                      const next = new Set(selectedIds);
                      const checked = e.target.checked;
                      pagedEntries.forEach((entry) => { if (checked) next.add(entry.id); else next.delete(entry.id); });
                      setSelectedIds(next);
                    }} /></th>
                  <th>{t("je.columnDocumentNumber", language)}</th>
                  <th>{t("je.columnPostingDate", language)}</th>
                  <th>{t("je.columnDescription", language)}</th>
                  <th>{t("je.filterSource", language)}</th>
                  <th>{t("je.columnCreatedBy", language)}</th>
                  <th>{t("je.columnStatus", language)}</th>
                  <th>{t("je.columnTotalDebits", language)}</th>
                  <th>{t("je.columnTotalCredits", language)}</th>
                </tr>
              </thead>
              <tbody>
                {pagedEntries.map((entry) => (
                  <tr key={entry.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", entry })}>
                    <td onClick={(e) => e.stopPropagation()}>
                      <input type="checkbox" checked={selectedIds.has(entry.id)} onChange={(e) => {
                        const next = new Set(selectedIds);
                        if (e.target.checked) next.add(entry.id); else next.delete(entry.id);
                        setSelectedIds(next);
                      }} />
                    </td>
                    <td><bdi dir="ltr">{entry.documentNumber}</bdi></td>
                    <td><bdi dir="ltr">{entry.postingDate}</bdi></td>
                    <td>{entry.description}</td>
                    <td>{translateSourceDocumentType(entry.sourceDocumentType, language)}</td>
                    <td>{entry.createdBy}</td>
                    <td>
                      <span className={`gl-status-pill gl-status-pill--${entry.status === "Posted" ? "active" : "inactive"}`}>
                        {translateStatus(entry.status, language)}
                      </span>
                    </td>
                    <td><bdi dir="ltr">{entry.totalDebits.toFixed(2)}</bdi></td>
                    <td><bdi dir="ltr">{entry.totalCredits.toFixed(2)}</bdi></td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="fin-report__pagination">
              <span>{t("gl.paginationShowing", language)
                .replace("{from}", filteredEntries.length === 0 ? "0" : String((page - 1) * rowsPerPage + 1))
                .replace("{to}", String(Math.min(page * rowsPerPage, filteredEntries.length)))
                .replace("{total}", String(filteredEntries.length))}</span>
              <div className="fin-report__pagination-controls">
                <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>‹</button>
                <span>{page} / {totalPages}</span>
                <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>›</button>
              </div>
            </div>
          </div>

          <aside className="fin-report__rail">
            <div className="fin-report__panel">
              <h2>{t("je.panelDocumentFlow", language)}</h2>
              <ol className="pcc-timeline-steps" style={{ flexDirection: "column", alignItems: "stretch" }}>
                {["je.flowSourceDocuments", "je.flowJournalEntry", "je.flowApproval", "je.flowPosting", "je.flowReversal", "je.flowReports"].map((key, i) => (
                  <li key={key} className={i === 1 ? "pcc-timeline-step--active" : "pcc-timeline-step--done"} style={{ flexDirection: "row", justifyContent: "flex-start", gap: "0.5rem", textAlign: "start" }}>
                    <span className="pcc-timeline-dot" style={{ flexShrink: 0 }}>{i === 1 ? "●" : "✓"}</span>
                    <span>{t(key as "je.flowSourceDocuments", language)}</span>
                  </li>
                ))}
              </ol>
            </div>
            <div className="fin-report__panel">
              <h2>{t("je.panelFiltersSummary", language)}</h2>
              <ul className="fin-report__top-accounts">
                {dateFrom && <li>{t("je.filterDateFrom", language)}: <bdi dir="ltr">{dateFrom}</bdi></li>}
                {dateTo && <li>{t("je.filterDateTo", language)}: <bdi dir="ltr">{dateTo}</bdi></li>}
                {statusFilter && <li>{t("je.columnStatus", language)}: {translateStatus(statusFilter, language)}</li>}
                {sourceFilter && <li>{t("je.filterSource", language)}: {translateSourceDocumentType(sourceFilter, language)}</li>}
                {createdByFilter && <li>{t("je.filterCreatedBy", language)}: {createdByFilter}</li>}
                {!dateFrom && !dateTo && !statusFilter && !sourceFilter && !createdByFilter && <li className="fin-report__panel-empty">{t("je.filterAllStatuses", language)}</li>}
              </ul>
            </div>
            <div className="fin-report__panel">
              <h2>{t("je.panelQuickReports", language)}</h2>
              <ul className="fin-report__top-accounts">
                <li><a href="#trial-balance">{t("nav.trialBalance", language)}</a></li>
                <li><a href="#income-statement">{t("nav.incomeStatement", language)}</a></li>
                <li><a href="#balance-sheet">{t("nav.balanceSheet", language)}</a></li>
              </ul>
            </div>
            <div className="fin-report__panel">
              <h2>{t("je.panelHelpfulTips", language)}</h2>
              <p className="fin-report__panel-empty">{t("je.helpfulTipsText", language)}</p>
            </div>
          </aside>
        </div>
      )}
    </section>
  );
}
