using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Models;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Creates a Catalog category (structured or legacy LocalizedNames).</summary>
public sealed record CreateCategoryCommand(CreateCategoryWriteModel Model)
    : IRequest<Result<CategoryReference>>;
