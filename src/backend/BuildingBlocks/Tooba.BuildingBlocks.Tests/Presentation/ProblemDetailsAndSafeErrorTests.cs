using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Presentation.ProblemDetails;

namespace Tooba.BuildingBlocks.Tests.Presentation;

public sealed class ProblemDetailsContextProviderTests
{
    [Fact]
    public void Context_includes_trace_span_correlation_without_throwing_when_ambient_missing()
    {
        var correlation = new CorrelationIdProvider();
        var expected = correlation.EnsureCorrelationId(Guid.NewGuid().ToString("N"));
        var provider = new ProblemDetailsContextProvider(
            new HttpContextAccessor(),
            new StubHostEnvironment(Environments.Production),
            correlation);

        var ctx = provider.GetCurrentContext();
        Assert.Equal(expected, ctx.CorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(ctx.TraceId));
        Assert.True(ctx.HideExceptionDetails);
        Assert.Null(ctx.TenantId);
        Assert.Null(ctx.ActorId);
    }

    [Fact]
    public void Development_does_not_hide_exception_details_flag()
    {
        var provider = new ProblemDetailsContextProvider(
            new HttpContextAccessor(),
            new StubHostEnvironment(Environments.Development),
            new CorrelationIdProvider());
        Assert.False(provider.GetCurrentContext().HideExceptionDetails);
    }

    private sealed class StubHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class ErrorDescriptorCatalogTests
{
    [Fact]
    public void Duplicate_code_registration_fails_fast()
    {
        var contributors = new IErrorCatalogContributor[]
        {
            new FoundationErrorCatalogContributor(),
            new DuplicateFoundationContributor(),
        };
        var ex = Assert.Throws<InvalidOperationException>(() => new ErrorDefinitionCatalog(contributors));
        Assert.Contains("duplicate_error_descriptor", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Foundation_codes_are_explicit()
    {
        var catalog = new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]);
        Assert.True(catalog.TryGet("validation.failed", out var validation));
        Assert.Equal(StatusCodes.Status400BadRequest, validation.HttpStatus);
        Assert.True(catalog.TryGet("platform.unexpected", out var unexpected));
        Assert.Equal(StatusCodes.Status500InternalServerError, unexpected.HttpStatus);
        Assert.True(catalog.TryGet("platform.error", out var platform));
        Assert.Equal(ErrorClassification.Platform, platform.Classification);
    }

    private sealed class DuplicateFoundationContributor : IErrorCatalogContributor
    {
        public IReadOnlyList<ErrorDescriptor> Contribute() =>
        [
            new("validation.failed", ErrorClassification.Validation, 400, "validation.failed", ErrorSeverity.Warning, "x"),
        ];
    }
}

public sealed class SafeErrorMapperTests
{
    private readonly SafeErrorMapper _mapper = new(new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]));

    [Fact]
    public void Unknown_semantic_code_uses_safe_business_fallback_not_name_heuristic()
    {
        var ex = new SemanticException(new SemanticError("catalog.item.not_found"));
        var mapped = _mapper.Map(ex);
        Assert.Equal(StatusCodes.Status400BadRequest, mapped.StatusCode);
        Assert.Equal(ErrorClassification.Business, mapped.Classification);
        Assert.Equal("catalog.item.not_found", mapped.ErrorCode);
        Assert.Equal(ErrorSeverity.Warning, mapped.Severity);
        Assert.DoesNotContain("Exception", mapped.SafeTitleFallback, StringComparison.Ordinal);
    }

    [Fact]
    public void Explicit_descriptor_drives_status_not_substring()
    {
        var catalog = new ErrorDefinitionCatalog(
        [
            new FoundationErrorCatalogContributor(),
            new SingleCodeContributor(new ErrorDescriptor(
                "demo.widget.gone",
                ErrorClassification.NotFound,
                StatusCodes.Status404NotFound,
                "demo.widget.gone",
                ErrorSeverity.Warning,
                "Widget gone.")),
        ]);
        var mapper = new SafeErrorMapper(catalog);
        var mapped = mapper.Map(new SemanticException(new SemanticError("demo.widget.gone")));
        Assert.Equal(StatusCodes.Status404NotFound, mapped.StatusCode);
        Assert.Equal(ErrorClassification.NotFound, mapped.Classification);
    }

    [Fact]
    public void Naming_heuristic_does_not_classify_duplicate_or_denied_without_catalog()
    {
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            _mapper.Map(new SemanticException(new SemanticError("offer.listing.duplicate_active"))).StatusCode);
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            _mapper.Map(new SemanticException(new SemanticError("policy.override_denied"))).StatusCode);
    }

    [Fact]
    public void Maps_validation_exception()
    {
        var ex = new ValidationException([
            new ValidationFailure("Name", "required") { ErrorCode = "name.required" }
        ]);
        var mapped = _mapper.Map(ex);
        Assert.Equal(StatusCodes.Status400BadRequest, mapped.StatusCode);
        Assert.Equal(ErrorClassification.Validation, mapped.Classification);
        Assert.NotNull(mapped.ValidationErrors);
        Assert.Contains("Name", mapped.ValidationErrors!.Keys);
        Assert.Contains("name.required", mapped.ValidationErrors["Name"]);
    }

    [Fact]
    public void Maps_platform_http_exception_as_legacy_input()
    {
        var ex = new PlatformHttpException(503, "Unavailable", "platform.unavailable");
        var mapped = _mapper.Map(ex);
        Assert.Equal(503, mapped.StatusCode);
        Assert.Equal("platform.unavailable", mapped.ErrorCode);
        Assert.Equal(ErrorSeverity.Error, mapped.Severity);
        Assert.Equal(ErrorClassification.Platform, mapped.Classification);
    }

    [Fact]
    public void Maps_unknown_to_generic_500_without_message_leak()
    {
        var ex = new InvalidOperationException("secret connection string leaked");
        var mapped = _mapper.Map(ex);
        Assert.Equal(500, mapped.StatusCode);
        Assert.Equal("platform.unexpected", mapped.ErrorCode);
        Assert.Equal(ErrorSeverity.Error, mapped.Severity);
        Assert.DoesNotContain("connection", mapped.SafeTitleFallback, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ex.Message, mapped.SafeTitleFallback, StringComparison.Ordinal);
    }

    [Fact]
    public void ClassifySemanticCode_method_is_absent()
    {
        Assert.Null(typeof(SafeErrorMapper).GetMethod(
            "ClassifySemanticCode",
            System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic));
    }

    private sealed class SingleCodeContributor(ErrorDescriptor descriptor) : IErrorCatalogContributor
    {
        public IReadOnlyList<ErrorDescriptor> Contribute() => [descriptor];
    }
}
