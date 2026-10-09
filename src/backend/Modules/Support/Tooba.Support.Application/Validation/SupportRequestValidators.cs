using FluentValidation;
using Tooba.Support.Application.Tickets.Commands;
using Tooba.Support.Application.Tickets.Queries;
using Tooba.Support.Domain.Aggregates;
using Tooba.Support.Domain.Entities;
using Tooba.Support.Domain.Enums;

namespace Tooba.Support.Application.Validation;

/// <summary>
/// اعتبارسنج‌های شکلِ transport برای Support (AMSC W1). دقیقاً همان درخواست‌های قابل‌دسترسی از
/// Endpoint که شکلشان می‌تواند نامعتبر شود اعتبارسنج می‌گیرند؛ درخواست‌های عمداً بدون اعتبارسنج در
/// شواهد W1 با دلیل پایدار ثبت شده‌اند (شناسهٔ route-محدودشدهٔ <c>:guid</c> یا actor مشتق‌شده از
/// سرور). فقط شکلِ transport این‌جا بررسی می‌شود — قواعد کسب‌وکار/دامنه در Domain/Application
/// می‌ماند.
/// </summary>
public sealed class CreateCustomerTicketCommandValidator : AbstractValidator<CreateCustomerTicketCommand>
{
    /// <summary>قواعد شکلِ transport ایجاد تیکت مشتری را تعریف می‌کند.</summary>
    public CreateCustomerTicketCommandValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.SubjectRequired)
            .MaximumLength(SupportTicket.SubjectMaxLength)
            .WithErrorCode(SupportValidationCodes.SubjectTooLong);
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.CategoryRequired)
            .Must(BeKnownCategory)
            .WithErrorCode(SupportValidationCodes.CategoryInvalid);
        RuleFor(x => x.Priority)
            .Must(BeKnownPriorityOrEmpty)
            .WithErrorCode(SupportValidationCodes.PriorityInvalid);
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.BodyRequired)
            .MaximumLength(TicketMessage.BodyMaxLength)
            .WithErrorCode(SupportValidationCodes.BodyTooLong);
        RuleFor(x => x.RelatedEntityType)
            .MaximumLength(SupportTicket.RelatedEntityTypeMaxLength)
            .WithErrorCode(SupportValidationCodes.RelatedEntityTypeTooLong);
        RuleFor(x => x.RelatedEntityId)
            .NotNull()
            .When(x => !string.IsNullOrWhiteSpace(x.RelatedEntityType))
            .WithErrorCode(SupportValidationCodes.RelatedEntityIdRequired);
        RuleFor(x => x.RelatedEntityType)
            .NotEmpty()
            .When(x => x.RelatedEntityId is not null)
            .WithErrorCode(SupportValidationCodes.RelatedEntityTypeRequired);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(SupportTicket.IdempotencyKeyMaxLength)
            .WithErrorCode(SupportValidationCodes.IdempotencyKeyTooLong);
    }

    private static bool BeKnownCategory(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && Enum.TryParse<TicketCategory>(raw, ignoreCase: true, out var parsed)
        && Enum.IsDefined(parsed);

    private static bool BeKnownPriorityOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketPriority>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}

/// <summary>اعتبارسنجی شکلِ transport ایجاد تیکت فروشنده.</summary>
public sealed class CreateSellerTicketCommandValidator : AbstractValidator<CreateSellerTicketCommand>
{
    /// <summary>قواعد شکلِ transport ایجاد تیکت فروشنده را تعریف می‌کند.</summary>
    public CreateSellerTicketCommandValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.SubjectRequired)
            .MaximumLength(SupportTicket.SubjectMaxLength)
            .WithErrorCode(SupportValidationCodes.SubjectTooLong);
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.CategoryRequired)
            .Must(BeKnownCategory)
            .WithErrorCode(SupportValidationCodes.CategoryInvalid);
        RuleFor(x => x.Priority)
            .Must(BeKnownPriorityOrEmpty)
            .WithErrorCode(SupportValidationCodes.PriorityInvalid);
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.BodyRequired)
            .MaximumLength(TicketMessage.BodyMaxLength)
            .WithErrorCode(SupportValidationCodes.BodyTooLong);
        RuleFor(x => x.RelatedEntityType)
            .MaximumLength(SupportTicket.RelatedEntityTypeMaxLength)
            .WithErrorCode(SupportValidationCodes.RelatedEntityTypeTooLong);
        RuleFor(x => x.RelatedEntityId)
            .NotNull()
            .When(x => !string.IsNullOrWhiteSpace(x.RelatedEntityType))
            .WithErrorCode(SupportValidationCodes.RelatedEntityIdRequired);
        RuleFor(x => x.RelatedEntityType)
            .NotEmpty()
            .When(x => x.RelatedEntityId is not null)
            .WithErrorCode(SupportValidationCodes.RelatedEntityTypeRequired);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(SupportTicket.IdempotencyKeyMaxLength)
            .WithErrorCode(SupportValidationCodes.IdempotencyKeyTooLong);
    }

    private static bool BeKnownCategory(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && Enum.TryParse<TicketCategory>(raw, ignoreCase: true, out var parsed)
        && Enum.IsDefined(parsed);

    private static bool BeKnownPriorityOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketPriority>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}

/// <summary>اعتبارسنجی شکلِ transport پاسخ مشتری.</summary>
public sealed class ReplyCustomerTicketCommandValidator : AbstractValidator<ReplyCustomerTicketCommand>
{
    /// <summary>قواعد شکلِ transport پاسخ مشتری را تعریف می‌کند.</summary>
    public ReplyCustomerTicketCommandValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.BodyRequired)
            .MaximumLength(TicketMessage.BodyMaxLength)
            .WithErrorCode(SupportValidationCodes.BodyTooLong);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(TicketMessage.IdempotencyKeyMaxLength)
            .WithErrorCode(SupportValidationCodes.IdempotencyKeyTooLong);
    }
}

/// <summary>اعتبارسنجی شکلِ transport پاسخ فروشنده.</summary>
public sealed class ReplySellerTicketCommandValidator : AbstractValidator<ReplySellerTicketCommand>
{
    /// <summary>قواعد شکلِ transport پاسخ فروشنده را تعریف می‌کند.</summary>
    public ReplySellerTicketCommandValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.BodyRequired)
            .MaximumLength(TicketMessage.BodyMaxLength)
            .WithErrorCode(SupportValidationCodes.BodyTooLong);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(TicketMessage.IdempotencyKeyMaxLength)
            .WithErrorCode(SupportValidationCodes.IdempotencyKeyTooLong);
    }
}

/// <summary>اعتبارسنجی شکلِ transport پاسخ مدیر.</summary>
public sealed class ReplyAdminTicketCommandValidator : AbstractValidator<ReplyAdminTicketCommand>
{
    /// <summary>قواعد شکلِ transport پاسخ مدیر را تعریف می‌کند.</summary>
    public ReplyAdminTicketCommandValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty()
            .WithErrorCode(SupportValidationCodes.BodyRequired)
            .MaximumLength(TicketMessage.BodyMaxLength)
            .WithErrorCode(SupportValidationCodes.BodyTooLong);
        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(TicketMessage.IdempotencyKeyMaxLength)
            .WithErrorCode(SupportValidationCodes.IdempotencyKeyTooLong);
    }
}

/// <summary>اعتبارسنجی شکلِ transport پچ مدیر.</summary>
public sealed class PatchAdminTicketCommandValidator : AbstractValidator<PatchAdminTicketCommand>
{
    /// <summary>قواعد شکلِ transport پچ مدیر را تعریف می‌کند.</summary>
    public PatchAdminTicketCommandValidator()
    {
        RuleFor(x => x.Status)
            .Must(BeKnownStatusOrEmpty)
            .WithErrorCode(SupportValidationCodes.StatusInvalid);
        RuleFor(x => x.Priority)
            .Must(BeKnownPriorityOrEmpty)
            .WithErrorCode(SupportValidationCodes.PriorityInvalid);
    }

    private static bool BeKnownStatusOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));

    private static bool BeKnownPriorityOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketPriority>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}

/// <summary>اعتبارسنجی شکلِ transport فهرست تیکت‌های مشتری.</summary>
public sealed class ListCustomerTicketsQueryValidator : AbstractValidator<ListCustomerTicketsQuery>
{
    /// <summary>قواعد شکلِ transport فهرست مشتری را تعریف می‌کند.</summary>
    public ListCustomerTicketsQueryValidator()
        => RuleFor(x => x.Status)
            .Must(BeKnownStatusOrEmpty)
            .WithErrorCode(SupportValidationCodes.StatusInvalid);

    private static bool BeKnownStatusOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}

/// <summary>اعتبارسنجی شکلِ transport فهرست تیکت‌های فروشنده.</summary>
public sealed class ListSellerTicketsQueryValidator : AbstractValidator<ListSellerTicketsQuery>
{
    /// <summary>قواعد شکلِ transport فهرست فروشنده را تعریف می‌کند.</summary>
    public ListSellerTicketsQueryValidator()
        => RuleFor(x => x.Status)
            .Must(BeKnownStatusOrEmpty)
            .WithErrorCode(SupportValidationCodes.StatusInvalid);

    private static bool BeKnownStatusOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}

/// <summary>اعتبارسنجی شکلِ transport فهرست مدیر (چهار فیلتر آزاد + عبارت جست‌وجو).</summary>
public sealed class ListAdminTicketsQueryValidator : AbstractValidator<ListAdminTicketsQuery>
{
    private const int SearchMaxLength = 200;

    /// <summary>قواعد شکلِ transport فهرست مدیر را تعریف می‌کند.</summary>
    public ListAdminTicketsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(BeKnownStatusOrEmpty)
            .WithErrorCode(SupportValidationCodes.StatusInvalid);
        RuleFor(x => x.RequesterKind)
            .Must(BeKnownRequesterKindOrEmpty)
            .WithErrorCode(SupportValidationCodes.RequesterKindInvalid);
        RuleFor(x => x.Category)
            .Must(BeKnownCategoryOrEmpty)
            .WithErrorCode(SupportValidationCodes.CategoryInvalid);
        RuleFor(x => x.Priority)
            .Must(BeKnownPriorityOrEmpty)
            .WithErrorCode(SupportValidationCodes.PriorityInvalid);
        RuleFor(x => x.Q)
            .MaximumLength(SearchMaxLength)
            .WithErrorCode(SupportValidationCodes.SearchTooLong);
    }

    private static bool BeKnownStatusOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketStatus>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));

    private static bool BeKnownRequesterKindOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<RequesterKind>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));

    private static bool BeKnownCategoryOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketCategory>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));

    private static bool BeKnownPriorityOrEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
        || (Enum.TryParse<TicketPriority>(raw, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
}
