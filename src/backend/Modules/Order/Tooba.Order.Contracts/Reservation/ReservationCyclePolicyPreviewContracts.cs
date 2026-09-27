namespace Tooba.Order.Contracts.Reservation;

/// <summary>یک فیلد سیاست رزرو با override و مؤثر و منبع backend (Admin JSON parity).</summary>
public sealed record ReservationPolicyFieldContract(
    int? OverrideValue,
    int EffectiveValue,
    string Source,
    bool Overridden,
    string SourceLabelFa,
    string SourceLabelEn,
    string LabelFa,
    string LabelEn,
    string HelperFa,
    string HelperEn);

/// <summary>ویرایشگر Store/Category/Offer (Admin JSON parity).</summary>
public sealed record ReservationPolicyEditorContract(
    string Scope,
    Guid? ScopeId,
    ReservationPolicyFieldContract InitialHold,
    ReservationPolicyFieldContract RetryHold,
    ReservationPolicyFieldContract MaxCycles,
    bool CanEdit,
    bool SellerCanMutate,
    bool FlashSaleStricter,
    bool LongHoldWarning,
    string InheritLabelFa,
    string InheritLabelEn,
    string MultiLineHelpFa,
    string MultiLineHelpEn,
    string FlashSaleHelpFa,
    string FlashSaleHelpEn,
    string StricterNoteFa,
    string StricterNoteEn,
    string LongHoldNoteFa,
    string LongHoldNoteEn);

/// <summary>
/// Order-owned reservation-cycle Admin preview without leaking Order.Application into Catalog Endpoints.
/// </summary>
public interface IReservationCyclePolicyPreviewPort
{
    /// <summary>Store-level editor preview (canEdit=true for Admin hold-policy aggregate).</summary>
    Task<ReservationPolicyEditorContract> PreviewStoreEditorAsync(CancellationToken cancellationToken);
}
