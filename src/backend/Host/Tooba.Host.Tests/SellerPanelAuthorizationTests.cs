using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Host.Seller;
using Tooba.Identity.Application;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// ماتریس مجوز Actor↔Seller؛ هدر SellerPartyId به‌تنهایی اجازه نمی‌دهد.
/// </summary>
public sealed class SellerPanelAuthorizationTests
{
    [Fact]
    public async Task Actor_matrix_allow_and_deny_via_authorization_guard()
    {
        var auth = CreateInMemory();
        var actorA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");
        var actorB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0002");
        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var sellerB = Guid.Parse("01a030d1-40db-7000-b90c-a0705133f0eb");

        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actorA),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Party, Id = sellerA.ToString("D") },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);
        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actorB),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Party, Id = sellerB.ToString("D") },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);

        await SellerPanelAccess.AuthorizeActorForSellerAsync(auth.Guard, actorA, sellerA, CancellationToken.None);
        await SellerPanelAccess.AuthorizeActorForSellerAsync(auth.Guard, actorB, sellerB, CancellationToken.None);

        var denyAb = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.AuthorizeActorForSellerAsync(auth.Guard, actorA, sellerB, CancellationToken.None));
        Assert.Equal(403, denyAb.StatusCode);
        Assert.Equal("seller.authorization.denied", denyAb.ErrorCode);

        var denyBa = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.AuthorizeActorForSellerAsync(auth.Guard, actorB, sellerA, CancellationToken.None));
        Assert.Equal(403, denyBa.StatusCode);
    }

    [Fact]
    public async Task Missing_actor_fails_closed()
    {
        var auth = CreateInMemory();
        var session = new CurrentAuthenticatedSession();
        var request = new DefaultHttpContext().Request;
        request.Headers[SellerPanelAccess.SellerPartyHeader] = "01a030d1-40cb-7000-8abe-6d31739956c5";

        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.RequireAuthorizedAsync(
                request,
                session,
                auth.Guard,
                new StubHostEnvironment(isDevelopment: true),
                CancellationToken.None));
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("seller.actor.missing", ex.ErrorCode);
    }

    [Fact]
    public async Task Changing_only_seller_party_header_does_not_grant_access()
    {
        var auth = CreateInMemory();
        var actorA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");
        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var sellerB = Guid.Parse("01a030d1-40db-7000-b90c-a0705133f0eb");
        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actorA),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Party, Id = sellerA.ToString("D") },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);

        var session = new CurrentAuthenticatedSession();
        session.Assign(new AuthenticatedIdentity(
            actorA,
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            "SingleStore",
            "store-alpha"));

        var allowed = new DefaultHttpContext().Request;
        allowed.Headers[SellerPanelAccess.SellerPartyHeader] = sellerA.ToString("D");
        var ok = await SellerPanelAccess.RequireAuthorizedAsync(
            allowed,
            session,
            auth.Guard,
            new StubHostEnvironment(isDevelopment: false),
            CancellationToken.None);
        Assert.Equal(actorA, ok.ActorUserId);
        Assert.Equal(sellerA, ok.SellerPartyId);

        var spoof = new DefaultHttpContext().Request;
        spoof.Headers[SellerPanelAccess.SellerPartyHeader] = sellerB.ToString("D");
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.RequireAuthorizedAsync(
                spoof,
                session,
                auth.Guard,
                new StubHostEnvironment(isDevelopment: false),
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
    }

    [Fact]
    public async Task Dev_actor_header_is_distinct_from_seller_party_and_verified()
    {
        var auth = CreateInMemory();
        var actorA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");
        var sellerA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actorA),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Party, Id = sellerA.ToString("D") },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);

        var request = new DefaultHttpContext().Request;
        request.Headers[SellerPanelAccess.DevActorHeader] = actorA.ToString("D");
        request.Headers[SellerPanelAccess.SellerPartyHeader] = sellerA.ToString("D");
        Assert.NotEqual(actorA, sellerA);

        var result = await SellerPanelAccess.RequireAuthorizedAsync(
            request,
            new CurrentAuthenticatedSession(),
            auth.Guard,
            new StubHostEnvironment(isDevelopment: true),
            CancellationToken.None);
        Assert.Equal(actorA, result.ActorUserId);
        Assert.Equal(sellerA, result.SellerPartyId);
    }

    [Fact]
    public async Task Unavailable_authorization_fails_closed()
    {
        var telemetry = new AuthorizationInstrumentation();
        IAuthorizationGuard guard = new AuthorizationGuard(new FailClosedAuthorizationAdapter("authorization.disabled", telemetry));
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            SellerPanelAccess.AuthorizeActorForSellerAsync(
                guard,
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001"),
                Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5"),
                CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("seller.authorization.unavailable", ex.ErrorCode);
    }

    private static (IAuthorizationService Service, IAuthorizationTupleWriter Writer, IAuthorizationGuard Guard) CreateInMemory()
    {
        var telemetry = new AuthorizationInstrumentation();
        var audit = new InMemoryAuthorizationSecurityEventSink();
        var adapter = new InMemoryAuthorizationAdapter(telemetry, audit);
        return (adapter, adapter, new AuthorizationGuard(adapter));
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(bool isDevelopment) =>
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
