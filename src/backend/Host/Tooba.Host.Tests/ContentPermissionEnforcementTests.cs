using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.AccessControl.Application;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Content;
using Tooba.Identity.Application;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// اعمال ریزدانهٔ content.view|create|edit|publish روی مرز Admin Content
/// (معادل PUT/publish/GET/POST/category PATCH/author deactivate در endpointها).
/// </summary>
public sealed class ContentPermissionEnforcementTests
{
    private static readonly Guid AdminActor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");

    [Fact]
    public void Catalog_exposes_four_content_permission_codes()
    {
        var ids = PermissionCatalog.All.Select(p => p.PermissionId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(ContentAdminAccess.View, ids);
        Assert.Contains(ContentAdminAccess.Create, ids);
        Assert.Contains(ContentAdminAccess.Edit, ids);
        Assert.Contains(ContentAdminAccess.Publish, ids);
        Assert.Equal("content.view", ContentAdminAccess.View);
        Assert.Equal("content.create", ContentAdminAccess.Create);
        Assert.Equal("content.edit", ContentAdminAccess.Edit);
        Assert.Equal("content.publish", ContentAdminAccess.Publish);
    }

    [Fact]
    public async Task Tenant_member_with_content_view_can_list_get()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.View);
        var actor = await ContentAdminAccess.RequireAsync(
            Request(AdminActor),
            new CurrentAuthenticatedSession(),
            harness.Tenant,
            harness.Guard,
            new StubEnvironment(),
            harness.Authz,
            ContentAdminAccess.View,
            CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Tenant_member_without_content_edit_denied_on_put_article_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.View);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            ContentAdminAccess.RequireAsync(
                Request(AdminActor),
                new CurrentAuthenticatedSession(),
                harness.Tenant,
                harness.Guard,
                new StubEnvironment(),
                harness.Authz,
                ContentAdminAccess.Edit,
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("admin.authorization.denied", denied.ErrorCode);
        Assert.Equal("دسترسی محتوا مجاز نیست.", denied.Title);
        Assert.DoesNotContain("content.edit", denied.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tenant_member_without_content_publish_denied_on_publish_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.Edit);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            ContentAdminAccess.RequireAsync(
                Request(AdminActor),
                new CurrentAuthenticatedSession(),
                harness.Tenant,
                harness.Guard,
                new StubEnvironment(),
                harness.Authz,
                ContentAdminAccess.Publish,
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("admin.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Tenant_member_without_content_create_denied_on_post_create_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.View);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            ContentAdminAccess.RequireAsync(
                Request(AdminActor),
                new CurrentAuthenticatedSession(),
                harness.Tenant,
                harness.Guard,
                new StubEnvironment(),
                harness.Authz,
                ContentAdminAccess.Create,
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("admin.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Category_patch_without_edit_denied()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.View);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            ContentAdminAccess.RequireAsync(
                Request(AdminActor),
                new CurrentAuthenticatedSession(),
                harness.Tenant,
                harness.Guard,
                new StubEnvironment(),
                harness.Authz,
                ContentAdminAccess.Edit,
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("admin.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Author_deactivate_without_edit_denied()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.Create);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            ContentAdminAccess.RequireAsync(
                Request(AdminActor),
                new CurrentAuthenticatedSession(),
                harness.Tenant,
                harness.Guard,
                new StubEnvironment(),
                harness.Authz,
                ContentAdminAccess.Edit,
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("admin.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Edit_capability_allows_edit_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.Edit);
        var actor = await ContentAdminAccess.RequireAsync(
            Request(AdminActor),
            new CurrentAuthenticatedSession(),
            harness.Tenant,
            harness.Guard,
            new StubEnvironment(),
            harness.Authz,
            ContentAdminAccess.Edit,
            CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Publish_capability_allows_publish_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminAccess.Publish);
        var actor = await ContentAdminAccess.RequireAsync(
            Request(AdminActor),
            new CurrentAuthenticatedSession(),
            harness.Tenant,
            harness.Guard,
            new StubEnvironment(),
            harness.Authz,
            ContentAdminAccess.Publish,
            CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Unavailable_capability_fail_opens_like_support()
    {
        var tenant = CurrentTenant();
        var adapter = new InMemoryAuthorizationAdapter(
            new AuthorizationInstrumentation(),
            new InMemoryAuthorizationSecurityEventSink());
        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(AdminActor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Tenant,
                    Id = tenant.Current!.TenantId.Value,
                },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);

        var unavailable = new FailClosedAuthorizationAdapter(
            "test-unavailable",
            new AuthorizationInstrumentation());

        var actor = await ContentAdminAccess.RequireAsync(
            Request(AdminActor),
            new CurrentAuthenticatedSession(),
            tenant,
            new AuthorizationGuard(adapter),
            new StubEnvironment(),
            unavailable,
            ContentAdminAccess.Edit,
            CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    private static async Task<(
        StubCurrentTenant Tenant,
        IAuthorizationGuard Guard,
        IAuthorizationService Authz)> CreateHarnessAsync(string grant)
    {
        var tenant = CurrentTenant();
        var adapter = new InMemoryAuthorizationAdapter(
            new AuthorizationInstrumentation(),
            new InMemoryAuthorizationSecurityEventSink());
        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(AdminActor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Tenant,
                    Id = tenant.Current!.TenantId.Value,
                },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);
        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(AdminActor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = grant,
                },
                Relation = AuthorizationRelations.Granted,
            },
            CancellationToken.None);
        return (tenant, new AuthorizationGuard(adapter), adapter);
    }

    private static HttpRequest Request(Guid actor)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[AdminPanelAccess.DevActorHeader] = actor.ToString("D");
        return request;
    }

    private static StubCurrentTenant CurrentTenant() =>
        new(new TenantContext(
            new TenantId("store-alpha"),
            TenantStatus.Active,
            new ConnectionReference("tenant-alpha"),
            "فروشگاه نمونه",
            null,
            null,
            "localhost",
            null));

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; } = current;
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
