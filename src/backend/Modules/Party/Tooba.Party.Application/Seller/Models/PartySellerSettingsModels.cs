namespace Tooba.Party.Application.Seller.Models;

/// <summary>
/// شکل پاسخ مسیر تنظیمات فروشنده.
/// <para>
/// رفتار قرارداد قبلی Host حفظ شده است: نام میدان <c>partyId</c> (نه <c>sellerPartyId</c>) و
/// میدان <c>canManage</c>.
/// </para>
/// </summary>
public sealed record PartySellerSettingsView(
    Guid PartyId,
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine,
    DateTimeOffset UpdatedAt,
    bool CanManage);

/// <summary>نتیجه ساخت-و-تأیید خواندن تنظیمات فروشنده.</summary>
public sealed record PartySellerSettingsReadResult(PartySellerSettingsView View);

/// <summary>ورودی سازمانی به‌روزرسانی تنظیمات فروشنده بدون شناسهٔ فروشندهٔ تحمیلی بدنه.</summary>
public sealed record PartySellerSettingsWriteModel(
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine);
