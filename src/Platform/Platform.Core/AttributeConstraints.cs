namespace Platform.Core;

/// <summary>
/// Shared attribute-constraint satisfaction logic — used by Platform.Security's ABAC grant checks
/// ("approve up to 50,000 SAR", a ceiling) and Platform.Workflow's step conditions ("only route to a
/// second approver if Amount exceeds 50,000 SAR", a floor). Extracted here rather than duplicated
/// because both are the same underlying question: does this resource context satisfy this set of
/// constraints — just checked in opposite directions depending on the caller's need.
/// </summary>
public static class AttributeConstraints
{
    /// <summary>
    /// Constraint keys named "Max{Attribute}" are numeric upper-bound checks against
    /// resourceContext["{Attribute}"] (e.g. "MaxAmount" vs. resourceContext["Amount"]); "Min{Attribute}"
    /// are numeric lower-bound checks the same way; any other constraint key is an exact-match check. An
    /// unconstrained set (null/empty) always satisfies.
    /// </summary>
    public static bool Satisfies(
        IReadOnlyDictionary<string, string>? constraints,
        IReadOnlyDictionary<string, string>? resourceContext)
    {
        if (constraints is null || constraints.Count == 0)
        {
            return true;
        }

        if (resourceContext is null)
        {
            return false;
        }

        foreach (var (constraintKey, constraintValue) in constraints)
        {
            if (constraintKey.StartsWith("Max", StringComparison.Ordinal))
            {
                if (!TryGetNumericAttribute(resourceContext, constraintKey["Max".Length..], constraintValue, out var actual, out var bound))
                {
                    return false;
                }

                if (actual > bound)
                {
                    return false;
                }
            }
            else if (constraintKey.StartsWith("Min", StringComparison.Ordinal))
            {
                if (!TryGetNumericAttribute(resourceContext, constraintKey["Min".Length..], constraintValue, out var actual, out var bound))
                {
                    return false;
                }

                if (actual < bound)
                {
                    return false;
                }
            }
            else if (!resourceContext.TryGetValue(constraintKey, out var value) || value != constraintValue)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetNumericAttribute(
        IReadOnlyDictionary<string, string> resourceContext,
        string attributeName,
        string constraintValue,
        out decimal actual,
        out decimal bound)
    {
        actual = default;
        bound = default;

        return resourceContext.TryGetValue(attributeName, out var actualRaw)
            && decimal.TryParse(actualRaw, out actual)
            && decimal.TryParse(constraintValue, out bound);
    }
}
