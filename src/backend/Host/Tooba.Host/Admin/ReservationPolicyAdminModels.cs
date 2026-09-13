namespace Tooba.Host.Admin;

/// <summary>یک فیلد سیاست رزرو با override و مؤثر و منبع backend.</summary>
public sealed record ReservationPolicyFieldView(
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

/// <summary>ویرایشگر Store/Category/Offer.</summary>
public sealed record ReservationPolicyEditorView(
    string Scope,
    Guid? ScopeId,
    ReservationPolicyFieldView InitialHold,
    ReservationPolicyFieldView RetryHold,
    ReservationPolicyFieldView MaxCycles,
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

/// <summary>بدنهٔ ذخیرهٔ override.</summary>
public sealed record ReservationPolicyWriteRequest(
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles);

/// <summary>ردیف ممیزی تنظیم.</summary>
public sealed record ReservationPolicyAuditView(
    Guid EventId,
    string Level,
    Guid? ScopeId,
    string Field,
    string? OldOverride,
    string? NewOverride,
    Guid ActorUserId,
    DateTimeOffset OccurredAt);
