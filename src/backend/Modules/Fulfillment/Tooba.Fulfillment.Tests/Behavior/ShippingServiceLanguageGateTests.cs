using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Localization.Contracts;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>TB-TMAR-NEXT-MODULE-BATCH-004-R4 — Infrastructure-owned shipping language gate.</summary>
public sealed class ShippingServiceLanguageGateTests
{
    private static readonly Guid Known = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid Unknown = Guid.Parse("01900000-0000-7000-8000-00000000aa99");

    [Fact]
    public async Task Known_language_ids_succeed()
    {
        var gate = new ShippingServiceLanguageGate(new FixedLookup(
        [
            new LanguageLookupSnapshot(Known, "fa", "fa-IR", "fa", true),
        ]));

        await gate.EnsureKnownAsync([Known], CancellationToken.None);
    }

    [Fact]
    public async Task Unknown_language_id_yields_stable_shipping_service_language_invalid()
    {
        var gate = new ShippingServiceLanguageGate(new FixedLookup(
        [
            new LanguageLookupSnapshot(Known, "fa", "fa-IR", "fa", true),
        ]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => gate.EnsureKnownAsync([Unknown], CancellationToken.None));
        Assert.Equal("shipping_service.language_invalid", ex.Message);
    }

    [Fact]
    public async Task Seed_language_mapping_preserves_lookup_fields()
    {
        var gate = new ShippingServiceLanguageGate(new FixedLookup(
        [
            new LanguageLookupSnapshot(Known, "fa", "fa-IR", "fa", true),
            new LanguageLookupSnapshot(Guid.Parse("01900000-0000-7000-8000-00000000aa02"), "en", "en-US", "en", false),
        ]));

        var seed = await gate.ListForSeedAsync(CancellationToken.None);
        Assert.Equal(2, seed.Count);
        Assert.Contains(seed, x => x.LanguageId == Known && x.Code == "fa" && x.Culture == "fa-IR" && x.IsDefault);
        Assert.Contains(seed, x => x.Code == "en" && !x.IsDefault);
    }

    [Fact]
    public void No_host_implementation_of_language_gate()
    {
        var root = RepoRoot();
        var hostAdmin = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin");
        foreach (var file in Directory.EnumerateFiles(hostAdmin, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("HostShippingServiceLanguageGate", text, StringComparison.Ordinal);
            Assert.DoesNotContain(": IShippingServiceLanguageGate", text, StringComparison.Ordinal);
            Assert.DoesNotContain(", IShippingServiceLanguageGate", text, StringComparison.Ordinal);
        }

        var infra = Path.Combine(
            root, "src", "backend", "Modules", "Fulfillment", "Tooba.Fulfillment.Infrastructure",
            "Shipping", "ShippingServiceLanguageGate.cs");
        Assert.True(File.Exists(infra));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs/architecture/TOOBA-LOCKS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo.root.not_found");
    }

    private sealed class FixedLookup(IReadOnlyList<LanguageLookupSnapshot> langs) : ILanguageLookup
    {
        public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(langs);
    }
}
