using System.Globalization;

namespace Platform.Localization.Calendar;

/// <summary>
/// Wraps .NET's built-in <see cref="UmAlQuraCalendar"/> — a table-based calendar licensed from the Saudi
/// government (not an astronomical approximation), which is exactly the "Hijri (Umm al-Qura)" calendar
/// docs/architecture/03-platform-services.md #1.2 calls for. Using the built-in .NET type means no
/// external dependency and no home-grown conversion math to get subtly wrong.
///
/// Supported range is 1900-04-30 through 2077-11-16 (Gregorian) — the range .NET's implementation ships
/// data for. That comfortably covers this platform's operating lifetime; dates outside it throw rather
/// than silently producing a wrong result.
/// </summary>
public sealed class UmAlQuraHijriCalendarService : IHijriCalendarService
{
    private readonly UmAlQuraCalendar _calendar = new();

    public HijriDate ToHijri(DateOnly gregorianDate)
    {
        var dateTime = gregorianDate.ToDateTime(TimeOnly.MinValue);
        EnsureSupported(dateTime);

        return new HijriDate(_calendar.GetYear(dateTime), _calendar.GetMonth(dateTime), _calendar.GetDayOfMonth(dateTime));
    }

    public DateOnly ToGregorian(HijriDate hijriDate)
    {
        var dateTime = _calendar.ToDateTime(hijriDate.Year, hijriDate.Month, hijriDate.Day, 0, 0, 0, 0);
        return DateOnly.FromDateTime(dateTime);
    }

    private void EnsureSupported(DateTime dateTime)
    {
        if (dateTime < _calendar.MinSupportedDateTime || dateTime > _calendar.MaxSupportedDateTime)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                $"Date {dateTime:yyyy-MM-dd} is outside the Umm al-Qura calendar's supported range " +
                $"({_calendar.MinSupportedDateTime:yyyy-MM-dd} to {_calendar.MaxSupportedDateTime:yyyy-MM-dd}).");
        }
    }
}
