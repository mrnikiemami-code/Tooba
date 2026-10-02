using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-MESSAGING-AMC-001-W1 — structure/namespace/test-double hygiene guard (not certification).
/// </summary>
public sealed class HostMessagingAmcW1GuardTests
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
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Messaging_exact_seven_files_exact_namespace_one_type_each()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Messaging");
        Assert.True(Directory.Exists(dir));
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
            var types = TopLevelType.Matches(text).Select(m => m.Groups[1].Value).ToArray();
            Assert.True(types.Length == 1, name + " types=" + string.Join(",", types));
            Assert.Equal(Path.GetFileNameWithoutExtension(name), types[0]);
        }
    }

    [Fact]
    public void Messaging_program_and_health_usings_registration_preserved()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Messaging;", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<MessagingHostOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("ValidateOnStart()", program, StringComparison.Ordinal);
        Assert.Contains("IValidateOptions<MessagingHostOptions>", program, StringComparison.Ordinal);
        Assert.Contains("AddToobaIntegrationPublisher(", program, StringComparison.Ordinal);

        var endpoints = Read("src/backend/Host/Tooba.Host/Health/HostHealthEndpoints.cs");
        var evaluator = Read("src/backend/Host/Tooba.Host/Health/HostReadinessEvaluator.cs");
        Assert.Contains("using Tooba.Host.Messaging;", endpoints, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Host.Messaging;", evaluator, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.Health;", endpoints, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.Health;", evaluator, StringComparison.Ordinal);
    }

    [Fact]
    public void Messaging_fail_closed_selection_and_testing_only_double()
    {
        var registration = Read("src/backend/Host/Tooba.Host/Messaging/MessagingRegistration.cs");
        Assert.Contains("UseInProcessTestDouble", registration, StringComparison.Ordinal);
        Assert.Contains("IsEnvironment(\"Testing\")", registration, StringComparison.Ordinal);
        Assert.Contains("InProcessIntegrationEventPublisher", registration, StringComparison.Ordinal);
        Assert.Contains("MassTransitIntegrationEventPublisher", registration, StringComparison.Ordinal);
        Assert.Contains("MessagingDisabledPublisher", registration, StringComparison.Ordinal);
        Assert.Contains("AddToobaMassTransitMessaging", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("RabbitMQ", registration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IntegrationEndpointName = \"tooba-integration\"", registration, StringComparison.Ordinal);
        Assert.Contains("sp.GetRequiredService", registration, StringComparison.Ordinal);
        Assert.Contains("context.GetRequiredService", registration, StringComparison.Ordinal);

        var inProcess = Read("src/backend/Host/Tooba.Host/Messaging/InProcessIntegrationEventPublisher.cs");
        Assert.Contains("IServiceProvider", inProcess, StringComparison.Ordinal);
        Assert.Contains("MakeGenericType", inProcess, StringComparison.Ordinal);
        Assert.Contains("GetServices", inProcess, StringComparison.Ordinal);

        foreach (var name in ExpectedFiles.Where(f => f != "InProcessIntegrationEventPublisher.cs"
                                                      && f != "MessagingRegistration.cs"))
        {
            var text = Read($"src/backend/Host/Tooba.Host/Messaging/{name}");
            Assert.DoesNotContain("IServiceProvider", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MakeGenericType", text, StringComparison.Ordinal);
        }

        var disabled = Read("src/backend/Host/Tooba.Host/Messaging/MessagingDisabledPublisher.cs");
        Assert.Contains("throw new InvalidOperationException", disabled, StringComparison.Ordinal);

        var retry = Read("src/backend/Host/Tooba.Host/Messaging/MessagingRetryConfigurator.cs");
        Assert.Contains("Immediate(2)", retry, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(5)", retry, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(15)", retry, StringComparison.Ordinal);
        Assert.Contains("FromSeconds(30)", retry, StringComparison.Ordinal);
    }

    [Fact]
    public void Messaging_options_validator_split_and_parity()
    {
        var options = Read("src/backend/Host/Tooba.Host/Messaging/MessagingHostOptions.cs");
        var validator = Read("src/backend/Host/Tooba.Host/Messaging/MessagingOptionsValidator.cs");
        Assert.Contains("class MessagingHostOptions", options, StringComparison.Ordinal);
        Assert.DoesNotContain("MessagingOptionsValidator", options, StringComparison.Ordinal);
        Assert.Contains("class MessagingOptionsValidator", validator, StringComparison.Ordinal);
        Assert.Contains("ConnectionReference is required when messaging is enabled", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("Production requires Tooba:Messaging:ConnectionReference", validator, StringComparison.Ordinal);
        Assert.Contains("CanonicalTransport = \"PostgreSql\"", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Messaging_zero_foreign_layers_and_message_text_classification()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Messaging");
        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(dir, "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (ForeignModuleLayer.IsMatch(line))
                    violations.Add(Path.GetFileName(path) + ": " + line);
            }
        }

        Assert.True(violations.Count == 0, string.Join("; ", violations));
    }

    [Fact]
    public void Messaging_protected_certifications_present_in_sot()
    {
        var state = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("\"hostMessagingAmc001W1\"", state, StringComparison.Ordinal);
        Assert.Contains("HostMessagingAmcW1GuardTests", state, StringComparison.Ordinal);
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
