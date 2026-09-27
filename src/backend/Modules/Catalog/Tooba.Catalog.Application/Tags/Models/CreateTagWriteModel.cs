namespace Tooba.Catalog.Application.Tags.Models;

/// <summary>Write model for Admin tag create.</summary>
public sealed record CreateTagWriteModel(
    string? Code,
    string? Slug,
    string? Locale,
    IReadOnlyDictionary<string, string> LocalizedNames);
