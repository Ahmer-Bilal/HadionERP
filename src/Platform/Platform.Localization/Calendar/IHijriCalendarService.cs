namespace Platform.Localization.Calendar;

/// <summary>
/// Converts between the Gregorian calendar (the only calendar ever stored — doc 03 #1.2: "always stored
/// in Gregorian/UTC internally... conversion is a presentation concern only") and the Hijri calendar (what
/// a Saudi user may prefer to see on screen or on a printed document).
/// </summary>
public interface IHijriCalendarService
{
    HijriDate ToHijri(DateOnly gregorianDate);
    DateOnly ToGregorian(HijriDate hijriDate);
}
