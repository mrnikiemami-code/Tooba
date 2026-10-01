using FluentValidation;
using MediatR;
using Tooba.OperatorProfile.Application;
using DomainProfile = Tooba.OperatorProfile.Domain.OperatorProfile;

namespace Tooba.OperatorProfile.Application.Admin.Commands;

/// <summary>ایجاد/به‌روزرسانی پروفایل اپراتور برای Actor مجاز.</summary>
public sealed record UpsertOperatorProfileCommand(
    Guid ActorUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio) : IRequest<OperatorProfileSnapshot>;

/// <summary>Handler نوشتن پروفایل اپراتور.</summary>
public sealed class UpsertOperatorProfileCommandHandler(IOperatorProfileDirectory directory)
    : IRequestHandler<UpsertOperatorProfileCommand, OperatorProfileSnapshot>
{
    /// <inheritdoc />
    public Task<OperatorProfileSnapshot> Handle(UpsertOperatorProfileCommand request, CancellationToken cancellationToken)
        => directory.UpsertAsync(
            request.ActorUserId,
            new OperatorProfileWrite(request.DisplayName, request.FirstName, request.LastName, request.Bio),
            cancellationToken);
}

/// <summary>اعتبارسنجی شکل حمل‌ونقل پروفایل اپراتور.</summary>
public sealed class UpsertOperatorProfileCommandValidator : AbstractValidator<UpsertOperatorProfileCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public UpsertOperatorProfileCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode("operator.profile.validation.actor_required");
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(DomainProfile.DisplayNameMinLength)
            .MaximumLength(DomainProfile.DisplayNameMaxLength)
            .WithErrorCode("operator.profile.validation.display_name");
        RuleFor(x => x.FirstName!)
            .MaximumLength(DomainProfile.NamePartMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName))
            .WithErrorCode("operator.profile.validation.first_name");
        RuleFor(x => x.LastName!)
            .MaximumLength(DomainProfile.NamePartMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName))
            .WithErrorCode("operator.profile.validation.last_name");
        RuleFor(x => x.Bio!)
            .MaximumLength(DomainProfile.BioMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Bio))
            .WithErrorCode("operator.profile.validation.bio");
    }
}
