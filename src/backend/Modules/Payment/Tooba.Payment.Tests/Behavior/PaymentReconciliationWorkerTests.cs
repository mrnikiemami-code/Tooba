using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Payment.Infrastructure.DependencyInjection;
using Tooba.Payment.Infrastructure.Providers;
using Tooba.Payment.Infrastructure.Workers;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// Focused Payment-owned reconciliation worker/options / registration tests
/// (TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001).
/// </summary>
public sealed class PaymentReconciliationWorkerTests
{
    [Fact]
    public void Disabled_worker_exits_without_polling_targets()
    {
        var targets = new CountingTargetSource();
        using var provider = BuildServices(new PaymentReconciliationOptions { Enabled = false }, targets);
        var worker = (IHostedService)provider.GetRequiredService<PaymentReconciliationWorker>();

        worker.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        worker.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        Assert.Equal(0, targets.Calls);
    }

    [Theory]
    [InlineData(1, 15)]
    [InlineData(5, 15)]
    [InlineData(14, 15)]
    [InlineData(15, 15)]
    [InlineData(60, 60)]
    [InlineData(120, 120)]
    public void Options_clamp_poll_interval_to_minimum_fifteen_seconds(int configured, int expectedSeconds)
    {
        var options = new PaymentReconciliationOptions { PollIntervalSeconds = configured };

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), options.NormalizedPollInterval);
    }

    [Fact]
    public void Options_normalize_unsafe_values_at_one_payment_owned_boundary()
    {
        var options = new PaymentReconciliationOptions
        {
            PollIntervalSeconds = 0,
            PendingAgeMinutes = 0,
            BatchSize = -5,
        };

        Assert.Equal(TimeSpan.FromSeconds(15), options.NormalizedPollInterval);
        Assert.Equal(TimeSpan.FromMinutes(1), options.NormalizedPendingAge);
        Assert.Equal(20, options.NormalizedBatchSize);
    }

    [Fact]
    public void Options_preserve_canonical_defaults_and_section_name()
    {
        var options = new PaymentReconciliationOptions();

        Assert.Equal("Tooba:PaymentReconciliation", PaymentReconciliationOptions.SectionName);
        Assert.True(options.Enabled);
        Assert.Equal(60, options.PollIntervalSeconds);
        Assert.Equal(5, options.PendingAgeMinutes);
        Assert.Equal(20, options.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(60), options.NormalizedPollInterval);
        Assert.Equal(TimeSpan.FromMinutes(5), options.NormalizedPendingAge);
        Assert.Equal(20, options.NormalizedBatchSize);
    }

    [Fact]
    public void PaymentModule_registers_worker_options_and_generic_seams_only()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOutboxPollTargetSource, NullTargetSource>();
        services.AddSingleton<IWorkerCommerceContextFactory, ThrowingCommerceContextFactory>();
        services.AddSingleton<IBackgroundWorkerRegistry, NullWorkerRegistry>();
        var module = new PaymentModule();
        module.AddServices(services, new ConfigurationBuilder().Build(), new EnvironmentStub());

        Assert.Contains(services, d => d.ImplementationType == typeof(PaymentReconciliationWorker) && d.ServiceType == typeof(IHostedService));
        Assert.Contains(services, d => d.ServiceType == typeof(IConfigureOptions<PaymentReconciliationOptions>));
    }

    private static ServiceProvider BuildServices(PaymentReconciliationOptions options, CountingTargetSource targets)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<IOutboxPollTargetSource>(targets);
        services.AddSingleton<IWorkerCommerceContextFactory, ThrowingCommerceContextFactory>();
        services.AddSingleton<IBackgroundWorkerRegistry, NullWorkerRegistry>();
        services.AddSingleton<PaymentGatewayInstrumentation>();
        services.AddSingleton<PaymentReconciliationWorker>();
        return services.BuildServiceProvider();
    }

    private sealed class CountingTargetSource : IOutboxPollTargetSource
    {
        public int Calls { get; private set; }

        public IReadOnlyList<OutboxPollTarget> GetTargets()
        {
            Calls++;
            return [];
        }
    }

    private sealed class NullTargetSource : IOutboxPollTargetSource
    {
        public IReadOnlyList<OutboxPollTarget> GetTargets() => [];
    }

    private sealed class ThrowingCommerceContextFactory : IWorkerCommerceContextFactory
    {
        public CommerceContext FromPollTarget(OutboxPollTarget target, string traceId) =>
            throw new InvalidOperationException("commerce.context.not_expected_in_test");
    }

    private sealed class NullWorkerRegistry : IBackgroundWorkerRegistry
    {
        public void RecordSuccess(string workerName, int processedCount)
        {
        }

        public void RecordFailure(string workerName, string errorType)
        {
        }
    }

    private sealed class EnvironmentStub : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Payment.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
