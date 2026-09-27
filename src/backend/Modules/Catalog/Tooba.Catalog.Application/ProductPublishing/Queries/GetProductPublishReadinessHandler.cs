using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductPublishing.Models;
using Tooba.Catalog.Application.ProductPublishing.Ports;

namespace Tooba.Catalog.Application.ProductPublishing.Queries;

/// <summary>Handles GetProductPublishReadinessQuery.</summary>
public sealed class GetProductPublishReadinessHandler
    : IRequestHandler<GetProductPublishReadinessQuery, Result<ProductPublishReadinessView>>
{
    private readonly IProductPublishReadinessReader _reader;

    /// <summary>Creates the handler.</summary>
    public GetProductPublishReadinessHandler(IProductPublishReadinessReader reader) => _reader = reader;

    /// <inheritdoc />
    public async Task<Result<ProductPublishReadinessView>> Handle(
        GetProductPublishReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var readiness = await _reader.GetAsync(request.ProductId, request.Locale, cancellationToken);
        if (readiness.IsFailure)
        {
            return Result.Failure<ProductPublishReadinessView>(readiness.Errors);
        }

        return Result.Success(Map(readiness.Value));
    }

    /// <summary>Maps directory readiness DTO to Admin HTTP view.</summary>
    public static ProductPublishReadinessView Map(ProductPublishReadiness readiness) =>
        new(
            readiness.IsReady,
            readiness.CategoryReady,
            readiness.TranslationReady,
            readiness.AttributeReady,
            readiness.VariantReady,
            readiness.MediaReady,
            readiness.SeoReady,
            readiness.MissingRequirements
                .Select(m => new ProductPublishMissingRequirementView(m.Code, m.MessageFa, m.WorkspaceTab))
                .ToList(),
            readiness.MessageFa);
}
