using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin.Access;
using Tooba.Host.Configuration;
using Tooba.Identity.Contracts.Auth;
using Xunit;

namespace Tooba.Host.Tests.Development;

/// <summary>
/// TB239-R1 focused facts for the opt-in Marketplace Development administrator provisioning seam.
/// These prove the GUARD SHAPE around the existing tuple writer; the main gate stays the real normal
/// HTTP admin-login/protected-route 200 and seller 403 control recorded in the task evidence.
/// </summary>
public sealed class MarketplaceAdminDevBootstrapTests
{
    private static readonly Guid RealAdmin = Guid.Parse("11111111-1111-4111-8111-111111111111");

    [Fact]
    public async Task Enabled_Development_Marketplace_identifier_binds_the_same_existing_tenant_member_relation()
    {
        var writer = new RecordingTupleWriter();
        var provider = Build(
            ToobaEdition.Marketplace,
            "Marketplace",
            options: new MarketplaceAdminDevOptions { Enabled = true, Identifier = "  dev.admin@example.invalid  " },
            writer: writer,
            resolve: (kind, identifier) =>
                kind == LoginIdentifierKind.Email && identifier == "dev.admin@example.invalid" ? RealAdmin : null);

        await MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync(provider, Environment("Development"), CancellationToken.None);

        var write = Assert.Single(writer.Writes);
        Assert.Equal(RealAdmin.ToString("D"), write.Subject.Id);
        Assert.Equal(AuthorizationObjectTypes.User, write.Subject.Type);
        Assert.Equal(HostAdminPanelAccess.MarketplacePlatformTenantId, write.Resource.Id);
        Assert.Equal(AuthorizationObjectTypes.Tenant, write.Resource.Type);
        Assert.Equal(AuthorizationRelations.Member, write.Relation);
    }

    [Fact]
    public async Task Repeat_provisioning_is_idempotent_and_writes_the_same_single_subject()
    {
        var writer = new RecordingTupleWriter();
        var provider = Build(
            ToobaEdition.Marketplace,
            "Development",
            new MarketplaceAdminDevOptions { Enabled = true, Identifier = "dev.admin@example.invalid" },
            writer,
            (_, _) => RealAdmin);

        await MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync(provider, Environment("Development"), CancellationToken.None);
        await MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync(provider, Environment("Development"), CancellationToken.None);

        Assert.Equal(2, writer.Writes.Count);
        Assert.All(writer.Writes, w => Assert.Equal(RealAdmin.ToString("D"), w.Subject.Id));
        Assert.All(writer.Writes, w => Assert.Equal(HostAdminPanelAccess.MarketplacePlatformTenantId, w.Resource.Id));
    }

    [Fact]
    public async Task Unknown_or_blank_identifier_grants_nothing()
    {
        foreach (var identifier in new[] { "unknown@example.invalid", "", "   " })
        {
            var writer = new RecordingTupleWriter();
            var provider = Build(
                ToobaEdition.Marketplace,
                "Development",
                new MarketplaceAdminDevOptions { Enabled = true, Identifier = identifier },
                writer,
                (_, _) => null);

            await MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync(provider, Environment("Development"), CancellationToken.None);

            Assert.Empty(writer.Writes);
        }
    }

    [Fact]
    public async Task Disabled_Production_or_SingleStore_configuration_adds_no_provisioning()
    {
        var cases = new (ToobaEdition Edition, string EnvironmentName, MarketplaceAdminDevOptions Options)[]
        {
            (ToobaEdition.Marketplace, "Development", new MarketplaceAdminDevOptions { Enabled = false, Identifier = "dev.admin@example.invalid" }),
            (ToobaEdition.Marketplace, "Production", new MarketplaceAdminDevOptions { Enabled = true, Identifier = "dev.admin@example.invalid" }),
            (ToobaEdition.Marketplace, "Staging", new MarketplaceAdminDevOptions { Enabled = true, Identifier = "dev.admin@example.invalid" }),
            (ToobaEdition.SingleStore, "Development", new MarketplaceAdminDevOptions { Enabled = true, Identifier = "dev.admin@example.invalid" }),
        };

        foreach (var (edition, environmentName, options) in cases)
        {
            var writer = new RecordingTupleWriter();
            var resolved = false;
            var provider = Build(
                edition,
                "Development",
                options,
                writer,
                (_, _) =>
                {
                    resolved = true;
                    return RealAdmin;
                });

            await MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync(provider, Environment(environmentName), CancellationToken.None);

            Assert.Empty(writer.Writes);
            Assert.False(resolved);
        }
    }

    [Fact]
    public void Checked_in_configuration_never_enables_the_seam()
    {
        var root = FindRepoRoot();
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, "src", "backend", "Host", "Tooba.Host"),
                     "appsettings*.json",
                     SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("MarketplaceAdmin", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IServiceProvider Build(
        ToobaEdition edition,
        string deploymentId,
        MarketplaceAdminDevOptions options,
        IAuthorizationTupleWriter writer,
        Func<LoginIdentifierKind, string, Guid?> resolve)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ControlPlaneRegistry
        {
            Edition = edition,
            DeploymentId = deploymentId,
            MarketplaceConnectionReference = edition == ToobaEdition.Marketplace
                ? new ConnectionReference("marketplace")
                : null,
            Hosts = new Dictionary<string, TenantRecord>(),
            Tenants = new Dictionary<string, TenantRecord>(),
        });
        services.AddSingleton<IOptions<MarketplaceAdminDevOptions>>(Options.Create(options));
        services.AddSingleton(writer);
        services.AddSingleton(IdentityDirectoryStub.Create(resolve));
        return services.BuildServiceProvider();
    }

    private static IHostEnvironment Environment(string name) => new EnvironmentStub(name);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tooba.Host";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class RecordingTupleWriter : IAuthorizationTupleWriter
    {
        public List<AuthorizationRelationshipWrite> Writes { get; } = [];

        public Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken)
        {
            Writes.Add(write);
            return Task.CompletedTask;
        }
    }

    /// <summary>Minimal identity directory stub; the seam must only need identifier lookup.</summary>
    private class IdentityDirectoryStub : DispatchProxy
    {
        private Func<LoginIdentifierKind, string, Guid?> _resolve = (_, _) => null;

        public static IIdentityAuthenticationService Create(Func<LoginIdentifierKind, string, Guid?> resolve)
        {
            var proxy = Create<IIdentityAuthenticationService, IdentityDirectoryStub>();
            ((IdentityDirectoryStub)(object)proxy)._resolve = resolve;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IIdentityAuthenticationService.FindUserIdByIdentifierAsync))
            {
                return Task.FromResult(_resolve((LoginIdentifierKind)args![0]!, (string)args[1]!));
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
