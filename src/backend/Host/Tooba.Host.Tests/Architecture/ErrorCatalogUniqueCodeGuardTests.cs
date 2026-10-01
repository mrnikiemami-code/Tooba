using System.Reflection;
using System.Runtime.Loader;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// Durable repository-level guard for the canonical error catalog: composes every public
/// <see cref="IErrorCatalogContributor"/> shipped by Tooba production assemblies and proves that
/// no machine error code is registered more than once. This generically catches future duplicate
/// descriptors (it does not hard-code today's set) while leaving
/// <see cref="ErrorDefinitionCatalog"/> fail-fast behavior intact.
/// </summary>
public sealed class ErrorCatalogUniqueCodeGuardTests
{
    [Fact]
    public void Composed_production_contributors_register_each_machine_code_exactly_once()
    {
        var contributors = DiscoverProductionContributors();
        Assert.NotEmpty(contributors);

        var ownersByCode = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var contributor in contributors)
        {
            foreach (var descriptor in contributor.Contribute())
            {
                Assert.False(string.IsNullOrWhiteSpace(descriptor.Code), "descriptor code required");
                if (!ownersByCode.TryGetValue(descriptor.Code, out var owners))
                {
                    owners = [];
                    ownersByCode[descriptor.Code] = owners;
                }

                owners.Add(contributor.GetType().FullName!);
            }
        }

        var duplicates = ownersByCode
            .Where(kv => kv.Value.Count > 1)
            .Select(kv => $"{kv.Key} <= {string.Join(", ", kv.Value)}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            "duplicate machine error codes across canonical contributors:\n" + string.Join("\n", duplicates));

        // The catalog must materialize from the real composed contributor set (fail-fast intact).
        var catalog = new ErrorDefinitionCatalog(contributors);
        Assert.Equal(ownersByCode.Count, catalog.RegisteredCodes.Count);
    }

    [Fact]
    public void Composed_catalog_and_safe_error_mapper_resolve_shared_codes()
    {
        var contributors = DiscoverProductionContributors();
        var catalog = new ErrorDefinitionCatalog(contributors);
        var mapper = new SafeErrorMapper(catalog);

        // Shared cross-cutting codes must resolve through the composed catalog (single owner),
        // and the mapper must return the descriptor's HTTP status — never a 500 fallback.
        var session = mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.CustomerSessionRequired)));
        Assert.Equal(401, session.StatusCode);
        Assert.Equal(FoundationErrorCodes.CustomerSessionRequired, session.ErrorCode);

        var adminDenied = mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
        Assert.Equal(403, adminDenied.StatusCode);

        var checkout = mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.CheckoutAuthenticationRequired)));
        Assert.Equal(401, checkout.StatusCode);
    }

    [Fact]
    public void Foundation_admin_codes_resolve_unique_statuses_through_foundation_catalog()
    {
        var catalog = new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]);
        var mapper = new SafeErrorMapper(catalog);

        Assert.True(catalog.TryGet(FoundationErrorCodes.AdminActorMissing, out var actor));
        Assert.Equal(401, actor.HttpStatus);
        Assert.True(catalog.TryGet(FoundationErrorCodes.AdminTenantMissing, out var tenant));
        Assert.Equal(503, tenant.HttpStatus);
        Assert.True(catalog.TryGet(FoundationErrorCodes.AdminAuthorizationUnavailable, out var unavailable));
        Assert.Equal(503, unavailable.HttpStatus);
        Assert.True(catalog.TryGet(FoundationErrorCodes.AdminAuthorizationDenied, out var denied));
        Assert.Equal(403, denied.HttpStatus);
        Assert.True(catalog.TryGet(FoundationErrorCodes.AdminDevUnavailable, out var dev));
        Assert.Equal(404, dev.HttpStatus);

        Assert.Equal(401, mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.AdminActorMissing))).StatusCode);
        Assert.Equal(503, mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.AdminTenantMissing))).StatusCode);
        Assert.Equal(503, mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable))).StatusCode);
        Assert.Equal(403, mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied))).StatusCode);
        Assert.Equal(404, mapper.Map(new SemanticException(new SemanticError(FoundationErrorCodes.AdminDevUnavailable))).StatusCode);

        var adminCodes = new FoundationErrorCatalogContributor().Contribute()
            .Select(d => d.Code)
            .Where(c => c.StartsWith("admin.", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(adminCodes.Length, adminCodes.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(FoundationErrorCodes.AdminActorMissing, adminCodes);
        Assert.Contains(FoundationErrorCodes.AdminTenantMissing, adminCodes);
        Assert.Contains(FoundationErrorCodes.AdminAuthorizationUnavailable, adminCodes);
        Assert.Contains(FoundationErrorCodes.AdminAuthorizationDenied, adminCodes);
        Assert.Contains(FoundationErrorCodes.AdminDevUnavailable, adminCodes);
    }

    private static IReadOnlyList<IErrorCatalogContributor> DiscoverProductionContributors()
    {
        LoadToobaAssemblies();

        var contributors = new List<IErrorCatalogContributor>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name is null || !name.StartsWith("Tooba.", StringComparison.Ordinal))
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (!typeof(IErrorCatalogContributor).IsAssignableFrom(type)
                    || !type.IsClass
                    || type.IsAbstract
                    || type.IsNested
                    || !type.IsPublic
                    || type.GetConstructor(Type.EmptyTypes) is null)
                {
                    continue;
                }

                contributors.Add((IErrorCatalogContributor)Activator.CreateInstance(type)!);
            }
        }

        return contributors;
    }

    private static void LoadToobaAssemblies()
    {
        var probe = typeof(FoundationErrorCatalogContributor).Assembly;
        var directory = Path.GetDirectoryName(probe.Location);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "Tooba.*.dll"))
        {
            var name = AssemblyName.GetAssemblyName(path).Name;
            if (name is null || AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == name))
            {
                continue;
            }

            try
            {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            catch (BadImageFormatException)
            {
                // Not a managed assembly; ignore.
            }
        }
    }
}
