using MediatR;

namespace Tooba.Fulfillment.Application;

/// <summary>Handler ایجاد سرویس ارسال.</summary>
public sealed class CreateShippingServiceHandler : IRequestHandler<CreateShippingServiceCommand, Guid>
{
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public CreateShippingServiceHandler(IShippingServiceDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Guid> Handle(CreateShippingServiceCommand request, CancellationToken cancellationToken)
        => _directory.CreateAsync(request.Model, cancellationToken);
}

/// <summary>Handler ویرایش سرویس ارسال.</summary>
public sealed class UpdateShippingServiceHandler : IRequestHandler<UpdateShippingServiceCommand, Guid>
{
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateShippingServiceHandler(IShippingServiceDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Guid> Handle(UpdateShippingServiceCommand request, CancellationToken cancellationToken)
        => _directory.UpdateAsync(request.ServiceId, request.Model, cancellationToken);
}

/// <summary>Handler غیرفعال‌سازی سرویس ارسال.</summary>
public sealed class DeactivateShippingServiceHandler : IRequestHandler<DeactivateShippingServiceCommand>
{
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeactivateShippingServiceHandler(IShippingServiceDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task Handle(DeactivateShippingServiceCommand request, CancellationToken cancellationToken)
        => _directory.DeactivateAsync(request.ServiceId, cancellationToken);
}

/// <summary>Handler seed کاتالوگ ارسال.</summary>
public sealed class EnsureShippingCatalogSeedHandler : IRequestHandler<EnsureShippingCatalogSeedCommand>
{
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public EnsureShippingCatalogSeedHandler(IShippingServiceDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task Handle(EnsureShippingCatalogSeedCommand request, CancellationToken cancellationToken)
        => _directory.EnsureSeedAsync(cancellationToken);
}
