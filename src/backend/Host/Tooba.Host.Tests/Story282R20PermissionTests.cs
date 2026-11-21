using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Host.Security.Seller;
using Tooba.Story.Endpoints;
using Tooba.Story.Endpoints.Admin;
using Tooba.Story.Endpoints.Permissions;
using Tooba.Story.Endpoints.Seller;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-282-R20 A5 — a Story route must be gated by the owning Story capability on the server,
/// not only by tenant/party view plus a hidden menu entry. DENY and Unavailable both fail closed.
/// </summary>
public sealed class Story282R20PermissionTests
{
    private static readonly Guid Actor = Guid.Parse("01a04246-b309-7000-b1de-681552e70532");
    private static readonly Guid Seller = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");

    [Fact]
    public async Task Admin_capability_allow_returns_actor_and_checks_the_owning_permission()
    {
        var authz = new FakeAuthorization(AuthorizationDecision.Allow());
        var authorizer = new StoryAdminAuthorizer(new FakeAdminAccess(Actor), authz, new FakeTenant());

        var actor = await authorizer.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.Approve, CancellationToken.None);

        Assert.Equal(Actor, actor);
        Assert.NotNull(authz.Last);
        Assert.Equal(StoryPermissionIds.Approve, authz.Last!.Resource.Id);
        Assert.Equal(AuthorizationObjectTypes.Permission, authz.Last.Resource.Type);
        Assert.Equal(AuthorizationRelations.Check, authz.Last.Permission);
        Assert.Equal(Actor.ToString("D"), authz.Last.Subject.Id);
    }

    [Fact]
    public async Task Admin_capability_deny_and_unavailable_are_refusals_not_allow()
    {
        var denied = new StoryAdminAuthorizer(
            new FakeAdminAccess(Actor),
            new FakeAuthorization(AuthorizationDecision.Deny()),
            new FakeTenant());
        var deny = await Assert.ThrowsAsync<SemanticException>(() => denied.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.Publish, CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, deny.Error.Code);

        var unavailable = new StoryAdminAuthorizer(
            new FakeAdminAccess(Actor),
            new FakeAuthorization(AuthorizationDecision.Unavailable("spicedb-down")),
            new FakeTenant());
        var down = await Assert.ThrowsAsync<SemanticException>(() => unavailable.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.Publish, CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, down.Error.Code);
    }

    [Fact]
    public async Task Seller_capability_gates_story_actions_and_seller_cannot_pass_a_moderation_permission()
    {
        var allow = new HostStorySellerAuthorizer(
            new FakeSellerAccess(Actor, Seller),
            new FakeAuthorization(AuthorizationDecision.Allow()),
            new FakeEdition());
        var (actor, shop) = await allow.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.Submit, CancellationToken.None);
        Assert.Equal(Actor, actor);
        Assert.Equal(Seller, shop);

        var deny = new HostStorySellerAuthorizer(
            new FakeSellerAccess(Actor, Seller),
            new FakeAuthorization(AuthorizationDecision.Deny()),
            new FakeEdition());
        var refusedView = await Assert.ThrowsAsync<SemanticException>(() => deny.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.View, CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.SellerAuthorizationDenied, refusedView.Error.Code);

        // A seller-side route must never be able to borrow admin moderation authority.
        var refusedApprove = await Assert.ThrowsAsync<SemanticException>(() => deny.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.Approve, CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.SellerAuthorizationDenied, refusedApprove.Error.Code);
    }

    [Fact]
    public async Task Cross_shop_panel_denial_is_preserved_before_any_capability_allow()
    {
        var authorizer = new HostStorySellerAuthorizer(
            new FakeSellerAccess(Actor, Seller, deny: true),
            new FakeAuthorization(AuthorizationDecision.Allow()),
            new FakeEdition());

        await Assert.ThrowsAsync<SemanticException>(() => authorizer.RequireAuthorizedAsync(
            new DefaultHttpContext(), StoryPermissionIds.View, CancellationToken.None));
    }

    [Theory]
    [InlineData("/{id:guid}/approve", StoryPermissionIds.Approve)]
    [InlineData("/{id:guid}/reject", StoryPermissionIds.Reject)]
    [InlineData("/{id:guid}/approve-and-publish", StoryPermissionIds.Approve)]
    [InlineData("/{id:guid}/enable", StoryPermissionIds.Publish)]
    [InlineData("/{id:guid}/disable", StoryPermissionIds.Publish)]
    public void Admin_route_wiring_uses_the_matching_owning_permission(string route, string permission)
    {
        var source = File.ReadAllText(AdminEndpointsPath());
        var handler = HandlerFor(source, route);
        var expected = permission switch
        {
            StoryPermissionIds.Approve => "StoryPermissionIds.Approve",
            StoryPermissionIds.Reject => "StoryPermissionIds.Reject",
            StoryPermissionIds.Publish => "StoryPermissionIds.Publish",
            _ => "StoryPermissionIds.Edit",
        };
        Assert.Contains(expected, handler, StringComparison.Ordinal);
        Assert.DoesNotContain("StoryPermissionIds.View", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_route_wiring_uses_submit_for_submit_and_view_for_reads()
    {
        var source = File.ReadAllText(SellerEndpointsPath());
        var submit = HandlerFor(source, "/{id:guid}/submit");
        Assert.Contains("StoryPermissionIds.Submit", submit, StringComparison.Ordinal);
        Assert.Contains("StoryPermissionIds.View", source, StringComparison.Ordinal);
    }

    private static string HandlerFor(string source, string route)
    {
        var registration = System.Text.RegularExpressions.Regex.Match(
            source,
            "Map(?:Get|Post|Put|Delete)\\(\"" + System.Text.RegularExpressions.Regex.Escape(route) + "\",\\s*(?<handler>[A-Za-z0-9_]+)\\)");
        Assert.True(registration.Success, $"registration for {route} not found");
        var handler = registration.Groups["handler"].Value;

        var index = source.IndexOf("private static", StringComparison.Ordinal);
        while (index >= 0)
        {
            var next = source.IndexOf("private static", index + 10, StringComparison.Ordinal);
            var block = next < 0 ? source[index..] : source[index..next];
            if (block.Contains(handler + "(", StringComparison.Ordinal))
            {
                return block;
            }

            index = next;
        }

        Assert.Fail($"handler {handler} for {route} not found");
        return string.Empty;
    }

    private static string AdminEndpointsPath() => Path.Combine(
        RepoRoot(), "src", "backend", "Modules", "Story", "Tooba.Story.Endpoints", "Admin", "StoryAdminEndpoints.cs");

    private static string SellerEndpointsPath() => Path.Combine(
        RepoRoot(), "src", "backend", "Modules", "Story", "Tooba.Story.Endpoints", "Seller", "StorySellerEndpoints.cs");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !Directory.Exists(Path.Combine(dir.FullName, "src", "backend", "Modules")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private sealed class FakeAdminAccess(Guid actor, bool deny = false) : IAdminPanelAccess
    {
        public Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken)
            => deny
                ? throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied))
                : Task.FromResult(actor);
    }

    private sealed class FakeSellerAccess(Guid actor, Guid seller, bool deny = false) : ISellerPanelAccess
    {
        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
            => deny
                ? throw new SemanticException(new SemanticError(FoundationErrorCodes.SellerAuthorizationDenied))
                : Task.FromResult((actor, seller));
    }

    private sealed class FakeAuthorization(AuthorizationDecision decision) : IAuthorizationService
    {
        public AuthorizationCheck? Last { get; private set; }

        public Task<AuthorizationDecision> CanAsync(
            AuthorizationCheck check,
            CancellationToken cancellationToken)
        {
            Last = check;
            return Task.FromResult(decision);
        }
    }

    private sealed class FakeTenant : ICurrentTenant
    {
        public TenantContext? Current => null;
    }

    private sealed class FakeEdition : ICurrentEdition
    {
        public EditionContext? Current { get; } =
            new EditionContext(ToobaEdition.SingleStore, "tb-282-r20");
    }
}
