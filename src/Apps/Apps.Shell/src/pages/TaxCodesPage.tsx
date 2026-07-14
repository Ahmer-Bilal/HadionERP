import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveTaxCode,
  createTaxCode,
  listTaxCodes,
  rejectTaxCode,
  submitTaxCode,
} from "../api/taxCodeApi";
import type { CreateTaxCodeInput, TaxCode } from "../api/taxCodeApi";

interface TaxCodesPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; taxCode: TaxCode };

const taxTypeKeys: Record<string, "tax.taxTypeStandard" | "tax.taxTypeZeroRated" | "tax.taxTypeExempt"> = {
  Standard: "tax.taxTypeStandard",
  ZeroRated: "tax.taxTypeZeroRated",
  Exempt: "tax.taxTypeExempt",
};

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
};

function translateTaxType(taxType: string, language: SupportedLanguageCode): string {
  const key = taxTypeKeys[taxType];
  return key ? t(key, language) : taxType;
}

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

export function TaxCodesPage({ language }: TaxCodesPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [taxCodes, setTaxCodes] = useState<TaxCode[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateTaxCodeInput>({
    taxCodeCode: "",
    taxCodeName: "",
    rate: 15,
    taxType: "Standard",
    taxCodeNameArabic: "",
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const result = await listTaxCodes(200, 0);
      setTaxCodes(result.items);
    } catch {
      setError(t("status.error", language));
    } finally {
      setBusy(false);
    }
  }, [language]);

  useEffect(() => {
    if (view.kind === "list") load();
  }, [view.kind, load]);

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const input: CreateTaxCodeInput = {
        taxCodeCode: form.taxCodeCode.trim(),
        taxCodeName: form.taxCodeName.trim(),
        rate: form.rate,
        taxType: form.taxType,
        taxCodeNameArabic: form.taxCodeNameArabic?.trim() || undefined,
      };
      await createTaxCode(input);
      setForm({ taxCodeCode: "", taxCodeName: "", rate: 15, taxType: "Standard", taxCodeNameArabic: "" });
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleStatusAction = async (taxCode: TaxCode, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: TaxCode;
      if (action === "submit") updated = await submitTaxCode(taxCode.id);
      else if (action === "approve") updated = await approveTaxCode(taxCode.id);
      else updated = await rejectTaxCode(taxCode.id);
      setView({ kind: "details", taxCode: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("tax.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("tax.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("tax.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("tax.fieldTaxCodeCode", language)}
            <input style={inputStyle} value={form.taxCodeCode} onChange={(e) => setForm({ ...form, taxCodeCode: e.target.value })} />
          </label>
          <label>{t("tax.fieldTaxCodeName", language)}
            <input style={inputStyle} value={form.taxCodeName} onChange={(e) => setForm({ ...form, taxCodeName: e.target.value })} />
          </label>
          <label>{t("tax.fieldTaxCodeNameArabic", language)}
            <input dir="rtl" style={inputStyle} value={form.taxCodeNameArabic ?? ""} onChange={(e) => setForm({ ...form, taxCodeNameArabic: e.target.value })} />
          </label>
          <label>{t("tax.fieldRate", language)}
            <input type="number" min="0" max="100" step="0.01" style={inputStyle} value={form.rate} onChange={(e) => setForm({ ...form, rate: Number(e.target.value) })} />
          </label>
          <label>{t("tax.fieldTaxType", language)}
            <select style={inputStyle} value={form.taxType} onChange={(e) => setForm({ ...form, taxType: e.target.value })}>
              <option value="Standard">{t("tax.taxTypeStandard", language)}</option>
              <option value="ZeroRated">{t("tax.taxTypeZeroRated", language)}</option>
              <option value="Exempt">{t("tax.taxTypeExempt", language)}</option>
            </select>
          </label>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const taxCode = view.taxCode;
    const actions: ActionItem[] = [];
    if (taxCode.status === "Draft")
      actions.push({ key: "submit", label: t("tax.actionSubmit", language), onClick: () => handleStatusAction(taxCode, "submit"), variant: "primary", isDisabled: busy });
    if (taxCode.status === "Submitted") {
      actions.push({ key: "approve", label: t("tax.actionApprove", language), onClick: () => handleStatusAction(taxCode, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("tax.actionReject", language), onClick: () => handleStatusAction(taxCode, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("tax.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{taxCode.taxCodeCode} — {taxCode.taxCodeName}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("tax.columnCode", language)}</dt>
              <dd>{taxCode.taxCodeCode}</dd>
              <dt>{t("tax.columnName", language)}</dt>
              <dd>{taxCode.taxCodeName}</dd>
              {taxCode.taxCodeNameArabic && (
                <>
                  <dt>{t("tax.fieldTaxCodeNameArabic", language)}</dt>
                  <dd dir="rtl">{taxCode.taxCodeNameArabic}</dd>
                </>
              )}
              <dt>{t("tax.columnRate", language)}</dt>
              <dd><bdi dir="ltr">{taxCode.rate}%</bdi></dd>
              <dt>{t("tax.columnType", language)}</dt>
              <dd>{translateTaxType(taxCode.taxType, language)}</dd>
              <dt>{t("tax.columnStatus", language)}</dt>
              <dd>{translateStatus(taxCode.status, language)}</dd>
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("tax.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("tax.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {taxCodes.length === 0 ? (
        <p>{t("tax.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("tax.columnCode", language)}</th>
              <th>{t("tax.columnName", language)}</th>
              <th>{t("tax.columnRate", language)}</th>
              <th>{t("tax.columnType", language)}</th>
              <th>{t("tax.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {taxCodes.map((taxCode) => (
              <tr key={taxCode.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", taxCode })}>
                <td><bdi dir="ltr">{taxCode.taxCodeCode}</bdi></td>
                <td>{taxCode.taxCodeName}</td>
                <td><bdi dir="ltr">{taxCode.rate}%</bdi></td>
                <td>{translateTaxType(taxCode.taxType, language)}</td>
                <td>{translateStatus(taxCode.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
