using Tooba.BuildingBlocks;

namespace Tooba.Host.Seller;

/// <summary>
/// درز احراز و مجوز پنل فروشنده. هدر SellerPartyId فقط زمینهٔ درخواست است؛ مرجع مجوز نیست.
/// </summary>
internal static class SellerPanelAccess
{
    /// <summary>
    /// هدر زمینهٔ Party فروشندهٔ درخواست‌شده.
    /// </summary>
    public const string SellerPartyHeader = "X-Tooba-Seller-Party-Id";

    /// <summary>
    /// هدر محدود Development برای Actor وقتی Bearer نشست نیست؛ با SellerPartyId یکی نیست.
    /// </summary>
    public const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>
    /// Actor احرازشده و SellerPartyId مجاز را پس از بررسی SpiceDB/موتور مجوز برمی‌گرداند.
    /// </summary>
    public static async Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId(request, session, environment);
        var sellerPartyId = RequireSellerPartyId(request);
        await AuthorizeActorForSellerAsync(guard, actorUserId, sellerPartyId, cancellationToken);
        return (actorUserId, sellerPartyId);
    }

    /// <summary>
    /// Actor را از نشست Bearer یا در Development از هدر جداگانه می‌خواند؛ بدون Actor fail-closed است.
    /// </summary>
    public static Guid ResolveActorUserId(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } authenticated)
        {
            return authenticated;
        }

        if (environment.IsDevelopment()
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        throw new PlatformHttpException(401, "هویت بازیگر احراز نشده است.", "seller.actor.missing");
    }

    /// <summary>
    /// زمینهٔ Party فروشنده را می‌خواند؛ این مقدار به‌تنهایی مجوز نمی‌دهد.
    /// </summary>
    public static Guid RequireSellerPartyId(HttpRequest request)
    {
        var raw = request.Headers[SellerPartyHeader].ToString();
        if (!Guid.TryParse(raw, out var sellerPartyId) || sellerPartyId == Guid.Empty)
        {
            throw new PlatformHttpException(400, "شناسهٔ فروشنده نامعتبر است.", "seller.identity.missing");
        }

        return sellerPartyId;
    }

    /// <summary>
    /// user → party#view را از طریق قرارداد مجوز Tooba بررسی می‌کند؛ DENY و Unavailable fail-closed هستند.
    /// </summary>
    public static async Task AuthorizeActorForSellerAsync(
        IAuthorizationGuard guard,
        Guid actorUserId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || sellerPartyId == Guid.Empty)
        {
            throw new PlatformHttpException(401, "هویت بازیگر احراز نشده است.", "seller.actor.missing");
        }

        var decision = await guard.AuthorizeUseCaseAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Party,
                    Id = sellerPartyId.ToString("D"),
                },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.SingleStore,
                },
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(503, "سرویس مجوز در دسترس نیست.", "seller.authorization.unavailable");
        }

        throw new PlatformHttpException(403, "دسترسی به این فروشنده مجاز نیست.", "seller.authorization.denied");
    }
}
