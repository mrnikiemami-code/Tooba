using Npgsql;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// The task-owned disposable PostgreSQL target for the TB-231 compact shipping-group evidence facts only.
/// </summary>
/// <remarks>
/// <para>
/// This slice gets its OWN exact identity (127.0.0.1:15433 / order_tb231 / tb231) so it can never write into
/// another family's database, the shared 5432 instance or <c>tooba_alpha</c>.
/// </para>
/// <para>
/// Validation is parsed (<see cref="NpgsqlConnectionStringBuilder"/>), before any connection, and the
/// expected identity never comes from the string being validated. A missing or mismatched configuration
/// throws, so a fact can never pass without a real server.
/// </para>
/// </remarks>
internal sealed record OrderEvidenceRelationalTestTarget(string ConnectionString)
{
    /// <summary>Environment variable holding this slice's disposable connection string.</summary>
    public const string EnvironmentVariable = "TOOBA_TB231_ORDER_POSTGRES";

    /// <summary>Exact loopback host of the task-owned container.</summary>
    public const string ExpectedHost = "127.0.0.1";

    /// <summary>Exact published port of the task-owned container (never 5432, never 15432).</summary>
    public const int ExpectedPort = 15433;

    /// <summary>Exact task-only database name.</summary>
    public const string ExpectedDatabase = "order_tb231";

    /// <summary>Exact task-only role name.</summary>
    public const string ExpectedUser = "tb231";

    /// <summary>Reads and validates the configuration, throwing when it is absent or unsafe.</summary>
    public static OrderEvidenceRelationalTestTarget FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariable} must point at the disposable {ExpectedHost}:{ExpectedPort}/{ExpectedDatabase} "
                + "instance; the compact order-evidence facts refuse to run (or to report success) without it.");
        }

        return Parse(raw);
    }

    /// <summary>Parses and validates one connection string. Purely offline: never opens a connection.</summary>
    /// <param name="raw">Candidate connection string.</param>
    public static OrderEvidenceRelationalTestTarget Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("a connection string is required for the order-evidence proof");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("the order-evidence connection string is not a valid Npgsql string");
        }

        var host = builder.Host ?? string.Empty;

        if (host.Contains(',', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("a multi-host order-evidence target is not the disposable instance");
        }

        if (host.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(host, ExpectedHost, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the order-evidence target host must be exactly {ExpectedHost}, not '{host}'");
        }

        if (builder.Port != ExpectedPort)
        {
            throw new InvalidOperationException(
                $"the order-evidence target port must be exactly {ExpectedPort} (never the shared 5432), not {builder.Port}");
        }

        if (!string.Equals(builder.Database, ExpectedDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the order-evidence target database must be exactly {ExpectedDatabase}, not '{builder.Database}'");
        }

        if (!string.Equals(builder.Username, ExpectedUser, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the order-evidence target user must be exactly {ExpectedUser}, not '{builder.Username}'");
        }

        return new OrderEvidenceRelationalTestTarget(builder.ConnectionString);
    }
}

/// <summary>Offline negative proof for <see cref="OrderEvidenceRelationalTestTarget"/>.</summary>
public sealed class OrderEvidenceRelationalTargetIsolationTests
{
    private const string Valid =
        "Host=127.0.0.1;Port=15433;Database=order_tb231;Username=tb231;Password=local";

    [Fact]
    public void The_exact_task_owned_target_is_accepted()
    {
        var target = OrderEvidenceRelationalTestTarget.Parse(Valid);

        Assert.Contains("order_tb231", target.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("15433", target.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    public void An_absent_configuration_fails_closed()
    {
        Assert.Throws<InvalidOperationException>(() => OrderEvidenceRelationalTestTarget.Parse(null));
        Assert.Throws<InvalidOperationException>(() => OrderEvidenceRelationalTestTarget.Parse("   "));
    }

    [Theory]
    [InlineData("Host=localhost;Port=15433;Database=order_tb231;Username=tb231")]
    [InlineData("Host=127.0.0.1,10.1.2.3;Port=15433;Database=order_tb231;Username=tb231")]
    [InlineData("Host=127.0.0.2;Port=15433;Database=order_tb231;Username=tb231")]
    public void A_foreign_multi_host_or_localhost_target_is_rejected(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => OrderEvidenceRelationalTestTarget.Parse(candidate));
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Port=5432;Database=order_tb231;Username=tb231")]
    [InlineData("Host=127.0.0.1;Port=15432;Database=order_tb231;Username=tb231")]
    public void The_shared_or_foreign_port_is_rejected(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => OrderEvidenceRelationalTestTarget.Parse(candidate));
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Port=15433;Database=tooba_alpha;Username=tb231")]
    [InlineData("Host=127.0.0.1;Port=15433;Database=shipping_tb214;Username=tb231")]
    [InlineData("Host=127.0.0.1;Port=15433;Database=postgres;Username=tb231")]
    public void A_shared_or_foreign_database_is_rejected(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => OrderEvidenceRelationalTestTarget.Parse(candidate));
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Port=15433;Database=order_tb231;Username=postgres")]
    [InlineData("Host=127.0.0.1;Port=15433;Database=order_tb231;Username=tb214")]
    public void A_foreign_role_is_rejected(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => OrderEvidenceRelationalTestTarget.Parse(candidate));
    }
}
