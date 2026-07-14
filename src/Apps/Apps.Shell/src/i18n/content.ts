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
  | "nav.homeModule"
  | "nav.homeArea"
  | "nav.home"
  | "home.heading"
  | "home.totalLabel"
  | "home.pendingApprovalLabel"
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
  | "bp.columnRoles"
  | "bp.columnStatus"
  | "bp.tabAddresses"
  | "bp.tabContacts"
  | "bp.tabBusinessRoles"
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
  | "bp.fieldBusinessRole"
  | "bp.fieldTrade"
  | "bp.actionAddRole"
  | "bp.actionRemoveRole"
  | "bp.columnRoleType"
  | "bp.columnTrade"
  | "bp.emptyBusinessRoles"
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
  | "bp.roleClient"
  | "bp.roleSupplier"
  | "bp.roleSubcontractor"
  | "bp.roleConsultant"
  | "bp.roleJointVenturePartner"
  | "bp.roleGovernmentAuthority"
  | "bp.roleRentalCompany"
  | "bp.roleManufacturer"
  | "bp.roleManpowerSupplier"
  | "bp.roleTestingLaboratory"
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
  | "item.itemTypeService"
  | "nav.costCentersArea"
  | "nav.allCostCenters"
  | "cc.heading"
  | "cc.newHeading"
  | "cc.emptyState"
  | "cc.actionNew"
  | "cc.actionCreate"
  | "cc.actionBack"
  | "cc.actionSubmit"
  | "cc.actionApprove"
  | "cc.actionReject"
  | "cc.columnCode"
  | "cc.columnName"
  | "cc.columnStatus"
  | "cc.fieldCostCenterCode"
  | "cc.fieldCostCenterName"
  | "cc.fieldCostCenterNameArabic"
  | "cc.fieldParentCostCenter"
  | "cc.fieldIsPostable"
  | "cc.noParent"
  | "nav.taxCodesArea"
  | "nav.allTaxCodes"
  | "tax.heading"
  | "tax.newHeading"
  | "tax.emptyState"
  | "tax.actionNew"
  | "tax.actionCreate"
  | "tax.actionBack"
  | "tax.actionSubmit"
  | "tax.actionApprove"
  | "tax.actionReject"
  | "tax.columnCode"
  | "tax.columnName"
  | "tax.columnRate"
  | "tax.columnType"
  | "tax.columnStatus"
  | "tax.fieldTaxCodeCode"
  | "tax.fieldTaxCodeName"
  | "tax.fieldTaxCodeNameArabic"
  | "tax.fieldRate"
  | "tax.fieldTaxType"
  | "tax.taxTypeStandard"
  | "tax.taxTypeZeroRated"
  | "tax.taxTypeExempt"
  | "nav.financeModule"
  | "nav.journalEntriesArea"
  | "nav.allJournalEntries"
  | "je.heading"
  | "je.newHeading"
  | "je.emptyState"
  | "je.actionNew"
  | "je.actionCreate"
  | "je.actionBack"
  | "je.actionSubmit"
  | "je.actionApprove"
  | "je.actionReject"
  | "je.actionPost"
  | "je.actionReverse"
  | "je.columnDocumentNumber"
  | "je.columnPostingDate"
  | "je.columnDescription"
  | "je.columnStatus"
  | "je.columnTotalDebits"
  | "je.columnTotalCredits"
  | "je.fieldPostingDate"
  | "je.fieldDescription"
  | "je.fieldGLAccount"
  | "je.fieldDebit"
  | "je.fieldCredit"
  | "je.fieldLineDescription"
  | "je.actionAddLine"
  | "je.balanced"
  | "je.unbalanced"
  | "je.reversalOf"
  | "je.statusPosted"
  | "je.statusReversed"
  | "nav.apInvoicesArea"
  | "nav.allAPInvoices"
  | "ap.heading"
  | "ap.newHeading"
  | "ap.emptyState"
  | "ap.actionNew"
  | "ap.actionCreate"
  | "ap.actionBack"
  | "ap.actionSubmit"
  | "ap.actionApprove"
  | "ap.actionReject"
  | "ap.actionPost"
  | "ap.actionReverse"
  | "ap.columnDocumentNumber"
  | "ap.columnVendorInvoiceNumber"
  | "ap.columnInvoiceDate"
  | "ap.columnVendor"
  | "ap.columnGrossAmount"
  | "ap.columnStatus"
  | "ap.fieldVendor"
  | "ap.fieldVendorInvoiceNumber"
  | "ap.fieldInvoiceDate"
  | "ap.fieldDescription"
  | "ap.fieldExpenseAccount"
  | "ap.fieldPayableAccount"
  | "ap.fieldTaxCode"
  | "ap.fieldVatAccount"
  | "ap.fieldNetAmount"
  | "ap.noTaxCode"
  | "ap.columnNetAmount"
  | "ap.columnTaxAmount"
  | "ap.linkedJournalEntry";

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
  "nav.homeModule": { en: "Home", ar: "الرئيسية" },
  "nav.homeArea": { en: "Overview", ar: "نظرة عامة" },
  "nav.home": { en: "Home", ar: "الرئيسية" },
  "home.heading": { en: "Home", ar: "الرئيسية" },
  "home.totalLabel": { en: "Total", ar: "الإجمالي" },
  "home.pendingApprovalLabel": { en: "pending approval", ar: "بانتظار الاعتماد" },
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
  "bp.columnRoles": { en: "Roles", ar: "الأدوار" },
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
  "bp.fieldBusinessRole": { en: "Business Role", ar: "دور العمل" },
  "bp.fieldTrade": { en: "Trade / Specialty", ar: "التخصص" },
  "bp.actionAddRole": { en: "Add Role", ar: "إضافة دور" },
  "bp.actionRemoveRole": { en: "Remove", ar: "إزالة" },
  "bp.columnRoleType": { en: "Role", ar: "الدور" },
  "bp.columnTrade": { en: "Trade / Specialty", ar: "التخصص" },
  "bp.emptyBusinessRoles": { en: "No business roles yet.", ar: "لا توجد أدوار عمل حتى الآن." },
  "bp.tabBusinessRoles": { en: "Business Roles", ar: "أدوار العمل" },
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
  "bp.roleClient": { en: "Client", ar: "عميل" },
  "bp.roleSupplier": { en: "Supplier", ar: "مورّد" },
  "bp.roleSubcontractor": { en: "Subcontractor", ar: "مقاول من الباطن" },
  "bp.roleConsultant": { en: "Consultant", ar: "استشاري" },
  "bp.roleJointVenturePartner": { en: "Joint Venture Partner", ar: "شريك ائتلاف" },
  "bp.roleGovernmentAuthority": { en: "Government Authority", ar: "جهة حكومية" },
  "bp.roleRentalCompany": { en: "Rental Company", ar: "شركة تأجير" },
  "bp.roleManufacturer": { en: "Manufacturer", ar: "مُصنّع" },
  "bp.roleManpowerSupplier": { en: "Manpower Supplier", ar: "مورّد قوى عاملة" },
  "bp.roleTestingLaboratory": { en: "Testing Laboratory", ar: "مختبر فحص" },
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
  "nav.costCentersArea": { en: "Cost Centers", ar: "مراكز التكلفة" },
  "nav.allCostCenters": { en: "All Cost Centers", ar: "جميع مراكز التكلفة" },
  "cc.heading": { en: "Cost Centers", ar: "مراكز التكلفة" },
  "cc.newHeading": { en: "New Cost Center", ar: "مركز تكلفة جديد" },
  "cc.emptyState": { en: "No cost centers yet.", ar: "لا توجد مراكز تكلفة حتى الآن." },
  "cc.actionNew": { en: "New", ar: "جديد" },
  "cc.actionCreate": { en: "Create", ar: "إنشاء" },
  "cc.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "cc.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "cc.actionApprove": { en: "Approve", ar: "اعتماد" },
  "cc.actionReject": { en: "Reject", ar: "رفض" },
  "cc.columnCode": { en: "Code", ar: "الرمز" },
  "cc.columnName": { en: "Cost Center Name", ar: "اسم مركز التكلفة" },
  "cc.columnStatus": { en: "Status", ar: "الحالة" },
  "cc.fieldCostCenterCode": { en: "Cost Center Code", ar: "رمز مركز التكلفة" },
  "cc.fieldCostCenterName": { en: "Cost Center Name", ar: "اسم مركز التكلفة" },
  "cc.fieldCostCenterNameArabic": { en: "Cost Center Name (Arabic)", ar: "اسم مركز التكلفة بالعربية" },
  "cc.fieldParentCostCenter": { en: "Parent Cost Center", ar: "مركز التكلفة الأصلي" },
  "cc.fieldIsPostable": { en: "Postable", ar: "قابل للترحيل" },
  "cc.noParent": { en: "— None (top level) —", ar: "— لا يوجد (مستوى أعلى) —" },
  "nav.taxCodesArea": { en: "Tax Codes", ar: "الرموز الضريبية" },
  "nav.allTaxCodes": { en: "All Tax Codes", ar: "جميع الرموز الضريبية" },
  "tax.heading": { en: "Tax Codes", ar: "الرموز الضريبية" },
  "tax.newHeading": { en: "New Tax Code", ar: "رمز ضريبي جديد" },
  "tax.emptyState": { en: "No tax codes yet.", ar: "لا توجد رموز ضريبية حتى الآن." },
  "tax.actionNew": { en: "New", ar: "جديد" },
  "tax.actionCreate": { en: "Create", ar: "إنشاء" },
  "tax.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "tax.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "tax.actionApprove": { en: "Approve", ar: "اعتماد" },
  "tax.actionReject": { en: "Reject", ar: "رفض" },
  "tax.columnCode": { en: "Code", ar: "الرمز" },
  "tax.columnName": { en: "Tax Code Name", ar: "اسم الرمز الضريبي" },
  "tax.columnRate": { en: "Rate", ar: "النسبة" },
  "tax.columnType": { en: "Type", ar: "النوع" },
  "tax.columnStatus": { en: "Status", ar: "الحالة" },
  "tax.fieldTaxCodeCode": { en: "Tax Code", ar: "الرمز الضريبي" },
  "tax.fieldTaxCodeName": { en: "Tax Code Name", ar: "اسم الرمز الضريبي" },
  "tax.fieldTaxCodeNameArabic": { en: "Tax Code Name (Arabic)", ar: "اسم الرمز الضريبي بالعربية" },
  "tax.fieldRate": { en: "Rate (%)", ar: "النسبة (٪)" },
  "tax.fieldTaxType": { en: "Tax Type", ar: "نوع الضريبة" },
  "tax.taxTypeStandard": { en: "Standard", ar: "قياسي" },
  "tax.taxTypeZeroRated": { en: "Zero-Rated", ar: "صفري النسبة" },
  "tax.taxTypeExempt": { en: "Exempt", ar: "معفى" },
  "nav.financeModule": { en: "Finance", ar: "المالية" },
  "nav.journalEntriesArea": { en: "Journal Entries", ar: "قيود اليومية" },
  "nav.allJournalEntries": { en: "All Journal Entries", ar: "جميع قيود اليومية" },
  "je.heading": { en: "Journal Entries", ar: "قيود اليومية" },
  "je.newHeading": { en: "New Journal Entry", ar: "قيد يومية جديد" },
  "je.emptyState": { en: "No journal entries yet.", ar: "لا توجد قيود يومية حتى الآن." },
  "je.actionNew": { en: "New", ar: "جديد" },
  "je.actionCreate": { en: "Create", ar: "إنشاء" },
  "je.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "je.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "je.actionApprove": { en: "Approve", ar: "اعتماد" },
  "je.actionReject": { en: "Reject", ar: "رفض" },
  "je.actionPost": { en: "Post", ar: "ترحيل" },
  "je.actionReverse": { en: "Reverse", ar: "عكس القيد" },
  "je.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "je.columnPostingDate": { en: "Posting Date", ar: "تاريخ الترحيل" },
  "je.columnDescription": { en: "Description", ar: "الوصف" },
  "je.columnStatus": { en: "Status", ar: "الحالة" },
  "je.columnTotalDebits": { en: "Total Debits", ar: "إجمالي المدين" },
  "je.columnTotalCredits": { en: "Total Credits", ar: "إجمالي الدائن" },
  "je.fieldPostingDate": { en: "Posting Date", ar: "تاريخ الترحيل" },
  "je.fieldDescription": { en: "Description", ar: "الوصف" },
  "je.fieldGLAccount": { en: "G/L Account", ar: "الحساب" },
  "je.fieldDebit": { en: "Debit", ar: "مدين" },
  "je.fieldCredit": { en: "Credit", ar: "دائن" },
  "je.fieldLineDescription": { en: "Line Description", ar: "وصف البند" },
  "je.actionAddLine": { en: "Add Line", ar: "إضافة بند" },
  "je.balanced": { en: "Balanced", ar: "متوازن" },
  "je.unbalanced": { en: "Not Balanced", ar: "غير متوازن" },
  "je.reversalOf": { en: "Reversal of", ar: "عكس لـ" },
  "je.statusPosted": { en: "Posted", ar: "مُرحّل" },
  "je.statusReversed": { en: "Reversed", ar: "معكوس" },
  "nav.apInvoicesArea": { en: "AP Invoices", ar: "فواتير الموردين" },
  "nav.allAPInvoices": { en: "All AP Invoices", ar: "جميع فواتير الموردين" },
  "ap.heading": { en: "AP Invoices", ar: "فواتير الموردين" },
  "ap.newHeading": { en: "New AP Invoice", ar: "فاتورة مورّد جديدة" },
  "ap.emptyState": { en: "No AP invoices yet.", ar: "لا توجد فواتير موردين حتى الآن." },
  "ap.actionNew": { en: "New", ar: "جديد" },
  "ap.actionCreate": { en: "Create", ar: "إنشاء" },
  "ap.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "ap.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "ap.actionApprove": { en: "Approve", ar: "اعتماد" },
  "ap.actionReject": { en: "Reject", ar: "رفض" },
  "ap.actionPost": { en: "Post", ar: "ترحيل" },
  "ap.actionReverse": { en: "Reverse", ar: "عكس القيد" },
  "ap.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "ap.columnVendorInvoiceNumber": { en: "Vendor Invoice #", ar: "رقم فاتورة المورّد" },
  "ap.columnInvoiceDate": { en: "Invoice Date", ar: "تاريخ الفاتورة" },
  "ap.columnVendor": { en: "Vendor", ar: "المورّد" },
  "ap.columnGrossAmount": { en: "Gross Amount", ar: "الإجمالي" },
  "ap.columnStatus": { en: "Status", ar: "الحالة" },
  "ap.fieldVendor": { en: "Vendor", ar: "المورّد" },
  "ap.fieldVendorInvoiceNumber": { en: "Vendor Invoice Number", ar: "رقم فاتورة المورّد" },
  "ap.fieldInvoiceDate": { en: "Invoice Date", ar: "تاريخ الفاتورة" },
  "ap.fieldDescription": { en: "Description", ar: "الوصف" },
  "ap.fieldExpenseAccount": { en: "Expense Account", ar: "حساب المصروف" },
  "ap.fieldPayableAccount": { en: "Payable Account", ar: "حساب الدائنين" },
  "ap.fieldTaxCode": { en: "Tax Code", ar: "الرمز الضريبي" },
  "ap.fieldVatAccount": { en: "VAT Account", ar: "حساب ضريبة القيمة المضافة" },
  "ap.fieldNetAmount": { en: "Net Amount", ar: "المبلغ الصافي" },
  "ap.noTaxCode": { en: "— None —", ar: "— لا يوجد —" },
  "ap.columnNetAmount": { en: "Net", ar: "الصافي" },
  "ap.columnTaxAmount": { en: "Tax", ar: "الضريبة" },
  "ap.linkedJournalEntry": { en: "Linked Journal Entry", ar: "قيد اليومية المرتبط" },
};

export function t(key: TranslationKey, language: SupportedLanguageCode): string {
  return content[key][language];
}
