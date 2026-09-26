using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Identity.Contracts.Problems;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// Durable V2 guards for the Host authentication boundary: explicit Identity error catalog with
/// locked HTTP semantics, canonical localization ownership, and no ad-hoc problem pipeline or
/// cross-module Application/Infrastructure leakage from Host/Authentication.
/// </summary>
public sealed class AuthenticationV2CanonicalizationGuardTests
{
    private static readonly IErrorDefinitionCatalog Catalog =
        new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor(), new IdentityErrorCatalogContributor()]);

    [Theory]
    [InlineData(IdentityErrorCodes.ValidationFailed, StatusCodes.Status400BadRequest)]
    [InlineData(IdentityErrorCodes.ChallengeInvalid, StatusCodes.Status400BadRequest)]
    [InlineData(IdentityErrorCodes.AuthenticationFailed, StatusCodes.Status401Unauthorized)]
    [InlineData(IdentityErrorCodes.SessionInvalid, StatusCodes.Status401Unauthorized)]
    [InlineData(IdentityErrorCodes.IdentifierConflict, StatusCodes.Status409Conflict)]
    [InlineData(IdentityErrorCodes.RateLimited, StatusCodes.Status429TooManyRequests)]
    [InlineData(IdentityErrorCodes.TenantUntrusted, StatusCodes.Status400BadRequest)]
    [InlineData(IdentityErrorCodes.OtpDeliveryUnavailable, StatusCodes.Status400BadRequest)]
    [InlineData(IdentityErrorCodes.PasswordChangeFailed, StatusCodes.Status400BadRequest)]
    public void Identity_codes_are_catalogued_with_locked_status(string code, int expectedStatus)
    {
        Assert.True(Catalog.TryGet(code, out var descriptor), $"missing catalog descriptor: {code}");
        Assert.Equal(expectedStatus, descriptor.HttpStatus);
        Assert.Equal(code, descriptor.LocalizationKey);
    }

    [Fact]
    public void Canonical_mapper_preserves_locked_status_for_identity_codes()
    {
        var mapper = new SafeErrorMapper(Catalog);
        Assert.Equal(StatusCodes.Status400BadRequest, mapper.Map(new SemanticError(IdentityErrorCodes.ValidationFailed)).StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, mapper.Map(new SemanticError(IdentityErrorCodes.AuthenticationFailed)).StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, mapper.Map(new SemanticError(IdentityErrorCodes.IdentifierConflict)).StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, mapper.Map(new SemanticError(IdentityErrorCodes.RateLimited)).StatusCode);
    }

    [Theory]
    [InlineData(IdentityErrorCodes.ValidationFailed)]
    [InlineData(IdentityErrorCodes.ChallengeInvalid)]
    [InlineData(IdentityErrorCodes.AuthenticationFailed)]
    [InlineData(IdentityErrorCodes.SessionInvalid)]
    [InlineData(IdentityErrorCodes.IdentifierConflict)]
    [InlineData(IdentityErrorCodes.RateLimited)]
    [InlineData(IdentityErrorCodes.TenantUntrusted)]
    [InlineData(IdentityErrorCodes.OtpDeliveryUnavailable)]
    [InlineData(IdentityErrorCodes.PasswordChangeFailed)]
    public void Identity_resource_set_owns_key_and_returns_canonical_title(string code)
    {
        var set = new IdentityErrorResourceSet();
        Assert.True(set.Owns(code));
        var title = set.GetString(code, System.Globalization.CultureInfo.GetCultureInfo("en"));
        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.DoesNotContain("Exception", title!, StringComparison.Ordinal);
    }

    [Fact]
    public void Authentication_files_do_not_leak_foreign_Application_or_Infrastructure()
    {
        var authRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Authentication");
        foreach (var file in Directory.GetFiles(authRoot, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tooba.Identity.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Identity.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Identity.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.CustomerProfile.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IdentityDuplicateIdentifierException", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Authentication_files_consume_cross_module_capability_only_through_contracts()
    {
        var authRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Authentication");
        foreach (var file in Directory.GetFiles(authRoot, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Identity.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Identity.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.CustomerProfile.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IdentityDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AuthSession", text, StringComparison.Ordinal);
            Assert.DoesNotContain("UserAccount", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Authentication_problem_helper_uses_canonical_factory_not_a_parallel_pipeline()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Authentication", "AuthenticationHttpProblem.cs"));
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new ProblemDetails", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("application/problem+json", source, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
