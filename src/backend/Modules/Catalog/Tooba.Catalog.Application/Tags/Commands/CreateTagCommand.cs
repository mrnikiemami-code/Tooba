using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Models;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>POST /v1/admin/catalog/tags/</summary>
public sealed record CreateTagCommand(CreateTagWriteModel Model) : IRequest<Result<TagView>>;
