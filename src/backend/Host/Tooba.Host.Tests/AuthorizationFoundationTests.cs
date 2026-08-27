using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Host;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation مجوز ReBAC بدون ماتریس محصول و بدون fail-open.
/// </summary>
public sealed class AuthorizationFoundationTests
{
    [Fact]
    public async Task User_subject_allow_deny_and_tenant_isolation()
    {
        var auth = CreateInMemory();
        var user = AuthorizationSubject.ForUser(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var tenantA = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-a" };
        var tenantB = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-b" };
        var ctx = new AuthorizationCallContext { TenantId = "tenant-a", Edition = ToobaEdition.SingleStore };

        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite { Subject = user, Resource = tenantA, Relation = AuthorizationRelations.Member },
            CancellationToken.None);

        var allow = await auth.Guard.AuthorizeUseCaseAsync(
            new AuthorizationCheck { Subject = user, Resource = tenantA, Permission = AuthorizationRelations.View, CallContext = ctx },
            CancellationToken.None);
        var denyMissing = await auth.Service.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenantB, Permission = AuthorizationRelations.View, CallContext = ctx with { TenantId = "tenant-b" } },
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Allow, allow.Kind);
        Assert.Equal(AuthorizationDecisionKind.Deny, denyMissing.Kind);
    }

    [Fact]
    public void Invalid_resource_or_relation_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => AuthorizationContractValidator.Validate(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(Guid.NewGuid()),
                Resource = new AuthorizationResource { Type = "Tenant", Id = "a" },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext(),
            }));
        Assert.Throws<ArgumentException>(() => AuthorizationContractValidator.Validate(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(Guid.NewGuid()),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "https://host.example/a" },
                Relation = AuthorizationRelations.Member,
            }));
    }

    [Fact]
    public async Task Unavailable_adapter_does_not_fail_open()
    {
        var telemetry = new AuthorizationInstrumentation();
        IAuthorizationService service = new FailClosedAuthorizationAdapter("spicedb.unavailable", telemetry);
        var decision = await service.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(Guid.NewGuid()),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-a" },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext { Edition = ToobaEdition.Marketplace },
            },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Unavailable, decision.Kind);
        Assert.False(decision.IsAllow);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ((IAuthorizationTupleWriter)service).WriteAsync(
                new AuthorizationRelationshipWrite
                {
                    Subject = AuthorizationSubject.ForUser(Guid.NewGuid()),
                    Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-a" },
                    Relation = AuthorizationRelations.Member,
                },
                CancellationToken.None));
    }

    [Fact]
    public void Domain_and_application_have_no_spicedb_sdk_and_user_has_no_role_column()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Identity", "Tooba.Identity.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Identity", "Tooba.Identity.Application"),
                     Path.Combine(root, "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks"),
                     Path.Combine(root, "src", "backend", "Modules", "Tooba.ModuleContracts"),
                 })
        {
            var csproj = Directory.GetFiles(project, "*.csproj").Single();
            var text = File.ReadAllText(csproj);
            Assert.DoesNotContain("Authzed", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SpiceDB", text, StringComparison.OrdinalIgnoreCase);
        }

        var userSource = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Identity", "Tooba.Identity.Domain", "IdentityDomain.cs"));
        Assert.DoesNotContain("Role", userSource, StringComparison.Ordinal);
        Assert.Contains("class UserAccount", userSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Schema_bootstrap_is_versioned_and_opt_in()
    {
        var schema = new FoundationAuthorizationSchemaProvider();
        Assert.Equal(3, schema.SchemaVersion);
        Assert.Contains("definition party", schema.SchemaText, StringComparison.Ordinal);
        Assert.Contains("definition user", schema.SchemaText, StringComparison.Ordinal);
        Assert.Contains("definition capability", schema.SchemaText, StringComparison.Ordinal);
        Assert.Contains("definition category", schema.SchemaText, StringComparison.Ordinal);
        var logger = LoggerFactory.Create(b => { }).CreateLogger<ConfiguredAuthorizationSchemaBootstrapper>();
        var skipped = new ConfiguredAuthorizationSchemaBootstrapper(
            Options.Create(new AuthorizationHostOptions { ApplySchemaOnStartup = false }),
            schema,
            logger);
        await skipped.BootstrapIfConfiguredAsync(CancellationToken.None);
        Assert.Null(skipped.AppliedVersion);

        var applied = new ConfiguredAuthorizationSchemaBootstrapper(
            Options.Create(new AuthorizationHostOptions { ApplySchemaOnStartup = true }),
            schema,
            logger);
        await applied.BootstrapIfConfiguredAsync(CancellationToken.None);
        Assert.Equal(3, applied.AppliedVersion);
    }

    [Fact]
    public async Task Authorization_token_is_not_logged_by_bootstrap()
    {
        var logger = new ListLogger<ConfiguredAuthorizationSchemaBootstrapper>();
        var bootstrapper = new ConfiguredAuthorizationSchemaBootstrapper(
            Options.Create(new AuthorizationHostOptions
            {
                ApplySchemaOnStartup = true,
                SpiceDb = new SpiceDbHostOptions { Token = "super-secret-token" },
            }),
            new FoundationAuthorizationSchemaProvider(),
            logger);
        await bootstrapper.BootstrapIfConfiguredAsync(CancellationToken.None);
        Assert.All(logger.Messages, message => Assert.DoesNotContain("super-secret-token", message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpiceDb_adapter_does_not_claim_allow_without_a_running_server()
    {
        IAuthorizationService adapter = new SpiceDbAuthorizationAdapter(
            Options.Create(new AuthorizationHostOptions
            {
                Mode = "SpiceDb",
                SpiceDb = new SpiceDbHostOptions
                {
                    Endpoint = "127.0.0.1:1",
                    Token = "test-only-not-for-production",
                    UseTls = false,
                    TimeoutSeconds = 2,
                },
            }),
            new AuthorizationInstrumentation(),
            new InMemoryAuthorizationSecurityEventSink(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpiceDbAuthorizationAdapter>.Instance);
        var decision = await adapter.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(Guid.NewGuid()),
                Resource = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-a" },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext(),
            },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Unavailable, decision.Kind);
    }

    [Fact]
    public void SpiceDb_mode_without_endpoint_fails_validation()
    {
        var validator = new AuthorizationOptionsValidator(new FakeHostEnvironment { EnvironmentName = Environments.Development });
        var result = validator.Validate(null, new AuthorizationHostOptions { Mode = "SpiceDb" });
        Assert.True(result.Failed);
    }

    [Fact]
    public async Task InMemory_revoke_removes_membership()
    {
        var auth = CreateInMemory();
        var user = AuthorizationSubject.ForUser(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        var tenant = new AuthorizationResource { Type = AuthorizationObjectTypes.Tenant, Id = "tenant-r" };
        var ctx = new AuthorizationCallContext { TenantId = "tenant-r", Edition = ToobaEdition.SingleStore };
        var write = new AuthorizationRelationshipWrite { Subject = user, Resource = tenant, Relation = AuthorizationRelations.Member };
        await auth.Writer.WriteAsync(write, CancellationToken.None);
        var allow = await auth.Service.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenant, Permission = AuthorizationRelations.View, CallContext = ctx },
            CancellationToken.None);
        await auth.Writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = write.Subject,
                Resource = write.Resource,
                Relation = write.Relation,
                Operation = AuthorizationRelationshipOperation.Delete,
            },
            CancellationToken.None);
        var deny = await auth.Service.CanAsync(
            new AuthorizationCheck { Subject = user, Resource = tenant, Permission = AuthorizationRelations.View, CallContext = ctx },
            CancellationToken.None);
        Assert.Equal(AuthorizationDecisionKind.Allow, allow.Kind);
        Assert.Equal(AuthorizationDecisionKind.Deny, deny.Kind);
    }

    private static (IAuthorizationService Service, IAuthorizationTupleWriter Writer, IAuthorizationGuard Guard) CreateInMemory()
    {
        var telemetry = new AuthorizationInstrumentation();
        var audit = new InMemoryAuthorizationSecurityEventSink();
        var adapter = new InMemoryAuthorizationAdapter(telemetry, audit);
        return (adapter, adapter, new AuthorizationGuard(adapter));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}

/// <summary>
/// محیط جعلی برای اعتبارسنجی Options.
/// </summary>
internal sealed class FakeHostEnvironment : IHostEnvironment
{
    /// <inheritdoc />
    public string EnvironmentName { get; set; } = Environments.Development;

    /// <inheritdoc />
    public string ApplicationName { get; set; } = "tests";

    /// <inheritdoc />
    public string ContentRootPath { get; set; } = ".";

    /// <inheritdoc />
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

/// <summary>
/// logger لیست برای اثبات عدم نشت توکن.
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    /// <summary>
    /// پیام‌های قالب‌بندی‌شده.
    /// </summary>
    public List<string> Messages { get; } = [];

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
