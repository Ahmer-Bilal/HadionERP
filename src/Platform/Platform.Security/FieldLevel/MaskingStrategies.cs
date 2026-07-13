namespace Platform.Security.FieldLevel;

/// <summary>Common masking strategies for sensitive fields (docs/architecture/03-platform-services.md #2.3)
/// — e.g. salary, IBAN, national ID/Iqama number.</summary>
public static class MaskingStrategies
{
    public static string FullMask(string value) => new('*', value.Length);

    /// <summary>Masks all but the last 4 characters — typical for IBAN/account numbers.</summary>
    public static string LastFourVisible(string value) =>
        value.Length <= 4 ? new string('*', value.Length) : new string('*', value.Length - 4) + value[^4..];
}
