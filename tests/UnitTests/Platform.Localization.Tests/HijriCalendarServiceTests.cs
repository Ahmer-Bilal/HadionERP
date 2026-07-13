using Platform.Localization.Calendar;

namespace Platform.Localization.Tests;

public class HijriCalendarServiceTests
{
    private readonly UmAlQuraHijriCalendarService _service = new();

    [Theory]
    [InlineData(2026, 7, 13)]
    [InlineData(2000, 1, 1)]
    [InlineData(1950, 6, 15)]
    [InlineData(2077, 1, 1)]
    public void Gregorian_to_Hijri_and_back_round_trips_exactly(int year, int month, int day)
    {
        var original = new DateOnly(year, month, day);

        var hijri = _service.ToHijri(original);
        var backToGregorian = _service.ToGregorian(hijri);

        Assert.Equal(original, backToGregorian);
    }

    [Fact]
    public void Todays_reference_date_falls_in_the_expected_Hijri_year_range()
    {
        // 2026-07-13 Gregorian is roughly 1447-1448 AH — a loose sanity bound, not a hand-computed
        // exact day (the round-trip tests above are what actually prove correctness; this just catches
        // a wildly-wrong calendar, e.g. one that's off by centuries).
        var hijri = _service.ToHijri(new DateOnly(2026, 7, 13));

        Assert.InRange(hijri.Year, 1446, 1449);
    }

    [Fact]
    public void Dates_outside_the_supported_range_throw_instead_of_producing_a_wrong_result()
    {
        var tooEarly = new DateOnly(1850, 1, 1);
        var tooLate = new DateOnly(2100, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.ToHijri(tooEarly));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.ToHijri(tooLate));
    }
}
