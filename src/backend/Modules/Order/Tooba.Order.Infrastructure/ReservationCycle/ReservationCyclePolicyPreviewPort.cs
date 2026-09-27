using Tooba.Order.Application.Admin.Settings.ReservationPolicy;
using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Contracts.Reservation;

namespace Tooba.Order.Infrastructure.ReservationCycle;

/// <summary>Contracts preview for Catalog hold-policy aggregate (store editor only).</summary>
public sealed class ReservationCyclePolicyPreviewPort : IReservationCyclePolicyPreviewPort
{
    private readonly IReservationCyclePolicyResolver _resolver;

    /// <summary>Creates the port.</summary>
    public ReservationCyclePolicyPreviewPort(IReservationCyclePolicyResolver resolver) => _resolver = resolver;

    /// <inheritdoc />
    public async Task<ReservationPolicyEditorContract> PreviewStoreEditorAsync(CancellationToken cancellationToken)
    {
        var preview = await _resolver.PreviewAsync(null, null, cancellationToken);
        return Map(ReservationPolicyComposer.ForStore(preview, true));
    }

    private static ReservationPolicyEditorContract Map(ReservationPolicyEditorView view) =>
        new(
            view.Scope,
            view.ScopeId,
            MapField(view.InitialHold),
            MapField(view.RetryHold),
            MapField(view.MaxCycles),
            view.CanEdit,
            view.SellerCanMutate,
            view.FlashSaleStricter,
            view.LongHoldWarning,
            view.InheritLabelFa,
            view.InheritLabelEn,
            view.MultiLineHelpFa,
            view.MultiLineHelpEn,
            view.FlashSaleHelpFa,
            view.FlashSaleHelpEn,
            view.StricterNoteFa,
            view.StricterNoteEn,
            view.LongHoldNoteFa,
            view.LongHoldNoteEn);

    private static ReservationPolicyFieldContract MapField(ReservationPolicyFieldView field) =>
        new(
            field.OverrideValue,
            field.EffectiveValue,
            field.Source,
            field.Overridden,
            field.SourceLabelFa,
            field.SourceLabelEn,
            field.LabelFa,
            field.LabelEn,
            field.HelperFa,
            field.HelperEn);
}
