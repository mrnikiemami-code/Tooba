using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Application.Composition;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;

namespace Tooba.Localization.Application.Languages.Commands;

/// <summary>Create a language in the Admin registry.</summary>
public sealed record CreateLanguageCommand(
    string Code,
    string UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool IsActive,
    bool IsDefault,
    int SortOrder) : IRequest<Result<LanguageAdminResponse>>;

/// <summary>Handler for <see cref="CreateLanguageCommand"/>.</summary>
public sealed class CreateLanguageCommandHandler(
    ILanguageDirectory directory,
    ILogger<CreateLanguageCommandHandler> logger)
    : IRequestHandler<CreateLanguageCommand, Result<LanguageAdminResponse>>
{
    /// <inheritdoc />
    public async Task<Result<LanguageAdminResponse>> Handle(
        CreateLanguageCommand request,
        CancellationToken cancellationToken)
    {
        var outcome = await LocalizationOperation.ExecuteAsync(async () =>
        {
            var created = await directory.CreateAsync(
                new CreateLanguageSpec(
                    request.Code,
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
            return LanguageMappings.ToAdminResponse(created, isReferenced: false);
        });

        if (outcome.IsFailure)
        {
            logger.LogInformation("localization.language.create.failed");
            return outcome;
        }

        logger.LogInformation("localization.language.create.succeeded");
        return outcome;
    }
}
