namespace Tooba.AccessControl.Application.Development.Seller;

/// <summary>
/// جفت Actor و Seller مجاز در Development برای پنل فروشنده. Actor ≠ SellerPartyId.
/// </summary>
/// <param name="ActorUserId">شناسهٔ کاربر Actor.</param>
/// <param name="ActorLabel">برچسب نمایشی Actor.</param>
/// <param name="SellerPartyId">شناسهٔ Party فروشنده.</param>
/// <param name="SellerLabel">برچسب نمایشی فروشنده.</param>
public sealed record SellerDevActorPair(
    Guid ActorUserId,
    string ActorLabel,
    Guid SellerPartyId,
    string SellerLabel);

/// <summary>
/// نگاشت demo برای UI و شواهد؛ Actor سوم = کارمند محدود ACC.
/// </summary>
/// <param name="ActorA">فروشندهٔ اصلی demo.</param>
/// <param name="ActorB">فروشندهٔ دوم demo.</param>
/// <param name="ScopedEmployee">کارمند محدود اختیاری.</param>
public sealed record SellerDevContextSnapshot(
    SellerDevActorPair ActorA,
    SellerDevActorPair ActorB,
    SellerDevActorPair? ScopedEmployee = null);

/// <summary>
/// ردیف Actor در پاسخ مسیر Development فروشنده.
/// </summary>
/// <param name="ActorUserId">شناسهٔ کاربر Actor.</param>
/// <param name="ActorLabel">برچسب نمایشی Actor.</param>
/// <param name="SellerPartyId">شناسهٔ Party فروشنده.</param>
/// <param name="SellerLabel">برچسب نمایشی فروشنده.</param>
/// <param name="ContextKind">گونهٔ زمینه: seller-owner | seller-owner-alt | scoped-employee.</param>
public sealed record SellerDevContextActorView(
    Guid ActorUserId,
    string ActorLabel,
    Guid SellerPartyId,
    string SellerLabel,
    string ContextKind);

/// <summary>
/// پاسخ مسیر <c>GET /v1/seller/dev-contexts</c>.
/// </summary>
/// <param name="Actors">ردیف‌های Actor/Seller.</param>
public sealed record SellerDevContextsView(IReadOnlyList<SellerDevContextActorView> Actors);
