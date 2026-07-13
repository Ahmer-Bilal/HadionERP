import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveBusinessPartner,
  createBusinessPartner,
  listBusinessPartners,
  submitBusinessPartner,
} from "../api/businessPartnerApi";
import type { BusinessPartner, CreateBusinessPartnerInput } from "../api/businessPartnerApi";

interface BusinessPartnersPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; partner: BusinessPartner };

const emptyForm: CreateBusinessPartnerInput = {
  name: "",
  partnerType: "Vendor",
  taxRegistrationNumber: "",
  email: "",
  phone: "",
  country: "",
  city: "",
  addressLine: "",
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
            {
              key: "contact",
              title: t("bp.tabContact", language),
              content: (
                <div className="bp-form">
                  <label>
                    {t("bp.fieldEmail", language)}
                    <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                  </label>
                  <label>
                    {t("bp.fieldPhone", language)}
                    <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
                  </label>
                  <label>
                    {t("bp.fieldCountry", language)}
                    <input value={form.country} onChange={(e) => setForm({ ...form, country: e.target.value })} />
                  </label>
                  <label>
                    {t("bp.fieldCity", language)}
                    <input value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} />
                  </label>
                  <label>
                    {t("bp.fieldAddressLine", language)}
                    <input value={form.addressLine} onChange={(e) => setForm({ ...form, addressLine: e.target.value })} />
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

                <dt>{t("bp.fieldTaxRegistrationNumber", language)}</dt>
                <dd><bdi dir="ltr">{partner.taxRegistrationNumber ?? "—"}</bdi></dd>
              </dl>
            ),
          },
          {
            key: "contact",
            title: t("bp.tabContact", language),
            content: (
              <dl className="status-page__facts">
                <dt>{t("bp.fieldEmail", language)}</dt>
                <dd>{partner.email ?? "—"}</dd>

                <dt>{t("bp.fieldPhone", language)}</dt>
                <dd><bdi dir="ltr">{partner.phone ?? "—"}</bdi></dd>

                <dt>{t("bp.fieldCountry", language)}</dt>
                <dd>{partner.country ?? "—"}</dd>

                <dt>{t("bp.fieldCity", language)}</dt>
                <dd>{partner.city ?? "—"}</dd>

                <dt>{t("bp.fieldAddressLine", language)}</dt>
                <dd>{partner.addressLine ?? "—"}</dd>
              </dl>
            ),
          },
        ]}
      />
    </section>
  );
}
