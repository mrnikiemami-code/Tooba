using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Media.Application;

namespace Tooba.Host.Content;

/// <summary>پل Host برای اعتبارسنج ارجاع DAM در Content.</summary>
public sealed class ContentMediaAssetValidator : IContentMediaAssetValidator
{
    private readonly IMediaDirectory _media;

    /// <summary>دایرکتوری Media را تزریق می‌کند.</summary>
    public ContentMediaAssetValidator(IMediaDirectory media) => _media = media;

    /// <inheritdoc />
    public async Task EnsureReadyAssetExistsAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var asset = await _media.GetAsync(mediaAssetId, cancellationToken);
        if (asset is null)
            throw new InvalidOperationException(ContentArticleErrorCodes.MediaNotFound);
    }
}
