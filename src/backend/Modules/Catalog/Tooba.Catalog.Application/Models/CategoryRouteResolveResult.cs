using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>نتیجهٔ resolve مسیر رده.</summary>
public sealed record CategoryRouteResolveResult(
    Guid CategoryId,
    string Locale,
    string CurrentSlug,
    bool IsRedirect,
    string CanonicalPath);
