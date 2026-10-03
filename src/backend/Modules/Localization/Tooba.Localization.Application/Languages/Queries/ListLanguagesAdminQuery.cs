using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Application.Composition;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;

namespace Tooba.Localization.Application.Languages.Queries;

/// <summary>List all languages for Admin registry.</summary>
public sealed record ListLanguagesAdminQuery : IRequest<Result<IReadOnlyList<LanguageAdminResponse>>>;

/// <summary>Handler for <see cref="ListLanguagesAdminQuery"/>.</summary>
public sealed class ListLanguagesAdminQueryHandler(
    ILanguageDirectory directory,
    ILogger<ListLanguagesAdminQueryHandler> logger)
    : IRequestHandler<ListLanguagesAdminQuery, Result<IReadOnlyList<LanguageAdminResponse>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LanguageAdminResponse>>> Handle(
        ListLanguagesAdminQuery request,
        CancellationToken cancellationToken)
    {
        var outcome = await LocalizationOperation.ExecuteAsync(async () =>
        {
            var rows = await directory.ListAdminAsync(cancellationToken);
            return (IReadOnlyList<LanguageAdminResponse>)rows.Select(LanguageMappings.ToAdminResponse).ToList();
        });

        if (outcome.IsFailure)
        {
            logger.LogInformation("localization.languages.list.failed");
            return outcome;
        }

        logger.LogInformation("localization.languages.list.succeeded");
        return outcome;
    }
}
