namespace Platform.Localization.Calendar;

/// <summary>A date in the Hijri (Islamic) calendar — year in AH, month 1-12, day 1-30.</summary>
public sealed record HijriDate(int Year, int Month, int Day)
{
    public override string ToString() => $"{Year:D4}-{Month:D2}-{Day:D2} AH";
}
