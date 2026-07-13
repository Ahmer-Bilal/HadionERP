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
  | "status.greetingHeading"
  | "status.eventsOutboxLabel"
  | "status.auditLabel"
  | "status.auditChainValid"
  | "status.auditChainBroken"
  | "status.tabGeneral"
  | "status.tabEventsAudit"
  | "status.tabLocalization"
  | "status.actionRefresh"
  | "status.defaultLanguageLabel"
  | "status.verboseStatusLabel"
  | "aria.languageSwitchGroup"
  | "aria.navigationLandmark"
  | "aria.actionToolbar"
  | "nav.masterData"
  | "nav.businessPartnersArea"
  | "nav.allBusinessPartners"
  | "bp.heading"
  | "bp.newHeading"
  | "bp.emptyState"
  | "bp.actionNew"
  | "bp.actionCreate"
  | "bp.actionBack"
  | "bp.actionSubmit"
  | "bp.actionApprove"
  | "bp.columnDocumentNumber"
  | "bp.columnName"
  | "bp.columnType"
  | "bp.columnStatus"
  | "bp.tabContact"
  | "bp.fieldName"
  | "bp.fieldPartnerType"
  | "bp.fieldTaxRegistrationNumber"
  | "bp.fieldEmail"
  | "bp.fieldPhone"
  | "bp.fieldCountry"
  | "bp.fieldCity"
  | "bp.fieldAddressLine"
  | "bp.partnerTypeCustomer"
  | "bp.partnerTypeVendor"
  | "bp.partnerTypeBoth"
  | "bp.statusDraft"
  | "bp.statusSubmitted"
  | "bp.statusInApproval"
  | "bp.statusApproved"
  | "bp.statusRejected"
  | "bp.statusCancelled"
  | "bp.statusReversed";

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
  "status.eventsOutboxLabel": { en: "Events published / pending", ar: "الأحداث المنشورة / المعلّقة" },
  "status.auditLabel": { en: "Audit entries", ar: "سجلات التدقيق" },
  "status.auditChainValid": { en: "chain intact", ar: "السلسلة سليمة" },
  "status.auditChainBroken": { en: "CHAIN BROKEN", ar: "السلسلة مكسورة" },
  "status.tabGeneral": { en: "General", ar: "عام" },
  "status.tabEventsAudit": { en: "Events & audit", ar: "الأحداث والتدقيق" },
  "status.tabLocalization": { en: "Localization", ar: "التوطين" },
  "status.actionRefresh": { en: "Refresh", ar: "تحديث" },
  "status.defaultLanguageLabel": { en: "Default language", ar: "اللغة الافتراضية" },
  "status.verboseStatusLabel": { en: "Verbose status", ar: "الحالة التفصيلية" },
  "aria.languageSwitchGroup": { en: "Language", ar: "اللغة" },
  "aria.navigationLandmark": { en: "Main", ar: "الرئيسية" },
  "aria.actionToolbar": { en: "Actions", ar: "إجراءات" },
  "nav.masterData": { en: "Master Data", ar: "البيانات الأساسية" },
  "nav.businessPartnersArea": { en: "Business Partners", ar: "شركاء الأعمال" },
  "nav.allBusinessPartners": { en: "All Business Partners", ar: "جميع شركاء الأعمال" },
  "bp.heading": { en: "Business Partners", ar: "شركاء الأعمال" },
  "bp.newHeading": { en: "New Business Partner", ar: "شريك أعمال جديد" },
  "bp.emptyState": { en: "No business partners yet.", ar: "لا يوجد شركاء أعمال حتى الآن." },
  "bp.actionNew": { en: "New", ar: "جديد" },
  "bp.actionCreate": { en: "Create", ar: "إنشاء" },
  "bp.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "bp.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "bp.actionApprove": { en: "Approve", ar: "اعتماد" },
  "bp.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "bp.columnName": { en: "Name", ar: "الاسم" },
  "bp.columnType": { en: "Type", ar: "النوع" },
  "bp.columnStatus": { en: "Status", ar: "الحالة" },
  "bp.tabContact": { en: "Contact", ar: "بيانات التواصل" },
  "bp.fieldName": { en: "Name", ar: "الاسم" },
  "bp.fieldPartnerType": { en: "Partner Type", ar: "نوع الشريك" },
  "bp.fieldTaxRegistrationNumber": { en: "Tax Registration Number", ar: "الرقم الضريبي" },
  "bp.fieldEmail": { en: "Email", ar: "البريد الإلكتروني" },
  "bp.fieldPhone": { en: "Phone", ar: "الهاتف" },
  "bp.fieldCountry": { en: "Country", ar: "الدولة" },
  "bp.fieldCity": { en: "City", ar: "المدينة" },
  "bp.fieldAddressLine": { en: "Address", ar: "العنوان" },
  "bp.partnerTypeCustomer": { en: "Customer", ar: "عميل" },
  "bp.partnerTypeVendor": { en: "Vendor", ar: "مورّد" },
  "bp.partnerTypeBoth": { en: "Customer & Vendor", ar: "عميل ومورّد" },
  "bp.statusDraft": { en: "Draft", ar: "مسودة" },
  "bp.statusSubmitted": { en: "Submitted", ar: "مُرسل" },
  "bp.statusInApproval": { en: "In Approval", ar: "قيد الاعتماد" },
  "bp.statusApproved": { en: "Approved", ar: "معتمد" },
  "bp.statusRejected": { en: "Rejected", ar: "مرفوض" },
  "bp.statusCancelled": { en: "Cancelled", ar: "ملغى" },
  "bp.statusReversed": { en: "Reversed", ar: "معكوس" },
};

export function t(key: TranslationKey, language: SupportedLanguageCode): string {
  return content[key][language];
}
