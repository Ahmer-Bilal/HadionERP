import type { SupportedLanguageCode } from "./language";

// The frontend's equivalent of Platform.Localization/LocalizationDefaults.cs on the backend: the one
// place literal display text lives, structured so a real i18n library (e.g. i18next, once Platform.UI's
// design system is built out) can read these same keys later without a rewrite. Component code calls
// t(key, language) — it never embeds a literal string itself.
type TranslationKey =
  | "shell.title"
  | "shell.tagline"
  | "shell.footer"
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
  | "nav.chartOfAccountsArea"
  | "nav.allGLAccounts"
  | "nav.itemsArea"
  | "bp.heading"
  | "bp.newHeading"
  | "bp.emptyState"
  | "bp.actionNew"
  | "bp.actionCreate"
  | "bp.actionBack"
  | "bp.actionSubmit"
  | "bp.actionApprove"
  | "bp.actionReject"
  | "bp.columnDocumentNumber"
  | "bp.columnName"
  | "bp.columnType"
  | "bp.columnStatus"
  | "bp.tabAddresses"
  | "bp.tabContacts"
  | "bp.tabAttachments"
  | "bp.actionUpload"
  | "bp.actionDownload"
  | "bp.actionDelete"
  | "bp.columnFileName"
  | "bp.columnFileSize"
  | "bp.columnUploadedBy"
  | "bp.columnUploadedAt"
  | "bp.emptyAttachments"
  | "bp.tabNotes"
  | "bp.actionAddNote"
  | "bp.fieldNoteText"
  | "bp.columnNoteCreatedBy"
  | "bp.columnNoteCreatedAt"
  | "bp.emptyNotes"
  | "bp.fieldName"
  | "bp.fieldNameArabic"
  | "bp.fieldPartnerType"
  | "bp.fieldTaxRegistrationNumber"
  | "bp.fieldEmail"
  | "bp.fieldPhone"
  | "bp.fieldCountry"
  | "bp.fieldCity"
  | "bp.fieldAddressLine"
  | "bp.fieldAddressType"
  | "bp.fieldJobTitle"
  | "bp.fieldContactName"
  | "bp.actionAddAddress"
  | "bp.actionAddContact"
  | "bp.emptyAddresses"
  | "bp.emptyContacts"
  | "bp.partnerTypeCustomer"
  | "bp.partnerTypeVendor"
  | "bp.partnerTypeBoth"
  | "bp.addressTypeHeadOffice"
  | "bp.addressTypeBilling"
  | "bp.addressTypeShipping"
  | "bp.addressTypeSiteOffice"
  | "bp.statusDraft"
  | "bp.statusSubmitted"
  | "bp.statusInApproval"
  | "bp.statusApproved"
  | "bp.statusRejected"
  | "bp.statusCancelled"
  | "bp.statusReversed"
  | "gl.heading"
  | "gl.newHeading"
  | "gl.emptyState"
  | "gl.actionNew"
  | "gl.actionCreate"
  | "gl.actionBack"
  | "gl.actionSubmit"
  | "gl.actionApprove"
  | "gl.actionReject"
  | "gl.columnCode"
  | "gl.columnName"
  | "gl.columnType"
  | "gl.columnNormalBalance"
  | "gl.columnStatus"
  | "gl.fieldAccountCode"
  | "gl.fieldAccountName"
  | "gl.fieldAccountNameArabic"
  | "gl.fieldAccountType"
  | "gl.fieldParentAccount"
  | "gl.fieldIsPostable"
  | "gl.noParent"
  | "gl.accountTypeAsset"
  | "gl.accountTypeLiability"
  | "gl.accountTypeEquity"
  | "gl.accountTypeRevenue"
  | "gl.accountTypeExpense"
  | "nav.allItems"
  | "item.heading"
  | "item.newHeading"
  | "item.emptyState"
  | "item.actionNew"
  | "item.actionCreate"
  | "item.actionBack"
  | "item.actionSubmit"
  | "item.actionApprove"
  | "item.actionReject"
  | "item.columnCode"
  | "item.columnName"
  | "item.columnType"
  | "item.columnUnitOfMeasure"
  | "item.columnStatus"
  | "item.fieldItemCode"
  | "item.fieldItemName"
  | "item.fieldItemNameArabic"
  | "item.fieldItemType"
  | "item.fieldUnitOfMeasure"
  | "item.itemTypeStock"
  | "item.itemTypeNonStock"
  | "item.itemTypeService";

const content: Record<TranslationKey, Record<SupportedLanguageCode, string>> = {
  "shell.title": { en: "HadionERP", ar: "HadionERP" },
  "shell.tagline": { en: "by hAdisHere", ar: "من hAdisHere" },
  "shell.footer": {
    en: "HadionERP by hAdisHere — Created by aHmAr",
    ar: "HadionERP من hAdisHere — صُنع بواسطة aHmAr",
  },
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
  "nav.chartOfAccountsArea": { en: "Chart of Accounts", ar: "دليل الحسابات" },
  "nav.allGLAccounts": { en: "All Accounts", ar: "جميع الحسابات" },
  "nav.itemsArea": { en: "Items", ar: "الأصناف" },
  "bp.heading": { en: "Business Partners", ar: "شركاء الأعمال" },
  "bp.newHeading": { en: "New Business Partner", ar: "شريك أعمال جديد" },
  "bp.emptyState": { en: "No business partners yet.", ar: "لا يوجد شركاء أعمال حتى الآن." },
  "bp.actionNew": { en: "New", ar: "جديد" },
  "bp.actionCreate": { en: "Create", ar: "إنشاء" },
  "bp.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "bp.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "bp.actionApprove": { en: "Approve", ar: "اعتماد" },
  "bp.actionReject": { en: "Reject", ar: "رفض" },
  "bp.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "bp.columnName": { en: "Name", ar: "الاسم" },
  "bp.columnType": { en: "Type", ar: "النوع" },
  "bp.columnStatus": { en: "Status", ar: "الحالة" },
  "bp.tabAddresses": { en: "Addresses", ar: "العناوين" },
  "bp.tabContacts": { en: "Contacts", ar: "جهات الاتصال" },
  "bp.tabAttachments": { en: "Attachments", ar: "المرفقات" },
  "bp.actionUpload": { en: "Upload", ar: "رفع" },
  "bp.actionDownload": { en: "Download", ar: "تنزيل" },
  "bp.actionDelete": { en: "Delete", ar: "حذف" },
  "bp.columnFileName": { en: "File Name", ar: "اسم الملف" },
  "bp.columnFileSize": { en: "Size", ar: "الحجم" },
  "bp.columnUploadedBy": { en: "Uploaded By", ar: "رفعه" },
  "bp.columnUploadedAt": { en: "Uploaded At", ar: "تاريخ الرفع" },
  "bp.emptyAttachments": { en: "No attachments yet.", ar: "لا توجد مرفقات حتى الآن." },
  "bp.tabNotes": { en: "Notes", ar: "الملاحظات" },
  "bp.actionAddNote": { en: "Add Note", ar: "إضافة ملاحظة" },
  "bp.fieldNoteText": { en: "Note", ar: "ملاحظة" },
  "bp.columnNoteCreatedBy": { en: "By", ar: "بواسطة" },
  "bp.columnNoteCreatedAt": { en: "Date", ar: "التاريخ" },
  "bp.emptyNotes": { en: "No notes yet.", ar: "لا توجد ملاحظات حتى الآن." },
  "bp.fieldName": { en: "Name", ar: "الاسم" },
  "bp.fieldNameArabic": { en: "Name (Arabic)", ar: "الاسم بالعربية" },
  "bp.fieldPartnerType": { en: "Partner Type", ar: "نوع الشريك" },
  "bp.fieldTaxRegistrationNumber": { en: "Tax Registration Number", ar: "الرقم الضريبي" },
  "bp.fieldEmail": { en: "Email", ar: "البريد الإلكتروني" },
  "bp.fieldPhone": { en: "Phone", ar: "الهاتف" },
  "bp.fieldCountry": { en: "Country", ar: "الدولة" },
  "bp.fieldCity": { en: "City", ar: "المدينة" },
  "bp.fieldAddressLine": { en: "Address", ar: "العنوان" },
  "bp.fieldAddressType": { en: "Address Type", ar: "نوع العنوان" },
  "bp.fieldJobTitle": { en: "Job Title", ar: "المسمى الوظيفي" },
  "bp.fieldContactName": { en: "Contact Name", ar: "اسم جهة الاتصال" },
  "bp.actionAddAddress": { en: "Add Address", ar: "إضافة عنوان" },
  "bp.actionAddContact": { en: "Add Contact", ar: "إضافة جهة اتصال" },
  "bp.emptyAddresses": { en: "No addresses yet.", ar: "لا توجد عناوين حتى الآن." },
  "bp.emptyContacts": { en: "No contacts yet.", ar: "لا توجد جهات اتصال حتى الآن." },
  "bp.partnerTypeCustomer": { en: "Customer", ar: "عميل" },
  "bp.partnerTypeVendor": { en: "Vendor", ar: "مورّد" },
  "bp.partnerTypeBoth": { en: "Customer & Vendor", ar: "عميل ومورّد" },
  "bp.addressTypeHeadOffice": { en: "Head Office", ar: "المكتب الرئيسي" },
  "bp.addressTypeBilling": { en: "Billing", ar: "الفوترة" },
  "bp.addressTypeShipping": { en: "Shipping", ar: "الشحن" },
  "bp.addressTypeSiteOffice": { en: "Site Office", ar: "مكتب الموقع" },
  "bp.statusDraft": { en: "Draft", ar: "مسودة" },
  "bp.statusSubmitted": { en: "Submitted", ar: "مُرسل" },
  "bp.statusInApproval": { en: "In Approval", ar: "قيد الاعتماد" },
  "bp.statusApproved": { en: "Approved", ar: "معتمد" },
  "bp.statusRejected": { en: "Rejected", ar: "مرفوض" },
  "bp.statusCancelled": { en: "Cancelled", ar: "ملغى" },
  "bp.statusReversed": { en: "Reversed", ar: "معكوس" },
  "gl.heading": { en: "Chart of Accounts", ar: "دليل الحسابات" },
  "gl.newHeading": { en: "New G/L Account", ar: "حساب جديد" },
  "gl.emptyState": { en: "No G/L accounts yet.", ar: "لا توجد حسابات حتى الآن." },
  "gl.actionNew": { en: "New", ar: "جديد" },
  "gl.actionCreate": { en: "Create", ar: "إنشاء" },
  "gl.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "gl.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "gl.actionApprove": { en: "Approve", ar: "اعتماد" },
  "gl.actionReject": { en: "Reject", ar: "رفض" },
  "gl.columnCode": { en: "Code", ar: "الرمز" },
  "gl.columnName": { en: "Account Name", ar: "اسم الحساب" },
  "gl.columnType": { en: "Type", ar: "النوع" },
  "gl.columnNormalBalance": { en: "Normal Balance", ar: "الرصيد الطبيعي" },
  "gl.columnStatus": { en: "Status", ar: "الحالة" },
  "gl.fieldAccountCode": { en: "Account Code", ar: "رمز الحساب" },
  "gl.fieldAccountName": { en: "Account Name", ar: "اسم الحساب" },
  "gl.fieldAccountNameArabic": { en: "Account Name (Arabic)", ar: "اسم الحساب بالعربية" },
  "gl.fieldAccountType": { en: "Account Type", ar: "نوع الحساب" },
  "gl.fieldParentAccount": { en: "Parent Account", ar: "الحساب الأصلي" },
  "gl.fieldIsPostable": { en: "Postable", ar: "قابل للترحيل" },
  "gl.noParent": { en: "— None (top level) —", ar: "— لا يوجد (مستوى أعلى) —" },
  "gl.accountTypeAsset": { en: "Asset", ar: "أصول" },
  "gl.accountTypeLiability": { en: "Liability", ar: "خصوم" },
  "gl.accountTypeEquity": { en: "Equity", ar: "حقوق ملكية" },
  "gl.accountTypeRevenue": { en: "Revenue", ar: "إيرادات" },
  "gl.accountTypeExpense": { en: "Expense", ar: "مصروفات" },
  "nav.allItems": { en: "All Items", ar: "جميع الأصناف" },
  "item.heading": { en: "Items", ar: "الأصناف" },
  "item.newHeading": { en: "New Item", ar: "صنف جديد" },
  "item.emptyState": { en: "No items yet.", ar: "لا توجد أصناف حتى الآن." },
  "item.actionNew": { en: "New", ar: "جديد" },
  "item.actionCreate": { en: "Create", ar: "إنشاء" },
  "item.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "item.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "item.actionApprove": { en: "Approve", ar: "اعتماد" },
  "item.actionReject": { en: "Reject", ar: "رفض" },
  "item.columnCode": { en: "Code", ar: "الرمز" },
  "item.columnName": { en: "Item Name", ar: "اسم الصنف" },
  "item.columnType": { en: "Type", ar: "النوع" },
  "item.columnUnitOfMeasure": { en: "Unit", ar: "الوحدة" },
  "item.columnStatus": { en: "Status", ar: "الحالة" },
  "item.fieldItemCode": { en: "Item Code", ar: "رمز الصنف" },
  "item.fieldItemName": { en: "Item Name", ar: "اسم الصنف" },
  "item.fieldItemNameArabic": { en: "Item Name (Arabic)", ar: "اسم الصنف بالعربية" },
  "item.fieldItemType": { en: "Item Type", ar: "نوع الصنف" },
  "item.fieldUnitOfMeasure": { en: "Unit of Measure", ar: "وحدة القياس" },
  "item.itemTypeStock": { en: "Stock", ar: "مخزون" },
  "item.itemTypeNonStock": { en: "Non-Stock", ar: "غير مخزون" },
  "item.itemTypeService": { en: "Service", ar: "خدمة" },
};

export function t(key: TranslationKey, language: SupportedLanguageCode): string {
  return content[key][language];
}
