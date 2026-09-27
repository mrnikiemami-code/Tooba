namespace Tooba.Catalog.Application.Attributes.Schema.Models;

/// <summary>Success payload for schema bind/update/unbind/reorder ({ ok = true }).</summary>
public sealed record CategoryAttributeSchemaMutationResult(bool Ok = true);
