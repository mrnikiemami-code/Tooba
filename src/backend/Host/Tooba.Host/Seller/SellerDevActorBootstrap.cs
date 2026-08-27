using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;
using Tooba.Party.Application;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Host.Seller;

/// <summary>
/// بازیگرهای Development پنل فروشنده و تصویر عضویت مجوز. Actor ≠ SellerPartyId.
/// </summary>
internal static class SellerDevActorBootstrap
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

    /// <summary>
    /// آخرین نگاشت Actor↔Seller پس از bootstrap برای مسیر dev-contexts.
    /// </summary>
    public static SellerDevContextSnapshot? Snapshot
    {
        get
        {
            lock (Gate)
            {
                return _snapshot;
            }
        }
    }

    /// <summary>
    /// کاربران demo، عضویت Party، و tupleهای مجوز را برای ماتریس Actor/Seller آماده می‌کند.
    /// باید روی همان scope با CommerceContext انتساب‌شده صدا زده شود؛ scope تو در تو نمی‌سازد.
    /// </summary>
    public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var authUsers = provider.GetRequiredService<IIdentityAuthenticationService>();
        var parties = provider.GetRequiredService<IPartyDirectory>();
        var partyDb = provider.GetRequiredService<PartyDbContext>();
        var tuples = provider.GetRequiredService<IAuthorizationTupleWriter>();

        var sellerA = await partyDb.Parties.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DisplayName == SellerADisplayName, cancellationToken);
        var sellerB = await partyDb.Parties.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DisplayName == SellerBDisplayName, cancellationToken);

        var actorA = await EnsureUserAsync(authUsers, ActorAEmail, cancellationToken);
        var actorB = await EnsureUserAsync(authUsers, ActorBEmail, cancellationToken);

        // اگر نام نمایشی در DB به‌خاطر encoding خراب شده باشد، از عضویت Actor بازیابی می‌کنیم.
        sellerA ??= await ResolveSellerPartyByMembershipAsync(partyDb, actorA, cancellationToken);
        sellerB ??= await ResolveSellerPartyByMembershipAsync(partyDb, actorB, cancellationToken);
        if (sellerA is null || sellerB is null)
        {
            return;
        }

        await EnsureMembershipAsync(parties, partyDb, actorA, sellerA.PartyId, cancellationToken);
        await EnsureMembershipAsync(parties, partyDb, actorB, sellerB.PartyId, cancellationToken);

        // InMemory پس از restart خالی است؛ نوشتن مجدد برای fail-closed امن است.
        // اگر Mode=Disabled باشد Write پرتاب می‌کند — Development باید InMemory/SpiceDb باشد.
        try
        {
            await WriteMemberTupleAsync(tuples, actorA, sellerA.PartyId, cancellationToken);
            await WriteMemberTupleAsync(tuples, actorB, sellerB.PartyId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تست‌های Host بدون موتور مجوز نباید استارت را بشکنند؛ مسیر Seller fail-closed می‌ماند.
            return;
        }

        lock (Gate)
        {
            _snapshot = new SellerDevContextSnapshot(
                new SellerDevActorPair(actorA, "اپراتور آرمان", sellerA.PartyId, SellerADisplayName),
                new SellerDevActorPair(actorB, "اپراتور دیجی‌استایل", sellerB.PartyId, SellerBDisplayName));
        }
    }

    /// <summary>Development marketplace: snapshot را با Party واقعی DB تنظیم می‌کند.</summary>
    internal static void PublishSnapshot(Guid actorUserId, string actorLabel, Guid sellerPartyId, string sellerLabel)
    {
        lock (Gate)
        {
            _snapshot = new SellerDevContextSnapshot(
                new SellerDevActorPair(actorUserId, actorLabel, sellerPartyId, sellerLabel),
                new SellerDevActorPair(actorUserId, actorLabel, sellerPartyId, sellerLabel));
        }
    }

    /// <summary>کارمند محدود ACC را به snapshot اضافه می‌کند.</summary>
    internal static void PublishScopedEmployee(SellerDevActorPair employee)
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

    private static async Task<BusinessParty?> ResolveSellerPartyByMembershipAsync(
        PartyDbContext partyDb,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await partyDb.Memberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.RelationCode == MembershipRelationCodes.Member)
            .OrderBy(x => x.PartyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (membership is null)
        {
            return null;
        }

        return await partyDb.Parties.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartyId == membership.PartyId, cancellationToken);
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
        catch (IdentityDuplicateIdentifierException)
        {
            return await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, email, cancellationToken)
                ?? throw new InvalidOperationException("Seller demo actor could not be resolved after duplicate.");
        }
    }

    private static async Task EnsureMembershipAsync(
        IPartyDirectory parties,
        PartyDbContext partyDb,
        Guid userId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var exists = await partyDb.Memberships.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.PartyId == sellerPartyId && x.RelationCode == MembershipRelationCodes.Member,
            cancellationToken);
        if (exists)
        {
            return;
        }

        await parties.EstablishMembershipAsync(userId, sellerPartyId, MembershipRelationCodes.Member, cancellationToken);
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

/// <summary>
/// جفت Actor و Seller مجاز در Development.
/// </summary>
internal sealed record SellerDevActorPair(Guid ActorUserId, string ActorLabel, Guid SellerPartyId, string SellerLabel);

/// <summary>
/// نگاشت demo برای UI و شواهد؛ Actor سوم = کارمند محدود ACC.
/// </summary>
internal sealed record SellerDevContextSnapshot(
    SellerDevActorPair ActorA,
    SellerDevActorPair ActorB,
    SellerDevActorPair? ScopedEmployee = null);
