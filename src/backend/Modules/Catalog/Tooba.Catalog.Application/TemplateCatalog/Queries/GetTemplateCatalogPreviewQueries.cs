using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.TemplateCatalog.Models;

namespace Tooba.Catalog.Application.TemplateCatalog.Queries;

/// <summary>GET /v1/storefront/template-catalog/fashion/preview</summary>
public sealed record GetFashionTemplatePreviewQuery
    : IRequest<Result<FashionTemplatePreviewDto>>;

/// <summary>GET /v1/storefront/template-catalog/{templateKey}/preview</summary>
public sealed record GetIndustryTemplatePreviewQuery(string TemplateKey)
    : IRequest<Result<FashionTemplatePreviewDto>>;
