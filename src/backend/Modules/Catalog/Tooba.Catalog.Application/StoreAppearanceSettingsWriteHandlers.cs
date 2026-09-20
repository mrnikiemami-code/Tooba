using MediatR;

namespace Tooba.Catalog.Application;

/// <summary>Handler CQRS نوشتن ظاهر — Command → Directory.</summary>
public sealed class SaveStoreAppearanceSettingsHandler : IRequestHandler<SaveStoreAppearanceSettingsCommand, Unit>
{
    private readonly IStoreAppearanceSettingsDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SaveStoreAppearanceSettingsHandler(IStoreAppearanceSettingsDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(SaveStoreAppearanceSettingsCommand request, CancellationToken cancellationToken)
    {
        await _directory.SaveAsync(request.Model, cancellationToken);
        return Unit.Value;
    }
}
