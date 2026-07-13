import type { SupportedLanguageCode } from "./language";

// Each language's own name for itself (autonym) — always shown the same way regardless of which
// language is currently active (a language switcher conventionally shows "English" and "العربية" in
// their own scripts no matter what's selected), so this is a distinct, fixed lookup table, not a
// translation that varies with the active language. Still centralized here rather than inlined in a
// component, for the same reason as everything else in src/i18n: one designated place for literal text.
export const LANGUAGE_NAMES: Record<SupportedLanguageCode, string> = {
  en: "English",
  ar: "العربية",
};
