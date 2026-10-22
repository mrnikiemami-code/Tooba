using Npgsql;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// The single disposable PostgreSQL target the TB-190-R1/R2 relational proof may ever touch.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why parsing.</b> The earlier version validated the raw connection string with substring
/// <c>Contains</c> assertions. That does not prove the actual target: a foreign server with the allowed
/// text inside <c>ApplicationName</c> passed. This type parses with <see cref="NpgsqlConnectionStringBuilder"/>
/// and then compares the parsed fields against test-owned constants, so the expected values never come from
/// the string they are validating.
/// </para>
/// <para>
/// <b>Fail closed.</b> A missing or mismatched configuration throws. No fact is silently skipped or reported
/// as passed without a real server, and no connection is attempted by the negative facts.
/// </para>
/// </remarks>
internal sealed record ShippingRelationalTestTarget(string ConnectionString)
{
    /// <summary>Environment variable holding the disposable connection string.</summary>
    public const string EnvironmentVariable = "TOOBA_TB190R1_POSTGRES";

    /// <summary>Exact loopback host of the task-owned container.</summary>
    public const string ExpectedHost = "127.0.0.1";

    /// <summary>Exact published port of the task-owned container (never 5432).</summary>
    public const int ExpectedPort = 55432;

    /// <summary>Exact task-only database name.</summary>
    public const string ExpectedDatabase = "shipping_tb190r1";

    /// <summary>Exact task-only role name.</summary>
    public const string ExpectedUser = "tb190r1";

    /// <summary>Reads and validates the configuration, throwing when it is absent or unsafe.</summary>
    public static ShippingRelationalTestTarget FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariable} must point at the disposable {ExpectedHost}:{ExpectedPort}/{ExpectedDatabase} "
                + "instance; the relational facts refuse to run (or to report success) without it.");
        }

        return Parse(raw);
    }

    /// <summary>
    /// Parses and validates one connection string. Purely offline: never opens a connection.
    /// </summary>
    /// <param name="raw">Candidate connection string.</param>
    public static ShippingRelationalTestTarget Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("a connection string is required for the disposable relational proof");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("the relational connection string is not a valid Npgsql string");
        }

        var host = builder.Host ?? string.Empty;

        // A multi-host host list would let a fallback server be reached; refuse it before it is ever used.
        if (host.Contains(',', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("a multi-host relational target is not the disposable instance");
        }

        if (!string.Equals(host, ExpectedHost, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the relational target host must be exactly {ExpectedHost}, not '{host}'");
        }

        if (builder.Port != ExpectedPort)
        {
            throw new InvalidOperationException(
                $"the relational target port must be exactly {ExpectedPort}, not {builder.Port}");
        }

        if (!string.Equals(builder.Database, ExpectedDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the relational target database must be exactly {ExpectedDatabase}, not '{builder.Database}'");
        }

        if (!string.Equals(builder.Username, ExpectedUser, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the relational target user must be exactly {ExpectedUser}, not '{builder.Username}'");
        }

        // The builder is also the normalized value used later: parsed facts, not the raw text.
        return new ShippingRelationalTestTarget(builder.ConnectionString);
    }
}

/// <summary>
/// Offline negative proof for <see cref="ShippingRelationalTestTarget"/>. Every case must be rejected
/// before any connection is attempted; no server is contacted here.
/// </summary>
public sealed class ShippingRelationalTargetIsolationTests
{
    private const string Valid =
        "Host=127.0.0.1;Port=55432;Database=shipping_tb190r1;Username=tb190r1;Password=local";

    [Fact]
    public void The_exact_task_target_is_accepted()
    {
        var target = ShippingRelationalTestTarget.Parse(Valid);

        Assert.Contains("shipping_tb190r1", target.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("55432", target.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    public void An_absent_configuration_fails_closed()
    {
        Assert.Throws<InvalidOperationException>(() => ShippingRelationalTestTarget.Parse(null));
        Assert.Throws<InvalidOperationException>(() => ShippingRelationalTestTarget.Parse("   "));
    }

    [Theory]
    [InlineData("Host=10.1.2.3;Port=55432;Database=shipping_tb190r1;Username=tb190r1;Application Name=127.0.0.1 shipping_tb190r1")]
    [InlineData("Host=db.internal;Port=55432;Database=shipping_tb190r1;Username=tb190r1;Application Name=shipping_tb190r1 127.0.0.1 55432")]
    [InlineData("Host=127.0.0.1,10.1.2.3;Port=55432;Database=shipping_tb190r1;Username=tb190r1")]
    [InlineData("Host=127.0.0.2;Port=55432;Database=shipping_tb190r1;Username=tb190r1")]
    public void A_foreign_or_multi_host_target_is_rejected_even_when_the_allowed_text_is_present(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => ShippingRelationalTestTarget.Parse(candidate));
    }

    [Fact]
    public void The_shared_port_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => ShippingRelationalTestTarget.Parse(
            "Host=127.0.0.1;Port=5432;Database=shipping_tb190r1;Username=tb190r1"));
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Port=55432;Database=tooba_alpha;Username=tb190r1")]
    [InlineData("Host=127.0.0.1;Port=55432;Database=store-alpha;Username=tb190r1")]
    [InlineData("Host=127.0.0.1;Port=55432;Database=postgres;Username=tb190r1")]
    public void A_shared_or_wrong_database_is_rejected(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => ShippingRelationalTestTarget.Parse(candidate));
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Port=55432;Database=shipping_tb190r1;Username=postgres")]
    [InlineData("Host=127.0.0.1;Port=55432;Database=shipping_tb190r1;Username=tooba_app")]
    public void A_wrong_role_is_rejected(string candidate)
    {
        Assert.Throws<InvalidOperationException>(() => ShippingRelationalTestTarget.Parse(candidate));
    }
}
