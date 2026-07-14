import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveJournalEntry,
  createJournalEntry,
  listJournalEntries,
  postJournalEntry,
  rejectJournalEntry,
  reverseJournalEntry,
  submitJournalEntry,
} from "../api/journalEntryApi";
import type { CreateJournalLineInput, JournalEntry } from "../api/journalEntryApi";
import { listGLAccounts } from "../api/glAccountApi";
import type { GLAccount } from "../api/glAccountApi";

interface JournalEntriesPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; entry: JournalEntry };

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

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function emptyLine(): DraftLine {
  return { glAccountId: "", debitAmount: "", creditAmount: "", lineDescription: "" };
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

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [entryResult, accountResult] = await Promise.all([listJournalEntries(200, 0), listGLAccounts(200, 0)]);
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

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

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

    return (
      <section>
        <h1>{entry.documentNumber} — {entry.description}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <>
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("je.columnPostingDate", language)}</dt>
                <dd><bdi dir="ltr">{entry.postingDate}</bdi></dd>
                <dt>{t("je.columnStatus", language)}</dt>
                <dd>{translateStatus(entry.status, language)}</dd>
                {entry.reversalOfEntryId && (
                  <>
                    <dt>{t("je.reversalOf", language)}</dt>
                    <dd><bdi dir="ltr">{entry.reversalOfEntryId}</bdi></dd>
                  </>
                )}
              </dl>
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
              <p>
                <bdi dir="ltr">{entry.totalDebits.toFixed(2)}</bdi> / <bdi dir="ltr">{entry.totalCredits.toFixed(2)}</bdi> —{" "}
                {entry.isBalanced ? t("je.balanced", language) : t("je.unbalanced", language)}
              </p>
            </>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("je.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("je.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {entries.length === 0 ? (
        <p>{t("je.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("je.columnDocumentNumber", language)}</th>
              <th>{t("je.columnPostingDate", language)}</th>
              <th>{t("je.columnDescription", language)}</th>
              <th>{t("je.columnTotalDebits", language)}</th>
              <th>{t("je.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", entry })}>
                <td><bdi dir="ltr">{entry.documentNumber}</bdi></td>
                <td><bdi dir="ltr">{entry.postingDate}</bdi></td>
                <td>{entry.description}</td>
                <td><bdi dir="ltr">{entry.totalDebits.toFixed(2)}</bdi></td>
                <td>{translateStatus(entry.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
