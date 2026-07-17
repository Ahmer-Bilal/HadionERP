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
  | "bp.selectHint"
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
  | "nav.arInvoicesArea"
  | "nav.allARInvoices"
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
  | "ap.linkedJournalEntry"
  | "ap.columnOutstandingBalance"
  | "ar.heading"
  | "ar.newHeading"
  | "ar.emptyState"
  | "ar.actionNew"
  | "ar.actionCreate"
  | "ar.actionBack"
  | "ar.actionSubmit"
  | "ar.actionApprove"
  | "ar.actionReject"
  | "ar.actionPost"
  | "ar.actionReverse"
  | "ar.columnDocumentNumber"
  | "ar.columnInvoiceDate"
  | "ar.columnCustomer"
  | "ar.columnGrossAmount"
  | "ar.columnStatus"
  | "ar.fieldCustomer"
  | "ar.fieldCustomerReference"
  | "ar.fieldInvoiceDate"
  | "ar.fieldDescription"
  | "ar.fieldRevenueAccount"
  | "ar.fieldReceivableAccount"
  | "ar.fieldTaxCode"
  | "ar.fieldVatAccount"
  | "ar.fieldNetAmount"
  | "ar.noTaxCode"
  | "ar.columnNetAmount"
  | "ar.columnTaxAmount"
  | "ar.linkedJournalEntry"
  | "ar.columnOutstandingBalance"
  | "nav.bankAccountsArea"
  | "nav.allBankAccounts"
  | "bank.heading"
  | "bank.newHeading"
  | "bank.emptyState"
  | "bank.actionNew"
  | "bank.actionCreate"
  | "bank.actionBack"
  | "bank.actionSubmit"
  | "bank.actionApprove"
  | "bank.actionReject"
  | "bank.columnAccountCode"
  | "bank.columnAccountName"
  | "bank.columnBankName"
  | "bank.columnStatus"
  | "bank.fieldAccountCode"
  | "bank.fieldAccountName"
  | "bank.fieldAccountNameArabic"
  | "bank.fieldBankName"
  | "bank.fieldIban"
  | "bank.fieldLinkedGLAccount"
  | "nav.paymentsArea"
  | "nav.allPayments"
  | "pay.heading"
  | "pay.newHeading"
  | "pay.emptyState"
  | "pay.actionNew"
  | "pay.actionCreate"
  | "pay.actionBack"
  | "pay.actionSubmit"
  | "pay.actionApprove"
  | "pay.actionReject"
  | "pay.actionPost"
  | "pay.actionReverse"
  | "pay.columnDocumentNumber"
  | "pay.columnAmount"
  | "pay.columnStatus"
  | "pay.columnInvoice"
  | "pay.columnOutstandingBalance"
  | "pay.fieldVendor"
  | "pay.fieldBankAccount"
  | "pay.fieldPaymentMethod"
  | "pay.fieldPaymentDate"
  | "pay.fieldReference"
  | "pay.fieldAllocatedAmount"
  | "pay.tabAllocations"
  | "pay.noOutstandingInvoices"
  | "nav.procurementModule"
  | "nav.vendorPrequalificationsArea"
  | "nav.allVendorPrequalifications"
  | "nav.allPurchaseRequisitions"
  | "nav.allRequestsForQuotation"
  | "nav.purchaseOrdersArea"
  | "nav.allPurchaseOrders"
  | "nav.goodsReceiptNotesArea"
  | "nav.allGoodsReceiptNotes"
  | "nav.projectManagementModule"
  | "nav.projectsArea"
  | "nav.allProjects"
  | "nav.constructionModule"
  | "nav.contractsArea"
  | "nav.allContracts"
  | "nav.subcontractsArea"
  | "nav.allSubcontracts"
  | "nav.measurementSheetsArea"
  | "nav.allMeasurementSheets"
  | "nav.ipcsArea"
  | "nav.allIpcs"
  | "nav.lookupDataArea"
  | "nav.allLookupTypes"
  | "nav.lookupCountries"
  | "nav.lookupBusinessRoleTypes"
  | "nav.lookupAddressTypes"
  | "nav.lookupUnitsOfMeasure"
  | "nav.lookupSubcontractorTrades"
  | "nav.lookupSupplierTrades"
  | "nav.lookupConsultantTrades"
  | "vpq.heading"
  | "vpq.newHeading"
  | "vpq.emptyState"
  | "vpq.actionNew"
  | "vpq.actionCreate"
  | "vpq.actionBack"
  | "vpq.actionSubmit"
  | "vpq.actionApprove"
  | "vpq.actionReject"
  | "vpq.columnDocumentNumber"
  | "vpq.columnVendor"
  | "vpq.columnRoleType"
  | "vpq.columnTrade"
  | "vpq.columnValidFrom"
  | "vpq.columnValidUntil"
  | "vpq.columnStatus"
  | "vpq.fieldVendor"
  | "vpq.fieldRoleType"
  | "vpq.fieldTrade"
  | "vpq.noTrade"
  | "nav.purchaseRequisitionsArea"
  | "pr.heading"
  | "pr.newHeading"
  | "pr.emptyState"
  | "pr.actionNew"
  | "pr.actionCreate"
  | "pr.actionBack"
  | "pr.actionSubmit"
  | "pr.actionApprove"
  | "pr.actionReject"
  | "pr.actionAddLine"
  | "pr.actionRemoveLine"
  | "pr.columnDocumentNumber"
  | "pr.columnDescription"
  | "pr.columnRequiredByDate"
  | "pr.columnEstimatedTotal"
  | "pr.columnStatus"
  | "pr.fieldDescription"
  | "pr.fieldRequiredByDate"
  | "pr.fieldItem"
  | "pr.fieldCostCenter"
  | "pr.fieldQuantity"
  | "pr.fieldEstimatedUnitPrice"
  | "pr.fieldLineDescription"
  | "pr.columnLineTotal"
  | "nav.requestsForQuotationArea"
  | "rfq.heading"
  | "rfq.newHeading"
  | "rfq.emptyState"
  | "rfq.actionNew"
  | "rfq.actionCreate"
  | "rfq.actionBack"
  | "rfq.actionSubmit"
  | "rfq.actionApprove"
  | "rfq.actionReject"
  | "rfq.actionRecordQuote"
  | "rfq.columnDocumentNumber"
  | "rfq.columnDescription"
  | "rfq.columnRequisition"
  | "rfq.columnStatus"
  | "rfq.fieldRequisition"
  | "rfq.fieldDescription"
  | "rfq.fieldResponseDeadline"
  | "rfq.fieldInvitedVendors"
  | "rfq.tabLines"
  | "rfq.tabInvitedVendors"
  | "rfq.tabQuotes"
  | "rfq.columnItem"
  | "rfq.columnQuantity"
  | "rfq.columnVendor"
  | "rfq.fieldLine"
  | "rfq.fieldVendor"
  | "rfq.fieldQuotedUnitPrice"
  | "rfq.columnQuotedUnitPrice"
  | "rfq.emptyQuotes"
  | "po.heading"
  | "po.newHeading"
  | "po.emptyState"
  | "po.actionNew"
  | "po.actionCreate"
  | "po.actionBack"
  | "po.actionSubmit"
  | "po.actionApprove"
  | "po.actionReject"
  | "po.actionAddLine"
  | "po.actionRemoveLine"
  | "po.fieldSource"
  | "po.sourceFromRfq"
  | "po.sourceDirect"
  | "po.fieldRequestForQuotation"
  | "po.fieldVendor"
  | "po.fieldItem"
  | "po.fieldCostCenter"
  | "po.fieldQuantity"
  | "po.fieldUnitPrice"
  | "po.noEligibleVendors"
  | "po.columnDocumentNumber"
  | "po.columnVendor"
  | "po.columnSourceRfq"
  | "po.columnTotal"
  | "po.columnStatus"
  | "po.columnLineTotal"
  | "po.tabLines"
  | "po.selectHint"
  | "po.tabThreeWayMatch"
  | "po.fieldApInvoice"
  | "po.actionCheckMatch"
  | "po.matchOrdered"
  | "po.matchReceived"
  | "po.matchInvoiced"
  | "po.matchResultLabel"
  | "po.matchMatched"
  | "po.matchVariance"
  | "grn.heading"
  | "grn.newHeading"
  | "grn.emptyState"
  | "grn.actionNew"
  | "grn.actionCreate"
  | "grn.actionBack"
  | "grn.actionSubmit"
  | "grn.actionApprove"
  | "grn.actionReject"
  | "grn.actionAddLine"
  | "grn.actionRemoveLine"
  | "grn.fieldPurchaseOrder"
  | "grn.fieldPurchaseOrderLine"
  | "grn.fieldReceivedDate"
  | "grn.fieldQuantityReceived"
  | "grn.columnDocumentNumber"
  | "grn.columnPurchaseOrder"
  | "grn.columnReceivedValue"
  | "grn.columnStatus"
  | "grn.columnItem"
  | "grn.columnUnitPrice"
  | "grn.columnLineValue"
  | "grn.tabLines"
  | "proj.heading"
  | "proj.newHeading"
  | "proj.emptyState"
  | "proj.selectHint"
  | "proj.actionNew"
  | "proj.actionCreate"
  | "proj.actionBack"
  | "proj.actionSubmit"
  | "proj.actionApprove"
  | "proj.actionReject"
  | "proj.actionAddWbsElement"
  | "proj.actionRemoveWbsElement"
  | "proj.fieldProjectName"
  | "proj.fieldProjectNameArabic"
  | "proj.fieldCustomer"
  | "proj.fieldStartDate"
  | "proj.fieldEndDate"
  | "proj.fieldWbsCode"
  | "proj.fieldWbsName"
  | "proj.fieldWbsParent"
  | "proj.fieldPlanningElement"
  | "proj.fieldAccountAssignmentElement"
  | "proj.fieldBillingElement"
  | "proj.wbsTopLevel"
  | "proj.columnDocumentNumber"
  | "proj.columnProjectName"
  | "proj.columnStatus"
  | "proj.tabWbsElements"
  | "con.heading"
  | "con.newHeading"
  | "con.emptyState"
  | "con.selectHint"
  | "con.selectProjectFirstHint"
  | "con.actionNew"
  | "con.actionCreate"
  | "con.actionBack"
  | "con.actionSubmit"
  | "con.actionApprove"
  | "con.actionReject"
  | "con.actionAddBoqLine"
  | "con.actionRemoveBoqLine"
  | "con.fieldProject"
  | "con.fieldContractType"
  | "con.fieldPaymentTerms"
  | "con.fieldAdvancePaymentPercentage"
  | "con.fieldDefectsLiabilityPeriodMonths"
  | "con.fieldBoqCode"
  | "con.fieldBoqDescription"
  | "con.fieldBoqDescriptionArabic"
  | "con.fieldBoqUnitOfMeasure"
  | "con.fieldBoqQuantity"
  | "con.fieldBoqRate"
  | "con.fieldBoqAmount"
  | "con.fieldBoqWbsElement"
  | "con.columnDocumentNumber"
  | "con.columnProject"
  | "con.columnContractValue"
  | "con.columnStatus"
  | "con.tabBoqLines"
  | "sub.heading"
  | "sub.newHeading"
  | "sub.emptyState"
  | "sub.selectHint"
  | "sub.selectProjectFirstHint"
  | "sub.backChargeRequiresApprovedHint"
  | "sub.actionNew"
  | "sub.actionCreate"
  | "sub.actionBack"
  | "sub.actionSubmit"
  | "sub.actionApprove"
  | "sub.actionReject"
  | "sub.actionAddLine"
  | "sub.actionRemoveLine"
  | "sub.actionAddBackCharge"
  | "sub.fieldProject"
  | "sub.fieldContract"
  | "sub.fieldSubcontractor"
  | "sub.fieldRetentionPercentage"
  | "sub.fieldMobilizationAdvancePercentage"
  | "sub.fieldDefectsLiabilityPeriodMonths"
  | "sub.fieldTotalBackCharges"
  | "sub.fieldNetPayableValue"
  | "sub.fieldLineCode"
  | "sub.fieldLineDescription"
  | "sub.fieldLineDescriptionArabic"
  | "sub.fieldLineUnitOfMeasure"
  | "sub.fieldLineQuantity"
  | "sub.fieldLineRate"
  | "sub.fieldLineAmount"
  | "sub.fieldLineWbsElement"
  | "sub.fieldBackChargeDescription"
  | "sub.fieldBackChargeAmount"
  | "sub.fieldBackChargeDate"
  | "sub.columnDocumentNumber"
  | "sub.columnProject"
  | "sub.columnSubcontractor"
  | "sub.columnSubcontractValue"
  | "sub.columnStatus"
  | "sub.tabLines"
  | "sub.tabBackCharges"
  | "meas.heading"
  | "meas.newHeading"
  | "meas.emptyState"
  | "meas.selectHint"
  | "meas.selectDocumentFirstHint"
  | "meas.actionNew"
  | "meas.actionCreate"
  | "meas.actionBack"
  | "meas.actionSubmit"
  | "meas.actionCertify"
  | "meas.actionReject"
  | "meas.actionAddLine"
  | "meas.actionRemoveLine"
  | "meas.fieldProject"
  | "meas.fieldDocumentType"
  | "meas.fieldDocument"
  | "meas.documentTypeContract"
  | "meas.documentTypeSubcontract"
  | "meas.fieldPeriodStart"
  | "meas.fieldPeriodEnd"
  | "meas.fieldNotes"
  | "meas.fieldLineDocumentLine"
  | "meas.fieldLineQuantitySubmitted"
  | "meas.fieldLineQuantityCertified"
  | "meas.fieldLineRemarks"
  | "meas.columnDocumentNumber"
  | "meas.columnProject"
  | "meas.columnPeriod"
  | "meas.columnStatus"
  | "meas.tabLines"
  | "ipc.heading"
  | "ipc.newHeading"
  | "ipc.emptyState"
  | "ipc.selectHint"
  | "ipc.noEligibleSheetsHint"
  | "ipc.actionNew"
  | "ipc.actionCreate"
  | "ipc.actionBack"
  | "ipc.actionSubmit"
  | "ipc.actionCertify"
  | "ipc.actionReject"
  | "ipc.fieldProject"
  | "ipc.fieldDocumentType"
  | "ipc.fieldDocument"
  | "ipc.fieldMeasurementSheet"
  | "ipc.fieldOtherDeductions"
  | "ipc.columnDocumentNumber"
  | "ipc.columnProject"
  | "ipc.columnNetPayable"
  | "ipc.columnStatus"
  | "ipc.tabWaterfall"
  | "ipc.fieldGrossValueToDate"
  | "ipc.fieldGrossValuePreviousIpc"
  | "ipc.fieldGrossValueThisPeriod"
  | "ipc.fieldRetentionAmount"
  | "ipc.fieldAdvanceRecoveryAmount"
  | "ipc.fieldNetPayable"
  | "ipc.fieldLineRate"
  | "ipc.fieldLineQuantityThisPeriod"
  | "ipc.fieldLineValueThisPeriod"
  | "ipc.fieldLineQuantityToDate"
  | "ipc.fieldLineValueToDate"
  | "lookup.hubHeading"
  | "lookup.newTypeHeading"
  | "lookup.columnCode"
  | "lookup.columnName"
  | "lookup.columnNameArabic"
  | "lookup.columnValueCount"
  | "lookup.columnKind"
  | "lookup.columnActions"
  | "lookup.columnSortOrder"
  | "lookup.columnStatus"
  | "lookup.statusActive"
  | "lookup.statusInactive"
  | "lookup.kindSystem"
  | "lookup.kindCustom"
  | "lookup.actionEdit"
  | "lookup.actionSave"
  | "lookup.actionCancel"
  | "lookup.actionActivate"
  | "lookup.actionDeactivate"
  | "lookup.actionDelete"
  | "lookup.actionAddValue"
  | "lookup.actionBackToHub"
  | "lookup.actionCreateType"
  | "lookup.fieldTypeCode"
  | "lookup.fieldTypeName"
  | "lookup.fieldTypeNameArabic"
  | "lookup.newCodePlaceholder"
  | "lookup.newNamePlaceholder"
  | "lookup.newNameArabicPlaceholder"
  | "nav.usersArea"
  | "nav.allUsers"
  | "auth.signInPrompt"
  | "auth.usernameLabel"
  | "auth.passwordLabel"
  | "auth.loginButton"
  | "auth.invalidCredentials"
  | "auth.loggedInAs"
  | "auth.logoutButton"
  | "users.heading"
  | "users.newHeading"
  | "users.emptyState"
  | "users.selectHint"
  | "users.actionNew"
  | "users.actionCreate"
  | "users.actionBack"
  | "users.actionActivate"
  | "users.actionDeactivate"
  | "users.actionAssignRole"
  | "users.actionRemoveRole"
  | "users.actionGrantExceptionAndAssign"
  | "users.actionResetPassword"
  | "users.columnUsername"
  | "users.columnDisplayName"
  | "users.columnStatus"
  | "users.statusActive"
  | "users.statusInactive"
  | "users.fieldUsername"
  | "users.fieldDisplayName"
  | "users.fieldEmail"
  | "users.fieldPassword"
  | "users.fieldNewPassword"
  | "users.fieldRoleKey"
  | "users.fieldOverrideReason"
  | "users.tabRoles"
  | "users.tabPassword"
  | "users.emptyRoles"
  | "users.sodConflictHeading";

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
  "bp.selectHint": { en: "Select a business partner from the list to see its details.", ar: "اختر شريك أعمال من القائمة لعرض تفاصيله." },
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
  "nav.arInvoicesArea": { en: "AR Invoices", ar: "فواتير العملاء" },
  "nav.allARInvoices": { en: "All AR Invoices", ar: "جميع فواتير العملاء" },
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
  "ap.columnOutstandingBalance": { en: "Outstanding Balance", ar: "الرصيد المستحق" },
  "ar.heading": { en: "AR Invoices", ar: "فواتير العملاء" },
  "ar.newHeading": { en: "New AR Invoice", ar: "فاتورة عميل جديدة" },
  "ar.emptyState": { en: "No AR invoices yet.", ar: "لا توجد فواتير عملاء حتى الآن." },
  "ar.actionNew": { en: "New", ar: "جديد" },
  "ar.actionCreate": { en: "Create", ar: "إنشاء" },
  "ar.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "ar.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "ar.actionApprove": { en: "Approve", ar: "اعتماد" },
  "ar.actionReject": { en: "Reject", ar: "رفض" },
  "ar.actionPost": { en: "Post", ar: "ترحيل" },
  "ar.actionReverse": { en: "Reverse", ar: "عكس القيد" },
  "ar.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "ar.columnInvoiceDate": { en: "Invoice Date", ar: "تاريخ الفاتورة" },
  "ar.columnCustomer": { en: "Customer", ar: "العميل" },
  "ar.columnGrossAmount": { en: "Gross Amount", ar: "المبلغ الإجمالي" },
  "ar.columnStatus": { en: "Status", ar: "الحالة" },
  "ar.fieldCustomer": { en: "Customer", ar: "العميل" },
  "ar.fieldCustomerReference": { en: "Customer Reference", ar: "مرجع العميل" },
  "ar.fieldInvoiceDate": { en: "Invoice Date", ar: "تاريخ الفاتورة" },
  "ar.fieldDescription": { en: "Description", ar: "الوصف" },
  "ar.fieldRevenueAccount": { en: "Revenue Account", ar: "حساب الإيرادات" },
  "ar.fieldReceivableAccount": { en: "Receivable Account", ar: "حساب المدينين" },
  "ar.fieldTaxCode": { en: "Tax Code", ar: "الرمز الضريبي" },
  "ar.fieldVatAccount": { en: "VAT Account", ar: "حساب ضريبة القيمة المضافة" },
  "ar.fieldNetAmount": { en: "Net Amount", ar: "المبلغ الصافي" },
  "ar.noTaxCode": { en: "No tax", ar: "بدون ضريبة" },
  "ar.columnNetAmount": { en: "Net Amount", ar: "المبلغ الصافي" },
  "ar.columnTaxAmount": { en: "Tax Amount", ar: "مبلغ الضريبة" },
  "ar.linkedJournalEntry": { en: "Linked Journal Entry", ar: "قيد اليومية المرتبط" },
  "ar.columnOutstandingBalance": { en: "Outstanding Balance", ar: "الرصيد المستحق" },

  "nav.bankAccountsArea": { en: "Bank Accounts", ar: "الحسابات البنكية" },
  "nav.allBankAccounts": { en: "All Bank Accounts", ar: "جميع الحسابات البنكية" },
  "bank.heading": { en: "Bank Accounts", ar: "الحسابات البنكية" },
  "bank.newHeading": { en: "New Bank Account", ar: "حساب بنكي جديد" },
  "bank.emptyState": { en: "No bank accounts yet.", ar: "لا توجد حسابات بنكية حتى الآن." },
  "bank.actionNew": { en: "New", ar: "جديد" },
  "bank.actionCreate": { en: "Create", ar: "إنشاء" },
  "bank.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "bank.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "bank.actionApprove": { en: "Approve", ar: "اعتماد" },
  "bank.actionReject": { en: "Reject", ar: "رفض" },
  "bank.columnAccountCode": { en: "Account Code", ar: "رمز الحساب" },
  "bank.columnAccountName": { en: "Account Name", ar: "اسم الحساب" },
  "bank.columnBankName": { en: "Bank Name", ar: "اسم البنك" },
  "bank.columnStatus": { en: "Status", ar: "الحالة" },
  "bank.fieldAccountCode": { en: "Account Code", ar: "رمز الحساب" },
  "bank.fieldAccountName": { en: "Account Name", ar: "اسم الحساب" },
  "bank.fieldAccountNameArabic": { en: "Account Name (Arabic)", ar: "اسم الحساب (عربي)" },
  "bank.fieldBankName": { en: "Bank Name", ar: "اسم البنك" },
  "bank.fieldIban": { en: "IBAN", ar: "رقم الآيبان" },
  "bank.fieldLinkedGLAccount": { en: "Linked G/L Account", ar: "حساب دفتر الأستاذ المرتبط" },

  "nav.paymentsArea": { en: "Payments", ar: "المدفوعات" },
  "nav.allPayments": { en: "All Payments", ar: "جميع المدفوعات" },
  "pay.heading": { en: "Payments", ar: "المدفوعات" },
  "pay.newHeading": { en: "New Payment", ar: "دفعة جديدة" },
  "pay.emptyState": { en: "No payments yet.", ar: "لا توجد مدفوعات حتى الآن." },
  "pay.actionNew": { en: "New", ar: "جديد" },
  "pay.actionCreate": { en: "Create", ar: "إنشاء" },
  "pay.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "pay.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "pay.actionApprove": { en: "Approve", ar: "اعتماد" },
  "pay.actionReject": { en: "Reject", ar: "رفض" },
  "pay.actionPost": { en: "Post", ar: "ترحيل" },
  "pay.actionReverse": { en: "Reverse", ar: "عكس القيد" },
  "pay.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "pay.columnAmount": { en: "Amount", ar: "المبلغ" },
  "pay.columnStatus": { en: "Status", ar: "الحالة" },
  "pay.columnInvoice": { en: "AP Invoice", ar: "فاتورة المورّد" },
  "pay.columnOutstandingBalance": { en: "Outstanding Balance", ar: "الرصيد المستحق" },
  "pay.fieldVendor": { en: "Vendor", ar: "المورّد" },
  "pay.fieldBankAccount": { en: "Bank Account", ar: "الحساب البنكي" },
  "pay.fieldPaymentMethod": { en: "Payment Method", ar: "طريقة الدفع" },
  "pay.fieldPaymentDate": { en: "Payment Date", ar: "تاريخ الدفع" },
  "pay.fieldReference": { en: "Reference", ar: "المرجع" },
  "pay.fieldAllocatedAmount": { en: "Allocated Amount", ar: "المبلغ المخصص" },
  "pay.tabAllocations": { en: "Allocations", ar: "التخصيصات" },
  "pay.noOutstandingInvoices": { en: "This vendor has no outstanding Posted invoices.", ar: "لا توجد فواتير مرحّلة مستحقة لهذا المورّد." },

  "nav.procurementModule": { en: "Procurement", ar: "المشتريات" },
  "nav.vendorPrequalificationsArea": { en: "Vendor Prequalification", ar: "تأهيل الموردين" },
  "nav.allVendorPrequalifications": { en: "All Prequalifications", ar: "جميع طلبات التأهيل" },
  "nav.allPurchaseRequisitions": { en: "All Requisitions", ar: "جميع طلبات الشراء" },
  "nav.allRequestsForQuotation": { en: "All RFQs", ar: "جميع طلبات عروض الأسعار" },
  "nav.purchaseOrdersArea": { en: "Purchase Orders", ar: "أوامر الشراء" },
  "nav.allPurchaseOrders": { en: "All Purchase Orders", ar: "جميع أوامر الشراء" },
  "nav.goodsReceiptNotesArea": { en: "Goods Receipt Notes", ar: "إشعارات استلام البضائع" },
  "nav.allGoodsReceiptNotes": { en: "All Goods Receipt Notes", ar: "جميع إشعارات استلام البضائع" },
  "nav.projectManagementModule": { en: "Project Management", ar: "إدارة المشاريع" },
  "nav.projectsArea": { en: "Projects", ar: "المشاريع" },
  "nav.constructionModule": { en: "Construction", ar: "الإنشاءات" },
  "nav.contractsArea": { en: "Contracts", ar: "العقود" },
  "nav.allContracts": { en: "All Contracts", ar: "جميع العقود" },
  "nav.subcontractsArea": { en: "Subcontracts", ar: "عقود الباطن" },
  "nav.allSubcontracts": { en: "All Subcontracts", ar: "جميع عقود الباطن" },
  "nav.measurementSheetsArea": { en: "Site Progress", ar: "تقدم الموقع" },
  "nav.allMeasurementSheets": { en: "Measurement Sheets", ar: "كشوفات القياس" },
  "nav.ipcsArea": { en: "Billing", ar: "الفوترة" },
  "nav.allIpcs": { en: "IPCs", ar: "شهادات الدفع المرحلية" },
  "nav.allProjects": { en: "All Projects", ar: "جميع المشاريع" },
  "nav.lookupDataArea": { en: "Lookup Data", ar: "بيانات القوائم" },
  "nav.allLookupTypes": { en: "All Lookup Types", ar: "جميع أنواع القوائم" },
  "nav.lookupCountries": { en: "Countries", ar: "الدول" },
  "nav.lookupBusinessRoleTypes": { en: "Business Role Types", ar: "أنواع أدوار الشركاء" },
  "nav.lookupAddressTypes": { en: "Address Types", ar: "أنواع العناوين" },
  "nav.lookupUnitsOfMeasure": { en: "Units of Measure", ar: "وحدات القياس" },
  "nav.lookupSubcontractorTrades": { en: "Subcontractor Trades", ar: "تخصصات مقاولي الباطن" },
  "nav.lookupSupplierTrades": { en: "Supplier Trades", ar: "تخصصات الموردين" },
  "nav.lookupConsultantTrades": { en: "Consultant Trades", ar: "تخصصات الاستشاريين" },

  "vpq.heading": { en: "Vendor Prequalification", ar: "تأهيل الموردين" },
  "vpq.newHeading": { en: "New Vendor Prequalification", ar: "تأهيل مورّد جديد" },
  "vpq.emptyState": { en: "No vendor prequalifications yet.", ar: "لا توجد طلبات تأهيل موردين حتى الآن." },
  "vpq.actionNew": { en: "New", ar: "جديد" },
  "vpq.actionCreate": { en: "Create", ar: "إنشاء" },
  "vpq.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "vpq.actionSubmit": { en: "Submit for Review", ar: "إرسال للمراجعة" },
  "vpq.actionApprove": { en: "Approve Current Step", ar: "اعتماد الخطوة الحالية" },
  "vpq.actionReject": { en: "Reject", ar: "رفض" },
  "vpq.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "vpq.columnVendor": { en: "Vendor", ar: "المورّد" },
  "vpq.columnRoleType": { en: "Role", ar: "الدور" },
  "vpq.columnTrade": { en: "Trade", ar: "التخصص" },
  "vpq.columnValidFrom": { en: "Valid From", ar: "ساري من" },
  "vpq.columnValidUntil": { en: "Valid Until", ar: "ساري حتى" },
  "vpq.columnStatus": { en: "Status", ar: "الحالة" },
  "vpq.fieldVendor": { en: "Vendor", ar: "المورّد" },
  "vpq.fieldRoleType": { en: "Role", ar: "الدور" },
  "vpq.fieldTrade": { en: "Trade", ar: "التخصص" },
  "vpq.noTrade": { en: "—", ar: "—" },

  "nav.purchaseRequisitionsArea": { en: "Purchase Requisitions", ar: "طلبات الشراء" },

  "pr.heading": { en: "Purchase Requisitions", ar: "طلبات الشراء" },
  "pr.newHeading": { en: "New Purchase Requisition", ar: "طلب شراء جديد" },
  "pr.emptyState": { en: "No purchase requisitions yet.", ar: "لا توجد طلبات شراء حتى الآن." },
  "pr.actionNew": { en: "New", ar: "جديد" },
  "pr.actionCreate": { en: "Create", ar: "إنشاء" },
  "pr.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "pr.actionSubmit": { en: "Submit for Approval", ar: "إرسال للاعتماد" },
  "pr.actionApprove": { en: "Approve", ar: "اعتماد" },
  "pr.actionReject": { en: "Reject", ar: "رفض" },
  "pr.actionAddLine": { en: "Add Line", ar: "إضافة بند" },
  "pr.actionRemoveLine": { en: "Remove", ar: "إزالة" },
  "pr.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "pr.columnDescription": { en: "Description", ar: "الوصف" },
  "pr.columnRequiredByDate": { en: "Required By", ar: "مطلوب بحلول" },
  "pr.columnEstimatedTotal": { en: "Estimated Total", ar: "الإجمالي التقديري" },
  "pr.columnStatus": { en: "Status", ar: "الحالة" },
  "pr.fieldDescription": { en: "Description", ar: "الوصف" },
  "pr.fieldRequiredByDate": { en: "Required By Date", ar: "تاريخ الاحتياج" },
  "pr.fieldItem": { en: "Item", ar: "الصنف" },
  "pr.fieldCostCenter": { en: "Cost Center", ar: "مركز التكلفة" },
  "pr.fieldQuantity": { en: "Quantity", ar: "الكمية" },
  "pr.fieldEstimatedUnitPrice": { en: "Est. Unit Price", ar: "سعر الوحدة التقديري" },
  "pr.fieldLineDescription": { en: "Line Description", ar: "وصف البند" },
  "pr.columnLineTotal": { en: "Line Total", ar: "إجمالي البند" },

  "nav.requestsForQuotationArea": { en: "Requests for Quotation", ar: "طلبات عروض الأسعار" },

  "rfq.heading": { en: "Requests for Quotation", ar: "طلبات عروض الأسعار" },
  "rfq.newHeading": { en: "New Request for Quotation", ar: "طلب عرض سعر جديد" },
  "rfq.emptyState": { en: "No requests for quotation yet.", ar: "لا توجد طلبات عروض أسعار حتى الآن." },
  "rfq.actionNew": { en: "New", ar: "جديد" },
  "rfq.actionCreate": { en: "Create", ar: "إنشاء" },
  "rfq.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "rfq.actionSubmit": { en: "Submit (Send to Vendors)", ar: "إرسال إلى الموردين" },
  "rfq.actionApprove": { en: "Approve", ar: "اعتماد" },
  "rfq.actionReject": { en: "Reject", ar: "رفض" },
  "rfq.actionRecordQuote": { en: "Record Quote", ar: "تسجيل عرض السعر" },
  "rfq.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "rfq.columnDescription": { en: "Description", ar: "الوصف" },
  "rfq.columnRequisition": { en: "Requisition", ar: "طلب الشراء" },
  "rfq.columnStatus": { en: "Status", ar: "الحالة" },
  "rfq.fieldRequisition": { en: "Purchase Requisition", ar: "طلب الشراء" },
  "rfq.fieldDescription": { en: "Description", ar: "الوصف" },
  "rfq.fieldResponseDeadline": { en: "Response Deadline", ar: "الموعد النهائي للرد" },
  "rfq.fieldInvitedVendors": { en: "Invited Vendors", ar: "الموردون المدعوون" },
  "rfq.tabLines": { en: "Lines", ar: "البنود" },
  "rfq.tabInvitedVendors": { en: "Invited Vendors", ar: "الموردون المدعوون" },
  "rfq.tabQuotes": { en: "Vendor Quotes", ar: "عروض أسعار الموردين" },
  "rfq.columnItem": { en: "Item", ar: "الصنف" },
  "rfq.columnQuantity": { en: "Quantity", ar: "الكمية" },
  "rfq.columnVendor": { en: "Vendor", ar: "المورّد" },
  "rfq.fieldLine": { en: "Line", ar: "البند" },
  "rfq.fieldVendor": { en: "Vendor", ar: "المورّد" },
  "rfq.fieldQuotedUnitPrice": { en: "Quoted Unit Price", ar: "سعر الوحدة المعروض" },
  "rfq.columnQuotedUnitPrice": { en: "Quoted Unit Price", ar: "سعر الوحدة المعروض" },
  "rfq.emptyQuotes": { en: "No vendor quotes recorded yet.", ar: "لم يتم تسجيل أي عروض أسعار حتى الآن." },

  "po.heading": { en: "Purchase Orders", ar: "أوامر الشراء" },
  "po.newHeading": { en: "New Purchase Order", ar: "أمر شراء جديد" },
  "po.emptyState": { en: "No purchase orders yet.", ar: "لا توجد أوامر شراء حتى الآن." },
  "po.actionNew": { en: "New", ar: "جديد" },
  "po.actionCreate": { en: "Create", ar: "إنشاء" },
  "po.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "po.actionSubmit": { en: "Submit", ar: "إرسال" },
  "po.actionApprove": { en: "Approve", ar: "اعتماد" },
  "po.actionReject": { en: "Reject", ar: "رفض" },
  "po.actionAddLine": { en: "Add Line", ar: "إضافة بند" },
  "po.actionRemoveLine": { en: "Remove", ar: "إزالة" },
  "po.fieldSource": { en: "Source", ar: "المصدر" },
  "po.sourceFromRfq": { en: "From an RFQ-selected quote", ar: "من عرض سعر تم اختياره من طلب عروض الأسعار" },
  "po.sourceDirect": { en: "Direct (no RFQ)", ar: "مباشر (بدون طلب عروض أسعار)" },
  "po.fieldRequestForQuotation": { en: "Request for Quotation", ar: "طلب عرض السعر" },
  "po.fieldVendor": { en: "Vendor", ar: "المورّد" },
  "po.fieldItem": { en: "Item", ar: "الصنف" },
  "po.fieldCostCenter": { en: "Cost Center", ar: "مركز التكلفة" },
  "po.fieldQuantity": { en: "Quantity", ar: "الكمية" },
  "po.fieldUnitPrice": { en: "Unit Price", ar: "سعر الوحدة" },
  "po.noEligibleVendors": {
    en: "No invited vendor has quoted every line of this RFQ yet.",
    ar: "لم يقدم أي مورّد مدعو عرض سعر لجميع بنود طلب عروض الأسعار هذا حتى الآن.",
  },
  "po.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "po.columnVendor": { en: "Vendor", ar: "المورّد" },
  "po.columnSourceRfq": { en: "Source RFQ", ar: "طلب عرض السعر المصدر" },
  "po.columnTotal": { en: "Total", ar: "الإجمالي" },
  "po.columnStatus": { en: "Status", ar: "الحالة" },
  "po.columnLineTotal": { en: "Line Total", ar: "إجمالي البند" },
  "po.tabLines": { en: "Lines", ar: "البنود" },
  "po.selectHint": { en: "Select a purchase order from the list to see its details.", ar: "اختر أمر شراء من القائمة لعرض تفاصيله." },
  "po.tabThreeWayMatch": { en: "3-Way Match", ar: "المطابقة الثلاثية" },
  "po.fieldApInvoice": { en: "AP Invoice", ar: "فاتورة الموردين" },
  "po.actionCheckMatch": { en: "Check Match", ar: "التحقق من المطابقة" },
  "po.matchOrdered": { en: "Ordered", ar: "المطلوب" },
  "po.matchReceived": { en: "Received", ar: "المستلم" },
  "po.matchInvoiced": { en: "Invoiced", ar: "المفوتر" },
  "po.matchResultLabel": { en: "Result", ar: "النتيجة" },
  "po.matchMatched": { en: "Matched", ar: "مطابق" },
  "po.matchVariance": { en: "Variance", ar: "يوجد فرق" },

  "grn.heading": { en: "Goods Receipt Notes", ar: "إشعارات استلام البضائع" },
  "grn.newHeading": { en: "New Goods Receipt Note", ar: "إشعار استلام بضائع جديد" },
  "grn.emptyState": { en: "No goods receipt notes yet.", ar: "لا توجد إشعارات استلام بضائع حتى الآن." },
  "grn.actionNew": { en: "New", ar: "جديد" },
  "grn.actionCreate": { en: "Create", ar: "إنشاء" },
  "grn.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "grn.actionSubmit": { en: "Submit", ar: "إرسال" },
  "grn.actionApprove": { en: "Approve", ar: "اعتماد" },
  "grn.actionReject": { en: "Reject", ar: "رفض" },
  "grn.actionAddLine": { en: "Add Line", ar: "إضافة بند" },
  "grn.actionRemoveLine": { en: "Remove", ar: "إزالة" },
  "grn.fieldPurchaseOrder": { en: "Purchase Order", ar: "أمر الشراء" },
  "grn.fieldPurchaseOrderLine": { en: "Purchase Order Line", ar: "بند أمر الشراء" },
  "grn.fieldReceivedDate": { en: "Received Date", ar: "تاريخ الاستلام" },
  "grn.fieldQuantityReceived": { en: "Quantity Received", ar: "الكمية المستلمة" },
  "grn.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "grn.columnPurchaseOrder": { en: "Purchase Order", ar: "أمر الشراء" },
  "grn.columnReceivedValue": { en: "Received Value", ar: "قيمة المستلم" },
  "grn.columnStatus": { en: "Status", ar: "الحالة" },
  "grn.columnItem": { en: "Item", ar: "الصنف" },
  "grn.columnUnitPrice": { en: "Unit Price", ar: "سعر الوحدة" },
  "grn.columnLineValue": { en: "Line Value", ar: "قيمة البند" },
  "grn.tabLines": { en: "Lines", ar: "البنود" },

  "proj.heading": { en: "Projects", ar: "المشاريع" },
  "proj.newHeading": { en: "New Project", ar: "مشروع جديد" },
  "proj.emptyState": { en: "No projects yet.", ar: "لا توجد مشاريع حتى الآن." },
  "proj.selectHint": { en: "Select a project from the list to see its details.", ar: "اختر مشروعًا من القائمة لعرض تفاصيله." },
  "proj.actionNew": { en: "New", ar: "جديد" },
  "proj.actionCreate": { en: "Create", ar: "إنشاء" },
  "proj.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "proj.actionSubmit": { en: "Submit", ar: "إرسال" },
  "proj.actionApprove": { en: "Approve (Release)", ar: "اعتماد (إطلاق)" },
  "proj.actionReject": { en: "Reject", ar: "رفض" },
  "proj.actionAddWbsElement": { en: "Add WBS Element", ar: "إضافة عنصر هيكل تجزئة العمل" },
  "proj.actionRemoveWbsElement": { en: "Remove", ar: "إزالة" },
  "proj.fieldProjectName": { en: "Project Name", ar: "اسم المشروع" },
  "proj.fieldProjectNameArabic": { en: "Project Name (Arabic)", ar: "اسم المشروع (عربي)" },
  "proj.fieldCustomer": { en: "Customer", ar: "العميل" },
  "proj.fieldStartDate": { en: "Start Date", ar: "تاريخ البدء" },
  "proj.fieldEndDate": { en: "End Date", ar: "تاريخ الانتهاء" },
  "proj.fieldWbsCode": { en: "WBS Code", ar: "رمز هيكل تجزئة العمل" },
  "proj.fieldWbsName": { en: "WBS Name", ar: "اسم عنصر هيكل تجزئة العمل" },
  "proj.fieldWbsParent": { en: "Parent", ar: "العنصر الأصل" },
  "proj.fieldPlanningElement": { en: "Planning", ar: "تخطيط" },
  "proj.fieldAccountAssignmentElement": { en: "Account Assignment", ar: "إسناد الحساب" },
  "proj.fieldBillingElement": { en: "Billing", ar: "فوترة" },
  "proj.wbsTopLevel": { en: "— Top level —", ar: "— المستوى الأعلى —" },
  "proj.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "proj.columnProjectName": { en: "Project Name", ar: "اسم المشروع" },
  "proj.columnStatus": { en: "Status", ar: "الحالة" },
  "proj.tabWbsElements": { en: "WBS Elements", ar: "عناصر هيكل تجزئة العمل" },
  "con.heading": { en: "Contracts", ar: "العقود" },
  "con.newHeading": { en: "New Contract", ar: "عقد جديد" },
  "con.emptyState": { en: "No contracts yet.", ar: "لا توجد عقود حتى الآن." },
  "con.selectHint": { en: "Select a contract from the list to see its details.", ar: "اختر عقدًا من القائمة لعرض تفاصيله." },
  "con.selectProjectFirstHint": { en: "Select a project above to build the BOQ against its WBS elements.", ar: "اختر مشروعًا أعلاه لبناء جدول الكميات وفق عناصر هيكل تجزئة العمل الخاصة به." },
  "con.actionNew": { en: "New", ar: "جديد" },
  "con.actionCreate": { en: "Create", ar: "إنشاء" },
  "con.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "con.actionSubmit": { en: "Submit", ar: "إرسال" },
  "con.actionApprove": { en: "Approve", ar: "اعتماد" },
  "con.actionReject": { en: "Reject", ar: "رفض" },
  "con.actionAddBoqLine": { en: "Add BOQ Line", ar: "إضافة بند جدول كميات" },
  "con.actionRemoveBoqLine": { en: "Remove", ar: "إزالة" },
  "con.fieldProject": { en: "Project", ar: "المشروع" },
  "con.fieldContractType": { en: "Contract Type", ar: "نوع العقد" },
  "con.fieldPaymentTerms": { en: "Payment Terms", ar: "شروط الدفع" },
  "con.fieldAdvancePaymentPercentage": { en: "Advance Payment %", ar: "نسبة الدفعة المقدمة" },
  "con.fieldDefectsLiabilityPeriodMonths": { en: "Defects Liability Period (months)", ar: "فترة ضمان العيوب (بالأشهر)" },
  "con.fieldBoqCode": { en: "Code", ar: "الرمز" },
  "con.fieldBoqDescription": { en: "Description", ar: "الوصف" },
  "con.fieldBoqDescriptionArabic": { en: "Description (Arabic)", ar: "الوصف (عربي)" },
  "con.fieldBoqUnitOfMeasure": { en: "Unit of Measure", ar: "وحدة القياس" },
  "con.fieldBoqQuantity": { en: "Quantity", ar: "الكمية" },
  "con.fieldBoqRate": { en: "Rate", ar: "السعر" },
  "con.fieldBoqAmount": { en: "Amount", ar: "المبلغ" },
  "con.fieldBoqWbsElement": { en: "WBS Element", ar: "عنصر هيكل تجزئة العمل" },
  "con.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "con.columnProject": { en: "Project", ar: "المشروع" },
  "con.columnContractValue": { en: "Contract Value", ar: "قيمة العقد" },
  "con.columnStatus": { en: "Status", ar: "الحالة" },
  "con.tabBoqLines": { en: "BOQ Lines", ar: "بنود جدول الكميات" },
  "sub.heading": { en: "Subcontracts", ar: "عقود الباطن" },
  "sub.newHeading": { en: "New Subcontract", ar: "عقد باطن جديد" },
  "sub.emptyState": { en: "No subcontracts yet.", ar: "لا توجد عقود باطن حتى الآن." },
  "sub.selectHint": { en: "Select a subcontract from the list to see its details.", ar: "اختر عقد باطن من القائمة لعرض تفاصيله." },
  "sub.selectProjectFirstHint": { en: "Select a project above to build the scope of work against its WBS elements.", ar: "اختر مشروعًا أعلاه لبناء نطاق العمل وفق عناصر هيكل تجزئة العمل الخاصة به." },
  "sub.backChargeRequiresApprovedHint": { en: "Back charges can only be recorded once the subcontract is Approved.", ar: "لا يمكن تسجيل خصومات الاسترداد إلا بعد اعتماد عقد الباطن." },
  "sub.actionNew": { en: "New", ar: "جديد" },
  "sub.actionCreate": { en: "Create", ar: "إنشاء" },
  "sub.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "sub.actionSubmit": { en: "Submit", ar: "إرسال" },
  "sub.actionApprove": { en: "Approve", ar: "اعتماد" },
  "sub.actionReject": { en: "Reject", ar: "رفض" },
  "sub.actionAddLine": { en: "Add Line", ar: "إضافة بند" },
  "sub.actionRemoveLine": { en: "Remove", ar: "إزالة" },
  "sub.actionAddBackCharge": { en: "Add Back Charge", ar: "إضافة خصم استرداد" },
  "sub.fieldProject": { en: "Project", ar: "المشروع" },
  "sub.fieldContract": { en: "Contract", ar: "العقد" },
  "sub.fieldSubcontractor": { en: "Subcontractor", ar: "المقاول من الباطن" },
  "sub.fieldRetentionPercentage": { en: "Retention %", ar: "نسبة الاحتجاز" },
  "sub.fieldMobilizationAdvancePercentage": { en: "Mobilization Advance %", ar: "نسبة دفعة التهيئة" },
  "sub.fieldDefectsLiabilityPeriodMonths": { en: "Defects Liability Period (months)", ar: "فترة ضمان العيوب (بالأشهر)" },
  "sub.fieldTotalBackCharges": { en: "Total Back Charges", ar: "إجمالي خصومات الاسترداد" },
  "sub.fieldNetPayableValue": { en: "Net Payable Value", ar: "صافي المبلغ المستحق" },
  "sub.fieldLineCode": { en: "Code", ar: "الرمز" },
  "sub.fieldLineDescription": { en: "Description", ar: "الوصف" },
  "sub.fieldLineDescriptionArabic": { en: "Description (Arabic)", ar: "الوصف (عربي)" },
  "sub.fieldLineUnitOfMeasure": { en: "Unit of Measure", ar: "وحدة القياس" },
  "sub.fieldLineQuantity": { en: "Quantity", ar: "الكمية" },
  "sub.fieldLineRate": { en: "Rate", ar: "السعر" },
  "sub.fieldLineAmount": { en: "Amount", ar: "المبلغ" },
  "sub.fieldLineWbsElement": { en: "WBS Element", ar: "عنصر هيكل تجزئة العمل" },
  "sub.fieldBackChargeDescription": { en: "Description", ar: "الوصف" },
  "sub.fieldBackChargeAmount": { en: "Amount", ar: "المبلغ" },
  "sub.fieldBackChargeDate": { en: "Date Incurred", ar: "تاريخ التكبد" },
  "sub.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "sub.columnProject": { en: "Project", ar: "المشروع" },
  "sub.columnSubcontractor": { en: "Subcontractor", ar: "المقاول من الباطن" },
  "sub.columnSubcontractValue": { en: "Subcontract Value", ar: "قيمة عقد الباطن" },
  "sub.columnStatus": { en: "Status", ar: "الحالة" },
  "sub.tabLines": { en: "Lines", ar: "البنود" },
  "sub.tabBackCharges": { en: "Back Charges", ar: "خصومات الاسترداد" },
  "meas.heading": { en: "Measurement Sheets", ar: "كشوفات القياس" },
  "meas.newHeading": { en: "New Measurement Sheet", ar: "كشف قياس جديد" },
  "meas.emptyState": { en: "No measurement sheets yet.", ar: "لا توجد كشوفات قياس حتى الآن." },
  "meas.selectHint": { en: "Select a measurement sheet from the list to see its details.", ar: "اختر كشف قياس من القائمة لعرض تفاصيله." },
  "meas.selectDocumentFirstHint": { en: "Select a Contract or Subcontract above to measure progress against its lines.", ar: "اختر عقدًا أو عقد باطن أعلاه لقياس التقدم مقابل بنوده." },
  "meas.actionNew": { en: "New", ar: "جديد" },
  "meas.actionCreate": { en: "Create", ar: "إنشاء" },
  "meas.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "meas.actionSubmit": { en: "Submit", ar: "إرسال" },
  "meas.actionCertify": { en: "Certify", ar: "اعتماد القياس" },
  "meas.actionReject": { en: "Reject", ar: "رفض" },
  "meas.actionAddLine": { en: "Add Line", ar: "إضافة بند" },
  "meas.actionRemoveLine": { en: "Remove", ar: "إزالة" },
  "meas.fieldProject": { en: "Project", ar: "المشروع" },
  "meas.fieldDocumentType": { en: "Document Type", ar: "نوع المستند" },
  "meas.fieldDocument": { en: "Contract / Subcontract", ar: "العقد / عقد الباطن" },
  "meas.documentTypeContract": { en: "Contract", ar: "عقد" },
  "meas.documentTypeSubcontract": { en: "Subcontract", ar: "عقد باطن" },
  "meas.fieldPeriodStart": { en: "Period Start", ar: "بداية الفترة" },
  "meas.fieldPeriodEnd": { en: "Period End", ar: "نهاية الفترة" },
  "meas.fieldNotes": { en: "Notes", ar: "ملاحظات" },
  "meas.fieldLineDocumentLine": { en: "Line", ar: "البند" },
  "meas.fieldLineQuantitySubmitted": { en: "Quantity Submitted", ar: "الكمية المقدمة" },
  "meas.fieldLineQuantityCertified": { en: "Quantity Certified", ar: "الكمية المعتمدة" },
  "meas.fieldLineRemarks": { en: "Remarks", ar: "ملاحظات" },
  "meas.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "meas.columnProject": { en: "Project", ar: "المشروع" },
  "meas.columnPeriod": { en: "Period", ar: "الفترة" },
  "meas.columnStatus": { en: "Status", ar: "الحالة" },
  "meas.tabLines": { en: "Lines", ar: "البنود" },
  "ipc.heading": { en: "Interim Payment Certificates", ar: "شهادات الدفع المرحلية" },
  "ipc.newHeading": { en: "New IPC", ar: "شهادة دفع مرحلية جديدة" },
  "ipc.emptyState": { en: "No IPCs yet.", ar: "لا توجد شهادات دفع مرحلية حتى الآن." },
  "ipc.selectHint": { en: "Select an IPC from the list to see its details.", ar: "اختر شهادة دفع مرحلية من القائمة لعرض تفاصيلها." },
  "ipc.noEligibleSheetsHint": { en: "No Certified measurement sheet is available to bill for this document (or all have already been billed).", ar: "لا يوجد كشف قياس معتمد متاح للفوترة لهذا المستند (أو تم إصدار فواتير لجميعها بالفعل)." },
  "ipc.actionNew": { en: "New", ar: "جديد" },
  "ipc.actionCreate": { en: "Create", ar: "إنشاء" },
  "ipc.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "ipc.actionSubmit": { en: "Submit", ar: "إرسال" },
  "ipc.actionCertify": { en: "Certify", ar: "اعتماد" },
  "ipc.actionReject": { en: "Reject", ar: "رفض" },
  "ipc.fieldProject": { en: "Project", ar: "المشروع" },
  "ipc.fieldDocumentType": { en: "Document Type", ar: "نوع المستند" },
  "ipc.fieldDocument": { en: "Contract / Subcontract", ar: "العقد / عقد الباطن" },
  "ipc.fieldMeasurementSheet": { en: "Measurement Sheet", ar: "كشف القياس" },
  "ipc.fieldOtherDeductions": { en: "Other Deductions", ar: "خصومات أخرى" },
  "ipc.columnDocumentNumber": { en: "Number", ar: "الرقم" },
  "ipc.columnProject": { en: "Project", ar: "المشروع" },
  "ipc.columnNetPayable": { en: "Net Payable", ar: "صافي المستحق" },
  "ipc.columnStatus": { en: "Status", ar: "الحالة" },
  "ipc.tabWaterfall": { en: "Calculation", ar: "الاحتساب" },
  "ipc.fieldGrossValueToDate": { en: "Gross Value to Date", ar: "القيمة الإجمالية حتى تاريخه" },
  "ipc.fieldGrossValuePreviousIpc": { en: "Less: Previous IPCs", ar: "ناقص: الشهادات السابقة" },
  "ipc.fieldGrossValueThisPeriod": { en: "Gross Value This Period", ar: "القيمة الإجمالية لهذه الفترة" },
  "ipc.fieldRetentionAmount": { en: "Less: Retention", ar: "ناقص: الاحتجاز" },
  "ipc.fieldAdvanceRecoveryAmount": { en: "Less: Advance Recovery", ar: "ناقص: استرداد الدفعة المقدمة" },
  "ipc.fieldNetPayable": { en: "Net Payable This IPC", ar: "صافي المستحق لهذه الشهادة" },
  "ipc.fieldLineRate": { en: "Rate", ar: "السعر" },
  "ipc.fieldLineQuantityThisPeriod": { en: "Qty This Period", ar: "الكمية لهذه الفترة" },
  "ipc.fieldLineValueThisPeriod": { en: "Value This Period", ar: "القيمة لهذه الفترة" },
  "ipc.fieldLineQuantityToDate": { en: "Qty To Date", ar: "الكمية حتى تاريخه" },
  "ipc.fieldLineValueToDate": { en: "Value To Date", ar: "القيمة حتى تاريخه" },
  "lookup.hubHeading": { en: "Lookup Data", ar: "بيانات القوائم" },
  "lookup.newTypeHeading": { en: "Create a new lookup type", ar: "إنشاء نوع قائمة جديد" },
  "lookup.columnCode": { en: "Code", ar: "الرمز" },
  "lookup.columnName": { en: "Name", ar: "الاسم" },
  "lookup.columnNameArabic": { en: "Name (Arabic)", ar: "الاسم (عربي)" },
  "lookup.columnValueCount": { en: "Values", ar: "عدد القيم" },
  "lookup.columnKind": { en: "Kind", ar: "النوع" },
  "lookup.columnActions": { en: "Actions", ar: "الإجراءات" },
  "lookup.columnSortOrder": { en: "Sort Order", ar: "ترتيب العرض" },
  "lookup.columnStatus": { en: "Status", ar: "الحالة" },
  "lookup.statusActive": { en: "Active", ar: "فعال" },
  "lookup.statusInactive": { en: "Inactive", ar: "غير فعال" },
  "lookup.kindSystem": { en: "System", ar: "نظامي" },
  "lookup.kindCustom": { en: "Custom", ar: "مخصص" },
  "lookup.actionEdit": { en: "Edit", ar: "تعديل" },
  "lookup.actionSave": { en: "Save", ar: "حفظ" },
  "lookup.actionCancel": { en: "Cancel", ar: "إلغاء" },
  "lookup.actionActivate": { en: "Activate", ar: "تفعيل" },
  "lookup.actionDeactivate": { en: "Deactivate", ar: "إيقاف" },
  "lookup.actionDelete": { en: "Delete", ar: "حذف" },
  "lookup.actionAddValue": { en: "Add", ar: "إضافة" },
  "lookup.actionBackToHub": { en: "Back to Lookup Types", ar: "العودة إلى أنواع القوائم" },
  "lookup.actionCreateType": { en: "Create Lookup Type", ar: "إنشاء نوع القائمة" },
  "lookup.fieldTypeCode": { en: "Code", ar: "الرمز" },
  "lookup.fieldTypeName": { en: "Name", ar: "الاسم" },
  "lookup.fieldTypeNameArabic": { en: "Name (Arabic)", ar: "الاسم (عربي)" },
  "lookup.newCodePlaceholder": { en: "New code", ar: "رمز جديد" },
  "lookup.newNamePlaceholder": { en: "New name", ar: "اسم جديد" },
  "lookup.newNameArabicPlaceholder": { en: "New name (Arabic)", ar: "اسم جديد (عربي)" },
  "nav.usersArea": { en: "Users", ar: "المستخدمون" },
  "nav.allUsers": { en: "All Users", ar: "جميع المستخدمين" },
  "auth.signInPrompt": { en: "Sign in to continue.", ar: "سجّل الدخول للمتابعة." },
  "auth.usernameLabel": { en: "Username", ar: "اسم المستخدم" },
  "auth.passwordLabel": { en: "Password", ar: "كلمة المرور" },
  "auth.loginButton": { en: "Log In", ar: "تسجيل الدخول" },
  "auth.invalidCredentials": { en: "Invalid username or password.", ar: "اسم المستخدم أو كلمة المرور غير صحيحة." },
  "auth.loggedInAs": { en: "Logged in as {username}", ar: "مسجل الدخول باسم {username}" },
  "auth.logoutButton": { en: "Logout", ar: "تسجيل الخروج" },
  "users.heading": { en: "Users", ar: "المستخدمون" },
  "users.newHeading": { en: "New User", ar: "مستخدم جديد" },
  "users.emptyState": { en: "No users yet.", ar: "لا يوجد مستخدمون بعد." },
  "users.selectHint": { en: "Select a user from the list to see its details.", ar: "اختر مستخدمًا من القائمة لعرض تفاصيله." },
  "users.actionNew": { en: "New", ar: "جديد" },
  "users.actionCreate": { en: "Create", ar: "إنشاء" },
  "users.actionBack": { en: "Back to list", ar: "العودة إلى القائمة" },
  "users.actionActivate": { en: "Activate", ar: "تفعيل" },
  "users.actionDeactivate": { en: "Deactivate", ar: "إيقاف" },
  "users.actionAssignRole": { en: "Assign Role", ar: "تعيين دور" },
  "users.actionRemoveRole": { en: "Remove", ar: "إزالة" },
  "users.actionGrantExceptionAndAssign": { en: "Grant Exception & Assign", ar: "منح استثناء وتعيين" },
  "users.actionResetPassword": { en: "Reset Password", ar: "إعادة تعيين كلمة المرور" },
  "users.columnUsername": { en: "Username", ar: "اسم المستخدم" },
  "users.columnDisplayName": { en: "Display Name", ar: "الاسم المعروض" },
  "users.columnStatus": { en: "Status", ar: "الحالة" },
  "users.statusActive": { en: "Active", ar: "فعال" },
  "users.statusInactive": { en: "Inactive", ar: "غير فعال" },
  "users.fieldUsername": { en: "Username", ar: "اسم المستخدم" },
  "users.fieldDisplayName": { en: "Display Name", ar: "الاسم المعروض" },
  "users.fieldEmail": { en: "Email", ar: "البريد الإلكتروني" },
  "users.fieldPassword": { en: "Password", ar: "كلمة المرور" },
  "users.fieldNewPassword": { en: "New Password", ar: "كلمة مرور جديدة" },
  "users.fieldRoleKey": { en: "Role Key", ar: "رمز الدور" },
  "users.fieldOverrideReason": { en: "Override Reason", ar: "سبب التجاوز" },
  "users.tabRoles": { en: "Roles", ar: "الأدوار" },
  "users.tabPassword": { en: "Password", ar: "كلمة المرور" },
  "users.emptyRoles": { en: "No roles assigned yet.", ar: "لم يتم تعيين أي أدوار بعد." },
  "users.sodConflictHeading": { en: "Segregation of Duties conflict:", ar: "تعارض في فصل المهام:" },
};

export function t(key: TranslationKey, language: SupportedLanguageCode): string {
  return content[key][language];
}
