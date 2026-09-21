

namespace Tooba.Returns.Domain.ValueObjects;


/// <summary>
/// مقصد بازگشت وجه. فقط مقادیر typed؛ free-form نیست.
/// </summary>
public enum RefundDestination
{
    /// <summary>بازگشت به روش پرداخت اصلی (PSP/gateway).</summary>
    OriginalPayment = 0,

    /// <summary>اعتبار به کیف پول مشتری.</summary>
    Wallet = 1,
}
