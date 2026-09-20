using MediatR;
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Application;

/// <summary>Handler CQRS نوشتن گرد کردن مقدار — Command → Directory.</summary>
public sealed class SaveStoreQuantitySettingsHandler : IRequestHandler<SaveStoreQuantitySettingsCommand, QuantityRoundingMode>
{
    private readonly IStoreQuantitySettingsDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SaveStoreQuantitySettingsHandler(IStoreQuantitySettingsDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<QuantityRoundingMode> Handle(SaveStoreQuantitySettingsCommand request, CancellationToken cancellationToken)
        => _directory.SaveAsync(request.GlobalRoundingMode, cancellationToken);
}
