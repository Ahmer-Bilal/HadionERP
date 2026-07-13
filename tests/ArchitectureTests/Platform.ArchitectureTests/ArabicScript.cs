namespace Platform.ArchitectureTests;

/// <summary>
/// Detects Arabic-script characters across the Unicode ranges Arabic text realistically uses. Range
/// boundaries are written as plain ASCII hex casts — (char)0x0600, not a literal Arabic character or a
/// \u escape — because this file is the thing verifying correctness elsewhere: its own boundaries need
/// to be exactly reviewable as plain text in a diff, with no dependency on font rendering, copy-paste
/// fidelity, or how an editor/tool chooses to redisplay a Unicode escape.
/// </summary>
internal static class ArabicScript
{
    private static readonly (char Start, char End)[] ArabicRanges =
    {
        ((char)0x0600, (char)0x06FF), // Arabic
        ((char)0x0750, (char)0x077F), // Arabic Supplement
        ((char)0x08A0, (char)0x08FF), // Arabic Extended-A
        ((char)0xFB50, (char)0xFDFF), // Arabic Presentation Forms-A
        ((char)0xFE70, (char)0xFEFF), // Arabic Presentation Forms-B
    };

    public static bool ContainsArabicScript(string text)
    {
        foreach (var ch in text)
        {
            foreach (var (start, end) in ArabicRanges)
            {
                if (ch >= start && ch <= end)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
