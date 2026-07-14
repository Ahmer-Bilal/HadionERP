namespace Modules.MasterData.Domain;

/// <summary>
/// How a tax code affects the amount it's applied to — determines whether <see cref="TaxCode.Rate"/> is
/// actually charged. Standard/Zero-Rated/Exempt is the ZATCA VAT taxonomy (Zero-Rated and Exempt both
/// charge 0%, but are reported differently on a VAT return — Zero-Rated is a taxable supply at 0%,
/// Exempt is outside the VAT system entirely).
/// </summary>
public enum TaxType
{
    Standard,
    ZeroRated,
    Exempt,
}
