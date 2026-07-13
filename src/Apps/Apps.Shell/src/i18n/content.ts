import type { SupportedLanguageCode } from "./language";

// The frontend's equivalent of Platform.Localization/LocalizationDefaults.cs on the backend: the one
// place literal display text lives, structured so a real i18n library (e.g. i18next, once Platform.UI's
// design system is built out) can read these same keys later without a rewrite. Component code calls
// t(key, language) — it never embeds a literal string itself.
type TranslationKey =
  | "shell.title"
  | "nav.platformAdministration"
  | "nav.system"
  | "nav.systemStatus"
  | "status.heading"
  | "status.loading"
  | "status.error"
  | "status.applicationLabel"
  | "status.phaseLabel"
  | "status.kernelServicesLabel"
  | "status.greetingHeading";

const content: Record<TranslationKey, Record<SupportedLanguageCode, string>> = {
  "shell.title": { en: "ERP Platform", ar: "منصة تخطيط الموارد" },
  "nav.platformAdministration": { en: "Platform Administration", ar: "إدارة المنصة" },
  "nav.system": { en: "System", ar: "النظام" },
  "nav.systemStatus": { en: "System Status", ar: "حالة النظام" },
  "status.heading": { en: "System Status", ar: "حالة النظام" },
  "status.loading": { en: "Loading…", ar: "جارٍ التحميل…" },
  "status.error": { en: "Could not reach the backend.", ar: "تعذر الوصول إلى الخادم." },
  "status.applicationLabel": { en: "Application", ar: "التطبيق" },
  "status.phaseLabel": { en: "Current phase", ar: "المرحلة الحالية" },
  "status.kernelServicesLabel": { en: "Kernel services wired", ar: "خدمات النواة المفعّلة" },
  "status.greetingHeading": { en: "Localization check", ar: "فحص التوطين" },
};

export function t(key: TranslationKey, language: SupportedLanguageCode): string {
  return content[key][language];
}
