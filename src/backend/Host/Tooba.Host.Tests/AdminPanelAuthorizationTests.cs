using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Identity.Application;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// ماتریس دسترسی مدیر: فقط Actor عضو Tenant مجاز است و Seller با تغییر هدر اختیار نمی‌گیرد.
/// </summary>
public sealed class AdminPanelAuthorizationTests
{
    [Fact]
    public async Task Admin_actor_is_allowed_and_seller_actor_is_denied()
    {
        var adapter = CreateAdapter();
        var admin = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");
        var seller = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0002");
        var tenant = CurrentTenant();
        await adapter.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(admin),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = tenant.Current!.TenantId.Value },
                Relation = AuthorizationRelations.Member,
            },
            CancellationToken.None);

        var allowed = await AdminPanelAccess.RequireAuthorizedAsync(
            Request(admin),
            new CurrentAuthenticatedSession(),
            tenant,
            adapter.Guard,
            new StubEnvironment(),
            CancellationToken.None);
        Assert.Equal(admin, allowed);

        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            AdminPanelAccess.RequireAuthorizedAsync(
                Request(seller),
                new CurrentAuthenticatedSession(),
                tenant,
                adapter.Guard,
                new StubEnvironment(),
                CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal("admin.authorization.denied", denied.ErrorCode);
    }

    [Fact]
    public async Task Missing_actor_fails_closed()
    {
        var adapter = CreateAdapter();
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            AdminPanelAccess.RequireAuthorizedAsync(
                new DefaultHttpContext().Request,
                new CurrentAuthenticatedSession(),
                CurrentTenant(),
                adapter.Guard,
                new StubEnvironment(),
                CancellationToken.None));
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("admin.actor.missing", ex.ErrorCode);
    }

    [Fact]
    public async Task Missing_tenant_fails_closed_before_authorization()
    {
        var adapter = CreateAdapter();
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            AdminPanelAccess.RequireAuthorizedAsync(
                Request(Guid.NewGuid()),
                new CurrentAuthenticatedSession(),
                new StubCurrentTenant(null),
                adapter.Guard,
                new StubEnvironment(),
                CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("admin.tenant.missing", ex.ErrorCode);
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

    private static (IAuthorizationTupleWriter Writer, IAuthorizationGuard Guard) CreateAdapter()
    {
        var adapter = new InMemoryAuthorizationAdapter(
            new AuthorizationInstrumentation(),
            new InMemoryAuthorizationSecurityEventSink());
        return (adapter, new AuthorizationGuard(adapter));
    }

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
