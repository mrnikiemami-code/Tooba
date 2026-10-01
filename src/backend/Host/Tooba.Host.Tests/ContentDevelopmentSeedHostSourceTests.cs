using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T010-R1 + Composition AMC: Content seed uses scoped Host binder → module bootstrap.</summary>
public sealed class ContentDevelopmentSeedHostSourceTests
{
    [Fact]
    public void Program_uses_scoped_content_seed_host_not_root_provider()
    {
        var testsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var hostDir = Path.GetFullPath(Path.Combine(testsDir, "..", "Tooba.Host"));
        var program = File.ReadAllText(Path.Combine(hostDir, "Program.cs"));
        var host = File.ReadAllText(Path.Combine(hostDir, "Composition", "ContentDevelopmentSeedHost.cs"));

        Assert.Contains("ContentDevelopmentSeedHost.ApplyAsync(app.Services)", program);
        Assert.DoesNotContain("ContentDevelopmentSeed.ApplyAsync(app.Services)", program);
        Assert.Contains("CreateAsyncScope()", host);
        Assert.Contains("ContentDevelopmentSeedBootstrap.ApplyAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ContentDbContext", host, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalizationDbContext", host, StringComparison.Ordinal);
        Assert.DoesNotContain("MediaDbContext", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", host, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.Composition", host);
        Assert.DoesNotContain("namespace Tooba.Host.Content", host);
    }
}
