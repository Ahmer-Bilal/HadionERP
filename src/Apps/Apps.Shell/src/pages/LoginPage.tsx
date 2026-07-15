import { useState } from "react";
import { useAuth } from "../AuthContext";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";

interface LoginPageProps {
  language: SupportedLanguageCode;
}

export function LoginPage({ language }: LoginPageProps) {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(username, password);
    } catch {
      // Never distinguish "unknown username" from "wrong password" in the UI either — same reasoning as
      // UserService.AuthenticateAsync never distinguishing them server-side.
      setError(t("auth.invalidCredentials", language));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="app-shell__login">
      <form onSubmit={handleSubmit} className="app-shell__login-form">
        <h1>{t("shell.title", language)}</h1>
        <p>{t("auth.signInPrompt", language)}</p>
        {error && <p style={{ color: "var(--pi-danger)" }}>{error}</p>}
        <label>
          {t("auth.usernameLabel", language)}
          <input
            autoFocus
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            disabled={busy}
          />
        </label>
        <label>
          {t("auth.passwordLabel", language)}
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={busy}
          />
        </label>
        <button type="submit" disabled={busy || !username || !password}>
          {t("auth.loginButton", language)}
        </button>
      </form>
    </div>
  );
}
