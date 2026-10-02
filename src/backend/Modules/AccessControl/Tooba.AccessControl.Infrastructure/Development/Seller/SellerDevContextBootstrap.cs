using Tooba.AccessControl.Application.Development.Seller;
using Tooba.BuildingBlocks;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;
using Tooba.Identity.Contracts.Auth;
using Tooba.Party.Contracts;

namespace Tooba.AccessControl.Infrastructure.Development.Seller;

/// <summary>
/// بازیگرهای Development پنل فروشنده و تصویر عضویت مجوز. Actor ≠ SellerPartyId.
/// مالکیت این قابلیت AccessControl است؛ Party فقط از طریق Party.Contracts و Identity فقط از طریق
/// Identity.Contracts مصرف می‌شود و هیچ DbContext/Domain خارجی خوانده نمی‌شود.
/// </summary>
public sealed class SellerDevContextBootstrap : ISellerDevContextStore
{
    /// <summary>ایمیل Actor A (آرمان).</summary>
    public const string ActorAEmail = "seller-actor-a@tooba.local";

    /// <summary>ایمیل Actor B (دیجی‌استایل).</summary>
    public const string ActorBEmail = "seller-actor-b@tooba.local";

    /// <summary>نام نمایشی سازمان فروشندهٔ A در seed.</summary>
    public const string SellerADisplayName = "فروشگاه آرمان";

    /// <summary>نام نمایشی سازمان فروشندهٔ B در seed.</summary>
    public const string SellerBDisplayName = "دیجی‌استایل نمونه";

    private static readonly object Gate = new();
    private static SellerDevContextSnapshot? _snapshot;

    private readonly IIdentityAuthenticationService _authUsers;
    private readonly IPartyDevelopmentSeedGateway _parties;
    private readonly IAuthorizationTupleWriter _tuples;

    /// <summary>بستهٔ Development فروشنده را روی درزهای Contracts می‌سازد.</summary>
    public SellerDevContextBootstrap(
        IIdentityAuthenticationService authUsers,
        IPartyDevelopmentSeedGateway parties,
        IAuthorizationTupleWriter tuples)
    {
        _authUsers = authUsers;
        _parties = parties;
        _tuples = tuples;
    }

    /// <inheritdoc />
    public SellerDevContextSnapshot? Current
    {
        get
        {
            lock (Gate)
            {
                return _snapshot;
            }
        }
    }

    /// <inheritdoc />
    public void Publish(SellerDevActorPair actorA, SellerDevActorPair actorB)
    {
        lock (Gate)
        {
            _snapshot = new SellerDevContextSnapshot(actorA, actorB);
        }
    }

    /// <inheritdoc />
    public void PublishScopedEmployee(SellerDevActorPair employee)
    {
        lock (Gate)
        {
            if (_snapshot is null)
            {
                return;
            }

            _snapshot = new SellerDevContextSnapshot(_snapshot.ActorA, _snapshot.ActorB, employee);
        }
    }

    /// <inheritdoc />
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        var actorA = await EnsureUserAsync(_authUsers, ActorAEmail, cancellationToken);
        var actorB = await EnsureUserAsync(_authUsers, ActorBEmail, cancellationToken);

        // نام نمایشی ممکن است به‌خاطر encoding خراب شده باشد؛ از عضویت Actor بازیابی می‌کنیم.
        var sellerA = await _parties.FindDevelopmentOrganizationByDisplayNameAsync(SellerADisplayName, cancellationToken)
            ?? await _parties.FindDevelopmentMembershipSellerPartyAsync(actorA, cancellationToken);
        var sellerB = await _parties.FindDevelopmentOrganizationByDisplayNameAsync(SellerBDisplayName, cancellationToken)
            ?? await _parties.FindDevelopmentMembershipSellerPartyAsync(actorB, cancellationToken);
        if (sellerA is null || sellerB is null)
        {
            return;
        }

        await _parties.EnsureDevelopmentMemberMembershipAsync(actorA, sellerA.Value, cancellationToken);
        await _parties.EnsureDevelopmentMemberMembershipAsync(actorB, sellerB.Value, cancellationToken);

        // InMemory پس از restart خالی است؛ نوشتن مجدد برای fail-closed امن است.
        // اگر Mode=Disabled باشد Write پرتاب می‌کند — Development باید InMemory/SpiceDb باشد.
        try
        {
            await WriteMemberTupleAsync(_tuples, actorA, sellerA.Value, cancellationToken);
            await WriteMemberTupleAsync(_tuples, actorB, sellerB.Value, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تست‌های Host بدون موتور مجوز نباید استارت را بشکنند؛ مسیر Seller fail-closed می‌ماند.
            return;
        }

        Publish(
            new SellerDevActorPair(actorA, "اپراتور آرمان", sellerA.Value, SellerADisplayName),
            new SellerDevActorPair(actorB, "اپراتور دیجی‌استایل", sellerB.Value, SellerBDisplayName));
    }

    private static async Task<Guid> EnsureUserAsync(
        IIdentityAuthenticationService auth,
        string email,
        CancellationToken cancellationToken)
    {
        var existing = await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, email, cancellationToken);
        if (existing is { } userId)
        {
            return userId;
        }

        try
        {
            var created = await auth.RegisterAsync(
                new RegisterUserCommand
                {
                    IdentifierKind = LoginIdentifierKind.Email,
                    Identifier = email,
                    Password = "seller-dev-horse-1",
                },
                cancellationToken);
            return created.UserId;
        }
        catch (IdentityDuplicateIdentifierFault)
        {
            return await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, email, cancellationToken)
                ?? throw new InvalidOperationException("Seller demo actor could not be resolved after duplicate.");
        }
    }

    private static Task WriteMemberTupleAsync(
        IAuthorizationTupleWriter writer,
        Guid userId,
        Guid sellerPartyId,
        CancellationToken cancellationToken) =>
        writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(userId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Party,
                    Id = sellerPartyId.ToString("D"),
                },
                Relation = AuthorizationRelations.Member,
            },
            cancellationToken);
}
