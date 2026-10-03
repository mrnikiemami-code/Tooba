using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>مورد ناقص آمادگی انتشار با هدایت به تب Workspace.</summary>
public sealed record ProductPublishMissingRequirement(
    string Code,
    string MessageFa,
    string WorkspaceTab);
