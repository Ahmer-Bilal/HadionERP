using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

/// <summary>
/// Idempotent startup seeding for the lookup engine's system-defined types — see
/// <see cref="LookupType.IsSystemDefined"/>'s doc comment for why these five exist as code (they back real
/// fields: <c>BusinessRoleType</c>/<c>AddressType</c> replace the enums that used to live in this Domain
/// project, <c>Country</c>/<c>UnitOfMeasure</c> replace previously-unvalidated free text, <c>Trade</c> backs
/// the roadmap's own "suggested-values list" design). Runs every startup (see Gateway.Api's
/// <c>Program.cs</c>) and only inserts what's missing — an administrator's own edits/additions through the
/// Lookup Data admin panel are never touched or overwritten.
/// </summary>
public static class LookupSeeder
{
    public static async Task SeedAsync(MasterDataDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedTypeAsync(dbContext, "BusinessRoleType", "Business Role Type", "نوع دور الشريك التجاري", BusinessRoleTypes, cancellationToken);
        await SeedTypeAsync(dbContext, "AddressType", "Address Type", "نوع العنوان", AddressTypes, cancellationToken);
        await SeedTypeAsync(dbContext, "Country", "Country", "الدولة", Countries, cancellationToken);
        await SeedTypeAsync(dbContext, "UnitOfMeasure", "Unit of Measure", "وحدة القياس", UnitsOfMeasure, cancellationToken);
        // Trade is split into one lookup type PER role family, not one flat undifferentiated list —
        // ROADMAP.md's own Phase 2 design is explicit that a Subcontractor's trades
        // (Electrical/Concrete/Steel Structure/...) are a different real-world taxonomy from a Supplier's
        // (Steel/Cement/MEP Materials/...) or a Consultant's (Structural/Architectural/MEP Design/...) —
        // mixing all three into one list would offer a Consultant "Earthworks" as a suggestion, which is
        // nonsensical. Each is its own Lookup Type so it gets its own admin-panel heading, matching how the
        // roadmap itself already describes these as three separate taxonomies.
        await SeedTypeAsync(dbContext, "SubcontractorTrade", "Subcontractor Trade", "تخصص مقاول الباطن", SubcontractorTrades, cancellationToken);
        await SeedTypeAsync(dbContext, "SupplierTrade", "Supplier Trade", "تخصص المورد", SupplierTrades, cancellationToken);
        await SeedTypeAsync(dbContext, "ConsultantTrade", "Consultant Trade", "تخصص الاستشاري", ConsultantTrades, cancellationToken);
        // Backs Modules.Finance.Domain.Payment.PaymentMethod (`MISSING-FEATURES-AUDIT.md` Part 2 §16) —
        // validated cross-module via the new Modules.MasterData.Contracts.ILookupCatalog, same "add a real
        // Contracts publication the moment a real consumer outside this module needs it" reasoning this
        // module's own README already anticipated.
        await SeedTypeAsync(dbContext, "PaymentMethod", "Payment Method", "طريقة الدفع", PaymentMethods, cancellationToken);
        // Backs Modules.Construction.Domain.Contract.ContractType — same cross-module ILookupCatalog
        // validation pattern as PaymentMethod above.
        await SeedTypeAsync(dbContext, "ContractType", "Contract Type", "نوع العقد", ContractTypes, cancellationToken);
    }

    private static async Task SeedTypeAsync(
        MasterDataDbContext dbContext, string code, string name, string nameArabic,
        (string Code, string Name, string? NameArabic)[] values, CancellationToken cancellationToken)
    {
        var type = await dbContext.LookupTypes.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);
        if (type is null)
        {
            type = new LookupType("system", code, name, nameArabic, isSystemDefined: true);
            dbContext.LookupTypes.Add(type);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingCodes = await dbContext.LookupValues
            .Where(v => v.LookupTypeCode == code)
            .Select(v => v.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existingCodes.ToHashSet();

        var sortOrder = existingCodes.Count;
        var added = false;
        foreach (var (valueCode, valueName, valueNameArabic) in values)
        {
            if (existingSet.Contains(valueCode)) continue;
            dbContext.LookupValues.Add(new LookupValue("system", code, valueCode, valueName, valueNameArabic, sortOrder++));
            added = true;
        }

        if (added) await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly (string, string, string?)[] BusinessRoleTypes =
    {
        ("Client", "Client", "عميل"),
        ("Supplier", "Supplier", "مورد"),
        ("Subcontractor", "Subcontractor", "مقاول من الباطن"),
        ("Consultant", "Consultant", "استشاري"),
        ("JointVenturePartner", "Joint Venture Partner", "شريك ائتلاف"),
        ("GovernmentAuthority", "Government Authority", "جهة حكومية"),
        ("RentalCompany", "Rental Company", "شركة تأجير"),
        ("Manufacturer", "Manufacturer", "مُصنّع"),
        ("ManpowerSupplier", "Manpower Supplier", "مورد قوى عاملة"),
        ("TestingLaboratory", "Testing Laboratory", "مختبر فحص"),
    };

    private static readonly (string, string, string?)[] AddressTypes =
    {
        ("HeadOffice", "Head Office", "المكتب الرئيسي"),
        ("Billing", "Billing", "الفوترة"),
        ("Shipping", "Shipping", "الشحن"),
        ("SiteOffice", "Site Office", "مكتب الموقع"),
    };

    private static readonly (string, string, string?)[] UnitsOfMeasure =
    {
        ("EA", "Each", "قطعة"),
        ("KG", "Kilogram", "كيلوغرام"),
        ("TON", "Metric Ton", "طن"),
        ("M2", "Square Meter", "متر مربع"),
        ("M3", "Cubic Meter", "متر مكعب"),
        ("LM", "Linear Meter", "متر طولي"),
        ("HR", "Hour", "ساعة"),
        ("DAY", "Day", "يوم"),
        ("MONTH", "Month", "شهر"),
        ("BAG", "Bag", "كيس"),
        ("LTR", "Liter", "لتر"),
        ("ROLL", "Roll", "لفة"),
        ("BOX", "Box", "صندوق"),
        ("SET", "Set", "طقم"),
        ("LOT", "Lot", "دفعة"),
    };

    private static readonly (string, string, string?)[] PaymentMethods =
    {
        ("BankTransfer", "Bank Transfer", "تحويل بنكي"),
        ("Check", "Check", "شيك"),
        ("CashPayment", "Cash Payment", "دفع نقدي"),
        ("OnlineTransfer", "Online Transfer", "تحويل إلكتروني"),
    };

    private static readonly (string, string, string?)[] ContractTypes =
    {
        ("LumpSum", "Lump Sum", "مبلغ مقطوع"),
        ("UnitPrice", "Unit Price", "سعر الوحدة"),
        ("CostPlus", "Cost Plus", "التكلفة زائد الربح"),
    };

    /// <summary>Matches ROADMAP.md's own Phase 2 example list verbatim (Electrical/
    /// Concrete/Mechanical/Steel Structure/Earthworks), expanded with the rest of a real construction
    /// subcontract package.</summary>
    private static readonly (string, string, string?)[] SubcontractorTrades =
    {
        ("Electrical", "Electrical", "كهرباء"),
        ("Concrete", "Concrete", "خرسانة"),
        ("Mechanical", "Mechanical", "ميكانيكا"),
        ("SteelStructure", "Steel Structure", "الهياكل الحديدية"),
        ("Earthworks", "Earthworks", "أعمال ترابية"),
        ("Plumbing", "Plumbing", "سباكة"),
        ("HVAC", "HVAC", "تكييف وتهوية"),
        ("Finishing", "Finishing", "تشطيبات"),
        ("Scaffolding", "Scaffolding", "سقالات"),
        ("Insulation", "Insulation", "عزل"),
        ("Carpentry", "Carpentry", "نجارة"),
        ("AluminumGlazing", "Aluminum & Glazing", "ألمنيوم وزجاج"),
        ("Painting", "Painting", "دهانات"),
        ("FireProtection", "Fire Protection", "الحماية من الحريق"),
        ("Landscaping", "Landscaping", "تنسيق حدائق"),
    };

    /// <summary>Matches the roadmap's own example list (Steel/Cement/MEP Materials/Aggregates), expanded
    /// with the rest of a real construction material-supply catalog.</summary>
    private static readonly (string, string, string?)[] SupplierTrades =
    {
        ("Steel", "Steel", "حديد"),
        ("Cement", "Cement", "إسمنت"),
        ("MepMaterials", "MEP Materials", "مواد كهروميكانيكية"),
        ("Aggregates", "Aggregates", "ركام"),
        ("Timber", "Timber", "أخشاب"),
        ("PaintsAndCoatings", "Paints & Coatings", "دهانات وطلاءات"),
        ("PlumbingMaterials", "Plumbing Materials", "مواد سباكة"),
        ("ElectricalMaterials", "Electrical Materials", "مواد كهربائية"),
        ("GlassAndAluminum", "Glass & Aluminum", "زجاج وألمنيوم"),
        ("SafetyEquipment", "Safety Equipment", "معدات السلامة"),
    };

    /// <summary>Matches the roadmap's own example list (Structural/Architectural/MEP Design/Geotechnical),
    /// expanded with the rest of a real consulting-services catalog.</summary>
    private static readonly (string, string, string?)[] ConsultantTrades =
    {
        ("Structural", "Structural", "إنشائي"),
        ("Architectural", "Architectural", "معماري"),
        ("MepDesign", "MEP Design", "تصميم كهروميكانيكي"),
        ("Geotechnical", "Geotechnical", "جيوتقني"),
        ("Survey", "Survey", "مساحة"),
        ("SoilTesting", "Soil Testing", "فحص التربة"),
        ("ProjectManagement", "Project Management", "إدارة المشاريع"),
        ("QuantitySurveying", "Quantity Surveying", "حصر الكميات"),
    };

    /// <summary>Countries an ERP built for a Saudi construction/EPC company realistically transacts with —
    /// every GCC/Levant/wider-MENA country plus every major global trading partner, not an exhaustive
    /// 249-territory ISO list (an admin can add any missing one through the Lookup Data panel in seconds —
    /// that's the entire point of this being admin-editable rather than a hardcoded enum).</summary>
    private static readonly (string, string, string?)[] Countries =
    {
        ("Saudi Arabia", "Saudi Arabia", "المملكة العربية السعودية"),
        ("United Arab Emirates", "United Arab Emirates", "الإمارات العربية المتحدة"),
        ("Kuwait", "Kuwait", "الكويت"),
        ("Qatar", "Qatar", "قطر"),
        ("Bahrain", "Bahrain", "البحرين"),
        ("Oman", "Oman", "عُمان"),
        ("Yemen", "Yemen", "اليمن"),
        ("Jordan", "Jordan", "الأردن"),
        ("Lebanon", "Lebanon", "لبنان"),
        ("Syria", "Syria", "سوريا"),
        ("Iraq", "Iraq", "العراق"),
        ("Egypt", "Egypt", "مصر"),
        ("Palestine", "Palestine", "فلسطين"),
        ("Libya", "Libya", "ليبيا"),
        ("Tunisia", "Tunisia", "تونس"),
        ("Algeria", "Algeria", "الجزائر"),
        ("Morocco", "Morocco", "المغرب"),
        ("Sudan", "Sudan", "السودان"),
        ("Somalia", "Somalia", "الصومال"),
        ("Djibouti", "Djibouti", "جيبوتي"),
        ("Mauritania", "Mauritania", "موريتانيا"),
        ("Turkey", "Turkey", "تركيا"),
        ("Iran", "Iran", "إيران"),
        ("Pakistan", "Pakistan", "باكستان"),
        ("India", "India", "الهند"),
        ("Bangladesh", "Bangladesh", "بنغلاديش"),
        ("Sri Lanka", "Sri Lanka", "سريلانكا"),
        ("Nepal", "Nepal", "نيبال"),
        ("Philippines", "Philippines", "الفلبين"),
        ("Indonesia", "Indonesia", "إندونيسيا"),
        ("Malaysia", "Malaysia", "ماليزيا"),
        ("Thailand", "Thailand", "تايلاند"),
        ("Vietnam", "Vietnam", "فيتنام"),
        ("China", "China", "الصين"),
        ("Japan", "Japan", "اليابان"),
        ("South Korea", "South Korea", "كوريا الجنوبية"),
        ("Singapore", "Singapore", "سنغافورة"),
        ("United Kingdom", "United Kingdom", "المملكة المتحدة"),
        ("Ireland", "Ireland", "أيرلندا"),
        ("Germany", "Germany", "ألمانيا"),
        ("France", "France", "فرنسا"),
        ("Italy", "Italy", "إيطاليا"),
        ("Spain", "Spain", "إسبانيا"),
        ("Portugal", "Portugal", "البرتغال"),
        ("Netherlands", "Netherlands", "هولندا"),
        ("Belgium", "Belgium", "بلجيكا"),
        ("Switzerland", "Switzerland", "سويسرا"),
        ("Austria", "Austria", "النمسا"),
        ("Sweden", "Sweden", "السويد"),
        ("Norway", "Norway", "النرويج"),
        ("Denmark", "Denmark", "الدنمارك"),
        ("Finland", "Finland", "فنلندا"),
        ("Poland", "Poland", "بولندا"),
        ("Czech Republic", "Czech Republic", "التشيك"),
        ("Greece", "Greece", "اليونان"),
        ("Russia", "Russia", "روسيا"),
        ("Ukraine", "Ukraine", "أوكرانيا"),
        ("United States", "United States", "الولايات المتحدة الأمريكية"),
        ("Canada", "Canada", "كندا"),
        ("Mexico", "Mexico", "المكسيك"),
        ("Brazil", "Brazil", "البرازيل"),
        ("Argentina", "Argentina", "الأرجنتين"),
        ("South Africa", "South Africa", "جنوب أفريقيا"),
        ("Nigeria", "Nigeria", "نيجيريا"),
        ("Kenya", "Kenya", "كينيا"),
        ("Ethiopia", "Ethiopia", "إثيوبيا"),
        ("Australia", "Australia", "أستراليا"),
        ("New Zealand", "New Zealand", "نيوزيلندا"),
        ("Afghanistan", "Afghanistan", "أفغانستان"),
        ("Azerbaijan", "Azerbaijan", "أذربيجان"),
        ("Georgia", "Georgia", "جورجيا"),
        ("Armenia", "Armenia", "أرمينيا"),
        ("Cyprus", "Cyprus", "قبرص"),
        ("Maldives", "Maldives", "جزر المالديف"),
    };
}
