using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductSeo.Models;

namespace Tooba.Catalog.Application.ProductSeo.Commands;

/// <summary>Admin PUT product SEO update.</summary>
public sealed record UpdateProductSeoCommand(Guid ProductId, UpdateProductSeoWriteModel Model)
    : IRequest<Result<ProductSeoDetailView>>;
