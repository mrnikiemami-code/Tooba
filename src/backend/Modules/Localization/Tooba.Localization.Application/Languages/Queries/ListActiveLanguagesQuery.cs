using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Application.Ports;

namespace Tooba.Localization.Application.Languages.Queries;

/// <summary>Public active language registry DTO; excludes admin edit metadata.</summary>
public sealed record LanguagePublicResponse(
    Guid LanguageId,
    string Code,
    string UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool IsActive,
    bool IsDefault,
    int SortOrder);

/// <summary>Read-only active languages for non-admin surfaces.</summary>
public sealed record ListActiveLanguagesQuery : IRequest<Result<IReadOnlyList<LanguagePublicResponse>>>;

/// <summary>Maps active snapshots to the public DTO without admin metadata.</summary>
public sealed class ListActiveLanguagesQueryHandler(ILanguageDirectory directory)
    : IRequestHandler<ListActiveLanguagesQuery, Result<IReadOnlyList<LanguagePublicResponse>>>
{
    public async Task<Result<IReadOnlyList<LanguagePublicResponse>>> Handle(
        ListActiveLanguagesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await directory.ListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LanguagePublicResponse>>(rows
            .Where(row => row.IsActive)
            .OrderBy(row => row.SortOrder)
            .Select(row => new LanguagePublicResponse(
                row.LanguageId, row.Code, row.UrlPrefix, row.DisplayName, row.NativeName,
                row.Direction, row.Culture, row.CalendarDisplay, row.IsActive, row.IsDefault, row.SortOrder))
            .ToList());
    }
}
