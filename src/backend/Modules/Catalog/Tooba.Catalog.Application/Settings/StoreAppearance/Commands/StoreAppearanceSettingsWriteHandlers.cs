using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Settings.StoreAppearance.Ports;

namespace Tooba.Catalog.Application.Settings.StoreAppearance.Commands;

/// <summary>Handler CQRS نوشتن ظاهر — Command → Directory + cache invalidate.</summary>
public sealed class SaveStoreAppearanceSettingsHandler : IRequestHandler<SaveStoreAppearanceSettingsCommand, Unit>
{
    private readonly IStoreAppearanceSettingsDirectory _directory;
    private readonly IStoreAppearanceProjector _projector;
    private readonly ICurrentCommerceContext _commerce;

    /// <summary>Handler را می‌سازد.</summary>
    public SaveStoreAppearanceSettingsHandler(
        IStoreAppearanceSettingsDirectory directory,
        IStoreAppearanceProjector projector,
        ICurrentCommerceContext commerce)
    {
        _directory = directory;
        _projector = projector;
        _commerce = commerce;
    }

    /// <inheritdoc />
    public async Task<Unit> Handle(SaveStoreAppearanceSettingsCommand request, CancellationToken cancellationToken)
    {
        await _directory.SaveAsync(request.Model, cancellationToken);
        _projector.Invalidate(_commerce.Current);
        return Unit.Value;
    }
}
