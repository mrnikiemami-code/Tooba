using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tooba.Persistence;

/// <summary>
/// Canonical Development/production schema migration order, one stable value per module.
/// This is the single authoritative ordering source so no second contradictory model exists.
/// </summary>
public static class ModuleSchemaMigrationOrder
{
    /// <summary>Catalog migration order.</summary>
    public const int Catalog = 1;
    /// <summary>Offer migration order.</summary>
    public const int Offer = 2;
    /// <summary>Pricing migration order.</summary>
    public const int Pricing = 3;
    /// <summary>Inventory migration order.</summary>
    public const int Inventory = 4;
    /// <summary>Tax migration order.</summary>
    public const int Tax = 5;
    /// <summary>Party migration order.</summary>
    public const int Party = 6;
    /// <summary>Identity migration order.</summary>
    public const int Identity = 7;
    /// <summary>Cart migration order.</summary>
    public const int Cart = 8;
    /// <summary>Order migration order.</summary>
    public const int Order = 9;
    /// <summary>Payment migration order.</summary>
    public const int Payment = 10;
    /// <summary>Fulfillment migration order.</summary>
    public const int Fulfillment = 11;
    /// <summary>Promotion migration order.</summary>
    public const int Promotion = 12;
    /// <summary>PlatformProbe migration order.</summary>
    public const int PlatformProbe = 13;
    /// <summary>Reviews migration order.</summary>
    public const int Reviews = 14;
    /// <summary>ProductQnA migration order.</summary>
    public const int ProductQnA = 15;
    /// <summary>BulkInquiry migration order.</summary>
    public const int BulkInquiry = 16;
    /// <summary>Wishlist migration order.</summary>
    public const int Wishlist = 17;
    /// <summary>AddressBook migration order.</summary>
    public const int AddressBook = 18;
    /// <summary>CustomerProfile migration order.</summary>
    public const int CustomerProfile = 19;
    /// <summary>UserPreference migration order.</summary>
    public const int UserPreference = 20;
    /// <summary>OperatorProfile migration order.</summary>
    public const int OperatorProfile = 21;
    /// <summary>Content migration order.</summary>
    public const int Content = 22;
    /// <summary>Media migration order.</summary>
    public const int Media = 23;
    /// <summary>PageComposition migration order.</summary>
    public const int PageComposition = 24;
    /// <summary>Story migration order.</summary>
    public const int Story = 25;
    /// <summary>Notification migration order.</summary>
    public const int Notification = 26;
    /// <summary>AccessControl migration order.</summary>
    public const int AccessControl = 27;
    /// <summary>Support migration order.</summary>
    public const int Support = 28;
}

/// <summary>
/// Neutral module-owned schema migration seam. Each owning module registers its own
/// persistence context here from its own Infrastructure composition root, so Host and
/// tooling orchestrate migrations without ever naming a foreign DbContext type.
/// </summary>
public interface IModuleSchemaMigrator
{
    /// <summary>Stable module identity (no DbContext type name).</summary>
    string Module { get; }

    /// <summary>Explicit deterministic ordering key; must be unique per module.</summary>
    int Order { get; }

    /// <summary>Applies pending schema migrations for this module only.</summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Neutral module-owned step that must run immediately after its module's schema migration
/// (for example module-owned Development seed prerequisites). Keeps ordering deterministic
/// without Host naming any module type.
/// </summary>
public interface IModuleSchemaMigrationStep
{
    /// <summary>Stable module identity (no DbContext type name).</summary>
    string Module { get; }

    /// <summary>Order of the module migration this step must follow.</summary>
    int AfterOrder { get; }

    /// <summary>Runs the module-owned post-migration step.</summary>
    Task RunAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic EF implementation for modules whose migration behavior is exactly
/// <see cref="DbContext.Database"/> <c>MigrateAsync</c>. One shared implementation avoids
/// per-module trivial adapter classes while keeping the seam neutral.
/// </summary>
public sealed class EfModuleSchemaMigrator<TContext> : IModuleSchemaMigrator
    where TContext : DbContext
{
    private readonly TContext _context;

    /// <summary>Creates a migrator for the owning module's context.</summary>
    public EfModuleSchemaMigrator(string module, int order, TContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentNullException.ThrowIfNull(context);
        Module = module;
        Order = order;
        _context = context;
    }

    /// <inheritdoc />
    public string Module { get; }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _context.Database.MigrateAsync(cancellationToken);
}

/// <summary>
/// Neutral bridge for modules that already expose a special migration entrypoint whose
/// behavior is not equivalent to a plain context migrate. The existing module contract is
/// preserved; only the neutral seam is exposed to Host.
/// </summary>
public sealed class DelegateModuleSchemaMigrator : IModuleSchemaMigrator
{
    private readonly Func<CancellationToken, Task> _migrate;

    /// <summary>Creates a migrator backed by a module-owned migration delegate.</summary>
    public DelegateModuleSchemaMigrator(string module, int order, Func<CancellationToken, Task> migrate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentNullException.ThrowIfNull(migrate);
        Module = module;
        Order = order;
        _migrate = migrate;
    }

    /// <inheritdoc />
    public string Module { get; }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public Task MigrateAsync(CancellationToken cancellationToken = default) => _migrate(cancellationToken);
}

/// <summary>
/// Registration helpers for the neutral module schema migration seam.
/// </summary>
public static class ModuleSchemaMigratorRegistration
{
    /// <summary>
    /// Registers a module context with the neutral schema migration seam. The context itself
    /// is resolved from DI so the owning module keeps its own connection/options composition.
    /// </summary>
    public static IServiceCollection AddModuleSchemaMigrator<TContext>(
        this IServiceCollection services,
        string module,
        int order)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IModuleSchemaMigrator>(
            sp => new EfModuleSchemaMigrator<TContext>(module, order, sp.GetRequiredService<TContext>()));
        return services;
    }

    /// <summary>
    /// Registers a module-owned special migration entrypoint with the neutral seam.
    /// </summary>
    public static IServiceCollection AddModuleSchemaMigrator(
        this IServiceCollection services,
        string module,
        int order,
        Func<IServiceProvider, CancellationToken, Task> migrate)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(migrate);
        services.AddScoped<IModuleSchemaMigrator>(
            sp => new DelegateModuleSchemaMigrator(module, order, ct => migrate(sp, ct)));
        return services;
    }

    /// <summary>
    /// Registers a module-owned step that must run immediately after the module migration.
    /// </summary>
    public static IServiceCollection AddModuleSchemaMigrationStep(
        this IServiceCollection services,
        string module,
        int afterOrder,
        Func<IServiceProvider, CancellationToken, Task> run)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(run);
        services.AddScoped<IModuleSchemaMigrationStep>(
            _ => new DelegateModuleSchemaMigrationStep(module, afterOrder, run));
        return services;
    }
}

/// <summary>
/// Neutral bridge for module-owned post-migration steps.
/// </summary>
public sealed class DelegateModuleSchemaMigrationStep : IModuleSchemaMigrationStep
{
    private readonly Func<IServiceProvider, CancellationToken, Task> _run;

    /// <summary>Creates a step backed by a module-owned delegate.</summary>
    public DelegateModuleSchemaMigrationStep(
        string module,
        int afterOrder,
        Func<IServiceProvider, CancellationToken, Task> run)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentNullException.ThrowIfNull(run);
        Module = module;
        AfterOrder = afterOrder;
        _run = run;
    }

    /// <inheritdoc />
    public string Module { get; }

    /// <inheritdoc />
    public int AfterOrder { get; }

    /// <inheritdoc />
    public Task RunAsync(IServiceProvider provider, CancellationToken cancellationToken = default) =>
        _run(provider, cancellationToken);
}
