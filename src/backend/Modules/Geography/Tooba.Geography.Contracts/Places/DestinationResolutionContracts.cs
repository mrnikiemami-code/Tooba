namespace Tooba.Geography.Contracts.Places;

/// <summary>شناسه‌های یک مقصد. هیچ‌کدام بخش «هویت» محسوب نمی‌شود مگر شناسهٔ پایدار.</summary>
/// <param name="CountryId">شناسهٔ کشور.</param>
/// <param name="AdministrativeAreaId">شناسهٔ ناحیهٔ اداری اختیاری.</param>
/// <param name="CityId">شناسهٔ شهر اختیاری.</param>
/// <param name="ServiceAreaId">شناسهٔ ناحیهٔ خدمت اختیاری.</param>
public sealed record DestinationIdentifiers(
    Guid CountryId,
    Guid? AdministrativeAreaId = null,
    Guid? CityId = null,
    Guid? ServiceAreaId = null);

/// <summary>نتیجهٔ حل مقصد از ورودی متنی قدیمی.</summary>
public enum DestinationResolutionOutcome
{
    /// <summary>مقصد به‌طور قطعی به شناسه‌های پایدار نگاشت شد.</summary>
    Resolved = 0,

    /// <summary>ورودی با بیش از یک مکان سازگار است و باید رفع ابهام شود.</summary>
    Ambiguous = 1,

    /// <summary>ورودی به هیچ مکانی در فهرست نگاشت نشد.</summary>
    Unresolved = 2,
}

/// <summary>
/// نتیجهٔ حل مقصد. حل‌کننده هرگز هویت نمی‌سازد و فقط متن را به شناسه نگاشت می‌کند.
/// </summary>
/// <param name="Outcome">وضعیت حل.</param>
/// <param name="Identifiers">شناسه‌های حل‌شده؛ تنها وقتی <see cref="Outcome"/> برابر <c>Resolved</c> است مقدار دارد.</param>
/// <param name="CandidateCount">تعداد کاندیدهای سازگار برای رفع ابهام.</param>
public sealed record DestinationResolution(
    DestinationResolutionOutcome Outcome,
    DestinationIdentifiers? Identifiers,
    int CandidateCount)
{
    /// <summary>نتیجهٔ حل‌شده برای شناسه‌های داده‌شده می‌سازد.</summary>
    /// <param name="identifiers">شناسه‌های قطعی مقصد.</param>
    public static DestinationResolution Resolved(DestinationIdentifiers identifiers)
        => new(DestinationResolutionOutcome.Resolved, identifiers, 1);

    /// <summary>نتیجهٔ مبهم با تعداد کاندید می‌سازد.</summary>
    /// <param name="candidateCount">تعداد کاندیدهای سازگار.</param>
    public static DestinationResolution Ambiguous(int candidateCount)
        => new(DestinationResolutionOutcome.Ambiguous, null, Math.Max(1, candidateCount));

    /// <summary>نتیجهٔ حل‌نشده می‌سازد.</summary>
    public static DestinationResolution Unresolved()
        => new(DestinationResolutionOutcome.Unresolved, null, 0);
}
