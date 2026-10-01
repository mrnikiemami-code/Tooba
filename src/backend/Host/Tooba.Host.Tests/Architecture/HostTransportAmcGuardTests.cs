using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-TRANSPORT-AMC-001 — KEEP_AS_GENERIC_HOST_TRANSPORT_INFRASTRUCTURE.
/// </summary>
public sealed class HostTransportAmcGuardTests
{
    private static readonly string[] AllowedFiles =
    [
        "SqlTransportOptionsMapper.cs",
        "ToobaIntegrationTransportConsumer.cs",
        "ToobaIntegrationTransportMessage.cs",
    ];

    [Fact]
    public void Host_transport_folder_retains_exact_three_platform_files()
    {
        var root = FindRepoRoot();
        var transport = Path.Combine(root, "src/backend/Host/Tooba.Host/Transport");
        Assert.True(Directory.Exists(transport));

        var files = Directory.EnumerateFiles(transport, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(AllowedFiles.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
    }

    [Fact]
    public void Path_namespace_exact_and_transport_safety_invariants()
    {
        var root = FindRepoRoot();
        var transport = Path.Combine(root, "src/backend/Host/Tooba.Host/Transport");
        var foreign = new Regex(
            @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Payment|Reviews|Cart|Wallet|Support|ProductQnA|Preferences|Story|Wishlist|Fulfillment|Settlement|Notification|Returns|Promotion|Inventory|Pricing|Tax|Media|Content|User)\.(Application|Domain|Infrastructure|Persistence)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var moduleDbContext = new Regex(
            @"\b\w+(DbContext)\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        foreach (var path in Directory.EnumerateFiles(transport, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
            Assert.True(nsMatch.Success, path);
            Assert.Equal("Tooba.Host.Transport", nsMatch.Groups[1].Value);

            if (Path.GetFileName(path) == "SqlTransportOptionsMapper.cs")
            {
                Assert.DoesNotContain("ILogger", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Console.Write", text, StringComparison.Ordinal);
                Assert.DoesNotContain("_logger", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("LogInformation", text, StringComparison.Ordinal);
                Assert.DoesNotContain("LogDebug", text, StringComparison.Ordinal);
                Assert.DoesNotContain("LogWarning", text, StringComparison.Ordinal);
                Assert.DoesNotContain("LogError", text, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("Contains(\"error\"", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("message.Contains", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                Assert.False(foreign.IsMatch(line), $"{path}: {line}");
                if (line.StartsWith("using ", StringComparison.Ordinal) || line.Contains("DbContext", StringComparison.Ordinal))
                {
                    if (moduleDbContext.IsMatch(line) && !line.Contains("Outbox", StringComparison.Ordinal))
                    {
                        Assert.False(
                            Regex.IsMatch(line, @"Tooba\.\w+\.(Application|Domain|Infrastructure|Persistence)"),
                            $"{path}: {line}");
                    }
                }
            }
        }

        var consumer = File.ReadAllText(Path.Combine(transport, "ToobaIntegrationTransportConsumer.cs"));
        Assert.Contains("IIntegrationEventHandler<>", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("switch (", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("PayloadJson", ExtractLogCalls(consumer), StringComparison.Ordinal);

        var envelope = File.ReadAllText(Path.Combine(transport, "ToobaIntegrationTransportMessage.cs"));
        Assert.Contains("EventType", envelope, StringComparison.Ordinal);
        Assert.Contains("PayloadJson", envelope, StringComparison.Ordinal);
        Assert.DoesNotContain("$type", envelope, StringComparison.Ordinal);
        Assert.DoesNotContain("AssemblyQualifiedName", envelope, StringComparison.Ordinal);
    }

    [Fact]
    public void Messaging_composition_uses_transport_namespace_and_sot_keep_disposition()
    {
        var root = FindRepoRoot();
        var registration = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Messaging/MessagingRegistration.cs"));
        Assert.Contains("using Tooba.Host.Transport;", registration, StringComparison.Ordinal);
        Assert.Contains("ToobaIntegrationTransportConsumer", registration, StringComparison.Ordinal);
        Assert.Contains("SqlTransportOptionsMapper", registration, StringComparison.Ordinal);

        var publisher = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Messaging/MassTransitIntegrationEventPublisher.cs"));
        Assert.Contains("using Tooba.Host.Transport;", publisher, StringComparison.Ordinal);
        Assert.Contains("ToobaIntegrationTransportMessage", publisher, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostTransportAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_AS_GENERIC_HOST_TRANSPORT_INFRASTRUCTURE", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-TRANSPORT-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_TRANSPORT_AMC_001_KEEP_GENERIC_HOST_INFRASTRUCTURE", sot, StringComparison.Ordinal);
        Assert.Contains("TRANSPORT_KEEP_GENERIC_HOST_INFRASTRUCTURE_USER_REVIEW_REQUIRED", sot, StringComparison.Ordinal);
    }

    private static string ExtractLogCalls(string source)
    {
        var matches = Regex.Matches(
            source,
            @"_logger\.Log\w+\s*\((?:[^;]|\n)*?\)\s*;",
            RegexOptions.Multiline);
        return string.Join("\n", matches.Select(m => m.Value));
    }

    private static string FindRepoRoot()
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
