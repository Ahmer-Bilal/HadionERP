// Mirrors the language/direction model in Platform.Localization (src/Platform/Platform.Localization/
// SupportedLanguage.cs, TextDirection.cs) on the frontend side — Arabic is a first-class layout
// direction here too, not a mirrored afterthought (docs/architecture/02-business-object-model.md #4).

export type SupportedLanguageCode = "en" | "ar";

export type TextDirection = "ltr" | "rtl";

export function directionFor(language: SupportedLanguageCode): TextDirection {
  return language === "ar" ? "rtl" : "ltr";
}
