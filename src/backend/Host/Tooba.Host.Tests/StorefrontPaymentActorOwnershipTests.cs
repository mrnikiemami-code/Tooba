using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P10-T004-R1 — payment actor must follow authenticated session (not hard-coded guest).
/// </summary>
public sealed class StorefrontPaymentActorOwnershipTests
{
    [Fact]
    public void Payment_composer_resolves_actor_from_authenticated_session()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(
            root,
            "src", "backend", "Host", "Tooba.Host", "Storefront", "StorefrontPaymentComposer.cs"));
        Assert.Contains("ResolvePaymentActor", src, StringComparison.Ordinal);
        Assert.Contains("CurrentAuthenticatedSession", src, StringComparison.Ordinal);
        Assert.Contains("_session.IsAuthenticated", src, StringComparison.Ordinal);
        Assert.Contains("_session.UserId", src, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "InitiatePaymentCommand(\n            StorefrontCheckoutComposer.StorefrontGuestActorId,",
            src,
            StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
