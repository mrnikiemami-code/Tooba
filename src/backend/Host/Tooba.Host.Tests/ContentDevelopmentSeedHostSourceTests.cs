using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P08-T010-R1: دانهٔ Content از root provider resolve نمی‌شود.</summary>
public sealed class ContentDevelopmentSeedHostSourceTests
{
    [Fact]
    public void Program_uses_scoped_content_seed_host_not_root_provider()
    {
        var testsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var hostDir = Path.GetFullPath(Path.Combine(testsDir, "..", "Tooba.Host"));
        var program = File.ReadAllText(Path.Combine(hostDir, "Program.cs"));
        var host = File.ReadAllText(Path.Combine(hostDir, "Content", "ContentDevelopmentSeedHost.cs"));

        Assert.Contains("ContentDevelopmentSeedHost.ApplyAsync(app.Services)", program);
        Assert.DoesNotContain("ContentDevelopmentSeed.ApplyAsync(app.Services)", program);
        Assert.Contains("CreateAsyncScope()", host);
        Assert.Contains("GetRequiredService<ContentDbContext>()", host);
        Assert.Contains("ContentDevelopmentSeed.ApplyAsync(provider)", host);
    }
}
