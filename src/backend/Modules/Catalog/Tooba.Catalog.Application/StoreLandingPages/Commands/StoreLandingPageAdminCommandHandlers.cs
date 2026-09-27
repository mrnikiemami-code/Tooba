using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreLandingPages.Models;
using Tooba.Catalog.Application.StoreLandingPages.Ports;

namespace Tooba.Catalog.Application.StoreLandingPages.Commands;

/// <summary>Handlers نوشتن Landing Admin — workspace + Result.</summary>
public sealed class CreateStoreLandingPageAdminHandler
    : IRequestHandler<CreateStoreLandingPageAdminCommand, Result<StoreLandingPageAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public CreateStoreLandingPageAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageAdminView>> Handle(
        CreateStoreLandingPageAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() => _workspace.CreateAsync(request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreLandingPageAdminHandler
    : IRequestHandler<UpdateStoreLandingPageAdminCommand, Result<StoreLandingPageAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public UpdateStoreLandingPageAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageAdminView>> Handle(
        UpdateStoreLandingPageAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.UpdateAsync(request.PageId, request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreLandingPageStatusAdminHandler
    : IRequestHandler<SetStoreLandingPageStatusAdminCommand, Result<StoreLandingPageAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public SetStoreLandingPageStatusAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageAdminView>> Handle(
        SetStoreLandingPageStatusAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.SetStatusAsync(request.PageId, request.Status, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreLandingPageAdminHandler
    : IRequestHandler<DeleteStoreLandingPageAdminCommand, Result<StoreLandingOkView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public DeleteStoreLandingPageAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingOkView>> Handle(
        DeleteStoreLandingPageAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(async () =>
        {
            await _workspace.DeletePageAsync(request.PageId, cancellationToken);
            return new StoreLandingOkView();
        });
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreHomePageAdminHandler
    : IRequestHandler<SetStoreHomePageAdminCommand, Result<StoreHomeSelectionView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public SetStoreHomePageAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreHomeSelectionView>> Handle(
        SetStoreHomePageAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.SetHomeAsync(request.HomePageId, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class AddStoreLandingPageSectionAdminHandler
    : IRequestHandler<AddStoreLandingPageSectionAdminCommand, Result<StoreLandingPageSectionAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public AddStoreLandingPageSectionAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageSectionAdminView>> Handle(
        AddStoreLandingPageSectionAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.AddSectionAsync(request.PageId, request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ReplaceStoreLandingPageCompositionAdminHandler
    : IRequestHandler<ReplaceStoreLandingPageCompositionAdminCommand, Result<IReadOnlyList<StoreLandingPageSectionAdminView>>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ReplaceStoreLandingPageCompositionAdminHandler(IStoreLandingPageWorkspace workspace) =>
        _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreLandingPageSectionAdminView>>> Handle(
        ReplaceStoreLandingPageCompositionAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.ReplaceCompositionAsync(request.PageId, request.Sections, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class UpdateStoreLandingPageSectionAdminHandler
    : IRequestHandler<UpdateStoreLandingPageSectionAdminCommand, Result<StoreLandingPageSectionAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public UpdateStoreLandingPageSectionAdminHandler(IStoreLandingPageWorkspace workspace) => _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageSectionAdminView>> Handle(
        UpdateStoreLandingPageSectionAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.UpdateSectionAsync(request.PageId, request.SectionId, request.Body, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class SetStoreLandingPageSectionEnabledAdminHandler
    : IRequestHandler<SetStoreLandingPageSectionEnabledAdminCommand, Result<StoreLandingPageSectionAdminView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public SetStoreLandingPageSectionEnabledAdminHandler(IStoreLandingPageWorkspace workspace) =>
        _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingPageSectionAdminView>> Handle(
        SetStoreLandingPageSectionEnabledAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.SetSectionEnabledAsync(request.PageId, request.SectionId, request.Enabled, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class ReorderStoreLandingPageSectionsAdminHandler
    : IRequestHandler<ReorderStoreLandingPageSectionsAdminCommand, Result<IReadOnlyList<StoreLandingPageSectionAdminView>>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public ReorderStoreLandingPageSectionsAdminHandler(IStoreLandingPageWorkspace workspace) =>
        _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoreLandingPageSectionAdminView>>> Handle(
        ReorderStoreLandingPageSectionsAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(() =>
            _workspace.ReorderSectionsAsync(request.PageId, request.SectionIds, cancellationToken));
}

/// <inheritdoc cref="IRequestHandler{TRequest,TResponse}" />
public sealed class DeleteStoreLandingPageSectionAdminHandler
    : IRequestHandler<DeleteStoreLandingPageSectionAdminCommand, Result<StoreLandingOkView>>
{
    private readonly IStoreLandingPageWorkspace _workspace;

    /// <summary>Creates the handler.</summary>
    public DeleteStoreLandingPageSectionAdminHandler(IStoreLandingPageWorkspace workspace) =>
        _workspace = workspace;

    /// <inheritdoc />
    public Task<Result<StoreLandingOkView>> Handle(
        DeleteStoreLandingPageSectionAdminCommand request,
        CancellationToken cancellationToken) =>
        StoreLandingPlatformResult.CaptureAsync(async () =>
        {
            await _workspace.DeleteSectionAsync(request.PageId, request.SectionId, cancellationToken);
            return new StoreLandingOkView();
        });
}
