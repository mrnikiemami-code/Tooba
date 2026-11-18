using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;


/// <summary>
/// snapshot خط مرجوعی.
/// <para>
/// TB-274-R1: <see cref="SaleableRestockedQuantity"/> is the EXACT inspected usable quantity this line actually returned
/// to saleable stock (null when no saleable restock was recorded). A mixed line restocks only that portion, so neither
/// the requested quantity nor <see cref="Condition"/>/<see cref="Disposition"/> may be read as "the whole requested
/// amount went back to saleable stock".
/// </para>
/// <para>
/// TB-276: <see cref="ProductLabel"/> and <see cref="UnitLabel"/> are operator DISPLAY facts resolved from the owning
/// Order line and Catalog variant through Contracts reads. They are null when the owning read cannot answer, so an
/// unknown product or unit stays unknown instead of being invented, and the stored quantity/unit precision is never
/// changed by them.
/// </para>
/// </summary>
public sealed record ReturnItemSnapshot(
    Guid ReturnItemId,
    Guid OrderLineId,
    decimal Quantity,
    decimal UnitPriceSnapshot,
    string Currency,
    Guid? ReservationId,
    ReturnItemCondition Condition = ReturnItemCondition.Usable,
    ReturnItemDisposition Disposition = ReturnItemDisposition.None,
    Guid? SourceShipmentId = null,
    Guid? SourceReceiptId = null,
    decimal? SaleableRestockedQuantity = null,
    string? ProductLabel = null,
    string? UnitLabel = null,
    // TB-277: the authoritative fractional precision of this line's unit, read from the ACCEPTED Order line snapshot
    // (<c>QuantityDecimalPlacesSnapshot</c>). Null when the owning line could not be answered — the UI then refuses to
    // invent a step and never silently falls back to whole pieces.
    int? UnitDecimalPlaces = null,
    // TB-279-R1 (F2): the exact accepted usable portion Inventory confirmed for this line (null = not yet). It is a
    // per-portion fact, so a mixed line can never claim completion from one coarse disposition enum.
    decimal? UsableRestockedQuantity = null,
    // TB-279-R1 (F2): the exact accepted damaged portion recorded non-saleable for this line (null = not yet).
    decimal? DamagedRecordedQuantity = null);
