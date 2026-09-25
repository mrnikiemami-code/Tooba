namespace Tooba.Order.Contracts.Fulfillment;

/// <summary>
/// مرجع پایدار و مشترک هویت Actor مهمان فروشگاه. تنها جای مجاز نگهداری مقدار Guid است
/// تا ماژول‌ها مقدار را تکرار نکنند.
/// </summary>
public static class StorefrontGuestActor
{
    /// <summary>شناسهٔ Actor مهمان فروشگاه؛ هویت پایدار قرارداد است.</summary>
    public static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");
}
