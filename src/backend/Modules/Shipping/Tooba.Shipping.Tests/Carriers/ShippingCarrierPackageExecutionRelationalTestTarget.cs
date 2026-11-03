using Npgsql;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

/// <summary>
/// TB-249-R4 disposable PostgreSQL target: parsed fail-closed identity before any connection.
/// </summary>
internal sealed record ShippingCarrierPackageExecutionRelationalTestTarget(string ConnectionString)
{
    public const string EnvironmentVariable = "TOOBA_TB249R4_POSTGRES";
    public const string ExpectedHost = "127.0.0.1";
    public const int ExpectedPort = 15433;
    public const string ExpectedDatabase = "shipping_tb249r4";
    public const string ExpectedUser = "tb249r4";

    public static ShippingCarrierPackageExecutionRelationalTestTarget FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariable} must point at the disposable {ExpectedHost}:{ExpectedPort}/{ExpectedDatabase} "
                + "instance; the TB249R4 relational matrix refuses to run without it.");
        }

        return Parse(raw);
    }

    public static ShippingCarrierPackageExecutionRelationalTestTarget Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("a connection string is required for the TB249R4 relational proof");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            throw new InvalidOperationException("the TB249R4 connection string is not a valid Npgsql string");
        }

        var host = builder.Host ?? string.Empty;
        if (host.Contains(',', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("a multi-host TB249R4 target is not the disposable instance");
        }

        if (!string.Equals(host, ExpectedHost, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the TB249R4 target host must be exactly {ExpectedHost}, not '{host}'");
        }

        if (builder.Port != ExpectedPort)
        {
            throw new InvalidOperationException(
                $"the TB249R4 target port must be exactly {ExpectedPort} (never shared 5432), not {builder.Port}");
        }

        if (!string.Equals(builder.Database, ExpectedDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the TB249R4 target database must be exactly {ExpectedDatabase}, not '{builder.Database}'");
        }

        if (!string.Equals(builder.Username, ExpectedUser, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"the TB249R4 target user must be exactly {ExpectedUser}, not '{builder.Username}'");
        }

        return new ShippingCarrierPackageExecutionRelationalTestTarget(builder.ConnectionString);
    }
}

public sealed class ShippingCarrierPackageExecutionRelationalTargetIsolationTests
{
    private const string Valid =
        "Host=127.0.0.1;Port=15433;Database=shipping_tb249r4;Username=tb249r4;Password=local";

    [Fact]
    [Trait("Category", "TB249R4")]
    public void The_exact_task_owned_target_is_accepted()
    {
        var target = ShippingCarrierPackageExecutionRelationalTestTarget.Parse(Valid);
        Assert.Contains("shipping_tb249r4", target.ConnectionString, StringComparison.Ordinal);
        Assert.Contains("15433", target.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    public void An_absent_configuration_fails_closed()
    {
        Assert.Throws<InvalidOperationException>(() => ShippingCarrierPackageExecutionRelationalTestTarget.Parse(null));
        Assert.Throws<InvalidOperationException>(() => ShippingCarrierPackageExecutionRelationalTestTarget.Parse("   "));
    }

    [Theory]
    [InlineData("Host=localhost;Port=15433;Database=shipping_tb249r4;Username=tb249r4")]
    [InlineData("Host=127.0.0.1,10.1.2.3;Port=15433;Database=shipping_tb249r4;Username=tb249r4")]
    [InlineData("Host=127.0.0.1;Port=5432;Database=shipping_tb249r4;Username=tb249r4")]
    [InlineData("Host=127.0.0.1;Port=15433;Database=tooba_alpha;Username=tb249r4")]
    [InlineData("Host=127.0.0.1;Port=15433;Database=shipping_tb249r4;Username=postgres")]
    [Trait("Category", "TB249R4")]
    public void Foreign_or_shared_targets_are_rejected(string candidate)
        => Assert.Throws<InvalidOperationException>(() => ShippingCarrierPackageExecutionRelationalTestTarget.Parse(candidate));
}
