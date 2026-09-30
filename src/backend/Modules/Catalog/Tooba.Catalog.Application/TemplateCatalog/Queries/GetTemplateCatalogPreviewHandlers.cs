using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.TemplateCatalog.Models;
using Tooba.Catalog.Application.TemplateCatalog.Ports;

namespace Tooba.Catalog.Application.TemplateCatalog.Queries;

/// <summary>Loads Fashion template-catalog preview.</summary>
public sealed class GetFashionTemplatePreviewHandler(IFashionTemplatePreviewReader reader)
    : IRequestHandler<GetFashionTemplatePreviewQuery, Result<FashionTemplatePreviewDto>>
{
    /// <inheritdoc />
    public async Task<Result<FashionTemplatePreviewDto>> Handle(
        GetFashionTemplatePreviewQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var preview = await reader.GetFashionSampleAsync(cancellationToken);
        return preview is null
            ? Result.Failure<FashionTemplatePreviewDto>(new SemanticError("template_catalog.fashion.missing"))
            : Result.Success(preview);
    }
}

/// <summary>Loads industry template-catalog preview by key.</summary>
public sealed class GetIndustryTemplatePreviewHandler(IIndustryTemplatePreviewReader reader)
    : IRequestHandler<GetIndustryTemplatePreviewQuery, Result<FashionTemplatePreviewDto>>
{
    /// <inheritdoc />
    public async Task<Result<FashionTemplatePreviewDto>> Handle(
        GetIndustryTemplatePreviewQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.Equals(request.TemplateKey, "fashion", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<FashionTemplatePreviewDto>(
                new SemanticError("template_catalog.use_fashion_route"));
        }

        var preview = await reader.GetSampleAsync(request.TemplateKey, cancellationToken);
        return preview is null
            ? Result.Failure<FashionTemplatePreviewDto>(
                new SemanticError($"template_catalog.{request.TemplateKey}.missing"))
            : Result.Success(preview);
    }
}
