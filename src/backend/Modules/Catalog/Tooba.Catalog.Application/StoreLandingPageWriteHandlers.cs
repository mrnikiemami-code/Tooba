using MediatR;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>Handlerهای CQRS نوشتن Landing — Command → Directory (مالک فعلی Catalog).</summary>
public sealed class CreateStoreLandingPageHandler : IRequestHandler<CreateStoreLandingPageCommand, StoreLandingPage>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public CreateStoreLandingPageHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPage> Handle(CreateStoreLandingPageCommand request, CancellationToken cancellationToken)
        => _directory.CreateAsync(request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreLandingPageHandler : IRequestHandler<UpdateStoreLandingPageCommand, StoreLandingPage>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateStoreLandingPageHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPage> Handle(UpdateStoreLandingPageCommand request, CancellationToken cancellationToken)
        => _directory.UpdateAsync(request.PageId, request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreLandingPageStatusHandler : IRequestHandler<SetStoreLandingPageStatusCommand, StoreLandingPage>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SetStoreLandingPageStatusHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPage> Handle(SetStoreLandingPageStatusCommand request, CancellationToken cancellationToken)
        => _directory.SetStatusAsync(request.PageId, request.Status, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreHomePageHandler : IRequestHandler<SetStoreHomePageCommand, StoreHomeWriteResult>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SetStoreHomePageHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreHomeWriteResult> Handle(SetStoreHomePageCommand request, CancellationToken cancellationToken)
        => _directory.SetHomeAsync(request.PageId, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class AddStoreLandingPageSectionHandler : IRequestHandler<AddStoreLandingPageSectionCommand, StoreLandingPageSection>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public AddStoreLandingPageSectionHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPageSection> Handle(AddStoreLandingPageSectionCommand request, CancellationToken cancellationToken)
        => _directory.AddSectionAsync(request.PageId, request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ReplaceStoreLandingPageCompositionHandler
    : IRequestHandler<ReplaceStoreLandingPageCompositionCommand, IReadOnlyList<StoreLandingPageSection>>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public ReplaceStoreLandingPageCompositionHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<StoreLandingPageSection>> Handle(
        ReplaceStoreLandingPageCompositionCommand request,
        CancellationToken cancellationToken)
        => _directory.ReplaceCompositionAsync(request.PageId, request.Sections, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreLandingPageSectionHandler : IRequestHandler<UpdateStoreLandingPageSectionCommand, StoreLandingPageSection>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateStoreLandingPageSectionHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPageSection> Handle(UpdateStoreLandingPageSectionCommand request, CancellationToken cancellationToken)
        => _directory.UpdateSectionAsync(request.PageId, request.SectionId, request.Model, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreLandingPageSectionEnabledHandler
    : IRequestHandler<SetStoreLandingPageSectionEnabledCommand, StoreLandingPageSection>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public SetStoreLandingPageSectionEnabledHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPageSection> Handle(SetStoreLandingPageSectionEnabledCommand request, CancellationToken cancellationToken)
        => _directory.SetSectionEnabledAsync(request.PageId, request.SectionId, request.Enabled, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ReorderStoreLandingPageSectionsHandler
    : IRequestHandler<ReorderStoreLandingPageSectionsCommand, IReadOnlyList<StoreLandingPageSection>>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public ReorderStoreLandingPageSectionsHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<StoreLandingPageSection>> Handle(
        ReorderStoreLandingPageSectionsCommand request,
        CancellationToken cancellationToken)
        => _directory.ReorderSectionsAsync(request.PageId, request.SectionIds, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreLandingPageHandler : IRequestHandler<DeleteStoreLandingPageCommand, StoreLandingPage>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeleteStoreLandingPageHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<StoreLandingPage> Handle(DeleteStoreLandingPageCommand request, CancellationToken cancellationToken)
        => _directory.DeletePageAsync(request.PageId, cancellationToken);
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreLandingPageSectionHandler : IRequestHandler<DeleteStoreLandingPageSectionCommand, Unit>
{
    private readonly IStoreLandingPageDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeleteStoreLandingPageSectionHandler(IStoreLandingPageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(DeleteStoreLandingPageSectionCommand request, CancellationToken cancellationToken)
    {
        await _directory.DeleteSectionAsync(request.PageId, request.SectionId, cancellationToken);
        return Unit.Value;
    }
}
