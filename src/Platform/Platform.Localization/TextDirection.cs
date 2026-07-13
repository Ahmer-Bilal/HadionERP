namespace Platform.Localization;

/// <summary>
/// Arabic is a first-class layout direction, not a mirrored afterthought
/// (docs/architecture/02-business-object-model.md #4) — this is the single place that decision is made,
/// so UI code asks this instead of hard-coding "ar means flip everything."
/// </summary>
public enum TextDirection
{
    LeftToRight,
    RightToLeft
}

public static class LanguageDirection
{
    public static TextDirection For(SupportedLanguage language) => language switch
    {
        SupportedLanguage.Arabic => TextDirection.RightToLeft,
        SupportedLanguage.English => TextDirection.LeftToRight,
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    /// <summary>The HTML "dir" attribute value — "rtl" or "ltr".</summary>
    public static string ToHtmlDirAttribute(this TextDirection direction) =>
        direction == TextDirection.RightToLeft ? "rtl" : "ltr";
}
