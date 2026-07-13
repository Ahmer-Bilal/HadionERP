namespace Platform.Localization.Tests;

public class LanguageDirectionTests
{
    [Fact]
    public void Arabic_is_right_to_left_and_English_is_left_to_right()
    {
        Assert.Equal(TextDirection.RightToLeft, LanguageDirection.For(SupportedLanguage.Arabic));
        Assert.Equal(TextDirection.LeftToRight, LanguageDirection.For(SupportedLanguage.English));
    }

    [Fact]
    public void Maps_to_the_correct_html_dir_attribute()
    {
        Assert.Equal("rtl", LanguageDirection.For(SupportedLanguage.Arabic).ToHtmlDirAttribute());
        Assert.Equal("ltr", LanguageDirection.For(SupportedLanguage.English).ToHtmlDirAttribute());
    }

    [Fact]
    public void Language_codes_round_trip()
    {
        Assert.Equal("ar", SupportedLanguage.Arabic.ToCode());
        Assert.Equal("en", SupportedLanguage.English.ToCode());
        Assert.Equal(SupportedLanguage.Arabic, SupportedLanguageCodes.FromCode("ar"));
        Assert.Equal(SupportedLanguage.English, SupportedLanguageCodes.FromCode("en"));
    }
}
