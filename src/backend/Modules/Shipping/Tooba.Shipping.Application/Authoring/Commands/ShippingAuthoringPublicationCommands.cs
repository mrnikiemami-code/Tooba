using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// انتشار یک پیش‌نویس موجود: capture دقیق بازبینی ذخیره‌شده و سپس ثبت آن به‌عنوان انتخاب‌شده.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بازبینی مورد انتظار پیش‌نویس و نسخهٔ مورد انتظار اشاره‌گر.</param>
public sealed record PublishShippingAuthoringDraftCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringPublicationPublishRequest Request)
    : IRequest<Result<ShippingAuthoringPublicationView>>;

/// <summary>
/// هندلر انتشار: خواندن پیش‌نویس، رد نسخهٔ کهنهٔ <b>شناخته‌شدهٔ</b> اشاره‌گر، یک capture، یک انتخاب.
/// </summary>
/// <remarks>
/// <para>
/// <b>دو مرز اتمی، نه یکی.</b> capture (TB-214) و اشاره‌گر (TB-219) دو نوشتن مستقل‌اند. اگر CAS اشاره‌گر
/// پس از capture شکست بخورد، بردن ‎409 درست است و capture مرحله‌بندی‌شده ممکن است باقی بماند؛ این مسیر
/// هیچ تراکنش بیرونی، rollback یا retry خودکار نمی‌سازد و ادعای اتمی‌بودن ترکیبی نمی‌کند.
/// </para>
/// <para>
/// پاسخ فقط از نتیجهٔ واقعی capture و انتخاب ساخته می‌شود: هیچ میدانی از dispatch، بدنهٔ درخواست یا
/// خواندن دوبارهٔ catalog/جغرافیا پس از commit پر نمی‌شود.
/// </para>
/// </remarks>
public sealed class PublishShippingAuthoringDraftHandler(
    IShippingAuthoringDraftDirectory drafts,
    IShippingSelectedCapturedRevisionDirectory pointers,
    IShippingSelectedCapturedRevisionLookup selected)
    : IRequestHandler<PublishShippingAuthoringDraftCommand, Result<ShippingAuthoringPublicationView>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringPublicationView>> Handle(
        PublishShippingAuthoringDraftCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        // Defense in depth for direct (non-HTTP) invocation: an omitted token is never "version zero".
        if (request.Request is null
            || request.Request.ExpectedRevision is not { } expectedRevision
            || expectedRevision <= 0
            || request.Request.ExpectedSelectionVersion is not { } expectedSelectionVersion
            || expectedSelectionVersion < 0)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
        }

        // 1. The owned draft is the only source of the binding; a foreign/missing draft is the safe refusal.
        var draft = await drafts.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(draft.Errors);
        }

        var stored = draft.Value;

        // A stale saved revision is refused BEFORE any capture write.
        if (stored.Revision != expectedRevision)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        var binding = ShippingAuthoringPublicationWorkflow.ReadRequest(request.Owner, stored);

        // 2. Read the CURRENT pointer and refuse a KNOWN stale expected-version before capturing. A pointer
        //    whose target cannot be proven keeps its version unknown here; the pointer CAS is still the
        //    final authority for that race.
        var current = await selected.ReadAsync(binding, cancellationToken);
        switch (current.Verdict)
        {
            case ShippingSelectedCaptureVerdict.Absent when expectedSelectionVersion != 0:
            case ShippingSelectedCaptureVerdict.Cleared or ShippingSelectedCaptureVerdict.Selected
                when current.SelectionVersion is { } knownVersion && knownVersion != expectedSelectionVersion:
                return Result.Failure<ShippingAuthoringPublicationView>(
                    new SemanticError(ShippingErrorCodes.SelectedCaptureConflict));
            case ShippingSelectedCaptureVerdict.OwnerScopeUnavailable:
                return Result.Failure<ShippingAuthoringPublicationView>(
                    new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeUnavailable));
            case ShippingSelectedCaptureVerdict.InvalidRequest:
                return Result.Failure<ShippingAuthoringPublicationView>(
                    new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
        }

        // 3. ONE capture of exactly this saved revision.
        var captured = await drafts.CaptureRevisionAsync(
            request.Owner,
            request.DraftId,
            expectedRevision,
            cancellationToken);
        if (captured.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(captured.Errors);
        }

        var snapshot = captured.Value;

        // 4. ONE pointer selection. A lost CAS after the capture is a stable conflict, never a success, and
        //    the staging capture may remain (no rollback and no combined atomicity claim).
        var selectedNow = await pointers.SelectAsync(
            request.Owner,
            stored.ServiceCode,
            stored.OptionCode,
            request.DraftId,
            snapshot.SourceRevision,
            expectedSelectionVersion,
            cancellationToken);
        if (selectedNow.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(selectedNow.Errors);
        }

        if (selectedNow.Value.SelectionVersion is not { } version || !selectedNow.Value.Exists)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
        }

        // 5. Provenance comes ONLY from the actual capture + selection results.
        return Result.Success(ShippingAuthoringPublicationWorkflow.View(
            stored,
            ShippingAuthoringPublicationState.Selected,
            version,
            snapshot.SnapshotId,
            snapshot.DraftId,
            snapshot.SourceRevision,
            snapshot.FormatVersion,
            snapshot.DraftId == stored.DraftId && snapshot.SourceRevision == stored.Revision));
    }
}

/// <summary>
/// برداشتن انتشار: پاک‌کردن اشاره‌گر انتخاب با نسخهٔ مورد انتظار. هیچ capture‌ای ساخته یا حذف نمی‌شود.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">نسخهٔ مورد انتظار اشاره‌گر.</param>
public sealed record WithdrawShippingAuthoringPublicationCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringPublicationWithdrawRequest Request)
    : IRequest<Result<ShippingAuthoringPublicationView>>;

/// <summary>
/// هندلر برداشتن انتشار: یک <c>ClearAsync</c> روی همان Binding مشتق‌شده از پیش‌نویس مالک.
/// </summary>
/// <remarks>
/// نسخهٔ کهنه به ‎409 پایدار می‌رسد و پاک‌کردن دوبارهٔ همان نسخه یک no-op است که ردیف و نسخه را نگه
/// می‌دارد. هیچ پرچم فعال‌سازی یا ادعای آمادگی مشتری ساخته نمی‌شود.
/// </remarks>
public sealed class WithdrawShippingAuthoringPublicationHandler(
    IShippingAuthoringDraftDirectory drafts,
    IShippingSelectedCapturedRevisionDirectory pointers)
    : IRequestHandler<WithdrawShippingAuthoringPublicationCommand, Result<ShippingAuthoringPublicationView>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringPublicationView>> Handle(
        WithdrawShippingAuthoringPublicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        if (request.Request is null
            || request.Request.ExpectedSelectionVersion is not { } expectedSelectionVersion
            || expectedSelectionVersion < 0)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
        }

        var draft = await drafts.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(draft.Errors);
        }

        var stored = draft.Value;
        var cleared = await pointers.ClearAsync(
            request.Owner,
            stored.ServiceCode,
            stored.OptionCode,
            expectedSelectionVersion,
            cancellationToken);
        if (cleared.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(cleared.Errors);
        }

        // An already-cleared pointer keeps its stored version; a lawful clear of an ABSENT pointer creates
        // nothing and therefore reports 0, which is the only state where 0 is truthful.
        return Result.Success(ShippingAuthoringPublicationWorkflow.View(
            stored,
            ShippingAuthoringPublicationState.Cleared,
            cleared.Value.SelectionVersion ?? 0));
    }
}
