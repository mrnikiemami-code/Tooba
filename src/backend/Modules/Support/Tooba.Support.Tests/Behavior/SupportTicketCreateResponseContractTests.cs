using System.Reflection;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Ports;
using Tooba.Support.Application.Tickets.Commands;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Endpoints.Customer;
using Tooba.Support.Endpoints.Errors;
using Tooba.Support.Endpoints.Resources;
using Tooba.Support.Endpoints.Seller;
using Tooba.Support.Infrastructure.Adapters;
using Tooba.Support.Infrastructure.Directories;
using Tooba.Support.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Support.Tests.Behavior;

/// <summary>
/// TB-TMAR-SUPPORT-AMSC-001-W3-R2 — executable response-contract proof for the two Support ticket
/// creation routes. Invokes the real endpoint handler through the real MediatR pipeline, the real
/// <see cref="ApiResponseFactory"/> and the real composed error catalog, then asserts the observable
/// wire contract: 201 + raw <c>TicketSnapshotDto</c> JSON + <c>application/json</c> + an additive
/// per-audience <c>Location</c> header carrying the returned TicketId, and unchanged
/// ProblemDetails-without-Location behaviour on failure.
/// </summary>
public sealed class SupportTicketCreateResponseContractTests
{
    private static readonly Guid Customer = Guid.Parse("55555555-5555-4555-8555-555555555555");
    private static readonly Guid SellerActor = Guid.Parse("77777777-7777-4777-8777-777777777777");
    private static readonly Guid SellerParty = Guid.Parse("88888888-8888-4888-8888-888888888888");

    [Fact]
    public async Task Customer_create_returns_201_raw_snapshot_with_customer_location()
    {
        await using var provider = BuildProvider();
        var (http, result) = await InvokeCreateAsync(
            provider,
            typeof(SupportCustomerEndpoints),
            new CustomerAuthorizer(Customer));

        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status201Created, http.Response.StatusCode);
        Assert.Equal(
            $"/v1/customer/support/tickets/{await ReadTicketIdAsync(http)}",
            http.Response.Headers.Location.ToString());
        Assert.Equal("application/json", http.Response.ContentType?.Split(';')[0]);
        await AssertRawSnapshotWithoutEnvelopeAsync(http);
    }

    [Fact]
    public async Task Seller_create_returns_201_raw_snapshot_with_seller_location()
    {
        await using var provider = BuildProvider();
        var (http, result) = await InvokeCreateAsync(
            provider,
            typeof(SupportSellerEndpoints),
            new SellerAuthorizer(SellerActor, SellerParty));

        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status201Created, http.Response.StatusCode);
        Assert.Equal(
            $"/v1/seller/support/tickets/{await ReadTicketIdAsync(http)}",
            http.Response.Headers.Location.ToString());
        Assert.Equal("application/json", http.Response.ContentType?.Split(';')[0]);
        await AssertRawSnapshotWithoutEnvelopeAsync(http);
    }

    [Fact]
    public async Task Customer_create_failure_stays_problem_details_without_location()
    {
        await using var provider = BuildProvider();
        var (http, result) = await InvokeCreateAsync(
            provider,
            typeof(SupportCustomerEndpoints),
            new CustomerAuthorizer(Customer),
            category: "NotACategory");

        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status400BadRequest, http.Response.StatusCode);
        Assert.True(string.IsNullOrEmpty(http.Response.Headers.Location.ToString()));
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("support.rejected", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Seller_create_failure_stays_problem_details_without_location()
    {
        await using var provider = BuildProvider();
        var (http, result) = await InvokeCreateAsync(
            provider,
            typeof(SupportSellerEndpoints),
            new SellerAuthorizer(SellerActor, SellerParty),
            category: "NotACategory");

        await result.ExecuteAsync(http);

        Assert.Equal(StatusCodes.Status400BadRequest, http.Response.StatusCode);
        Assert.True(string.IsNullOrEmpty(http.Response.Headers.Location.ToString()));
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("support.rejected", doc.RootElement.GetProperty("errorCode").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // Harness
    // ---------------------------------------------------------------------------------------------

    private static async Task<(DefaultHttpContext Http, IResult Result)> InvokeCreateAsync(
        IServiceProvider provider,
        Type endpoint,
        object authorizer,
        string category = "Order")
    {
        var http = new DefaultHttpContext { RequestServices = provider };
        http.Response.Body = new MemoryStream();
        http.Request.Headers["Idempotency-Key"] = "w3-r2-" + Guid.NewGuid().ToString("N");
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;

        var method = endpoint.GetMethod("CreateAsync", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{endpoint.Name}.CreateAsync not found");
        var body = new CreateTicketBody("subject", category, "Normal", "hello", null, null);

        var task = (Task<IResult>)method.Invoke(null, [
            body,
            provider.GetRequiredService<ISender>(),
            authorizer,
            provider.GetRequiredService<ApiResponseFactory>(),
            http,
            CancellationToken.None,
        ])!;

        return (http, await task);
    }

    private static async Task<Guid> ReadTicketIdAsync(DefaultHttpContext http)
    {
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        return doc.RootElement.GetProperty("ticketId").GetGuid();
    }

    private static async Task AssertRawSnapshotWithoutEnvelopeAsync(DefaultHttpContext http)
    {
        http.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.False(doc.RootElement.TryGetProperty("data", out _));
        Assert.Equal("Open", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Order", doc.RootElement.GetProperty("category").GetString());
        Assert.True(doc.RootElement.GetProperty("messages").ValueKind == JsonValueKind.Array);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddHttpContextAccessor();
        services.AddToobaObservabilityFoundation();
        services.AddSingleton<IErrorCatalogContributor, SupportErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, SupportErrorResourceSet>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCustomerTicketCommand).Assembly));
        services.AddSingleton<IClock, SystemUtcClock>();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        services.AddSingleton<INotificationCreationPort, NoopNotificationPort>();
        services.AddSingleton<ISupportDemoPreviewPort, SupportDemoPreviewAdapter>();
        services.AddDbContext<SupportDbContext>(options =>
            options.UseInMemoryDatabase("support-w3-r2-" + Guid.NewGuid().ToString("N")));
        services.AddScoped<ISupportDirectory, SupportDirectory>();
        return services.BuildServiceProvider();
    }

    private sealed class CustomerAuthorizer(Guid actor) : ISupportCustomerAuthorizer
    {
        public Guid? TryResolveActor(HttpContext httpContext) => actor;
    }

    private sealed class SellerAuthorizer(Guid actor, Guid sellerParty) : ISupportSellerAuthorizer
    {
        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpContext httpContext, string permissionId, CancellationToken cancellationToken) =>
            Task.FromResult((actor, sellerParty));
    }

    private sealed class NoopNotificationPort : INotificationCreationPort
    {
        public Task<bool> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
