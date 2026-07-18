import "./LoginPage.css";
import { useEffect, useState } from "react";
import { useAuth } from "../AuthContext";
import type { SupportedLanguageCode } from "../i18n/language";
import { t } from "../i18n/content";

interface LoginPageProps {
  language: SupportedLanguageCode;
  languages: { code: SupportedLanguageCode; label: string }[];
  onLanguageChange: (language: SupportedLanguageCode) => void;
}

type IconKey =
  | "globe" | "sun" | "moon" | "building" | "user" | "lock" | "eye" | "eyeOff" | "arrowRight"
  | "shield" | "cloud" | "chartLine" | "finance" | "vendors" | "procurement" | "projects"
  | "inventory" | "hrPayroll" | "microsoft" | "google";

// A small, self-contained icon set for this page only — deliberately not Platform.UI's DepartmentIcon,
// since the hero's orbit graphic is decorative marketing content (Vendors/Inventory/HR & Payroll aren't
// real nav departments in this app yet — HR & Payroll is Phase 4, not started per ROADMAP.md), and
// borrowing the shared nav icon vocabulary for concepts that don't exist yet in the app would misleadingly
// imply they do. Same stroke-based line-icon style as DepartmentIcon for visual consistency.
function Icon({ icon, size = 18 }: { icon: IconKey; size?: number }) {
  const common = {
    width: size, height: size, viewBox: "0 0 20 20", fill: "none", stroke: "currentColor",
    strokeWidth: 1.5, strokeLinecap: "round" as const, strokeLinejoin: "round" as const, "aria-hidden": true,
  };
  switch (icon) {
    case "globe":
      return <svg {...common}><circle cx="10" cy="10" r="7" /><path d="M3 10h14M10 3c2.6 2 2.6 12 0 14M10 3c-2.6 2-2.6 12 0 14" /></svg>;
    case "sun":
      return <svg {...common}><circle cx="10" cy="10" r="3.3" /><path d="M10 2.2v2M10 15.8v2M2.2 10h2M15.8 10h2M4.4 4.4l1.4 1.4M14.2 14.2l1.4 1.4M4.4 15.6l1.4-1.4M14.2 5.8l1.4-1.4" /></svg>;
    case "moon":
      return <svg {...common} stroke="none" fill="currentColor"><path d="M16 12.5A6.5 6.5 0 0 1 8.2 4a6.5 6.5 0 1 0 7.8 8.5Z" /></svg>;
    case "building":
      return <svg {...common}><rect x="4" y="3" width="12" height="14" rx="1" /><path d="M7.2 6.6h.01M12.8 6.6h.01M7.2 10h.01M12.8 10h.01M7.2 13.4h.01M12.8 13.4h.01" strokeWidth="2" /></svg>;
    case "user":
      return <svg {...common}><circle cx="10" cy="7" r="3" /><path d="M4 17c0-3.3 2.7-5.5 6-5.5s6 2.2 6 5.5" /></svg>;
    case "lock":
      return <svg {...common}><rect x="5" y="9" width="10" height="8" rx="1.5" /><path d="M7 9V6.5a3 3 0 0 1 6 0V9" /></svg>;
    case "eye":
      return <svg {...common}><path d="M2 10s3-6 8-6 8 6 8 6-3 6-8 6-8-6-8-6Z" /><circle cx="10" cy="10" r="2.2" /></svg>;
    case "eyeOff":
      return <svg {...common}><path d="M2 10s3-6 8-6 8 6 8 6-3 6-8 6-8-6-8-6Z" /><circle cx="10" cy="10" r="2.2" /><path d="M3 3l14 14" /></svg>;
    case "arrowRight":
      return <svg {...common}><path d="M4 10h12M12 5l5 5-5 5" /></svg>;
    case "shield":
      return <svg {...common}><path d="M10 3 16 5.2v4.3c0 4-2.6 7-6 7.5-3.4-.5-6-3.5-6-7.5V5.2L10 3Z" /><path d="M7.5 10.2 9.3 12l3.2-3.6" /></svg>;
    case "cloud":
      return <svg {...common}><path d="M6.2 15a3.4 3.4 0 0 1-.4-6.8A4.4 4.4 0 0 1 14 7.1 3.4 3.4 0 0 1 14.4 15H6.2Z" /></svg>;
    case "chartLine":
      return <svg {...common}><path d="M3 14.5 7 10l3 2.5 6-6.5" /><path d="M3 17h14" /></svg>;
    case "finance":
      return <svg {...common}><path d="M4 4.5h9a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H4Z" /><path d="M4 4.5v13" /><path d="M7 8h6M7 11h6M7 14h4" /></svg>;
    case "vendors":
      return <svg {...common}><path d="M2 12l5-4 3 2 3-2 5 4" /><path d="M7 8l3 6 3-6" /></svg>;
    case "procurement":
      return <svg {...common}><circle cx="7.5" cy="17" r="1.1" /><circle cx="14" cy="17" r="1.1" /><path d="M2.5 3.5h2l2 10.5h9.5l1.7-7.5H6" /></svg>;
    case "projects":
      return <svg {...common}><path d="M5 3v14" /><path d="M5 4h9l-2.5 3L14 10H5" /></svg>;
    case "inventory":
      return <svg {...common}><path d="M10 3 17 6.5 10 10 3 6.5Z" /><path d="M3 6.5V14L10 17.5V10" /><path d="M17 6.5V14L10 17.5" /></svg>;
    case "hrPayroll":
      return <svg {...common}><circle cx="7" cy="7" r="2.4" /><path d="M2.6 16c0-2.4 2-4 4.4-4s4.4 1.6 4.4 4" /><circle cx="14.2" cy="7.2" r="2.1" /><path d="M10.8 16c.3-1.9 2-3.2 3.6-3.2 1.9 0 3.5 1.3 3.5 3.2" /></svg>;
    case "microsoft":
      return (
        <svg width={size} height={size} viewBox="0 0 20 20" aria-hidden="true">
          <rect x="2" y="2" width="7.2" height="7.2" fill="#f25022" />
          <rect x="10.8" y="2" width="7.2" height="7.2" fill="#7fba00" />
          <rect x="2" y="10.8" width="7.2" height="7.2" fill="#00a4ef" />
          <rect x="10.8" y="10.8" width="7.2" height="7.2" fill="#ffb900" />
        </svg>
      );
    case "google":
      return (
        <svg width={size} height={size} viewBox="0 0 20 20" aria-hidden="true">
          <path fill="#4285F4" d="M19.6 10.23c0-.68-.06-1.32-.17-1.94H10v3.9h5.38a4.6 4.6 0 0 1-2 3.02v2.4h3.2c1.87-1.72 2.95-4.26 2.95-7.38Z" />
          <path fill="#34A853" d="M10 20c2.7 0 4.96-.9 6.6-2.4l-3.2-2.4c-.9.6-2.05.95-3.4.95-2.6 0-4.8-1.76-5.6-4.12H1.1v2.5A10 10 0 0 0 10 20Z" />
          <path fill="#FBBC05" d="M4.4 11.97a5.9 5.9 0 0 1 0-3.86v-2.5H1.1a10 10 0 0 0 0 8.86l3.3-2.5Z" />
          <path fill="#EA4335" d="M10 3.98c1.47 0 2.8.5 3.83 1.5l2.87-2.83A9.9 9.9 0 0 0 10 0 10 10 0 0 0 1.1 5.6l3.3 2.5C5.2 5.75 7.4 3.98 10 3.98Z" />
        </svg>
      );
  }
}

const ORBIT_NODES: { key: string; angle: string; color: string; icon: IconKey; labelKey: "auth.orbitFinance" | "auth.orbitVendors" | "auth.orbitProcurement" | "auth.orbitInventory" | "auth.orbitHrPayroll" | "auth.orbitProjects" }[] = [
  { key: "finance", angle: "0deg", color: "#2f8fe0", icon: "finance", labelKey: "auth.orbitFinance" },
  { key: "procurement", angle: "60deg", color: "#1fa97a", icon: "procurement", labelKey: "auth.orbitProcurement" },
  { key: "inventory", angle: "120deg", color: "#e08a1f", icon: "inventory", labelKey: "auth.orbitInventory" },
  { key: "hrPayroll", angle: "180deg", color: "#7b5fe0", icon: "hrPayroll", labelKey: "auth.orbitHrPayroll" },
  { key: "projects", angle: "240deg", color: "#d94f8c", icon: "projects", labelKey: "auth.orbitProjects" },
  { key: "vendors", angle: "300deg", color: "#7b61c9", icon: "vendors", labelKey: "auth.orbitVendors" },
];

function readStoredTheme(): "light" | "dark" {
  const stored = typeof localStorage !== "undefined" ? localStorage.getItem("hadion-theme") : null;
  if (stored === "light" || stored === "dark") return stored;
  return typeof matchMedia !== "undefined" && matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function LoginPage({ language, languages, onLanguageChange }: LoginPageProps) {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [theme, setTheme] = useState<"light" | "dark">(readStoredTheme);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem("hadion-theme", theme);
  }, [theme]);

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
    <div className="login-page">
      <div className="login-left">
        <div className="hero-content">
          <div className="brand-lockup">
            <img src="/images/hadion-icon.png" alt="" className="brand-icon" />
            <div className="brand-text">
              <span className="brand-name">Hadion<span className="brand-accent">ERP</span></span>
              <span className="brand-tagline">{t("auth.heroTagline", language)}</span>
            </div>
          </div>

          <div className="hero-headline">
            <h1>
              {t("auth.heroHeadlineLine1", language)}<br />
              {t("auth.heroHeadlineLine2", language)}{" "}
              <span className="accent">{t("auth.heroHeadlineAccent", language)}</span>
            </h1>
            <p>{t("auth.heroSubtext", language)}</p>
          </div>

          <div className="orbit" aria-hidden="true">
            <div className="orbit-ring" />
            <div className="orbit-center">
              <img src="/images/hadion-icon.png" alt="" />
            </div>
            {ORBIT_NODES.map((node) => (
              <div key={node.key} className="orbit-node" style={{ "--angle": node.angle } as React.CSSProperties}>
                <span className="orbit-node__badge" style={{ background: node.color }}>
                  <Icon icon={node.icon} size={20} />
                </span>
                <span className="orbit-node__label">{t(node.labelKey, language)}</span>
              </div>
            ))}
          </div>

          <div className="feature-strip">
            <div className="feature">
              <span className="feature__icon"><Icon icon="shield" /></span>
              <div>
                <strong>{t("auth.featureSecureTitle", language)}</strong>
                <p>{t("auth.featureSecureDesc", language)}</p>
              </div>
            </div>
            <div className="feature">
              <span className="feature__icon"><Icon icon="cloud" /></span>
              <div>
                <strong>{t("auth.featureCloudTitle", language)}</strong>
                <p>{t("auth.featureCloudDesc", language)}</p>
              </div>
            </div>
            <div className="feature">
              <span className="feature__icon"><Icon icon="chartLine" /></span>
              <div>
                <strong>{t("auth.featureRealTimeTitle", language)}</strong>
                <p>{t("auth.featureRealTimeDesc", language)}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="login-right">
        <div className="top-controls">
          <label className="language-select">
            <Icon icon="globe" size={16} />
            <select value={language} onChange={(e) => onLanguageChange(e.target.value as SupportedLanguageCode)}>
              {languages.map((l) => <option key={l.code} value={l.code}>{l.label}</option>)}
            </select>
          </label>
          <button
            type="button"
            className="theme-toggle"
            onClick={() => setTheme((prev) => (prev === "dark" ? "light" : "dark"))}
            aria-label={t(theme === "dark" ? "auth.themeToggleToLight" : "auth.themeToggleToDark", language)}
            title={t(theme === "dark" ? "auth.themeToggleToLight" : "auth.themeToggleToDark", language)}
          >
            <Icon icon={theme === "dark" ? "sun" : "moon"} />
          </button>
        </div>

        <form className="login-card" onSubmit={handleSubmit}>
          <h2>{t("auth.welcomeHeading", language)} 👋</h2>
          <p className="subtitle">{t("auth.welcomeSubtitle", language)}</p>

          {error && <p className="login-error">{error}</p>}

          <label>
            {t("auth.organizationLabel", language)}
            <div className="input-with-icon">
              <Icon icon="building" />
              <select disabled>
                <option>{t("auth.organizationValue", language)}</option>
              </select>
            </div>
          </label>

          <label>
            {t("auth.usernameLabel", language)}
            <div className="input-with-icon">
              <Icon icon="user" />
              <input
                autoFocus
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder={t("auth.usernamePlaceholder", language)}
                disabled={busy}
              />
            </div>
          </label>

          <label>
            {t("auth.passwordLabel", language)}
            <div className="input-with-icon">
              <Icon icon="lock" />
              <input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={t("auth.passwordPlaceholder", language)}
                disabled={busy}
              />
              <button
                type="button"
                className="reveal-password"
                onClick={() => setShowPassword((v) => !v)}
                aria-label={t(showPassword ? "auth.hidePassword" : "auth.showPassword", language)}
              >
                <Icon icon={showPassword ? "eyeOff" : "eye"} size={16} />
              </button>
            </div>
          </label>

          <div className="row-between">
            <label className="remember-me">
              <input type="checkbox" checked={rememberMe} onChange={(e) => setRememberMe(e.target.checked)} />
              {t("auth.rememberMe", language)}
            </label>
            <span className="forgot-password" title={t("auth.forgotPasswordUnavailableHint", language)}>
              {t("auth.forgotPassword", language)}
            </span>
          </div>

          <button type="submit" className="sign-in-button" disabled={busy || !username || !password}>
            {t("auth.loginButton", language)} <Icon icon="arrowRight" />
          </button>

          <div className="divider"><span>{t("auth.orContinueWith", language)}</span></div>

          <div className="sso-row">
            <button type="button" className="sso-button" disabled title={t("auth.ssoUnavailableHint", language)}>
              <Icon icon="microsoft" size={16} /> {t("auth.continueWithMicrosoft", language)}
            </button>
            <button type="button" className="sso-button" disabled title={t("auth.ssoUnavailableHint", language)}>
              <Icon icon="google" size={16} /> {t("auth.continueWithGoogle", language)}
            </button>
          </div>
        </form>

        <div className="login-footer">
          <p>
            <Icon icon="shield" size={14} /> {t("auth.securityFooter", language)}
            <span className="dot">•</span>{t("auth.privacyPolicy", language)}
            <span className="dot">•</span>{t("auth.termsOfUse", language)}
          </p>
          <p className="version-line">HadionERP <span className="dot">•</span> {t("auth.versionLabel", language)}</p>
        </div>
      </div>
    </div>
  );
}
