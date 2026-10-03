using FluentValidation;
using MediatR;
using Tooba.OperatorProfile.Application.Models;
using Tooba.OperatorProfile.Application.Ports;
using Tooba.OperatorProfile.Contracts.Errors;
using DomainProfile = Tooba.OperatorProfile.Domain.Aggregates.OperatorProfile;

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
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode(OperatorProfileErrorCodes.ActorRequired);
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(DomainProfile.DisplayNameMinLength)
            .MaximumLength(DomainProfile.DisplayNameMaxLength)
            .WithErrorCode(OperatorProfileErrorCodes.InvalidDisplayName);
        RuleFor(x => x.FirstName!)
            .MaximumLength(DomainProfile.NamePartMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName))
            .WithErrorCode(OperatorProfileErrorCodes.InvalidFirstName);
        RuleFor(x => x.LastName!)
            .MaximumLength(DomainProfile.NamePartMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName))
            .WithErrorCode(OperatorProfileErrorCodes.InvalidLastName);
        RuleFor(x => x.Bio!)
            .MaximumLength(DomainProfile.BioMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Bio))
            .WithErrorCode(OperatorProfileErrorCodes.InvalidBio);
    }
}
