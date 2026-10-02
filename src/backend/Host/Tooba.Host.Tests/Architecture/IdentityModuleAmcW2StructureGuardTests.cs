using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-IDENTITY-AMC-001-W2 — Domain Aggregates/Enums + Application Ports/Options; no root dumps.</summary>
public sealed class IdentityModuleAmcW2StructureGuardTests
{
    [Fact]
    public void Identity_domain_and_application_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/Identity/Tooba.Identity.Domain");
        var app = Path.Combine(root, "src/backend/Modules/Identity/Tooba.Identity.Application");
        var contracts = Path.Combine(root, "src/backend/Modules/Identity/Tooba.Identity.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "IdentityDomain.cs")));
        Assert.False(File.Exists(Path.Combine(app, "IdentityContracts.cs")));
        Assert.True(Directory.Exists(Path.Combine(domain, "Aggregates")));
        Assert.True(Directory.Exists(Path.Combine(domain, "Enums")));
        Assert.True(Directory.Exists(Path.Combine(domain, "Rules")));
        Assert.True(Directory.Exists(Path.Combine(domain, "Events")));
        Assert.True(Directory.Exists(Path.Combine(app, "Ports")));
        Assert.True(Directory.Exists(Path.Combine(app, "Options")));
        Assert.True(Directory.Exists(Path.Combine(app, "Models")));
        Assert.True(File.Exists(Path.Combine(contracts, "Contacts", "ActorContactContracts.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Actors", "ActorIdentifierResolverContracts.cs")));
        Assert.False(File.Exists(Path.Combine(contracts, "ActorContactContracts.cs")));
        Assert.False(File.Exists(Path.Combine(contracts, "ActorIdentifierResolverContracts.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
