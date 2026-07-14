import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  addBusinessPartnerAddress,
  addBusinessPartnerContact,
  addBusinessPartnerNote,
  approveBusinessPartner,
  businessPartnerAttachmentDownloadUrl,
  createBusinessPartner,
  deleteBusinessPartnerAttachment,
  deleteBusinessPartnerNote,
  listBusinessPartnerAttachments,
  listBusinessPartnerNotes,
  listBusinessPartners,
  rejectBusinessPartner,
  submitBusinessPartner,
  uploadBusinessPartnerAttachment,
} from "../api/businessPartnerApi";
import type {
  AddBusinessPartnerAddressInput,
  AddBusinessPartnerContactInput,
  Attachment,
  BusinessPartner,
  CreateBusinessPartnerInput,
  Note,
} from "../api/businessPartnerApi";

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface BusinessPartnersPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; partner: BusinessPartner };

const emptyForm: CreateBusinessPartnerInput = {
  name: "",
  partnerType: "Vendor",
  taxRegistrationNumber: "",
  nameArabic: "",
};

const emptyAddressForm: AddBusinessPartnerAddressInput = {
  addressType: "HeadOffice",
  country: "",
  city: "",
  addressLine: "",
};

const emptyContactForm: AddBusinessPartnerContactInput = {
  name: "",
  jobTitle: "",
  email: "",
  phone: "",
};

// The backend returns raw enum names (English) — real data values, not UI copy, so they're never
// hardcoded strings in this file. But displaying them AS-IS in Arabic would leave the one part of the
// screen still reading in English; these translate them for display only. Logic (e.g. `status === "Draft"`)
// always compares against the untranslated backend value, never the display label.
function translatePartnerType(partnerType: string, language: SupportedLanguageCode): string {
  switch (partnerType) {
    case "Customer":
      return t("bp.partnerTypeCustomer", language);
    case "Vendor":
      return t("bp.partnerTypeVendor", language);
    case "Both":
      return t("bp.partnerTypeBoth", language);
    default:
      return partnerType;
  }
}

function translateStatus(status: string, language: SupportedLanguageCode): string {
  switch (status) {
    case "Draft":
      return t("bp.statusDraft", language);
    case "Submitted":
      return t("bp.statusSubmitted", language);
    case "InApproval":
      return t("bp.statusInApproval", language);
    case "Approved":
      return t("bp.statusApproved", language);
    case "Rejected":
      return t("bp.statusRejected", language);
    case "Cancelled":
      return t("bp.statusCancelled", language);
    case "Reversed":
      return t("bp.statusReversed", language);
    default:
      return status;
  }
}

function translateAddressType(addressType: string, language: SupportedLanguageCode): string {
  switch (addressType) {
    case "HeadOffice":
      return t("bp.addressTypeHeadOffice", language);
    case "Billing":
      return t("bp.addressTypeBilling", language);
    case "Shipping":
      return t("bp.addressTypeShipping", language);
    case "SiteOffice":
      return t("bp.addressTypeSiteOffice", language);
    default:
      return addressType;
  }
}

/// The first real business screen in the application (Modules.MasterData's Business Partner) — a
/// List + create/details view. Not yet using a shared "List+Details form template" from Platform.UI:
/// that template is deferred until a SECOND business object needs the same shape (see
/// Platform.UI/README.md), so the common pattern is extracted from real usage, not guessed at.
export function BusinessPartnersPage({ language }: BusinessPartnersPageProps) {
  const [partners, setPartners] = useState<BusinessPartner[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [form, setForm] = useState<CreateBusinessPartnerInput>(emptyForm);
  const [addressForm, setAddressForm] = useState<AddBusinessPartnerAddressInput>(emptyAddressForm);
  const [contactForm, setContactForm] = useState<AddBusinessPartnerContactInput>(emptyContactForm);
  const [attachments, setAttachments] = useState<Attachment[]>([]);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [notes, setNotes] = useState<Note[]>([]);
  const [noteText, setNoteText] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    listBusinessPartners()
      .then((result) => setPartners(result.items))
      .catch((err) => setError(err instanceof Error ? err.message : String(err)))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  // Attachments/Notes aren't embedded in BusinessPartnerDto (metadata-only lists, fetched separately) —
  // reload whenever the details view's partner changes (opening a partner, or after add/delete above).
  useEffect(() => {
    if (view.kind === "details") {
      listBusinessPartnerAttachments(view.partner.id)
        .then(setAttachments)
        .catch((err) => setError(err instanceof Error ? err.message : String(err)));
      listBusinessPartnerNotes(view.partner.id)
        .then(setNotes)
        .catch((err) => setError(err instanceof Error ? err.message : String(err)));
    } else {
      setAttachments([]);
      setNotes([]);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [view.kind === "details" ? view.partner.id : null]);

  const openCreate = () => {
    setForm(emptyForm);
    setError(null);
    setView({ kind: "create" });
  };

  const openDetails = (partner: BusinessPartner) => {
    setError(null);
    setView({ kind: "details", partner });
  };

  const backToList = () => setView({ kind: "list" });

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createBusinessPartner(form);
      load();
      setView({ kind: "details", partner: created });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleSubmitForApproval = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await submitBusinessPartner(partner.id);
      setView({ kind: "details", partner: updated });
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleApprove = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await approveBusinessPartner(partner.id);
      setView({ kind: "details", partner: updated });
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleReject = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await rejectBusinessPartner(partner.id);
      setView({ kind: "details", partner: updated });
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleUploadAttachment = async (partner: BusinessPartner) => {
    if (!pendingFile) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await uploadBusinessPartnerAttachment(partner.id, pendingFile);
      setPendingFile(null);
      setAttachments(await listBusinessPartnerAttachments(partner.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteAttachment = async (partner: BusinessPartner, attachmentId: string) => {
    setBusy(true);
    setError(null);
    try {
      await deleteBusinessPartnerAttachment(partner.id, attachmentId);
      setAttachments(await listBusinessPartnerAttachments(partner.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleAddNote = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      await addBusinessPartnerNote(partner.id, noteText);
      setNoteText("");
      setNotes(await listBusinessPartnerNotes(partner.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteNote = async (partner: BusinessPartner, noteId: string) => {
    setBusy(true);
    setError(null);
    try {
      await deleteBusinessPartnerNote(partner.id, noteId);
      setNotes(await listBusinessPartnerNotes(partner.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleAddAddress = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await addBusinessPartnerAddress(partner.id, addressForm);
      setView({ kind: "details", partner: updated });
      setAddressForm(emptyAddressForm);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleAddContact = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      const updated = await addBusinessPartnerContact(partner.id, contactForm);
      setView({ kind: "details", partner: updated });
      setContactForm(emptyContactForm);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  if (view.kind === "list") {
    const actions: ActionItem[] = [
      { key: "new", label: t("bp.actionNew", language), onClick: openCreate, variant: "primary" },
    ];

    return (
      <section className="bp-page">
        <h1>{t("bp.heading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />

        {error && <p className="status-page__error">{error}</p>}
        {loading && <p>{t("status.loading", language)}</p>}
        {!loading && partners.length === 0 && <p>{t("bp.emptyState", language)}</p>}

        {!loading && partners.length > 0 && (
          <table className="bp-table">
            <thead>
              <tr>
                <th>{t("bp.columnDocumentNumber", language)}</th>
                <th>{t("bp.columnName", language)}</th>
                <th>{t("bp.columnType", language)}</th>
                <th>{t("bp.columnStatus", language)}</th>
              </tr>
            </thead>
            <tbody>
              {partners.map((partner) => (
                <tr
                  key={partner.id}
                  className="bp-table__row"
                  role="button"
                  tabIndex={0}
                  onClick={() => openDetails(partner)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      openDetails(partner);
                    }
                  }}
                >
                  <td><bdi dir="ltr">{partner.documentNumber ?? "—"}</bdi></td>
                  <td>{partner.name}</td>
                  <td>{translatePartnerType(partner.partnerType, language)}</td>
                  <td>{translateStatus(partner.status, language)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    );
  }

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      {
        key: "save",
        label: t("bp.actionCreate", language),
        onClick: handleCreate,
        variant: "primary",
        isDisabled: busy || form.name.trim().length === 0,
      },
      { key: "cancel", label: t("bp.actionBack", language), onClick: backToList },
    ];

    return (
      <section className="bp-page">
        <h1>{t("bp.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p className="status-page__error">{error}</p>}

        <FastTabs
          tabs={[
            {
              key: "general",
              title: t("status.tabGeneral", language),
              defaultExpanded: true,
              content: (
                <div className="bp-form">
                  <label>
                    {t("bp.fieldName", language)}
                    <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
                  </label>
                  <label>
                    {t("bp.fieldNameArabic", language)}
                    <input
                      dir="rtl"
                      value={form.nameArabic}
                      onChange={(e) => setForm({ ...form, nameArabic: e.target.value })}
                    />
                  </label>
                  <label>
                    {t("bp.fieldPartnerType", language)}
                    <select
                      value={form.partnerType}
                      onChange={(e) => setForm({ ...form, partnerType: e.target.value })}
                    >
                      <option value="Customer">{t("bp.partnerTypeCustomer", language)}</option>
                      <option value="Vendor">{t("bp.partnerTypeVendor", language)}</option>
                      <option value="Both">{t("bp.partnerTypeBoth", language)}</option>
                    </select>
                  </label>
                  <label>
                    {t("bp.fieldTaxRegistrationNumber", language)}
                    <input
                      value={form.taxRegistrationNumber}
                      onChange={(e) => setForm({ ...form, taxRegistrationNumber: e.target.value })}
                    />
                  </label>
                </div>
              ),
            },
          ]}
        />
      </section>
    );
  }

  const { partner } = view;
  const detailActions: ActionItem[] = [{ key: "back", label: t("bp.actionBack", language), onClick: backToList }];
  if (partner.status === "Draft") {
    detailActions.push({
      key: "submit",
      label: t("bp.actionSubmit", language),
      onClick: () => handleSubmitForApproval(partner),
      variant: "primary",
      isDisabled: busy,
    });
  }
  if (partner.status === "Submitted") {
    detailActions.push({
      key: "approve",
      label: t("bp.actionApprove", language),
      onClick: () => handleApprove(partner),
      variant: "primary",
      isDisabled: busy,
    });
    detailActions.push({
      key: "reject",
      label: t("bp.actionReject", language),
      onClick: () => handleReject(partner),
      isDisabled: busy,
    });
  }

  return (
    <section className="bp-page">
      <h1>{partner.name}</h1>
      <ActionPane actions={detailActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p className="status-page__error">{error}</p>}

      <FastTabs
        tabs={[
          {
            key: "general",
            title: t("status.tabGeneral", language),
            defaultExpanded: true,
            content: (
              <dl className="status-page__facts">
                <dt>{t("bp.columnDocumentNumber", language)}</dt>
                <dd><bdi dir="ltr">{partner.documentNumber ?? "—"}</bdi></dd>

                <dt>{t("bp.columnStatus", language)}</dt>
                <dd>{translateStatus(partner.status, language)}</dd>

                <dt>{t("bp.fieldPartnerType", language)}</dt>
                <dd>{translatePartnerType(partner.partnerType, language)}</dd>

                <dt>{t("bp.fieldNameArabic", language)}</dt>
                <dd>{partner.nameArabic ?? "—"}</dd>

                <dt>{t("bp.fieldTaxRegistrationNumber", language)}</dt>
                <dd><bdi dir="ltr">{partner.taxRegistrationNumber ?? "—"}</bdi></dd>
              </dl>
            ),
          },
          {
            key: "addresses",
            title: t("bp.tabAddresses", language),
            content: (
              <div className="bp-form">
                {partner.addresses.length === 0 && <p>{t("bp.emptyAddresses", language)}</p>}
                {partner.addresses.length > 0 && (
                  <table className="bp-table">
                    <thead>
                      <tr>
                        <th>{t("bp.fieldAddressType", language)}</th>
                        <th>{t("bp.fieldCountry", language)}</th>
                        <th>{t("bp.fieldCity", language)}</th>
                        <th>{t("bp.fieldAddressLine", language)}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {partner.addresses.map((address) => (
                        <tr key={address.id}>
                          <td>{translateAddressType(address.addressType, language)}</td>
                          <td>{address.country ?? "—"}</td>
                          <td>{address.city ?? "—"}</td>
                          <td>{address.addressLine ?? "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                <label>
                  {t("bp.fieldAddressType", language)}
                  <select
                    value={addressForm.addressType}
                    onChange={(e) => setAddressForm({ ...addressForm, addressType: e.target.value })}
                  >
                    <option value="HeadOffice">{t("bp.addressTypeHeadOffice", language)}</option>
                    <option value="Billing">{t("bp.addressTypeBilling", language)}</option>
                    <option value="Shipping">{t("bp.addressTypeShipping", language)}</option>
                    <option value="SiteOffice">{t("bp.addressTypeSiteOffice", language)}</option>
                  </select>
                </label>
                <label>
                  {t("bp.fieldCountry", language)}
                  <input
                    value={addressForm.country}
                    onChange={(e) => setAddressForm({ ...addressForm, country: e.target.value })}
                  />
                </label>
                <label>
                  {t("bp.fieldCity", language)}
                  <input
                    value={addressForm.city}
                    onChange={(e) => setAddressForm({ ...addressForm, city: e.target.value })}
                  />
                </label>
                <label>
                  {t("bp.fieldAddressLine", language)}
                  <input
                    value={addressForm.addressLine}
                    onChange={(e) => setAddressForm({ ...addressForm, addressLine: e.target.value })}
                  />
                </label>
                <ActionPane
                  actions={[
                    {
                      key: "add-address",
                      label: t("bp.actionAddAddress", language),
                      onClick: () => handleAddAddress(partner),
                      variant: "primary",
                      isDisabled: busy,
                    },
                  ]}
                  ariaLabel={t("aria.actionToolbar", language)}
                />
              </div>
            ),
          },
          {
            key: "contacts",
            title: t("bp.tabContacts", language),
            content: (
              <div className="bp-form">
                {partner.contacts.length === 0 && <p>{t("bp.emptyContacts", language)}</p>}
                {partner.contacts.length > 0 && (
                  <table className="bp-table">
                    <thead>
                      <tr>
                        <th>{t("bp.fieldContactName", language)}</th>
                        <th>{t("bp.fieldJobTitle", language)}</th>
                        <th>{t("bp.fieldEmail", language)}</th>
                        <th>{t("bp.fieldPhone", language)}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {partner.contacts.map((contact) => (
                        <tr key={contact.id}>
                          <td>{contact.name}</td>
                          <td>{contact.jobTitle ?? "—"}</td>
                          <td>{contact.email ?? "—"}</td>
                          <td><bdi dir="ltr">{contact.phone ?? "—"}</bdi></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                <label>
                  {t("bp.fieldContactName", language)}
                  <input
                    value={contactForm.name}
                    onChange={(e) => setContactForm({ ...contactForm, name: e.target.value })}
                  />
                </label>
                <label>
                  {t("bp.fieldJobTitle", language)}
                  <input
                    value={contactForm.jobTitle}
                    onChange={(e) => setContactForm({ ...contactForm, jobTitle: e.target.value })}
                  />
                </label>
                <label>
                  {t("bp.fieldEmail", language)}
                  <input
                    value={contactForm.email}
                    onChange={(e) => setContactForm({ ...contactForm, email: e.target.value })}
                  />
                </label>
                <label>
                  {t("bp.fieldPhone", language)}
                  <input
                    value={contactForm.phone}
                    onChange={(e) => setContactForm({ ...contactForm, phone: e.target.value })}
                  />
                </label>
                <ActionPane
                  actions={[
                    {
                      key: "add-contact",
                      label: t("bp.actionAddContact", language),
                      onClick: () => handleAddContact(partner),
                      variant: "primary",
                      isDisabled: busy || contactForm.name.trim().length === 0,
                    },
                  ]}
                  ariaLabel={t("aria.actionToolbar", language)}
                />
              </div>
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
                            <a href={businessPartnerAttachmentDownloadUrl(partner.id, attachment.id)}>
                              {t("bp.actionDownload", language)}
                            </a>
                            {" · "}
                            <button type="button" onClick={() => handleDeleteAttachment(partner, attachment.id)} disabled={busy}>
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
                  <input
                    type="file"
                    onChange={(e) => setPendingFile(e.target.files?.[0] ?? null)}
                  />
                </label>
                <ActionPane
                  actions={[
                    {
                      key: "upload-attachment",
                      label: t("bp.actionUpload", language),
                      onClick: () => handleUploadAttachment(partner),
                      variant: "primary",
                      isDisabled: busy || !pendingFile,
                    },
                  ]}
                  ariaLabel={t("aria.actionToolbar", language)}
                />
              </div>
            ),
          },
          {
            key: "notes",
            title: t("bp.tabNotes", language),
            content: (
              <div className="bp-form">
                {notes.length === 0 && <p>{t("bp.emptyNotes", language)}</p>}
                {notes.length > 0 && (
                  <table className="bp-table">
                    <thead>
                      <tr>
                        <th>{t("bp.fieldNoteText", language)}</th>
                        <th>{t("bp.columnNoteCreatedBy", language)}</th>
                        <th>{t("bp.columnNoteCreatedAt", language)}</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {notes.map((note) => (
                        <tr key={note.id}>
                          <td>{note.text}</td>
                          <td>{note.createdBy}</td>
                          <td><bdi dir="ltr">{new Date(note.createdAt).toLocaleString(language)}</bdi></td>
                          <td>
                            <button type="button" onClick={() => handleDeleteNote(partner, note.id)} disabled={busy}>
                              {t("bp.actionDelete", language)}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}

                <label>
                  {t("bp.fieldNoteText", language)}
                  <textarea value={noteText} onChange={(e) => setNoteText(e.target.value)} rows={3} />
                </label>
                <ActionPane
                  actions={[
                    {
                      key: "add-note",
                      label: t("bp.actionAddNote", language),
                      onClick: () => handleAddNote(partner),
                      variant: "primary",
                      isDisabled: busy || noteText.trim().length === 0,
                    },
                  ]}
                  ariaLabel={t("aria.actionToolbar", language)}
                />
              </div>
            ),
          },
        ]}
      />
    </section>
  );
}
