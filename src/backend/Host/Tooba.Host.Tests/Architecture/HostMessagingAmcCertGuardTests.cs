using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT — durable certification of Host/Messaging platform boundary.
/// </summary>
public sealed class HostMessagingAmcCertGuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "InProcessIntegrationEventPublisher.cs",
        "MassTransitIntegrationEventPublisher.cs",
        "MessagingDisabledPublisher.cs",
        "MessagingHostOptions.cs",
        "MessagingOptionsValidator.cs",
        "MessagingRegistration.cs",
        "MessagingRetryConfigurator.cs",
    ];

    private static readonly Regex TopLevelType = new(
        @"^\s*(?:internal\s+|public\s+|file\s+)?(?:sealed\s+|static\s+|abstract\s+)*(?:partial\s+)?(?:class|record|struct|enum|interface)\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart|StoreContext)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Messaging_certified_exact_tree_namespace_selection_and_boundaries()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Messaging");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        foreach (var name in ExpectedFiles)
        {
            var text = File.ReadAllText(Path.Combine(dir, name!));
            Assert.Contains("namespace Tooba.Host.Messaging;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
            var types = TopLevelType.Matches(text).Select(m => m.Groups[1].Value).ToArray();
            Assert.True(types.Length == 1, name + " types=" + string.Join(",", types));
            Assert.Equal(Path.GetFileNameWithoutExtension(name), types[0]);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            foreach (Match m in ForeignModuleLayer.Matches(text))
                Assert.Fail("foreign module layer: " + name + ": " + m.Value);
        }

        var registration = Read("src/backend/Host/Tooba.Host/Messaging/MessagingRegistration.cs");
        Assert.Contains("UseInProcessTestDouble", registration, StringComparison.Ordinal);
        Assert.Contains("IsEnvironment(\"Testing\")", registration, StringComparison.Ordinal);
        Assert.Contains("InProcessIntegrationEventPublisher", registration, StringComparison.Ordinal);
        Assert.Contains("MassTransitIntegrationEventPublisher", registration, StringComparison.Ordinal);
        Assert.Contains("MessagingDisabledPublisher", registration, StringComparison.Ordinal);
        Assert.Contains("IntegrationEndpointName = \"tooba-integration\"", registration, StringComparison.Ordinal);
        Assert.Contains("CreateDatabase = false", registration, StringComparison.Ordinal);
        Assert.Contains("CreateInfrastructure = true", registration, StringComparison.Ordinal);
        Assert.Contains("WaitUntilStarted = true", registration, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(30)", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMQ", registration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sp.GetRequiredService", registration, StringComparison.Ordinal);
        Assert.Contains("context.GetRequiredService", registration, StringComparison.Ordinal);

        var inProcess = Read("src/backend/Host/Tooba.Host/Messaging/InProcessIntegrationEventPublisher.cs");
        Assert.Contains("internal sealed class InProcessIntegrationEventPublisher", inProcess, StringComparison.Ordinal);
        Assert.Contains("IServiceProvider", inProcess, StringComparison.Ordinal);
        Assert.Contains("MakeGenericType", inProcess, StringComparison.Ordinal);
        Assert.Contains("GetServices", inProcess, StringComparison.Ordinal);
        Assert.Contains("tooba.outbox.published", inProcess, StringComparison.Ordinal);
        Assert.Contains("ToobaTelemetry", inProcess, StringComparison.Ordinal);
        Assert.Contains("MessagingCorrelation", inProcess, StringComparison.Ordinal);

        foreach (var name in ExpectedFiles.Where(f => f != "InProcessIntegrationEventPublisher.cs"
                                                      && f != "MessagingRegistration.cs"))
        {
            var text = Read($"src/backend/Host/Tooba.Host/Messaging/{name}");
            Assert.DoesNotContain("IServiceProvider", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MakeGenericType", text, StringComparison.Ordinal);
        }

        var mt = Read("src/backend/Host/Tooba.Host/Messaging/MassTransitIntegrationEventPublisher.cs");
        Assert.Contains("IBus", mt, StringComparison.Ordinal);
        Assert.Contains("IIntegrationEventSerializer", mt, StringComparison.Ordinal);
        Assert.Contains("ToobaIntegrationTransportMessage", mt, StringComparison.Ordinal);
        Assert.Contains("EventType = meta.EventType", mt, StringComparison.Ordinal);
        Assert.Contains("PayloadJson = _serializer.SerializePayload", mt, StringComparison.Ordinal);
        Assert.Contains("tooba.event-type", mt, StringComparison.Ordinal);
        Assert.Contains("tooba.tenant-id", mt, StringComparison.Ordinal);
        Assert.Contains("tooba.edition", mt, StringComparison.Ordinal);
        Assert.Contains("tooba.deployment-id", mt, StringComparison.Ordinal);
        Assert.Contains("tooba.event-id", mt, StringComparison.Ordinal);
        Assert.Contains("traceparent", mt, StringComparison.Ordinal);
        Assert.Contains("tracestate", mt, StringComparison.Ordinal);
        Assert.Contains("tooba.messaging.published", mt, StringComparison.Ordinal);
        Assert.DoesNotContain("IIntegrationEventHandler", mt, StringComparison.Ordinal);

        var disabled = Read("src/backend/Host/Tooba.Host/Messaging/MessagingDisabledPublisher.cs");
        Assert.Contains("throw new InvalidOperationException", disabled, StringComparison.Ordinal);

        var options = Read("src/backend/Host/Tooba.Host/Messaging/MessagingHostOptions.cs");
        Assert.Contains("CanonicalTransport = \"PostgreSql\"", options, StringComparison.Ordinal);
        Assert.Contains("public bool Enabled", options, StringComparison.Ordinal);
        Assert.Contains("public string Transport", options, StringComparison.Ordinal);
        Assert.Contains("public string ConnectionReference", options, StringComparison.Ordinal);
        Assert.Contains("public string Schema", options, StringComparison.Ordinal);
        Assert.Contains("public bool UseInProcessTestDouble", options, StringComparison.Ordinal);

        var validator = Read("src/backend/Host/Tooba.Host/Messaging/MessagingOptionsValidator.cs");
        Assert.Contains("ConnectionReference is required when messaging is enabled", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("Production requires Tooba:Messaging:ConnectionReference", validator, StringComparison.Ordinal);

        var retry = Read("src/backend/Host/Tooba.Host/Messaging/MessagingRetryConfigurator.cs");
        Assert.Contains("Immediate(2)", retry, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(5)", retry, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(15)", retry, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(30)", retry, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Messaging;", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<MessagingHostOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("AddToobaIntegrationPublisher(", program, StringComparison.Ordinal);
        Assert.Contains("IValidateOptions<MessagingHostOptions>", program, StringComparison.Ordinal);

        var healthEndpoints = Read("src/backend/Host/Tooba.Host/Health/HostHealthEndpoints.cs");
        var healthEval = Read("src/backend/Host/Tooba.Host/Health/HostReadinessEvaluator.cs");
        Assert.Contains("using Tooba.Host.Messaging;", healthEndpoints, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Host.Messaging;", healthEval, StringComparison.Ordinal);
        Assert.DoesNotContain("InProcessIntegrationEventPublisher", healthEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MassTransitIntegrationEventPublisher", healthEval, StringComparison.Ordinal);

        var outbox = Read("src/backend/Host/Tooba.Host/Outbox/OutboxDispatcher.cs");
        Assert.Contains("IIntegrationEventPublisher", outbox, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_certifies_messaging_and_preserves_prior_host_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostMessagingAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"implementationCommit\": \"f29a881370b9a8813035715ef6973145ce3f1723\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_MESSAGING_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("TESTING_ONLY_EXCEPTION_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("FRAMEWORK_COMPOSITION_CALLBACK_ALLOWED", sot, StringComparison.Ordinal);
    }

    private static string Dir(string relative) => Path.Combine(Repo(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Repo(string? relative = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return relative is null
                    ? directory.FullName
                    : Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
