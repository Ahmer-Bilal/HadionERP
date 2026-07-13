namespace Platform.Core.NumberRanges;

/// <summary>
/// Configuration for one document-numbering rule. In production this is a configuration record
/// (docs/architecture/04-data-and-api.md §3), not code — a functional consultant maintains these,
/// they are not hard-coded per module. This type is the shape that configuration fills in.
/// </summary>
public sealed record NumberRangeDefinition(string RangeKey, string ModuleAbbreviation, string DocumentAbbreviation, int SequenceDigits = 6)
{
    public string Format(int fiscalYear, long sequence)
        => $"{ModuleAbbreviation}-{DocumentAbbreviation}-{fiscalYear}-{sequence.ToString().PadLeft(SequenceDigits, '0')}";
}
