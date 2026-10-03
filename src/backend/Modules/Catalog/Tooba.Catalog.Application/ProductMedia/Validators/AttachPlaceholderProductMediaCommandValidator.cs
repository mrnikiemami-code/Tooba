using FluentValidation;
using Tooba.Catalog.Application.ProductMedia.Commands;

namespace Tooba.Catalog.Application.ProductMedia.Validators;

/// <summary>Transport validator for AttachPlaceholderProductMediaCommand.</summary>
public sealed class AttachPlaceholderProductMediaCommandValidator : AbstractValidator<AttachPlaceholderProductMediaCommand>
{
    /// <summary>Creates the validator.</summary>
    public AttachPlaceholderProductMediaCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}