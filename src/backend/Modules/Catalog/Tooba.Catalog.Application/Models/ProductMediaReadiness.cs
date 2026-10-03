using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>آمادگی گالری رسانه برای انتشار بعدی (T016).</summary>
public sealed record ProductMediaReadiness(
    bool HasPrimaryImage,
    int MediaCount,
    bool IsReady,
    string? MessageFa);
