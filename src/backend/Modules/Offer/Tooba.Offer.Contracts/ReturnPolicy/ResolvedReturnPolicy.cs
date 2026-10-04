namespace Tooba.Offer.Contracts.ReturnPolicy;

/// <summary>
/// نتیجهٔ resolve برای snapshot خط سفارش.
/// </summary>
public sealed record ResolvedReturnPolicy(
    bool IsReturnable,
    int WindowDays,
    string Source,
    string LabelFa);
