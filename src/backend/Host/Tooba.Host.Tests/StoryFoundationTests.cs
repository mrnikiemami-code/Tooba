using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Persistence;
using global::Tooba.Story.Application;
using global::Tooba.Story.Domain;
using global::Tooba.Story.Infrastructure;
using global::Tooba.Story.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>پوشش foundation Story: seed عمومی، وضعیت‌ها، CTA ناامن، auth admin، reorder و locale.</summary>
[Collection("PostgresSerial")]
public sealed class StoryFoundationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_story")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>مرز schema و ثبت دایرکتوری Story.</summary>
    [Fact]
    public void Story_module_boundary_static_checks()
    {
        Assert.Equal("story", StoryDbContext.Schema);
        Assert.NotNull(typeof(IStoryDirectory).GetMethod(nameof(IStoryDirectory.GetPublicStoriesAsync)));
        Assert.NotNull(typeof(IStoryDirectory).GetMethod(nameof(IStoryDirectory.AdminSoftDisableAsync)));
        Assert.Equal(StoryStatus.Active, (StoryStatus)2);
        var endpoints = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Story", "StoryEndpoints.cs"));
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
    }

    /// <summary>seed فعال عمومی است؛ draft/scheduled/expired/disabled پنهان؛ CTA ناامن رد؛ reorder و locale فیلتر می‌شوند.</summary>
    [SkippableFact]
    public async Task Public_visibility_status_cta_reorder_locale_and_admin_auth_behave()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var directory = new StoryDirectory(db);
        var tenantId = StoryTenantIds.StoreAlpha;
        var now = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

        await StoryDevelopmentSeed.EnsureAsync(db, tenantId, now, CancellationToken.None);
        var publicSeeded = await directory.GetPublicStoriesAsync(tenantId, "fa", null, now, CancellationToken.None);
        Assert.True(publicSeeded.Count >= 2);
        Assert.Contains(publicSeeded, story => story.Title == "موبایل");
        Assert.Contains(publicSeeded, story => story.Title == "بازی");
        Assert.DoesNotContain(publicSeeded, story => story.Title == "English rail");
        Assert.Contains(publicSeeded, story => story.IsVideo);

        var draft = await directory.AdminCreateAsync(
            tenantId,
            new CreateStoryCommand("پیش‌نویس", "fa", null, null, "/images/stories/1.jpg", null, "none", null),
            CancellationToken.None);
        Assert.Equal(StoryStatus.Draft, draft.Status);
        Assert.DoesNotContain(
            await directory.GetPublicStoriesAsync(tenantId, "fa", null, now, CancellationToken.None),
            story => story.StoryId == draft.StoryId);

        var scheduled = await directory.AdminCreateAsync(
            tenantId,
            new CreateStoryCommand("آینده", "fa", null, null, "/images/stories/1.jpg", null, "none", null),
            CancellationToken.None);
        scheduled = await directory.AdminSetScheduleAsync(
            tenantId,
            scheduled.StoryId,
            new SetStoryScheduleCommand(now.AddDays(2), now.AddDays(5)),
            CancellationToken.None);
        Assert.Equal(StoryStatus.Scheduled, scheduled.Status);
        Assert.DoesNotContain(
            await directory.GetPublicStoriesAsync(tenantId, "fa", null, now, CancellationToken.None),
            story => story.StoryId == scheduled.StoryId);

        var expired = await directory.AdminCreateAsync(
            tenantId,
            new CreateStoryCommand("منقضی", "fa", null, null, "/images/stories/1.jpg", null, "none", null),
            CancellationToken.None);
        expired = await directory.AdminSetScheduleAsync(
            tenantId,
            expired.StoryId,
            new SetStoryScheduleCommand(now.AddDays(-5), now.AddDays(-1)),
            CancellationToken.None);
        Assert.Equal(StoryStatus.Expired, expired.Status);
        Assert.DoesNotContain(
            await directory.GetPublicStoriesAsync(tenantId, "fa", null, now, CancellationToken.None),
            story => story.StoryId == expired.StoryId);

        var disabled = await directory.AdminCreateAsync(
            tenantId,
            new CreateStoryCommand("خاموش", "fa", null, null, "/images/stories/1.jpg", null, "none", null),
            CancellationToken.None);
        await directory.AdminSetStatusAsync(tenantId, disabled.StoryId, StoryStatus.Active, CancellationToken.None);
        disabled = await directory.AdminSoftDisableAsync(tenantId, disabled.StoryId, CancellationToken.None);
        Assert.Equal(StoryStatus.Disabled, disabled.Status);
        Assert.DoesNotContain(
            await directory.GetPublicStoriesAsync(tenantId, "fa", null, now, CancellationToken.None),
            story => story.StoryId == disabled.StoryId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.AdminCreateAsync(
                tenantId,
                new CreateStoryCommand(
                    "ناامن",
                    "fa",
                    null,
                    null,
                    "/images/stories/1.jpg",
                    null,
                    "external",
                    "javascript:alert(1)"),
                CancellationToken.None));

        var listed = await directory.AdminListAsync(tenantId, CancellationToken.None);
        var reorderedIds = listed
            .OrderByDescending(story => story.DisplayOrder)
            .Select(story => story.StoryId)
            .ToList();
        var reordered = await directory.AdminReorderStoriesAsync(tenantId, reorderedIds, CancellationToken.None);
        Assert.Equal(reorderedIds, reordered.Select(story => story.StoryId).ToList());

        var missingActor = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            AdminPanelAccess.RequireAuthorizedAsync(
                new DefaultHttpContext().Request,
                new CurrentAuthenticatedSession(),
                CurrentTenant(),
                CreateAdapter().Guard,
                new StubEnvironment(),
                CancellationToken.None));
        Assert.Equal(401, missingActor.StatusCode);

        var sellerDenied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            AdminPanelAccess.RequireAuthorizedAsync(
                Request(Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbb0002")),
                new CurrentAuthenticatedSession(),
                CurrentTenant(),
                CreateAdapter().Guard,
                new StubEnvironment(),
                CancellationToken.None));
        Assert.Equal(403, sellerDenied.StatusCode);
    }

    private static StoryDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            StoryDbContext.Schema,
            typeof(StoryDbContext));
        return new StoryDbContext(options.Options);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
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

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; } = current;
    }
}
