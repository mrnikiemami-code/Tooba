using Tooba.BuildingBlocks;
using Tooba.Support.Domain.ValueObjects;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Composition;

/// <summary>
/// کمک‌های پارس enum برای مرز Application.
/// <para>
/// مقادیر نامعتبر با خطای نوع‌دار <see cref="ContractOperationException"/> و کد پایدار Support
/// اعلام می‌شوند تا تشخیص خطا فقط بر پایهٔ کد نوع‌دار انجام شود، نه متن پیام. در عمل این مقادیر در
/// مرز transport توسط اعتبارسنج‌های FluentValidation رد می‌شوند؛ این متدها برای فراخوان‌های داخلی
/// و مسیرهای دفاعی باقی می‌مانند.
/// </para>
/// </summary>
public static class SupportEnumParsing
{
    /// <summary>دسته را پارس می‌کند.</summary>
    /// <param name="value">مقدار متنی دسته.</param>
    /// <returns>دستهٔ معتبر.</returns>
    /// <exception cref="ContractOperationException">وقتی مقدار یک دستهٔ شناخته‌شده نباشد.</exception>
    public static TicketCategory ParseCategory(string value) =>
        Enum.TryParse<TicketCategory>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ContractOperationException(SupportErrorCodes.CategoryInvalid);

    /// <summary>اولویت را پارس می‌کند؛ مقدار خالی به پیش‌فرض Normal نگاشت می‌شود.</summary>
    /// <param name="value">مقدار متنی اولویت (اختیاری).</param>
    /// <returns>اولویت معتبر.</returns>
    /// <exception cref="ContractOperationException">وقتی مقدار یک اولویت شناخته‌شده نباشد.</exception>
    public static TicketPriority ParsePriority(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? TicketPriority.Normal
            : Enum.TryParse<TicketPriority>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new ContractOperationException(SupportErrorCodes.PriorityInvalid);

    /// <summary>وضعیت اختیاری فیلتر را پارس می‌کند.</summary>
    /// <param name="value">مقدار متنی وضعیت (اختیاری).</param>
    /// <returns>وضعیت معتبر یا <c>null</c> برای مقدار خالی.</returns>
    /// <exception cref="ContractOperationException">وقتی مقدار یک وضعیت شناخته‌شده نباشد.</exception>
    public static TicketStatus? TryParseStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<TicketStatus>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new ContractOperationException(SupportErrorCodes.StatusInvalid);

    /// <summary>وضعیت اجباری پچ را پارس می‌کند.</summary>
    /// <param name="value">مقدار متنی وضعیت.</param>
    /// <returns>وضعیت معتبر.</returns>
    /// <exception cref="ContractOperationException">وقتی مقدار یک وضعیت شناخته‌شده نباشد.</exception>
    public static TicketStatus ParseStatus(string value) =>
        Enum.TryParse<TicketStatus>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ContractOperationException(SupportErrorCodes.StatusInvalid);

    /// <summary>RequesterKind فیلتر را پارس می‌کند.</summary>
    /// <param name="value">مقدار متنی نوع درخواست‌کننده (اختیاری).</param>
    /// <returns>نوع معتبر یا <c>null</c> برای مقدار خالی.</returns>
    /// <exception cref="ContractOperationException">وقتی مقدار یک نوع شناخته‌شده نباشد.</exception>
    public static RequesterKind? TryParseRequesterKind(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<RequesterKind>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new ContractOperationException(SupportErrorCodes.RequesterKindInvalid);
}
