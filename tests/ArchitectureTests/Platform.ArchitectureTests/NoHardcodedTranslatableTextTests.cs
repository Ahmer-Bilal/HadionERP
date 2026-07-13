using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Platform.ArchitectureTests;

/// <summary>
/// Enforces docs/architecture/03-platform-services.md #1.1 at build time, not just by convention: no
/// source file may contain a literal Arabic string unless it is on the explicit allow-list below.
///
/// This exists because the rule was violated once already during development — an early version of
/// Platform.Localization's currency formatter hardcoded Arabic display text directly in formatting
/// logic (see PROGRESS.md, 2026-07-13). A promise to "be careful next time" does not survive a different
/// AI tool or a future session picking up this codebase with no memory of this conversation; an
/// automated test that fails `dotnet test` does. That is the whole point of this project existing.
///
/// Scope: parses actual C# string/char literal TOKENS (via Roslyn), not comments — a comment mentioning
/// example Arabic text for documentation purposes doesn't affect runtime behavior and isn't the problem
/// this guards against, so it's deliberately not flagged.
/// </summary>
public class NoHardcodedTranslatableTextTests
{
    /// <summary>
    /// Files explicitly allowed to contain a literal Arabic string, and why. Adding an entry here is a
    /// deliberate, reviewed decision — it will show up in a code review diff, never a silent workaround.
    /// Paths are relative to the repository's src/ folder, using forward slashes.
    /// </summary>
    private static readonly Dictionary<string, string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Platform/Platform.Localization/LocalizationDefaults.cs"] =
            "The shipped default translated content — SAP's OTR / Dynamics 365's default .resx label " +
            "equivalent. This is the one place literal display text is allowed to live; everything else " +
            "looks text up by resource key through ITranslationService.",

        ["Platform/Platform.Localization/Formatting/ArabicIndicDigits.cs"] =
            "A fixed Unicode digit-mapping table (٠-٩), a structural/technical constant like a Base64 " +
            "alphabet table — not translatable business content, so it isn't subject to the same rule.",
    };

    [Fact]
    public void No_source_file_outside_the_allow_list_contains_a_literal_Arabic_string()
    {
        var srcRoot = Path.Combine(RepoPaths.FindRepoRoot(), "src");
        var violations = new List<string>();

        foreach (var filePath in EnumerateSourceFiles(srcRoot))
        {
            var relativePath = Path.GetRelativePath(srcRoot, filePath).Replace('\\', '/');
            var isAllowed = AllowedFiles.ContainsKey(relativePath);

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath);
            var literalTexts = tree.GetRoot()
                .DescendantTokens()
                .Where(IsLiteralTextToken)
                .Select(token => token.ValueText);

            foreach (var text in literalTexts)
            {
                if (ArabicScript.ContainsArabicScript(text) && !isAllowed)
                {
                    violations.Add($"{relativePath}: literal \"{text}\" contains Arabic script.");
                }
            }
        }

        Assert.True(violations.Count == 0, BuildFailureMessage(violations));
    }

    [Fact]
    public void Every_allow_listed_file_still_exists()
    {
        // Guards the allow-list itself against going stale (e.g. a file rename) — a listed file that
        // silently stops existing should be noticed and cleaned up, not forgotten forever.
        var srcRoot = Path.Combine(RepoPaths.FindRepoRoot(), "src");

        foreach (var relativePath in AllowedFiles.Keys)
        {
            var fullPath = Path.Combine(srcRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(fullPath), $"Allow-listed file '{relativePath}' no longer exists — remove its entry from AllowedFiles.");
        }
    }

    private static bool IsLiteralTextToken(SyntaxToken token) => token.Kind() switch
    {
        SyntaxKind.StringLiteralToken => true,
        SyntaxKind.InterpolatedStringTextToken => true,
        SyntaxKind.CharacterLiteralToken => true,
        SyntaxKind.SingleLineRawStringLiteralToken => true,
        SyntaxKind.MultiLineRawStringLiteralToken => true,
        SyntaxKind.Utf8StringLiteralToken => true,
        _ => false
    };

    private static IEnumerable<string> EnumerateSourceFiles(string srcRoot) =>
        Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    private static string BuildFailureMessage(List<string> violations) =>
        "Found hardcoded Arabic text outside the allowed content files:\n" +
        string.Join("\n", violations) +
        "\n\nMove the text into a dedicated *Defaults.cs content file (see " +
        "src/Platform/Platform.Localization/LocalizationDefaults.cs for the pattern) and look it up via " +
        "ITranslationService instead — see src/Platform/Platform.Localization/README.md. If this really " +
        "is a structural constant (not translatable content), add it to AllowedFiles with a justification.";
}
