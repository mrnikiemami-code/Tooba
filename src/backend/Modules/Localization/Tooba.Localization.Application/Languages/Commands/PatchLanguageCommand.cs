using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Application.Composition;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;

namespace Tooba.Localization.Application.Languages.Commands;

/// <summary>Partially update a language in the Admin registry.</summary>
public sealed record PatchLanguageCommand(
    string Code,
    bool? IsActive,
    bool? IsDefault,
    int? SortOrder) : IRequest<Result<LanguageAdminResponse>>;

/// <summary>Handler for <see cref="PatchLanguageCommand"/>.</summary>
public sealed class PatchLanguageCommandHandler(
    ILanguageDirectory directory,
    ILogger<PatchLanguageCommandHandler> logger)
    : IRequestHandler<PatchLanguageCommand, Result<LanguageAdminResponse>>
{
    /// <inheritdoc />
    public async Task<Result<LanguageAdminResponse>> Handle(
        PatchLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var outcome = await LocalizationOperation.ExecuteAsync(async () =>
        {
            var updated = await directory.PatchAsync(
                request.Code,
                new PatchLanguageSpec(request.IsActive, request.IsDefault, request.SortOrder),
                cancellationToken);
            var admin = await directory.GetAdminByCodeAsync(updated.Code, cancellationToken);
            return admin is null
                ? LanguageMappings.ToAdminResponse(updated, isReferenced: false)
                : LanguageMappings.ToAdminResponse(admin);
        });

        if (outcome.IsFailure)
        {
            logger.LogInformation("localization.language.patch.failed");
            return outcome;
        }

        logger.LogInformation("localization.language.patch.succeeded");
        return outcome;
    }
}
