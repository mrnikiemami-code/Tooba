using MediatR;

namespace Tooba.Catalog.Application;

/// <summary>Handler ایجاد واحد.</summary>
public sealed class CreateUnitOfMeasureHandler : IRequestHandler<CreateUnitOfMeasureCommand, Guid>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public CreateUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Guid> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
        => _directory.CreateAsync(request.Model, cancellationToken);
}

/// <summary>Handler ویرایش واحد.</summary>
public sealed class UpdateUnitOfMeasureHandler : IRequestHandler<UpdateUnitOfMeasureCommand, Guid>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Guid> Handle(UpdateUnitOfMeasureCommand request, CancellationToken cancellationToken)
        => _directory.UpdateAsync(request.UnitId, request.Model, cancellationToken);
}

/// <summary>Handler غیرفعال‌سازی واحد.</summary>
public sealed class DeactivateUnitOfMeasureHandler : IRequestHandler<DeactivateUnitOfMeasureCommand, (Guid UnitId, bool IsActive)>
{
    private readonly IUnitOfMeasureDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeactivateUnitOfMeasureHandler(IUnitOfMeasureDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<(Guid UnitId, bool IsActive)> Handle(DeactivateUnitOfMeasureCommand request, CancellationToken cancellationToken)
        => _directory.DeactivateAsync(request.UnitId, cancellationToken);
}
