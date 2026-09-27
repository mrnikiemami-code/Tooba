using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Content.Endpoints.Admin;
using Tooba.Host.Admin;
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
        Assert.Contains(ContentAdminPermissions.View, ids);
        Assert.Contains(ContentAdminPermissions.Create, ids);
        Assert.Contains(ContentAdminPermissions.Edit, ids);
        Assert.Contains(ContentAdminPermissions.Publish, ids);
        Assert.Equal("content.view", ContentAdminPermissions.View);
        Assert.Equal("content.create", ContentAdminPermissions.Create);
        Assert.Equal("content.edit", ContentAdminPermissions.Edit);
        Assert.Equal("content.publish", ContentAdminPermissions.Publish);
    }

    [Fact]
    public async Task Tenant_member_with_content_view_can_list_get()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.View);
        var actor = await RequireAsync(harness, ContentAdminPermissions.View);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Tenant_member_without_content_edit_denied_on_put_article_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.View);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            RequireAsync(harness, ContentAdminPermissions.Edit));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("content.authorization.denied", denied.ErrorCode);
        Assert.Equal("Authorization denied", denied.Title);
    }

    [Fact]
    public async Task Tenant_member_without_content_publish_denied_on_publish_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.Edit);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            RequireAsync(harness, ContentAdminPermissions.Publish));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("content.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Tenant_member_without_content_create_denied_on_post_create_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.View);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            RequireAsync(harness, ContentAdminPermissions.Create));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("content.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Category_patch_without_edit_denied()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.View);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            RequireAsync(harness, ContentAdminPermissions.Edit));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("content.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Author_deactivate_without_edit_denied()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.Create);
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            RequireAsync(harness, ContentAdminPermissions.Edit));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("content.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Edit_capability_allows_edit_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.Edit);
        var actor = await RequireAsync(harness, ContentAdminPermissions.Edit);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Publish_capability_allows_publish_gate()
    {
        var harness = await CreateHarnessAsync(grant: ContentAdminPermissions.Publish);
        var actor = await RequireAsync(harness, ContentAdminPermissions.Publish);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Unavailable_capability_fail_closes_with_503()
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
        var adminAccess = new StubAdminPanelAccess(
            new CurrentAuthenticatedSession(),
            tenant,
            new AuthorizationGuard(adapter),
            new StubEnvironment());

        foreach (var permission in new[]
                 {
                     ContentAdminPermissions.Create,
                     ContentAdminPermissions.Edit,
                     ContentAdminPermissions.Publish,
                 })
        {
            var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
                RequireAsync(adminAccess, tenant, unavailable, permission));
            Assert.Equal(503, denied.StatusCode);
            Assert.Equal("content.authorization.unavailable", denied.ErrorCode);
            Assert.Equal("Authorization unavailable", denied.Title);
        }
    }

    
    private static Task<Guid> RequireAsync(
        (StubCurrentTenant Tenant, IAdminPanelAccess AdminAccess, IAuthorizationService Authz) harness,
        string permission) =>
        new ContentAdminAuthorizer(harness.AdminAccess, harness.Tenant, harness.Authz)
            .RequireAsync(Http(AdminActor), permission, CancellationToken.None);

    private static Task<Guid> RequireAsync(
        IAdminPanelAccess adminAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        string permission) =>
        new ContentAdminAuthorizer(adminAccess, tenant, authz)
            .RequireAsync(Http(AdminActor), permission, CancellationToken.None);

    private static HttpContext Http(Guid actor)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[AdminPanelAccess.DevActorHeader] = actor.ToString("D");
        return http;
    }

    private static async Task<(
        StubCurrentTenant Tenant,
        IAdminPanelAccess AdminAccess,
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
        var guard = new AuthorizationGuard(adapter);
        var env = new StubEnvironment();
        var adminAccess = new StubAdminPanelAccess(new CurrentAuthenticatedSession(), tenant, guard, env);
        return (tenant, adminAccess, adapter);
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

    private sealed class StubAdminPanelAccess(
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment) : IAdminPanelAccess
    {
        public Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken) =>
            AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
    }
}

