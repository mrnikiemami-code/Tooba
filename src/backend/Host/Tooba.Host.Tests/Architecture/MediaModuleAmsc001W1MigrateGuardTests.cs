using Tooba.Media.Application.Assets.Validators;
using Tooba.Media.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-MEDIA-AMSC-001-W1 — migrate wave durable guard.
/// Locks the canonical typed-fault seam, the declared-code catalog, the transport-validation codes,
/// the canonical error localization path and the zero-foreign-coupling boundary established by the
/// migrate wave.
/// </summary>
public sealed class MediaModuleAmsc001W1MigrateGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Media.Contracts",
        "Tooba.Media.Domain",
        "Tooba.Media.Application",
        "Tooba.Media.Infrastructure",
        "Tooba.Media.Endpoints",
    ];

    private static readonly string[] AllowedExternalNamespaces =
    [
        "Tooba.Media",
        "Tooba.BuildingBlocks",
        "Tooba.ModuleContracts",
        "Tooba.Persistence",
    ];

    [Fact]
    public void Operation_seam_maps_typed_codes_and_never_parses_message_text()
    {
        var text = Read("src/backend/Modules/Media/Tooba.Media.Application/Composition/MediaOperation.cs");

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("MediaErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        Assert.True(MediaErrorCodes.IsKnown(MediaErrorCodes.UploadFailed));
        Assert.True(MediaErrorCodes.IsKnown(MediaErrorCodes.StorageUnavailable));
        Assert.False(MediaErrorCodes.IsKnown("content.publish.check.body"));
        Assert.False(MediaErrorCodes.IsKnown(null));
        Assert.False(MediaErrorCodes.IsKnown(" "));
    }

    [Fact]
    public void Infrastructure_raises_typed_faults_and_never_the_legacy_platform_seam()
    {
        foreach (var relative in new[]
                 {
                     "src/backend/Modules/Media/Tooba.Media.Infrastructure/Assets/MediaDirectory.cs",
                     "src/backend/Modules/Media/Tooba.Media.Infrastructure/Assets/MediaDirectory.Upload.cs",
                     "src/backend/Modules/Media/Tooba.Media.Infrastructure/Assets/MediaDirectory.Queries.cs",
                     "src/backend/Modules/Media/Tooba.Media.Infrastructure/Storage/LocalFileMediaStore.cs",
                     "src/backend/Modules/Media/Tooba.Media.Infrastructure/Adapters/MediaAssetReadinessBridge.cs",
                 })
        {
            var text = Read(relative);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        }

        var upload = Read("src/backend/Modules/Media/Tooba.Media.Infrastructure/Assets/MediaDirectory.Upload.cs");
        Assert.Contains("ContractOperationException(MediaErrorCodes.TypeUnsupported", upload, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(MediaErrorCodes.TooLarge", upload, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(MediaErrorCodes.StorageUnavailable", upload, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException(MediaErrorCodes.UploadFailed", upload, StringComparison.Ordinal);

        var store = Read("src/backend/Modules/Media/Tooba.Media.Infrastructure/Storage/LocalFileMediaStore.cs");
        Assert.Contains("ContractOperationException(MediaErrorCodes.StorageUnavailable", store, StringComparison.Ordinal);

        var readiness = Read("src/backend/Modules/Media/Tooba.Media.Infrastructure/Adapters/MediaAssetReadinessBridge.cs");
        Assert.Contains("ContractOperationException(MediaAssetContractCodes.AssetMissing)", readiness, StringComparison.Ordinal);
    }

    [Fact]
    public void Media_directory_is_decomposed_into_cohesive_partials()
    {
        var root = Path.Combine(Repo(), "src/backend/Modules/Media/Tooba.Media.Infrastructure/Assets");

        Assert.True(File.Exists(Path.Combine(root, "MediaDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(root, "MediaDirectory.Upload.cs")));
        Assert.True(File.Exists(Path.Combine(root, "MediaDirectory.Queries.cs")));

        var shared = File.ReadAllText(Path.Combine(root, "MediaDirectory.cs"));
        Assert.Contains("partial class MediaDirectory", shared, StringComparison.Ordinal);
        Assert.DoesNotContain("UploadAsync", shared, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryAsync", shared, StringComparison.Ordinal);

        var upload = File.ReadAllText(Path.Combine(root, "MediaDirectory.Upload.cs"));
        Assert.Contains("partial class MediaDirectory", upload, StringComparison.Ordinal);
        Assert.Contains("UploadAsync", upload, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryAsync", upload, StringComparison.Ordinal);

        var queries = File.ReadAllText(Path.Combine(root, "MediaDirectory.Queries.cs"));
        Assert.Contains("partial class MediaDirectory", queries, StringComparison.Ordinal);
        Assert.Contains("QueryAsync", queries, StringComparison.Ordinal);
        Assert.DoesNotContain("UploadAsync", queries, StringComparison.Ordinal);
    }

    [Fact]
    public void Validators_emit_transport_codes_and_never_business_codes()
    {
        foreach (var file in new[]
                 {
                     "UploadMediaAssetCommandValidator.cs",
                     "QueryMediaAssetsQueryValidator.cs",
                     "GetMediaAssetQueryValidator.cs",
                 })
        {
            var text = Read($"src/backend/Modules/Media/Tooba.Media.Application/Assets/Validators/{file}");
            Assert.Contains("MediaValidationCodes", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MediaErrorCodes", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validation_codes_are_not_registered_as_error_catalog_descriptors()
    {
        var contributor = Read("src/backend/Modules/Media/Tooba.Media.Contracts/Errors/MediaErrorCatalogContributor.cs");
        Assert.DoesNotContain("MediaValidationCodes", contributor, StringComparison.Ordinal);

        var codes = Read("src/backend/Modules/Media/Tooba.Media.Application/Assets/Validators/MediaValidationCodes.cs");
        Assert.Contains("media.validation.", codes, StringComparison.Ordinal);
        Assert.DoesNotContain("ValidationFailed", codes, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_code_has_zero_raw_media_error_code_literals()
    {
        var declared = typeof(MediaErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(6, declared.Length);

        foreach (var project in ProductionProjects)
        {
            if (project == "Tooba.Media.Contracts")
            {
                continue;
            }

            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                foreach (var code in declared)
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Media_error_codes_match_their_published_contract_values()
    {
        Assert.Equal("media.upload.failed", MediaErrorCodes.UploadFailed);
        Assert.Equal("media.type.unsupported", MediaErrorCodes.TypeUnsupported);
        Assert.Equal("media.too_large", MediaErrorCodes.TooLarge);
        Assert.Equal("media.storage.unavailable", MediaErrorCodes.StorageUnavailable);
        Assert.Equal("media.missing", MediaErrorCodes.Missing);
        Assert.Equal("media.validation.failed", MediaErrorCodes.ValidationFailed);

        Assert.Equal("media.validation.upload_content_required", MediaValidationCodes.UploadContentRequired);
        Assert.Equal("media.validation.upload_original_file_name_required", MediaValidationCodes.UploadOriginalFileNameRequired);
        Assert.Equal("media.validation.upload_content_type_required", MediaValidationCodes.UploadContentTypeRequired);
        Assert.Equal("media.validation.page_out_of_range", MediaValidationCodes.PageOutOfRange);
        Assert.Equal("media.validation.page_size_out_of_range", MediaValidationCodes.PageSizeOutOfRange);
        Assert.Equal("media.validation.media_asset_id_required", MediaValidationCodes.MediaAssetIdRequired);
    }

    [Fact]
    public void Contracts_boundary_is_typed_without_mixed_contracts_bundle()
    {
        var assets = Path.Combine(Repo(), "src/backend/Modules/Media/Tooba.Media.Contracts/Assets");
        Assert.False(File.Exists(Path.Combine(assets, "MediaAssetContracts.cs")));
        Assert.True(File.Exists(Path.Combine(assets, "IMediaAssetUploadPort.cs")));
        Assert.True(File.Exists(Path.Combine(assets, "IMediaAssetDemoPort.cs")));
    }

    [Fact]
    public void Endpoints_use_the_canonical_localizer_and_dispatch_through_sender_only()
    {
        var admin = Read("src/backend/Modules/Media/Tooba.Media.Endpoints/Admin/MediaAdminEndpoints.cs");
        Assert.Contains("IErrorMessageLocalizer", admin, StringComparison.Ordinal);
        Assert.Contains("IRequestLocaleResolver", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MediaErrorResources", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CultureInfo.GetCultureInfo", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IMediaDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", admin, StringComparison.Ordinal);

        var serving = Read("src/backend/Modules/Media/Tooba.Media.Endpoints/Admin/MediaAssetServing.cs");
        Assert.Contains("ISender", serving, StringComparison.Ordinal);
        Assert.DoesNotContain("IMediaDirectory", serving, StringComparison.Ordinal);
        Assert.DoesNotContain("TryServeStoredMediaAsync", serving, StringComparison.Ordinal);
        Assert.Contains("Results.File", serving, StringComparison.Ordinal);
        Assert.Contains("Results.Text", serving, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_code_has_zero_foreign_module_edges()
    {
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                foreach (var line in File.ReadAllLines(file))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("using Tooba.", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var allowed = AllowedExternalNamespaces.Any(ns =>
                        trimmed.StartsWith($"using {ns}", StringComparison.Ordinal));
                    Assert.True(allowed, $"foreign module edge in {Relative(file)}: {trimmed}");
                }
            }
        }
    }

    [Fact]
    public void Domain_and_application_raise_typed_codes_only()
    {
        foreach (var project in new[] { "Tooba.Media.Domain", "Tooba.Media.Application" })
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
                Assert.DoesNotContain("new Exception(", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Media_schema_and_migrations_are_unchanged_by_the_migrate_wave()
    {
        var root = Path.Combine(Repo(), "src/backend/Modules/Media/Tooba.Media.Infrastructure/Persistence/Migrations");
        var migrations = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name!.EndsWith("_InitialMedia.cs", StringComparison.Ordinal)
                || name.EndsWith("_AddMediaFocalPoint.cs", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(2, migrations.Length);
        Assert.Equal("media", ReadSchema());
    }

    private static string ReadSchema()
    {
        var context = Read("src/backend/Modules/Media/Tooba.Media.Infrastructure/Persistence/MediaDbContext.cs");
        const string marker = "public const string Schema = \"";
        var start = context.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = context.IndexOf('"', start);
        return context[start..end];
    }

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), "src/backend/Modules/Media", project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("DbContextModelSnapshot.cs", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(Repo(), relativePath));

    private static string Relative(string absolute) =>
        absolute.Replace(Repo() + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
