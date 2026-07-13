using Platform.Security;

namespace Platform.Security.FieldLevel;

public interface IFieldSecurityService
{
    /// <summary>Returns <paramref name="rawValue"/> unchanged if no policy is registered for
    /// <paramref name="fieldKey"/>, or if <paramref name="principal"/> holds the field's unmask
    /// Privilege; otherwise returns the masked value.</summary>
    string Apply(SecurityPrincipal principal, string fieldKey, string rawValue);
}
