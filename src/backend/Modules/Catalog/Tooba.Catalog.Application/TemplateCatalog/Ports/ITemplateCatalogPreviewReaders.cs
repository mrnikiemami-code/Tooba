using Tooba.Catalog.Application.TemplateCatalog.Models;

namespace Tooba.Catalog.Application.TemplateCatalog.Ports;

/// <summary>Reads Fashion sample from Template Catalog only (no operational Catalog union).</summary>
public interface IFashionTemplatePreviewReader
{
    /// <summary>Origin constant for Fashion template seed.</summary>
    string Origin { get; }

    /// <summary>Loads Fashion sample preview or <see langword="null"/> when missing.</summary>
    Task<FashionTemplatePreviewDto?> GetFashionSampleAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reads industry Batch A/B/C samples from Template Catalog only.</summary>
public interface IIndustryTemplatePreviewReader
{
    /// <summary>Loads industry sample for <paramref name="templateKey"/> or <see langword="null"/> when unsupported/missing.</summary>
    Task<FashionTemplatePreviewDto?> GetSampleAsync(string templateKey, CancellationToken cancellationToken = default);
}
