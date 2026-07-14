import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  approveItem,
  createItem,
  listItems,
  rejectItem,
  submitItem,
} from "../api/itemApi";
import type { CreateItemInput, Item } from "../api/itemApi";

interface ItemsPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "list" } | { kind: "create" } | { kind: "details"; item: Item };

const itemTypeKeys: Record<string, "item.itemTypeStock" | "item.itemTypeNonStock" | "item.itemTypeService"> = {
  Stock: "item.itemTypeStock",
  NonStock: "item.itemTypeNonStock",
  Service: "item.itemTypeService",
};

const statusKeys: Record<string, "bp.statusDraft" | "bp.statusSubmitted" | "bp.statusApproved" | "bp.statusRejected"> = {
  Draft: "bp.statusDraft",
  Submitted: "bp.statusSubmitted",
  Approved: "bp.statusApproved",
  Rejected: "bp.statusRejected",
};

function translateItemType(itemType: string, language: SupportedLanguageCode): string {
  const key = itemTypeKeys[itemType];
  return key ? t(key, language) : itemType;
}

function translateStatus(status: string, language: SupportedLanguageCode): string {
  const key = statusKeys[status];
  return key ? t(key, language) : status;
}

export function ItemsPage({ language }: ItemsPageProps) {
  const [view, setView] = useState<ViewState>({ kind: "list" });
  const [items, setItems] = useState<Item[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateItemInput>({
    itemCode: "",
    itemName: "",
    itemType: "Stock",
    unitOfMeasure: "",
    itemNameArabic: "",
  });

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const result = await listItems(200, 0);
      setItems(result.items);
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
      const input: CreateItemInput = {
        itemCode: form.itemCode.trim(),
        itemName: form.itemName.trim(),
        itemType: form.itemType,
        unitOfMeasure: form.unitOfMeasure.trim(),
        itemNameArabic: form.itemNameArabic?.trim() || undefined,
      };
      await createItem(input);
      setForm({ itemCode: "", itemName: "", itemType: "Stock", unitOfMeasure: "", itemNameArabic: "" });
      setView({ kind: "list" });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleStatusAction = async (item: Item, action: "submit" | "approve" | "reject") => {
    setBusy(true);
    setError(null);
    try {
      let updated: Item;
      if (action === "submit") updated = await submitItem(item.id);
      else if (action === "approve") updated = await approveItem(item.id);
      else updated = await rejectItem(item.id);
      setView({ kind: "details", item: updated });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("item.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("item.actionBack", language), onClick: () => setView({ kind: "list" }) },
    ];
    return (
      <section>
        <h1>{t("item.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("item.fieldItemCode", language)}
            <input style={inputStyle} value={form.itemCode} onChange={(e) => setForm({ ...form, itemCode: e.target.value })} />
          </label>
          <label>{t("item.fieldItemName", language)}
            <input style={inputStyle} value={form.itemName} onChange={(e) => setForm({ ...form, itemName: e.target.value })} />
          </label>
          <label>{t("item.fieldItemNameArabic", language)}
            <input dir="rtl" style={inputStyle} value={form.itemNameArabic ?? ""} onChange={(e) => setForm({ ...form, itemNameArabic: e.target.value })} />
          </label>
          <label>{t("item.fieldItemType", language)}
            <select style={inputStyle} value={form.itemType} onChange={(e) => setForm({ ...form, itemType: e.target.value })}>
              <option value="Stock">{t("item.itemTypeStock", language)}</option>
              <option value="NonStock">{t("item.itemTypeNonStock", language)}</option>
              <option value="Service">{t("item.itemTypeService", language)}</option>
            </select>
          </label>
          <label>{t("item.fieldUnitOfMeasure", language)}
            <input style={inputStyle} value={form.unitOfMeasure} onChange={(e) => setForm({ ...form, unitOfMeasure: e.target.value })} />
          </label>
        </div>
      </section>
    );
  }

  if (view.kind === "details") {
    const item = view.item;
    const actions: ActionItem[] = [];
    if (item.status === "Draft")
      actions.push({ key: "submit", label: t("item.actionSubmit", language), onClick: () => handleStatusAction(item, "submit"), variant: "primary", isDisabled: busy });
    if (item.status === "Submitted") {
      actions.push({ key: "approve", label: t("item.actionApprove", language), onClick: () => handleStatusAction(item, "approve"), variant: "primary", isDisabled: busy });
      actions.push({ key: "reject", label: t("item.actionReject", language), onClick: () => handleStatusAction(item, "reject"), isDisabled: busy });
    }
    actions.push({ key: "back", label: t("item.actionBack", language), onClick: () => setView({ kind: "list" }) });

    return (
      <section>
        <h1>{item.itemCode} — {item.itemName}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <FastTabs tabs={[{
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("item.columnCode", language)}</dt>
              <dd>{item.itemCode}</dd>
              <dt>{t("item.columnName", language)}</dt>
              <dd>{item.itemName}</dd>
              {item.itemNameArabic && (
                <>
                  <dt>{t("item.fieldItemNameArabic", language)}</dt>
                  <dd dir="rtl">{item.itemNameArabic}</dd>
                </>
              )}
              <dt>{t("item.columnType", language)}</dt>
              <dd>{translateItemType(item.itemType, language)}</dd>
              <dt>{t("item.columnUnitOfMeasure", language)}</dt>
              <dd><bdi dir="ltr">{item.unitOfMeasure}</bdi></dd>
              <dt>{t("item.columnStatus", language)}</dt>
              <dd>{translateStatus(item.status, language)}</dd>
            </dl>
          ),
        }]} />
      </section>
    );
  }

  // list view
  const listActions: ActionItem[] = [
    { key: "new", label: t("item.actionNew", language), onClick: () => setView({ kind: "create" }), variant: "primary" },
  ];
  return (
    <section>
      <h1>{t("item.heading", language)}</h1>
      <ActionPane actions={listActions} ariaLabel={t("aria.actionToolbar", language)} />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      {items.length === 0 ? (
        <p>{t("item.emptyState", language)}</p>
      ) : (
        <table className="bp-table">
          <thead>
            <tr>
              <th>{t("item.columnCode", language)}</th>
              <th>{t("item.columnName", language)}</th>
              <th>{t("item.columnType", language)}</th>
              <th>{t("item.columnUnitOfMeasure", language)}</th>
              <th>{t("item.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id} style={{ cursor: "pointer" }} onClick={() => setView({ kind: "details", item })}>
                <td><bdi dir="ltr">{item.itemCode}</bdi></td>
                <td>{item.itemName}</td>
                <td>{translateItemType(item.itemType, language)}</td>
                <td><bdi dir="ltr">{item.unitOfMeasure}</bdi></td>
                <td>{translateStatus(item.status, language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
