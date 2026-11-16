using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Domain.Aggregates;

/// <summary>
/// TB-274: one inspected quantity of one return line inside one receipt occurrence.
/// <para>
/// The pair (item, condition) is unique inside one occurrence, and the quantity is stored at the same
/// <c>numeric(18,6)</c> scale as the requested quantity so received/usable/damaged sums compare exactly with what the
/// customer was asked to return. A line split across conditions (for example 2 usable + 1 damaged of the same item) is
/// two rows of the same occurrence, not two requested lines.
/// </para>
/// </summary>
public sealed class ReturnReceiptLine
{
    private ReturnReceiptLine()
    {
    }

    /// <summary>شناسه خط رسید.</summary>
    public Guid ReturnReceiptLineId { get; init; }

    /// <summary>رسید مالک.</summary>
    public Guid ReturnReceiptId { get; init; }

    /// <summary>پروندهٔ مرجوعی مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط درخواست مرجوعی که این مقدار از آن دریافت شده است.</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>مقدار دریافتی در این رخداد.</summary>
    public decimal Quantity { get; init; }

    /// <summary>وضعیت بازرسی‌شدهٔ این مقدار.</summary>
    public ReturnItemCondition Condition { get; init; }

    internal static ReturnReceiptLine Create(
        Guid returnReceiptLineId,
        Guid returnReceiptId,
        Guid returnRequestId,
        Guid returnItemId,
        decimal quantity,
        ReturnItemCondition condition) =>
        new()
        {
            ReturnReceiptLineId = returnReceiptLineId,
            ReturnReceiptId = returnReceiptId,
            ReturnRequestId = returnRequestId,
            ReturnItemId = returnItemId,
            Quantity = quantity,
            Condition = condition,
        };
}
