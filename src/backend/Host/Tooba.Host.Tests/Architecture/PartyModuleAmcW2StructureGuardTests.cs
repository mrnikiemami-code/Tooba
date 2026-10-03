using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PARTY-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class PartyModuleAmcW2StructureGuardTests
{
    [Fact]
    public void Party_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/Party/Tooba.Party.Domain");
        var app = Path.Combine(root, "src/backend/Modules/Party/Tooba.Party.Application");
        var infra = Path.Combine(root, "src/backend/Modules/Party/Tooba.Party.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/Party/Tooba.Party.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "PartyDomain.cs")));
        Assert.False(File.Exists(Path.Combine(app, "PartyContracts.cs")));
        Assert.False(File.Exists(Path.Combine(contracts, "IPartyLookup.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "BusinessParty.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "PartyKind.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Events", "PartyMembershipEstablishedDomainEvent.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IPartyDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "PartyReferences.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "PartyDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "PartyOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Projections", "PartyMembershipProjectionHandler.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Ports", "IPartyLookup.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "PartyModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
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
