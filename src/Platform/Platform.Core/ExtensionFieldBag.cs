using System.Text.Json;

namespace Platform.Core;

/// <summary>
/// Typed custom-field storage for a Business Object, backed by a single JSON blob (the "extension_data
/// jsonb column" in docs/architecture/04-data-and-api.md §1.3). This is what lets an extension
/// (docs/architecture/05-engineering-standards.md §3) add a field to any BO without a schema migration
/// of core tables.
/// </summary>
public sealed class ExtensionFieldBag
{
    private readonly Dictionary<string, JsonElement> _fields;

    private ExtensionFieldBag(Dictionary<string, JsonElement> fields)
    {
        _fields = fields;
    }

    public static ExtensionFieldBag Empty() => new(new Dictionary<string, JsonElement>());

    public static ExtensionFieldBag FromJson(string json)
    {
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? new Dictionary<string, JsonElement>();
        return new ExtensionFieldBag(parsed);
    }

    public void Set<T>(string fieldName, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        var element = JsonSerializer.SerializeToElement(value);
        _fields[fieldName] = element;
    }

    public bool Has(string fieldName) => _fields.ContainsKey(fieldName);

    public T? Get<T>(string fieldName)
    {
        if (!_fields.TryGetValue(fieldName, out var element))
        {
            return default;
        }

        return element.Deserialize<T>();
    }

    public bool Remove(string fieldName) => _fields.Remove(fieldName);

    public IReadOnlyCollection<string> FieldNames => _fields.Keys;

    public string ToJson() => JsonSerializer.Serialize(_fields);
}
