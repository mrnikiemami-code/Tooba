#pragma warning disable CS1591
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Models;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// Development-only diagnostic seam for Catalog Development seed publish rejections (LOCK-SF-402).
/// <para>
/// The aggregate publish gate surfaces a refusal only as
/// <see cref="Tooba.Catalog.Domain.Products.ProductPublishRules.MessageNotReadyFa"/> through
/// <see cref="ICatalogDirectory.PublishProductAsync"/>; the six underlying rule codes are lost at that boundary.
/// This seam re-reads the REAL <see cref="ProductPublishReadiness.MissingRequirements"/> for the exact product from
/// the existing readiness contract and emits the existing rule codes with a non-sensitive product reference and the
/// seed stage, so a rejected Development bootstrap names its blocking prerequisite instead of only the opaque
/// Persian message.
/// </para>
/// <para>
/// It never changes the publish decision, never force-publishes, never touches persistence directly and emits
/// nothing outside Development. The original exception (type and message) is always rethrown unchanged.
/// </para>
/// </summary>
public static class CatalogDevelopmentPublishDiagnostics
{
    /// <summary>Structured-log category for publish-readiness diagnostics.</summary>
    public const string LoggerCategory = "Tooba.Catalog.Development.PublishReadiness";

    /// <summary>Seed stage label for the attribute-schema demo product publish.</summary>
    public const string AttributeSchemaStage = "catalog-dev.attribute-schema.publish-product";

    /// <summary>Message template of the single structured diagnostic event.</summary>
    public const string DiagnosticTemplate = "{CatalogDevelopmentPublishReadiness}";

    /// <summary>Locale used for the readiness read (the Development seed writes fa-IR first).</summary>
    public const string DiagnosticLocale = "fa-IR";

    /// <summary>
    /// Publishes through <see cref="ICatalogDirectory.PublishProductAsync"/> and, <b>only in Development</b>, emits the
    /// real missing-rule codes when the publish is refused. The refusal itself is never altered.
    /// </summary>
    public static Task PublishWithDiagnosticsAsync(
        ICatalogDirectory catalog,
        IServiceProvider provider,
        Guid productId,
        string stage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return PublishWithDiagnosticsAsync(
            catalog,
            provider.GetService<IHostEnvironment>(),
            provider.GetService<ILoggerFactory>(),
            productId,
            stage,
            cancellationToken);
    }

    /// <summary>
    /// Publishes and diagnoses. Outside Development the diagnostic is skipped entirely (no readiness read, no log) and
    /// the original refusal is rethrown untouched.
    /// </summary>
    public static async Task PublishWithDiagnosticsAsync(
        ICatalogDirectory catalog,
        IHostEnvironment? environment,
        ILoggerFactory? loggerFactory,
        Guid productId,
        string stage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await catalog.PublishProductAsync(productId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            if (environment is not null && environment.IsDevelopment())
            {
                await EmitAsync(catalog, loggerFactory, productId, stage, cancellationToken);
            }

            throw;
        }
    }

    /// <summary>
    /// Reads the real readiness of one product and emits the bounded diagnostic (stage + non-sensitive product
    /// reference + real missing-rule codes). Never throws and never influences the publish decision; returns the
    /// emitted summary. Callers must have established Development-only intent.
    /// </summary>
    public static async Task<string> EmitAsync(
        ICatalogDirectory catalog,
        ILoggerFactory? loggerFactory,
        Guid productId,
        string stage,
        CancellationToken cancellationToken = default)
    {
        string summary;
        try
        {
            var readiness = await catalog.GetProductPublishReadinessAsync(
                productId,
                DiagnosticLocale,
                cancellationToken);
            summary = Summarize(stage, productId, readiness);
        }
        catch (Exception ex)
        {
            // Diagnostics must never mask the original refusal with a secondary failure.
            summary = $"stage={stage}; product={productId:D}; readiness=unavailable:{ex.GetType().Name}";
        }

        loggerFactory?
            .CreateLogger(LoggerCategory)
            .LogError(DiagnosticTemplate, summary);
        return summary;
    }

    /// <summary>
    /// Pure, bounded projection of a readiness reading: seed stage, non-sensitive product reference, readiness flag
    /// and the EXISTING missing-rule codes only. No entity payloads, no tokens, no connection data, no new rules.
    /// </summary>
    public static string Summarize(string stage, Guid productId, ProductPublishReadiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        var codes = readiness.MissingRequirements.Select(m => m.Code);
        return $"stage={stage}; product={productId:D}; isReady={readiness.IsReady}; missing=[{string.Join(",", codes)}]";
    }
}
