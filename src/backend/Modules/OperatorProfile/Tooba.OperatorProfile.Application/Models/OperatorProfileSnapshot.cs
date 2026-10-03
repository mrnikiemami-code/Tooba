namespace Tooba.OperatorProfile.Application.Models;

/// <summary>نمایهٔ خصوصی پروفایل اپراتور بدون شناسهٔ مالک در پاسخ API.</summary>
public sealed record OperatorProfileSnapshot(
    string FirstName,
    string LastName,
    string DisplayName,
    string? Bio,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن پروفایل اپراتور؛ تنظیمات سراسری platform ندارد.</summary>
public sealed record OperatorProfileWrite(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio);
