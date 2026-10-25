using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Application.Authoring.Queries;

/// <summary>
/// خواندن وضعیت «انتشار» یک پیش‌نویس موجود: وضعیت صریح انتخاب capture و provenance همان capture.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
public sealed record GetShippingAuthoringPublicationQuery(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId)
    : IRequest<Result<ShippingAuthoringPublicationView>>;

/// <summary>
/// هندلر خواندن انتشار. ابتدا پیش‌نویس داخل محدودهٔ کامل مالک خوانده می‌شود تا Binding سرویس/گزینه از
/// دادهٔ ذخیره‌شده بیاید، سپس همان Binding با هویت مالک مجاز به خواندن ۲۱۹ فرستاده می‌شود.
/// </summary>
/// <remarks>
/// هیچ fallback به «جدیدترین» یا پیکربندی وجود ندارد: امتناع خواندن به یک کد پایدار تبدیل می‌شود و
/// هرگز به‌عنوان Selected گزارش نمی‌شود. یک capture قدیمی انتخاب‌شده همان‌طور که هست برگردانده می‌شود و
/// با «آیا برابر بازبینی فعلی است» فقط یک برابری داده‌ای گزارش می‌شود.
/// </remarks>
public sealed class GetShippingAuthoringPublicationHandler(
    IShippingAuthoringDraftDirectory drafts,
    IShippingSelectedCapturedRevisionLookup selected)
    : IRequestHandler<GetShippingAuthoringPublicationQuery, Result<ShippingAuthoringPublicationView>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringPublicationView>> Handle(
        GetShippingAuthoringPublicationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);

        var draft = await drafts.GetAsync(request.Owner, request.DraftId, cancellationToken);
        if (draft.IsFailure)
        {
            return Result.Failure<ShippingAuthoringPublicationView>(draft.Errors);
        }

        var stored = draft.Value;
        var read = await selected.ReadAsync(
            ShippingAuthoringPublicationWorkflow.ReadRequest(request.Owner, stored),
            cancellationToken);

        return ShippingAuthoringPublicationWorkflow.FromRead(stored, read);
    }
}

/// <summary>
/// نگاشت مشترک خواندن ۲۱۹ و نتیجهٔ نوشتن به شکل Contracts — تنها جای این نگاشت.
/// </summary>
internal static class ShippingAuthoringPublicationWorkflow
{
    /// <summary>Binding دقیق پیش‌نویس با هویت مالک مجاز را به درخواست خواندن تبدیل می‌کند.</summary>
    /// <param name="owner">مالک مجاز.</param>
    /// <param name="draft">پیش‌نویس خوانده‌شده در همان محدوده.</param>
    public static ShippingSelectedCapturedRevisionRequest ReadRequest(
        ShippingAuthoringOwnerScope owner,
        Domain.Authoring.ShippingAuthoringDraft draft)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(draft);

        return new ShippingSelectedCapturedRevisionRequest(
            ToContractKind(owner.Kind),
            owner.TenantId,
            owner.SellerPartyId,
            draft.ServiceCode,
            draft.OptionCode);
    }

    /// <summary>گونهٔ مالک داخلی را به گونهٔ Contracts نگاشت می‌کند.</summary>
    /// <param name="kind">گونهٔ مالک داخلی.</param>
    public static ShippingCapturedQuoteOwnerKind ToContractKind(ShippingAuthoringOwnerKind kind) =>
        kind switch
        {
            ShippingAuthoringOwnerKind.Store => ShippingCapturedQuoteOwnerKind.Store,
            ShippingAuthoringOwnerKind.Marketplace => ShippingCapturedQuoteOwnerKind.Marketplace,
            ShippingAuthoringOwnerKind.Seller => ShippingCapturedQuoteOwnerKind.Seller,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown owner kind."),
        };

    /// <summary>نتیجهٔ خواندن ۲۱۹ را به نمای انتشار نگاشت می‌کند؛ امتناع همیشه یک خطای پایدار است.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    /// <param name="read">نتیجهٔ خواندن اشاره‌گر.</param>
    public static Result<ShippingAuthoringPublicationView> FromRead(
        Domain.Authoring.ShippingAuthoringDraft draft,
        ShippingSelectedCapturedRevisionResult read)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(read);

        if (read.Verdict == ShippingSelectedCaptureVerdict.Absent)
        {
            return Result.Success(Absent(draft));
        }

        if (read.Verdict is ShippingSelectedCaptureVerdict.Cleared or ShippingSelectedCaptureVerdict.Selected)
        {
            // "0 only for absent": a cleared/selected pointer always carries its real stored version.
            if (read.SelectionVersion is not { } version)
            {
                return Result.Failure<ShippingAuthoringPublicationView>(
                    new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
            }

            if (read.Verdict == ShippingSelectedCaptureVerdict.Cleared)
            {
                return Result.Success(View(draft, ShippingAuthoringPublicationState.Cleared, version));
            }

            if (read.SnapshotId is not { } snapshotId
                || read.DraftId is not { } sourceDraftId
                || read.SourceRevision is not { } sourceRevision)
            {
                return Result.Failure<ShippingAuthoringPublicationView>(
                    new SemanticError(ShippingErrorCodes.SelectedCaptureSnapshotUnavailable));
            }

            return Result.Success(View(
                draft,
                ShippingAuthoringPublicationState.Selected,
                version,
                snapshotId,
                sourceDraftId,
                sourceRevision,
                read.FormatVersion,
                sourceDraftId == draft.DraftId && sourceRevision == draft.Revision));
        }

        return Result.Failure<ShippingAuthoringPublicationView>(new SemanticError(read.Verdict switch
        {
            ShippingSelectedCaptureVerdict.OwnerScopeUnavailable => ShippingErrorCodes.AuthoringOwnerScopeUnavailable,
            ShippingSelectedCaptureVerdict.SnapshotUnavailable => ShippingErrorCodes.SelectedCaptureSnapshotUnavailable,
            _ => ShippingErrorCodes.SelectedCaptureInvalid,
        }));
    }

    /// <summary>نمای «هیچ اشاره‌گری نیست»: نسخهٔ صفر و بدون هیچ provenance.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    public static ShippingAuthoringPublicationView Absent(Domain.Authoring.ShippingAuthoringDraft draft) =>
        View(draft, ShippingAuthoringPublicationState.Absent, 0);

    /// <summary>نمای وضعیت ذخیره‌شده با provenance اختیاری.</summary>
    /// <param name="draft">پیش‌نویس مالک.</param>
    /// <param name="state">وضعیت صریح.</param>
    /// <param name="selectionVersion">نسخهٔ واقعی اشاره‌گر (یا صفر فقط در حالت Absent).</param>
    /// <param name="snapshotId">capture انتخاب‌شده.</param>
    /// <param name="sourceDraftId">پیش‌نویس مبدأ capture.</param>
    /// <param name="sourceRevision">بازبینی مبدأ capture.</param>
    /// <param name="formatVersion">نسخهٔ قالب capture.</param>
    /// <param name="matchesCurrentRevision">آیا مبدأ برابر بازبینی ذخیره‌شدهٔ فعلی است.</param>
    public static ShippingAuthoringPublicationView View(
        Domain.Authoring.ShippingAuthoringDraft draft,
        ShippingAuthoringPublicationState state,
        long selectionVersion,
        Guid? snapshotId = null,
        Guid? sourceDraftId = null,
        long? sourceRevision = null,
        int? formatVersion = null,
        bool matchesCurrentRevision = false) =>
        new(
            draft.DraftId,
            draft.Revision,
            draft.ServiceCode,
            draft.OptionCode,
            state,
            selectionVersion,
            snapshotId,
            sourceDraftId,
            sourceRevision,
            formatVersion,
            matchesCurrentRevision);
}
