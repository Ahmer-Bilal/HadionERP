import { useCallback, useEffect, useState } from "react";
import { ActionPane, FastTabs, SplitView } from "@platform/ui";
import type { ActionItem } from "@platform/ui";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";
import {
  activateUser,
  assignRole,
  createUser,
  deactivateUser,
  listUsers,
  removeRole,
  resetPassword,
  SodConflictError,
} from "../api/usersApi";
import type { ManagedUser } from "../api/usersApi";

interface UsersPageProps {
  language: SupportedLanguageCode;
}

type ViewState = { kind: "browse"; selectedId: string | null } | { kind: "create" };

const emptyCreateForm = { username: "", displayName: "", password: "", email: "" };

export function UsersPage({ language }: UsersPageProps) {
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [view, setView] = useState<ViewState>({ kind: "browse", selectedId: null });
  const [busy, setBusy] = useState(false);
  const [createForm, setCreateForm] = useState(emptyCreateForm);

  const [newRoleKey, setNewRoleKey] = useState("");
  const [sodConflicts, setSodConflicts] = useState<string[] | null>(null);
  const [overrideReason, setOverrideReason] = useState("");
  const [newPassword, setNewPassword] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    listUsers()
      .then(setUsers)
      .catch((err) => setError(err instanceof Error ? err.message : String(err)))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const selectedId = view.kind === "browse" ? view.selectedId : null;
  const selectedUser = selectedId ? users.find((u) => u.id === selectedId) ?? null : null;

  const applyUserUpdate = (updated: ManagedUser) => {
    setUsers((prev) => prev.map((u) => (u.id === updated.id ? updated : u)));
  };

  const openCreate = () => {
    setCreateForm(emptyCreateForm);
    setError(null);
    setView({ kind: "create" });
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await createUser({
        username: createForm.username.trim(),
        displayName: createForm.displayName.trim(),
        password: createForm.password,
        email: createForm.email.trim() || undefined,
      });
      setUsers((prev) => [...prev, created]);
      setView({ kind: "browse", selectedId: created.id });
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleAssignRole = async (user: ManagedUser, reason?: string) => {
    if (!newRoleKey.trim()) return;
    setBusy(true);
    setError(null);
    setSodConflicts(null);
    try {
      const updated = await assignRole(user.id, { roleKey: newRoleKey.trim(), overrideReason: reason });
      applyUserUpdate(updated);
      setNewRoleKey("");
      setOverrideReason("");
    } catch (e) {
      if (e instanceof SodConflictError) {
        setSodConflicts(e.conflicts);
      } else {
        setError(e instanceof Error ? e.message : String(e));
      }
    } finally {
      setBusy(false);
    }
  };

  const handleRemoveRole = async (user: ManagedUser, roleKey: string) => {
    setBusy(true);
    setError(null);
    try {
      applyUserUpdate(await removeRole(user.id, roleKey));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleToggleActive = async (user: ManagedUser) => {
    setBusy(true);
    setError(null);
    try {
      applyUserUpdate(user.isActive ? await deactivateUser(user.id) : await activateUser(user.id));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const handleResetPassword = async (user: ManagedUser) => {
    if (!newPassword.trim()) return;
    setBusy(true);
    setError(null);
    try {
      applyUserUpdate(await resetPassword(user.id, newPassword));
      setNewPassword("");
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const inputStyle: React.CSSProperties = { display: "block", marginBlockEnd: "0.5rem", inlineSize: "100%", padding: "0.3rem" };

  if (view.kind === "create") {
    const actions: ActionItem[] = [
      { key: "create", label: t("users.actionCreate", language), onClick: handleCreate, variant: "primary", isDisabled: busy },
      { key: "back", label: t("users.actionBack", language), onClick: () => setView({ kind: "browse", selectedId: null }) },
    ];
    return (
      <section>
        <h1>{t("users.newHeading", language)}</h1>
        <ActionPane actions={actions} ariaLabel={t("aria.actionToolbar", language)} />
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <div style={{ maxInlineSize: "32rem" }}>
          <label>{t("users.fieldUsername", language)}
            <input style={inputStyle} value={createForm.username} onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })} />
          </label>
          <label>{t("users.fieldDisplayName", language)}
            <input style={inputStyle} value={createForm.displayName} onChange={(e) => setCreateForm({ ...createForm, displayName: e.target.value })} />
          </label>
          <label>{t("users.fieldEmail", language)}
            <input style={inputStyle} value={createForm.email} onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })} />
          </label>
          <label>{t("users.fieldPassword", language)}
            <input type="password" style={inputStyle} value={createForm.password} onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })} />
          </label>
        </div>
      </section>
    );
  }

  const listPane = (
    <section>
      <h1>{t("users.heading", language)}</h1>
      <ActionPane
        actions={[{ key: "new", label: t("users.actionNew", language), onClick: openCreate, variant: "primary" }]}
        ariaLabel={t("aria.actionToolbar", language)}
      />
      {loading ? (
        <p>{t("status.loading", language)}</p>
      ) : users.length === 0 ? (
        <p>{t("users.emptyState", language)}</p>
      ) : (
        <table className="pi-dense-table">
          <thead>
            <tr>
              <th>{t("users.columnUsername", language)}</th>
              <th>{t("users.columnDisplayName", language)}</th>
              <th>{t("users.columnStatus", language)}</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className={u.id === selectedId ? "is-selected" : ""}>
                <td>
                  <a className="pi-link" href="#" onClick={(e) => { e.preventDefault(); setView({ kind: "browse", selectedId: u.id }); }}>
                    <bdi dir="ltr">{u.username}</bdi>
                  </a>
                </td>
                <td>{u.displayName}</td>
                <td>{u.isActive ? t("users.statusActive", language) : t("users.statusInactive", language)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );

  const detailPane = selectedUser ? (
    <section>
      <h1>{selectedUser.displayName}</h1>
      <ActionPane
        actions={[
          {
            key: "toggle-active",
            label: selectedUser.isActive ? t("users.actionDeactivate", language) : t("users.actionActivate", language),
            onClick: () => handleToggleActive(selectedUser),
            isDisabled: busy,
          },
        ]}
        ariaLabel={t("aria.actionToolbar", language)}
      />
      {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
      <FastTabs tabs={[
        {
          key: "general",
          title: t("status.tabGeneral", language),
          defaultExpanded: true,
          content: (
            <dl style={{ maxInlineSize: "32rem" }}>
              <dt>{t("users.columnUsername", language)}</dt>
              <dd><bdi dir="ltr">{selectedUser.username}</bdi></dd>
              <dt>{t("users.fieldEmail", language)}</dt>
              <dd>{selectedUser.email ?? "—"}</dd>
              <dt>{t("users.columnStatus", language)}</dt>
              <dd>{selectedUser.isActive ? t("users.statusActive", language) : t("users.statusInactive", language)}</dd>
            </dl>
          ),
        },
        {
          key: "roles",
          title: t("users.tabRoles", language),
          defaultExpanded: true,
          content: (
            <div>
              {selectedUser.roleKeys.length === 0 ? (
                <p>{t("users.emptyRoles", language)}</p>
              ) : (
                <ul>
                  {selectedUser.roleKeys.map((role) => (
                    <li key={role}>
                      <bdi dir="ltr">{role}</bdi>{" "}
                      <button onClick={() => handleRemoveRole(selectedUser, role)} disabled={busy}>{t("users.actionRemoveRole", language)}</button>
                    </li>
                  ))}
                </ul>
              )}
              <label>{t("users.fieldRoleKey", language)}
                <input style={inputStyle} value={newRoleKey} onChange={(e) => setNewRoleKey(e.target.value)} />
              </label>
              {sodConflicts && (
                <div style={{ color: "var(--pi-danger)" }}>
                  <p>{t("users.sodConflictHeading", language)}</p>
                  <ul>{sodConflicts.map((c) => <li key={c}>{c}</li>)}</ul>
                  <label>{t("users.fieldOverrideReason", language)}
                    <input style={inputStyle} value={overrideReason} onChange={(e) => setOverrideReason(e.target.value)} />
                  </label>
                  <button onClick={() => handleAssignRole(selectedUser, overrideReason)} disabled={busy || !overrideReason.trim()}>
                    {t("users.actionGrantExceptionAndAssign", language)}
                  </button>
                </div>
              )}
              <button onClick={() => handleAssignRole(selectedUser)} disabled={busy || !newRoleKey.trim()}>
                {t("users.actionAssignRole", language)}
              </button>
            </div>
          ),
        },
        {
          key: "password",
          title: t("users.tabPassword", language),
          content: (
            <div style={{ maxInlineSize: "32rem" }}>
              <label>{t("users.fieldNewPassword", language)}
                <input type="password" style={inputStyle} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
              </label>
              <button onClick={() => handleResetPassword(selectedUser)} disabled={busy || !newPassword.trim()}>
                {t("users.actionResetPassword", language)}
              </button>
            </div>
          ),
        },
      ]} />
    </section>
  ) : null;

  return (
    <SplitView
      list={listPane}
      detail={detailPane}
      detailKey={selectedId ?? "none"}
      emptyDetailHint={t("users.selectHint", language)}
      ariaLabel={t("users.heading", language)}
    />
  );
}
