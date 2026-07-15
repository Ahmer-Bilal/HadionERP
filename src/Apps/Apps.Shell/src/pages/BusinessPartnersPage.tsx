import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  addBusinessPartnerAddress,
  addBusinessPartnerContact,
  addBusinessPartnerNote,
  addBusinessPartnerRole,
  approveBusinessPartner,
  businessPartnerAttachmentDownloadUrl,
  createBusinessPartner,
  deleteBusinessPartnerAttachment,
  deleteBusinessPartnerNote,
  listBusinessPartnerAttachments,
  listBusinessPartnerNotes,
  listBusinessPartners,
  rejectBusinessPartner,
  removeBusinessPartnerRole,
  submitBusinessPartner,
  uploadBusinessPartnerAttachment,
} from "../api/businessPartnerApi";
import type {
  AddBusinessPartnerAddressInput,
  AddBusinessPartnerContactInput,
  AddBusinessRoleInput,
  Attachment,
  BusinessPartner,
  CreateBusinessPartnerInput,
  Note,
} from "../api/businessPartnerApi";
import { listLookupValues } from "../api/lookupApi";
import type { LookupValue } from "../api/lookupApi";

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface BusinessPartnersPageProps {
  language: SupportedLanguageCode;
}

// The list pane always stays on screen (Platform.UI's SplitView — see its own doc comment for why);
// "browse" tracks which record (if any) is showing in the detail pane, "create" is a separate full-width
// flow. Same shape as PurchaseOrdersPage, the reference implementation for this pattern.
type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

const emptyForm: CreateBusinessPartnerInput = {
  name: "",
  initialRole: "Supplier",
  taxRegistrationNumber: "",
  nameArabic: "",
  initialTrade: "",
};

const emptyRoleForm: AddBusinessRoleInput = {
  roleType: "Supplier",
  trade: "",
};

// A lookup value's own localized display label — used everywhere a Business Role Type/Address Type/
// Country dropdown is populated, instead of a hardcoded option list or a switch statement with one case per
// known value (which would silently show nothing sensible for a role/address type/country an admin adds
// later through the Lookup Data admin panel; a plain lookup value always has a name to fall back to).
function lookupLabel(value: LookupValue, language: SupportedLanguageCode): string {
  return language === "ar" && value.nameArabic ? value.nameArabic : value.name;
}

// Only the Supplier/Subcontractor/Consultant-family roles have a meaningful Trade/Specialty concept —
// docs/architecture/06-roadmap.md's Phase 2 design (Client/JointVenturePartner/GovernmentAuthority have
// none).
const TRADE_ELIGIBLE_ROLES = new Set(["Supplier", "Subcontractor", "Consultant"]);

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
function translateRoleType(roleType: string, language: SupportedLanguageCode): string {
  switch (roleType) {
    case "Client":
      return t("bp.roleClient", language);
    case "Supplier":
      return t("bp.roleSupplier", language);
    case "Subcontractor":
      return t("bp.roleSubcontractor", language);
    case "Consultant":
      return t("bp.roleConsultant", language);
    case "JointVenturePartner":
      return t("bp.roleJointVenturePartner", language);
    case "GovernmentAuthority":
      return t("bp.roleGovernmentAuthority", language);
    case "RentalCompany":
      return t("bp.roleRentalCompany", language);
    case "Manufacturer":
      return t("bp.roleManufacturer", language);
    case "ManpowerSupplier":
      return t("bp.roleManpowerSupplier", language);
    case "TestingLaboratory":
      return t("bp.roleTestingLaboratory", language);
    default:
      return roleType;
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

/// The first real business screen in the application (Modules.MasterData's Business Partner). Converted to
/// Platform.UI's SplitView (2026-07-15 UI/Visual Density Pass) — the list pane stays visible while the
/// detail pane shows the selected record, rather than navigating away to a separate details view. See
/// PurchaseOrdersPage.tsx, the pattern's reference implementation, and project_visual_identity_decisions.
export function BusinessPartnersPage({ language }: BusinessPartnersPageProps) {
  const [partners, setPartners] = useState<BusinessPartner[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [form, setForm] = useState<CreateBusinessPartnerInput>(emptyForm);
  const [addressForm, setAddressForm] = useState<AddBusinessPartnerAddressInput>(emptyAddressForm);
  const [contactForm, setContactForm] = useState<AddBusinessPartnerContactInput>(emptyContactForm);
  const [roleForm, setRoleForm] = useState<AddBusinessRoleInput>(emptyRoleForm);
  const [attachments, setAttachments] = useState<Attachment[]>([]);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [notes, setNotes] = useState<Note[]>([]);
  const [noteText, setNoteText] = useState("");
  const [busy, setBusy] = useState(false);
  const [roleTypeOptions, setRoleTypeOptions] = useState<LookupValue[]>([]);
  const [addressTypeOptions, setAddressTypeOptions] = useState<LookupValue[]>([]);
  const [countryOptions, setCountryOptions] = useState<LookupValue[]>([]);
  // Trade suggestions are role-scoped, not one flat list — a Subcontractor's trades (Electrical/Concrete/
  // Steel Structure/...) are a different real-world taxonomy from a Supplier's (Steel/Cement/MEP
  // Materials/...) or a Consultant's (Structural/Architectural/MEP Design/...), matching
  // docs/architecture/06-roadmap.md's own Phase 2 design.
  const [tradeOptionsByRole, setTradeOptionsByRole] = useState<Record<string, LookupValue[]>>({});

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

  // Populates the Business Role Type/Address Type/Country dropdowns from the admin-configurable Lookup
  // Data engine (only the currently-active values — a deactivated value stays valid on records that
  // already reference it, but isn't offered for new entries) instead of a hardcoded option list, so an
  // administrator's own additions through the Lookup Data admin panel show up here immediately.
  useEffect(() => {
    listLookupValues("BusinessRoleType", false).then(setRoleTypeOptions).catch(() => {});
    listLookupValues("AddressType", false).then(setAddressTypeOptions).catch(() => {});
    listLookupValues("Country", false).then(setCountryOptions).catch(() => {});
    listLookupValues("SubcontractorTrade", false)
      .then((values) => setTradeOptionsByRole((prev) => ({ ...prev, Subcontractor: values })))
      .catch(() => {});
    listLookupValues("SupplierTrade", false)
      .then((values) => setTradeOptionsByRole((prev) => ({ ...prev, Supplier: values })))
      .catch(() => {});
    listLookupValues("ConsultantTrade", false)
      .then((values) => setTradeOptionsByRole((prev) => ({ ...prev, Consultant: values })))
      .catch(() => {});
  }, []);

  const selectedId = view.kind === "browse" ? view.selectedId : null;
  const selectedPartner = selectedId ? partners.find((p) => p.id === selectedId) ?? null : null;

  // Applying a mutation's response directly into the `partners` list (instead of a full reload) keeps the
  // detail pane and the still-visible list row in sync from one round trip — the point of SplitView is that
  // both stay on screen together, so both need to reflect the same fresh data.
  const applyPartnerUpdate = (updated: BusinessPartner) => {
    setPartners((prev) => prev.map((p) => (p.id === updated.id ? updated : p)));
  };

  // Attachments/Notes aren't embedded in BusinessPartnerDto (metadata-only lists, fetched separately) —
  // reload whenever the selected partner changes.
  useEffect(() => {
    if (selectedPartner) {
      listBusinessPartnerAttachments(selectedPartner.id)
        .then(setAttachments)
        .catch((err) => setError(err instanceof Error ? err.message : String(err)));
      listBusinessPartnerNotes(selectedPartner.id)
        .then(setNotes)
        .catch((err) => setError(err instanceof Error ? err.message : String(err)));
    } else {
      setAttachments([]);
      setNotes([]);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedPartner?.id ?? null]);

  const openCreate = () => {
    setForm(emptyForm);
    setError(null);
    setView({ kind: "create" });
  };

  const openDetails = (id: string) => {
    setError(null);
    setView({ kind: "browse", selectedId: id });
  };

  const backToBrowse = () => setView({ kind: "browse", selectedId: null });

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createBusinessPartner(form);
      setPartners((prev) => [created, ...prev]);
      setView({ kind: "browse", selectedId: created.id });
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
      applyPartnerUpdate(await submitBusinessPartner(partner.id));
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
      applyPartnerUpdate(await approveBusinessPartner(partner.id));
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
      applyPartnerUpdate(await rejectBusinessPartner(partner.id));
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
      applyPartnerUpdate(await addBusinessPartnerAddress(partner.id, addressForm));
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
      applyPartnerUpdate(await addBusinessPartnerContact(partner.id, contactForm));
      setContactForm(emptyContactForm);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleAddRole = async (partner: BusinessPartner) => {
    setBusy(true);
    setError(null);
    try {
      const input: AddBusinessRoleInput = {
        roleType: roleForm.roleType,
        trade: TRADE_ELIGIBLE_ROLES.has(roleForm.roleType) ? roleForm.trade?.trim() || undefined : undefined,
      };
      applyPartnerUpdate(await addBusinessPartnerRole(partner.id, input));
      setRoleForm(emptyRoleForm);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleRemoveRole = async (partner: BusinessPartner, roleId: string) => {
    setBusy(true);
    setError(null);
    try {
      applyPartnerUpdate(await removeBusinessPartnerRole(partner.id, roleId));
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      {
        key: "save",
        label: t("bp.actionCreate", language),
        onClick: handleCreate,
        variant: "primary",
        isDisabled: busy || form.name.trim().length === 0,
      },
      { key: "cancel", label: t("bp.actionBack", language), onClick: backToBrowse },
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
                    {t("bp.fieldBusinessRole", language)}
                    <select
                      value={form.initialRole}
                      onChange={(e) => setForm({ ...form, initialRole: e.target.value })}
                    >
                      {roleTypeOptions.map((role) => (
                        <option key={role.code} value={role.code}>{lookupLabel(role, language)}</option>
                      ))}
                    </select>
                  </label>
                  {TRADE_ELIGIBLE_ROLES.has(form.initialRole) && (
                    <label>
                      {t("bp.fieldTrade", language)}
                      <input
                        list="initial-trade-suggestions"
                        value={form.initialTrade}
                        onChange={(e) => setForm({ ...form, initialTrade: e.target.value })}
                      />
                      <datalist id="initial-trade-suggestions">
                        {(tradeOptionsByRole[form.initialRole] ?? []).map((trade) => (
                          <option key={trade.code} value={trade.code}>{lookupLabel(trade, language)}</option>
                        ))}
                      </datalist>
                    </label>
                  )}
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

  // browse view: list pane always visible, detail pane shows the selected record (Platform.UI's SplitView)
  const listActions: ActionItem[] = [
    { key: "new", label: t("bp.actionNew", language), onClick: openCreate, variant: "primary" },
  ];

  const listPane = (
    <>
      {loading && <p>{t("status.loading", language)}</p>}
      {!loading && partners.length === 0 && <p>{t("bp.emptyState", language)}</p>}
      {!loading && partners.length > 0 && (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("bp.columnDocumentNumber", language)}</th>
              <th>{t("bp.columnName", language)}</th>
              <th>{t("bp.columnRoles", language)}</th>
              <th>{t("bp.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {partners.map((partner) => (
              <tr
                key={partner.id}
                className={partner.id === selectedId ? "is-selected" : undefined}
                onClick={() => openDetails(partner.id)}
              >
                <td>
                  <button type="button" className="pi-link" onClick={(e) => { e.stopPropagation(); openDetails(partner.id); }}>
                    <bdi dir="ltr">{partner.documentNumber ?? "—"}</bdi>
                  </button>
                </td>
                <td>{partner.name}</td>
                <td>{partner.businessRoles.map((r) => translateRoleType(r.roleType, language)).join(", ")}</td>
                <td>{translateStatus(partner.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </>
  );

  let detailPane: React.ReactNode = null;
  if (selectedPartner) {
    const partner = selectedPartner;
    const detailActions: ActionItem[] = [];
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

    detailPane = (
      <>
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

                  <dt>{t("bp.fieldNameArabic", language)}</dt>
                  <dd>{partner.nameArabic ?? "—"}</dd>

                  <dt>{t("bp.fieldTaxRegistrationNumber", language)}</dt>
                  <dd><bdi dir="ltr">{partner.taxRegistrationNumber ?? "—"}</bdi></dd>
                </dl>
              ),
            },
            {
              key: "business-roles",
              title: t("bp.tabBusinessRoles", language),
              content: (
                <div className="bp-form">
                  {partner.businessRoles.length === 0 && <p>{t("bp.emptyBusinessRoles", language)}</p>}
                  {partner.businessRoles.length > 0 && (
                    <table className="bp-table">
                      <thead>
                        <tr>
                          <th>{t("bp.columnRoleType", language)}</th>
                          <th>{t("bp.columnTrade", language)}</th>
                          <th></th>
                        </tr>
                      </thead>
                      <tbody>
                        {partner.businessRoles.map((role) => (
                          <tr key={role.id}>
                            <td>{translateRoleType(role.roleType, language)}</td>
                            <td>{role.trade ?? "—"}</td>
                            <td>
                              <button type="button" disabled={busy} onClick={() => handleRemoveRole(partner, role.id)}>
                                {t("bp.actionRemoveRole", language)}
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}

                  <label>
                    {t("bp.fieldBusinessRole", language)}
                    <select
                      value={roleForm.roleType}
                      onChange={(e) => setRoleForm({ ...roleForm, roleType: e.target.value })}
                    >
                      {roleTypeOptions.map((role) => (
                        <option key={role.code} value={role.code}>{lookupLabel(role, language)}</option>
                      ))}
                    </select>
                  </label>
                  {TRADE_ELIGIBLE_ROLES.has(roleForm.roleType) && (
                    <label>
                      {t("bp.fieldTrade", language)}
                      <input
                        list="role-trade-suggestions"
                        value={roleForm.trade}
                        onChange={(e) => setRoleForm({ ...roleForm, trade: e.target.value })}
                      />
                      <datalist id="role-trade-suggestions">
                        {(tradeOptionsByRole[roleForm.roleType] ?? []).map((trade) => (
                          <option key={trade.code} value={trade.code}>{lookupLabel(trade, language)}</option>
                        ))}
                      </datalist>
                    </label>
                  )}
                  <ActionPane
                    actions={[
                      {
                        key: "add-role",
                        label: t("bp.actionAddRole", language),
                        onClick: () => handleAddRole(partner),
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
                      {addressTypeOptions.map((addressType) => (
                        <option key={addressType.code} value={addressType.code}>{lookupLabel(addressType, language)}</option>
                      ))}
                    </select>
                  </label>
                  <label>
                    {t("bp.fieldCountry", language)}
                    <select
                      value={addressForm.country}
                      onChange={(e) => setAddressForm({ ...addressForm, country: e.target.value })}
                    >
                      <option value="">—</option>
                      {countryOptions.map((country) => (
                        <option key={country.code} value={country.code}>{lookupLabel(country, language)}</option>
                      ))}
                    </select>
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
      </>
    );
  }

  return (
    <section className="bp-page">
      <h1>{t("bp.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && !selectedPartner && <p className="status-page__error">{error}</p>}
      <SplitView
        list={listPane}
        detail={detailPane}
        detailKey={selectedId ?? "none"}
        emptyDetailHint={t("bp.selectHint", language)}
        ariaLabel={t("bp.heading", language)}
      />
    </section>
  );
}
