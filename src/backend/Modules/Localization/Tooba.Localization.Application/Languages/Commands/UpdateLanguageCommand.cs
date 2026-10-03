using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Application.Composition;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;

namespace Tooba.Localization.Application.Languages.Commands;

/// <summary>Replace a language in the Admin registry.</summary>
public sealed record UpdateLanguageCommand(
    string Code,
    string? NextCode,
    string? UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool IsActive,
    bool IsDefault,
    int SortOrder) : IRequest<Result<LanguageAdminResponse>>;

/// <summary>Handler for <see cref="UpdateLanguageCommand"/>.</summary>
public sealed class UpdateLanguageCommandHandler(
    ILanguageDirectory directory,
    ILogger<UpdateLanguageCommandHandler> logger)
    : IRequestHandler<UpdateLanguageCommand, Result<LanguageAdminResponse>>
{
    /// <inheritdoc />
    public async Task<Result<LanguageAdminResponse>> Handle(
        UpdateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var outcome = await LocalizationOperation.ExecuteAsync(async () =>
        {
            var updated = await directory.UpdateAsync(
                request.Code,
                new UpdateLanguageSpec(
                    request.NextCode,
                    request.UrlPrefix,
                    request.DisplayName,
                    request.NativeName,
                    request.Direction,
                    request.Culture,
                    request.CalendarDisplay,
                    request.IsActive,
                    request.IsDefault,
                    request.SortOrder),
                cancellationToken);
            var admin = await directory.GetAdminByCodeAsync(updated.Code, cancellationToken);
            return admin is null
                ? LanguageMappings.ToAdminResponse(updated, isReferenced: false)
                : LanguageMappings.ToAdminResponse(admin);
        });

        if (outcome.IsFailure)
        {
            logger.LogInformation("localization.language.update.failed");
            return outcome;
        }

        logger.LogInformation("localization.language.update.succeeded");
        return outcome;
    }
}
