import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveVendorPrequalification,
  createVendorPrequalification,
  deleteVendorPrequalificationAttachment,
  listVendorPrequalificationAttachments,
  listVendorPrequalifications,
  rejectVendorPrequalification,
  submitVendorPrequalification,
  uploadVendorPrequalificationAttachment,
  vendorPrequalificationAttachmentDownloadUrl,
} from "../api/vendorPrequalificationApi";
import type { Attachment, CreateVendorPrequalificationInput, VendorPrequalification } from "../api/vendorPrequalificationApi";
import { listBusinessPartners } from "../api/businessPartnerApi";
import type { BusinessPartner } from "../api/businessPartnerApi";

interface VendorPrequalificationsPageProps {
  language: SupportedLanguageCode;
}

type ViewState =
  | { kind: "list" }
  | { kind: "create" }
  | { kind: "details"; prequalification: VendorPrequalification };

// Excludes GovernmentAuthority — Modules.Procurement.Application.VendorPrequalificationService rejects it
// outright ("not prequalified at all", per docs/architecture/06-roadmap.md).
const QUALIFIABLE_ROLE_TYPES = [
  "Client", "Supplier", "Subcontractor", "Consultant", "JointVenturePartner",
  "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
] as const;

const TRADE_ELIGIBLE_ROLES = new Set(["Supplier", "Subcontractor", "Consultant"]);

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

function translateRoleType(roleType: string, language: SupportedLanguageCode): string {
  switch (roleType) {
    case "Client": return t("bp.roleClient", language);
    case "Supplier": return t("bp.roleSupplier", language);
    case "Subcontractor": return t("bp.roleSubcontractor", language);
    case "Consultant": return t("bp.roleConsultant", language);
    case "JointVenturePartner": return t("bp.roleJointVenturePartner", language);
    case "GovernmentAuthority": return t("bp.roleGovernmentAuthority", language);
    case "RentalCompany": return t("bp.roleRentalCompany", language);
    case "Manufacturer": return t("bp.roleManufacturer", language);
    case "ManpowerSupplier": return t("bp.roleManpowerSupplier", language);
    case "TestingLaboratory": return t("bp.roleTestingLaboratory", language);
    default: return roleType;
  }
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function VendorPrequalificationsPage({ language }: VendorPrequalificationsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [prequalifications, setPrequalifications] = useState<VendorPrequalification[]>([]);
  const [vendors, setVendors] = useState<BusinessPartner[]>([]);
  const [attachments, setAttachments] = useState<Attachment[]>([]);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState({ businessPartnerId: "", roleType: "Supplier", trade: "" });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [prequalResult, vendorResult] = await Promise.all([
        listVendorPrequalifications(200, 0),
        listBusinessPartners(200, 0),
      ]);
      setPrequalifications(prequalResult.items);
      setVendors(vendorResult.items.filter((v) => v.status === "Approved"));
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
    if (view.kind === "details") {
      listVendorPrequalificationAttachments(view.prequalification.id)
        .then(setAttachments)
        .catch((err) => setError(err instanceof Error ? err.message : String(err)));
    } else {
      setAttachments([]);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [view.kind === "details" ? view.prequalification.id : null]);

  const vendorLabel = (id: string) => vendors.find((v) => v.id === id)?.name ?? id;

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const input: CreateVendorPrequalificationInput = {
        businessPartnerId: form.businessPartnerId,
        roleType: form.roleType,
        trade: TRADE_ELIGIBLE_ROLES.has(form.roleType) ? form.trade.trim() || undefined : undefined,
      };
      const created = await createVendorPrequalification(input);
      setForm({ businessPartnerId: "", roleType: "Supplier", trade: "" });
      setView({ kind: "details", prequalification: created });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (
    prequalification: VendorPrequalification, action: "submit" | "approve" | "reject",
  ) => {
    setBusy(true);
    setError(null);
    try {
      let updated: VendorPrequalification;
      if (action === "submit") updated = await submitVendorPrequalification(prequalification.id);
      else if (action === "approve") updated = await approveVendorPrequalification(prequalification.id);
      else updated = await rejectVendorPrequalification(prequalification.id);
      setView({ kind: "details", prequalification: updated });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleUploadAttachment = async (prequalification: VendorPrequalification) => {
    if (!pendingFile) return;
    setBusy(true);
    setError(null);
    try {
      await uploadVendorPrequalificationAttachment(prequalification.id, pendingFile);
      setPendingFile(null);
      setAttachments(await listVendorPrequalificationAttachments(prequalification.id));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteAttachment = async (prequalification: VendorPrequalification, attachmentId: string) => {
    setBusy(true);
    setError(null);
    try {
      await deleteVendorPrequalificationAttachment(prequalification.id, attachmentId);
      setAttachments(await listVendorPrequalificationAttachments(prequalification.id));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("vpq.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy || !form.businessPartnerId },
      { key: "back", label: t("vpq.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("vpq.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("vpq.fieldVendor", language)}
            <select style={inputStyle} value={form.businessPartnerId} onChange={(e) => setForm({ ...form, businessPartnerId: e.target.value })}>
              <option value=""></option>
              {vendors.map((v) => <option key={v.id} value={v.id}>{v.name}</option>)}
            </select>
          </label>
          <label>{t("vpq.fieldRoleType", language)}
            <select style={inputStyle} value={form.roleType} onChange={(e) => setForm({ ...form, roleType: e.target.value })}>
              {QUALIFIABLE_ROLE_TYPES.map((role) => (
                <option key={role} value={role}>{translateRoleType(role, language)}</option>
              ))}
            </select>
          </label>
          {TRADE_ELIGIBLE_ROLES.has(form.roleType) && (
            <label>{t("vpq.fieldTrade", language)}
              <input style={inputStyle} value={form.trade} onChange={(e) => setForm({ ...form, trade: e.target.value })} />
            </label>
          )}
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const prequalification = view.prequalification;
    const actions: ActionItem[] = [];
    if (prequalification.status === "Draft")
      actions.push({ key: "submit", label: t("vpq.actionSubmit", language), onClick: () => handleAction(prequalification, "submit"), variant: "primary", isDisabled: busy });
    if (prequalification.status === "Submitted") {
      actions.push({ key: "approve", label: t("vpq.actionApprove", language), onClick: () => handleAction(prequalification, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("vpq.actionReject", language), onClick: () => handleAction(prequalification, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("vpq.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{prequalification.documentNumber} — {vendorLabel(prequalification.businessPartnerId)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl style={{ maxInlineSize: "32rem" }}>
                <dt>{t("vpq.columnVendor", language)}</dt>
                <dd>{vendorLabel(prequalification.businessPartnerId)}</dd>
                <dt>{t("vpq.columnRoleType", language)}</dt>
                <dd>{translateRoleType(prequalification.roleType, language)}</dd>
                <dt>{t("vpq.columnTrade", language)}</dt>
                <dd>{prequalification.trade ?? t("vpq.noTrade", language)}</dd>
                <dt>{t("vpq.columnValidFrom", language)}</dt>
                <dd><bdi dir="ltr">{prequalification.validFrom ?? t("vpq.noTrade", language)}</bdi></dd>
                <dt>{t("vpq.columnValidUntil", language)}</dt>
                <dd><bdi dir="ltr">{prequalification.validUntil ?? t("vpq.noTrade", language)}</bdi></dd>
                <dt>{t("vpq.columnStatus", language)}</dt>
                <dd>{translateStatus(prequalification.status, language)}</dd>
              </dl>
            ),
          },
          {
            key: "attachments",
            title: t("bp.tabAttachments", language),
            content: (
              <div className="bp-form">
                {attachments.length === 0 && <p>{t("bp.emptyAttachments", language)}</p>}
                {attachments.length > 0 && (
                  <table className="bp-table">
                    <thead>
                      <tr>
                        <th>{t("bp.columnFileName", language)}</th>
                        <th>{t("bp.columnFileSize", language)}</th>
                        <th>{t("bp.columnUploadedBy", language)}</th>
                        <th>{t("bp.columnUploadedAt", language)}</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {attachments.map((attachment) => (
                        <tr key={attachment.id}>
                          <td>{attachment.fileName}</td>
                          <td><bdi dir="ltr">{formatFileSize(attachment.sizeBytes)}</bdi></td>
                          <td>{attachment.uploadedBy}</td>
                          <td><bdi dir="ltr">{new Date(attachment.uploadedAt).toLocaleString(language)}</bdi></td>
                          <td>
                            <a href={vendorPrequalificationAttachmentDownloadUrl(prequalification.id, attachment.id)}>
                              {t("bp.actionDownload", language)}
                            </a>
                            {" · "}
                            <button type="button" onClick={() => handleDeleteAttachment(prequalification, attachment.id)} disabled={busy}>
                              {t("bp.actionDelete", language)}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                <label>
                  {t("bp.actionUpload", language)}
                  <input type="file" onChange={(e) => setPendingFile(e.target.files?.[0] ?? null)} />
                </label>
                <ActionPane
                  actions={[{
                    key: "upload-attachment",
                    label: t("bp.actionUpload", language),
                    onClick: () => handleUploadAttachment(prequalification),
                    variant: "primary",
                    isDisabled: busy || !pendingFile,
                  }]}
                  ariaLabel={t("aria.actionToolbar", language)}
                />
              </div>
            ),
          },
        ]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("vpq.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("vpq.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {prequalifications.length === 0 ? (
        <p>{t("vpq.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("vpq.columnDocumentNumber", language)}</th>
              <th>{t("vpq.columnVendor", language)}</th>
              <th>{t("vpq.columnRoleType", language)}</th>
              <th>{t("vpq.columnTrade", language)}</th>
              <th>{t("vpq.columnValidUntil", language)}</th>
              <th>{t("vpq.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {prequalifications.map((prequalification) => (
              <tr key={prequalification.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", prequalification })}>
                <td><bdi dir="ltr">{prequalification.documentNumber}</bdi></td>
                <td>{vendorLabel(prequalification.businessPartnerId)}</td>
                <td>{translateRoleType(prequalification.roleType, language)}</td>
                <td>{prequalification.trade ?? t("vpq.noTrade", language)}</td>
                <td><bdi dir="ltr">{prequalification.validUntil ?? t("vpq.noTrade", language)}</bdi></td>
                <td>{translateStatus(prequalification.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
