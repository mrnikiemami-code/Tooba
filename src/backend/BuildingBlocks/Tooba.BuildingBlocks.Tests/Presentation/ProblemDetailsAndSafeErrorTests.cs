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

public sealed class SafeErrorMapperTests
{
    private readonly SafeErrorMapper _mapper = new();

    [Fact]
    public void Maps_semantic_not_found_and_never_leaks_exception_message()
    {
        var ex = new SemanticException(new SemanticError("catalog.item.not_found"));
        var mapped = _mapper.Map(ex);
        Assert.Equal(StatusCodes.Status404NotFound, mapped.StatusCode);
        Assert.Equal("catalog.item.not_found", mapped.ErrorCode);
        Assert.Equal(ErrorSeverity.Warning, mapped.Severity);
        Assert.DoesNotContain("Exception", mapped.SafeTitleFallback, StringComparison.Ordinal);
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
    }

    [Fact]
    public void Maps_platform_http_exception()
    {
        var ex = new PlatformHttpException(503, "Unavailable", "platform.unavailable");
        var mapped = _mapper.Map(ex);
        Assert.Equal(503, mapped.StatusCode);
        Assert.Equal("platform.unavailable", mapped.ErrorCode);
        Assert.Equal(ErrorSeverity.Error, mapped.Severity);
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
    public void Maps_conflict_and_forbidden_by_convention()
    {
        Assert.Equal(
            StatusCodes.Status409Conflict,
            _mapper.Map(new SemanticException(new SemanticError("offer.listing.duplicate_active"))).StatusCode);
        Assert.Equal(
            StatusCodes.Status403Forbidden,
            _mapper.Map(new SemanticException(new SemanticError("policy.override_denied"))).StatusCode);
    }
}
