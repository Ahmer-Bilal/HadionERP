import { useCallback, useEffect, useState } from "react";
import { ActionPane } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  activateLookupValue,
  createLookupType,
  createLookupValue,
  deactivateLookupValue,
  deleteLookupType,
  deleteLookupValue,
  listLookupTypes,
  listLookupValues,
  updateLookupValue,
} from "../api/lookupApi";
import type { LookupType, LookupValue } from "../api/lookupApi";

interface LookupDataPageProps {
  language: SupportedLanguageCode;
  /** When set, this page opens directly into that lookup type's editable grid instead of the hub — used
   * by the dedicated "Countries"/"Business Role Types"/etc. nav items so each has its own one-click entry
   * point, the same way GLAccounts/Items/CostCenters/TaxCodes are separate nav items despite sharing a
   * similar list-page shape. "Back to Lookup Types" still returns to the hub from there. */
  initialTypeCode?: string;
}

type ViewState = { kind: "hub" } | { kind: "values"; typeCode: string };

type EditableRow = LookupValue & { isNew?: boolean };

export function LookupDataPage({ language, initialTypeCode }: LookupDataPageProps) {
  const [view, setView] = useState<ViewState>(initialTypeCode ? { kind: "values", typeCode: initialTypeCode } : { kind: "hub" });
  const [types, setTypes] = useState<LookupType[]>([]);
  const [values, setValues] = useState<LookupValue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [newTypeCode, setNewTypeCode] = useState("");
  const [newTypeName, setNewTypeName] = useState("");
  const [newTypeNameArabic, setNewTypeNameArabic] = useState("");

  const [draft, setDraft] = useState<{ code: string; name: string; nameArabic: string } | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<{ name: string; nameArabic: string; sortOrder: number } | null>(null);

  useEffect(() => {
    setView(initialTypeCode ? { kind: "values", typeCode: initialTypeCode } : { kind: "hub" });
  }, [initialTypeCode]);

  const loadTypes = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      setTypes(await listLookupTypes());
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  const loadValues = useCallback(async (typeCode: string) => {
    setBusy(true);
    setError(null);
    try {
      setValues(await listLookupValues(typeCode, true));
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    // `types` (needed for the heading's localized name) is loaded regardless of entry point — a direct
    // nav link straight into one type's grid (e.g. "Countries") never visits the hub view, so it must not
    // rely on the hub having already populated this list.
    loadTypes();
    if (view.kind === "values") loadValues(view.typeCode);
  }, [view, loadTypes, loadValues]);

  const handleCreateType = async () => {
    setBusy(true);
    setError(null);
    try {
      await createLookupType({
        code: newTypeCode.trim(),
        name: newTypeName.trim(),
        nameArabic: newTypeNameArabic.trim() || undefined,
      });
      setNewTypeCode("");
      setNewTypeName("");
      setNewTypeNameArabic("");
      await loadTypes();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteType = async (typeCode: string) => {
    setBusy(true);
    setError(null);
    try {
      await deleteLookupType(typeCode);
      await loadTypes();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAddValue = async (typeCode: string) => {
    if (!draft || !draft.code.trim() || !draft.name.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await createLookupValue(typeCode, {
        code: draft.code.trim(),
        name: draft.name.trim(),
        nameArabic: draft.nameArabic.trim() || undefined,
        sortOrder: values.length,
      });
      setDraft(null);
      await loadValues(typeCode);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const startEdit = (row: EditableRow) => {
    setEditingId(row.id);
    setEditForm({ name: row.name, nameArabic: row.nameArabic ?? "", sortOrder: row.sortOrder });
  };

  const handleSaveEdit = async (typeCode: string, id: string) => {
    if (!editForm) return;
    setBusy(true);
    setError(null);
    try {
      await updateLookupValue(typeCode, id, {
        name: editForm.name.trim(),
        nameArabic: editForm.nameArabic.trim() || undefined,
        sortOrder: editForm.sortOrder,
      });
      setEditingId(null);
      setEditForm(null);
      await loadValues(typeCode);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleToggleActive = async (typeCode: string, row: LookupValue) => {
    setBusy(true);
    setError(null);
    try {
      if (row.isActive) await deactivateLookupValue(typeCode, row.id);
      else await activateLookupValue(typeCode, row.id);
      await loadValues(typeCode);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteValue = async (typeCode: string, id: string) => {
    setBusy(true);
    setError(null);
    try {
      await deleteLookupValue(typeCode, id);
      await loadValues(typeCode);
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "values") {
    const typeCode = view.typeCode;
    const type = types.find((t2) => t2.code === typeCode);
    const backAction: ActionItem = { key: "back", label: t("lookup.actionBackToHub", language), onClick: () => setView({ kind: "hub" }) };

    return (
      <section>
        <h1>{type ? (language === "ar" && type.nameArabic ? type.nameArabic : type.name) : typeCode}</h1>
        <ActionPane actions={[backAction]} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("lookup.columnCode", language)}</th>
              <th>{t("lookup.columnName", language)}</th>
              <th>{t("lookup.columnNameArabic", language)}</th>
              <th>{t("lookup.columnSortOrder", language)}</th>
              <th>{t("lookup.columnStatus", language)}</th>
              <th>{t("lookup.columnActions", language)}</th>
            </tr>
          </thead>
          <tbody>
            {values.map((row) => (
              <tr key={row.id}>
                <td><bdi dir="ltr">{row.code}</bdi></td>
                {editingId === row.id && editForm ? (
                  <>
                    <td><input style={inputStyle} value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} /></td>
                    <td><input dir="rtl" style={inputStyle} value={editForm.nameArabic} onChange={(e) => setEditForm({ ...editForm, nameArabic: e.target.value })} /></td>
                    <td><input type="number" style={inputStyle} value={editForm.sortOrder} onChange={(e) => setEditForm({ ...editForm, sortOrder: Number(e.target.value) })} /></td>
                    <td>{row.isActive ? t("lookup.statusActive", language) : t("lookup.statusInactive", language)}</td>
                    <td>
                      <button onClick={() => handleSaveEdit(typeCode, row.id)} disabled={busy}>{t("lookup.actionSave", language)}</button>{" "}
                      <button onClick={() => { setEditingId(null); setEditForm(null); }}>{t("lookup.actionCancel", language)}</button>
                    </td>
                  </>
                ) : (
                  <>
                    <td>{row.name}</td>
                    <td dir="rtl">{row.nameArabic ?? ""}</td>
                    <td><bdi dir="ltr">{row.sortOrder}</bdi></td>
                    <td>{row.isActive ? t("lookup.statusActive", language) : t("lookup.statusInactive", language)}</td>
                    <td>
                      <button onClick={() => startEdit(row)} disabled={busy}>{t("lookup.actionEdit", language)}</button>{" "}
                      <button onClick={() => handleToggleActive(typeCode, row)} disabled={busy}>
                        {row.isActive ? t("lookup.actionDeactivate", language) : t("lookup.actionActivate", language)}
                      </button>{" "}
                      <button onClick={() => handleDeleteValue(typeCode, row.id)} disabled={busy}>{t("lookup.actionDelete", language)}</button>
                    </td>
                  </>
                )}
              </tr>
            ))}
            <tr>
              <td><input style={inputStyle} placeholder={t("lookup.newCodePlaceholder", language)} value={draft?.code ?? ""} onChange={(e) => setDraft({ code: e.target.value, name: draft?.name ?? "", nameArabic: draft?.nameArabic ?? "" })} /></td>
              <td><input style={inputStyle} placeholder={t("lookup.newNamePlaceholder", language)} value={draft?.name ?? ""} onChange={(e) => setDraft({ code: draft?.code ?? "", name: e.target.value, nameArabic: draft?.nameArabic ?? "" })} /></td>
              <td><input dir="rtl" style={inputStyle} placeholder={t("lookup.newNameArabicPlaceholder", language)} value={draft?.nameArabic ?? ""} onChange={(e) => setDraft({ code: draft?.code ?? "", name: draft?.name ?? "", nameArabic: e.target.value })} /></td>
              <td></td>
              <td></td>
              <td><button onClick={() => handleAddValue(typeCode)} disabled={busy || !draft?.code.trim() || !draft?.name.trim()}>{t("lookup.actionAddValue", language)}</button></td>
            </tr>
          </tbody>
        </table>
      </section>
    );
  }

  // hub view — every lookup type as its own heading, per-type value counts, plus a form to create a
  // brand-new custom lookup type (the "add and change... like SAP/Dynamics" capability generalized beyond
  // the five types this platform's own code already consumes).
  return (
    <section>
      <h1>{t("lookup.hubHeading", language)}</h1>
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <table className="pi-dense-table">
        <thead>
          <tr>
            <th>{t("lookup.columnCode", language)}</th>
            <th>{t("lookup.columnName", language)}</th>
            <th>{t("lookup.columnNameArabic", language)}</th>
            <th>{t("lookup.columnValueCount", language)}</th>
            <th>{t("lookup.columnKind", language)}</th>
            <th>{t("lookup.columnActions", language)}</th>
          </tr>
        </thead>
        <tbody>
          {types.map((type) => (
            <tr key={type.id}>
              <td>
                <a className="pi-link" href="#" onClick={(e) => { e.preventDefault(); setView({ kind: "values", typeCode: type.code }); }}>
                  <bdi dir="ltr">{type.code}</bdi>
                </a>
              </td>
              <td>{type.name}</td>
              <td dir="rtl">{type.nameArabic ?? ""}</td>
              <td><bdi dir="ltr">{type.valueCount}</bdi></td>
              <td>{type.isSystemDefined ? t("lookup.kindSystem", language) : t("lookup.kindCustom", language)}</td>
              <td>
                {!type.isSystemDefined && type.valueCount === 0 && (
                  <button onClick={() => handleDeleteType(type.code)} disabled={busy}>{t("lookup.actionDelete", language)}</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <h2>{t("lookup.newTypeHeading", language)}</h2>
      <div style={{ maxInlineSize: "32rem" }}>
        <label>{t("lookup.fieldTypeCode", language)}
          <input style={inputStyle} value={newTypeCode} onChange={(e) => setNewTypeCode(e.target.value)} />
        </label>
        <label>{t("lookup.fieldTypeName", language)}
          <input style={inputStyle} value={newTypeName} onChange={(e) => setNewTypeName(e.target.value)} />
        </label>
        <label>{t("lookup.fieldTypeNameArabic", language)}
          <input dir="rtl" style={inputStyle} value={newTypeNameArabic} onChange={(e) => setNewTypeNameArabic(e.target.value)} />
        </label>
        <button onClick={handleCreateType} disabled={busy || !newTypeCode.trim() || !newTypeName.trim()}>
          {t("lookup.actionCreateType", language)}
        </button>
      </div>
    </section>
  );
}
