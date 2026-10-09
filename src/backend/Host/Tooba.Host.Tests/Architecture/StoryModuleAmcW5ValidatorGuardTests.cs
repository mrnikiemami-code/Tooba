using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Story.Application.Stories.Commands.Admin;
using Tooba.Story.Application.Stories.Commands.Seller;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Application.Stories.Queries.Admin;
using Tooba.Story.Application.Stories.Queries.Seller;
using Tooba.Story.Application.Stories.Queries.Storefront;
using Tooba.Story.Application.Stories.Validators;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMC-001-W5 — exhaustive Story endpoint-reachable request/validator coverage.
/// TB-TMAR-STORY-AMSC-001-W1 closed the single provenance gap: GetPublicStoriesQuery's caller-controlled
/// locale/market now carry a transport-shape validator, so the matrix is 16 VALIDATOR_REQUIRED + 9
/// NO_VALIDATOR_REQUIRED over the same 25 endpoint-reachable requests.
/// </summary>
public sealed class StoryModuleAmcW5ValidatorGuardTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT";

    private static readonly (string RequestTypeName, string Route, string Classification)[] Manifest =
    [
        ("ListAdminStoriesQuery", "GET /v1/admin/stories", ValidatorRequired),
        ("QueryAdminStoryGridQuery", "POST /v1/admin/stories/query", ValidatorRequired),
        ("GetAdminStoryQuery", "GET /v1/admin/stories/{id}", NoValidatorRequired),
        ("CreateAdminStoryCommand", "POST /v1/admin/stories", ValidatorRequired),
        ("UpdateAdminStoryCommand", "PUT /v1/admin/stories/{id}", ValidatorRequired),
        ("ReorderAdminStoriesCommand", "PUT /v1/admin/stories/reorder", ValidatorRequired),
        ("EnableAdminStoryCommand", "POST /v1/admin/stories/{id}/enable", NoValidatorRequired),
        ("DisableAdminStoryCommand", "POST /v1/admin/stories/{id}/disable", NoValidatorRequired),
        ("ScheduleAdminStoryCommand", "POST /v1/admin/stories/{id}/schedule", ValidatorRequired),
        ("ApproveAdminStoryCommand", "POST /v1/admin/stories/{id}/approve", NoValidatorRequired),
        ("RejectAdminStoryCommand", "POST /v1/admin/stories/{id}/reject", ValidatorRequired),
        ("AddAdminStoryItemCommand", "POST /v1/admin/stories/{id}/items", ValidatorRequired),
        ("UpdateAdminStoryItemCommand", "PUT /v1/admin/stories/{id}/items/{itemId}", ValidatorRequired),
        ("RemoveAdminStoryItemCommand", "DELETE /v1/admin/stories/{id}/items/{itemId}", NoValidatorRequired),
        ("ReorderAdminStoryItemsCommand", "PUT /v1/admin/stories/{id}/items/reorder", ValidatorRequired),
        ("ListSellerStoriesQuery", "GET /v1/seller/stories", NoValidatorRequired),
        ("GetSellerStoryQuery", "GET /v1/seller/stories/{id}", NoValidatorRequired),
        ("CreateSellerStoryDraftCommand", "POST /v1/seller/stories", ValidatorRequired),
        ("UpdateSellerStoryCommand", "PUT /v1/seller/stories/{id}", ValidatorRequired),
        ("SubmitSellerStoryCommand", "POST /v1/seller/stories/{id}/submit", NoValidatorRequired),
        ("AddSellerStoryItemCommand", "POST /v1/seller/stories/{id}/items", ValidatorRequired),
        ("UpdateSellerStoryItemCommand", "PUT /v1/seller/stories/{id}/items/{itemId}", ValidatorRequired),
        ("RemoveSellerStoryItemCommand", "DELETE /v1/seller/stories/{id}/items/{itemId}", NoValidatorRequired),
        ("ReorderSellerStoryItemsCommand", "PUT /v1/seller/stories/{id}/items/reorder", ValidatorRequired),
        ("GetPublicStoriesQuery", "GET /v1/storefront/stories", ValidatorRequired),
    ];

    private static readonly (string Request, Type Validator)[] RequiredValidators =
    [
        ("ListAdminStoriesQuery", typeof(ListAdminStoriesQueryValidator)),
        ("QueryAdminStoryGridQuery", typeof(QueryAdminStoryGridQueryValidator)),
        ("CreateAdminStoryCommand", typeof(CreateAdminStoryCommandValidator)),
        ("UpdateAdminStoryCommand", typeof(UpdateAdminStoryCommandValidator)),
        ("ReorderAdminStoriesCommand", typeof(ReorderAdminStoriesCommandValidator)),
        ("ScheduleAdminStoryCommand", typeof(ScheduleAdminStoryCommandValidator)),
        ("RejectAdminStoryCommand", typeof(RejectAdminStoryCommandValidator)),
        ("AddAdminStoryItemCommand", typeof(AddAdminStoryItemCommandValidator)),
        ("UpdateAdminStoryItemCommand", typeof(UpdateAdminStoryItemCommandValidator)),
        ("ReorderAdminStoryItemsCommand", typeof(ReorderAdminStoryItemsCommandValidator)),
        ("CreateSellerStoryDraftCommand", typeof(CreateSellerStoryDraftCommandValidator)),
        ("UpdateSellerStoryCommand", typeof(UpdateSellerStoryCommandValidator)),
        ("AddSellerStoryItemCommand", typeof(AddSellerStoryItemCommandValidator)),
        ("UpdateSellerStoryItemCommand", typeof(UpdateSellerStoryItemCommandValidator)),
        ("ReorderSellerStoryItemsCommand", typeof(ReorderSellerStoryItemsCommandValidator)),
        ("GetPublicStoriesQuery", typeof(GetPublicStoriesQueryValidator)),
    ];

    [Fact]
    public void Complete_endpoint_inventory_is_exactly_25_with_16_required_and_9_no_validator()
    {
        Assert.Equal(25, Manifest.Length);
        Assert.Equal(16, Manifest.Count(x => x.Classification == ValidatorRequired));
        Assert.Equal(9, Manifest.Count(x => x.Classification == NoValidatorRequired));
        Assert.Equal(Manifest.Length, Manifest.Select(x => x.RequestTypeName).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Endpoint_constructions_match_the_manifest_exactly()
    {
        var constructed = new List<string>();
        foreach (var file in EndpointFiles())
            constructed.AddRange(ConstructedRequests(file));

        var manifested = Manifest.Select(x => x.RequestTypeName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            manifested,
            constructed.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Sixteen_required_validators_resolve_via_foundation_DI_and_nine_have_none()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(IStoryDirectory).Assembly);
        using var sp = services.BuildServiceProvider();
        var appAsm = typeof(IStoryDirectory).Assembly;

        foreach (var (request, validatorType) in RequiredValidators)
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == request);
            var registered = sp.GetService(typeof(IValidator<>).MakeGenericType(requestType));
            Assert.NotNull(registered);
            Assert.Equal(validatorType, registered!.GetType());
        }

        foreach (var name in Manifest.Where(x => x.Classification == NoValidatorRequired).Select(x => x.RequestTypeName))
        {
            var requestType = appAsm.GetTypes().Single(t => t.Name == name);
            Assert.True(
                sp.GetService(typeof(IValidator<>).MakeGenericType(requestType)) is null,
                $"{name} is NO_VALIDATOR_REQUIRED but a validator is registered");
        }
    }

    [Fact]
    public void All_requests_are_real_mediatr_requests_and_endpoints_use_ISender()
    {
        var appAsm = typeof(IStoryDirectory).Assembly;
        foreach (var (name, _, _) in Manifest)
            Assert.True(appAsm.GetTypes().Single(t => t.Name == name).IsAssignableTo(typeof(MediatR.IBaseRequest)));

        foreach (var file in EndpointFiles())
        {
            var text = StripDocComments(File.ReadAllText(file));
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IStoryDirectory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SoT_records_W5_validator_coverage()
    {
        var sot = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"storyModuleAmc001W5\"", sot, StringComparison.Ordinal);
        Assert.Contains("STORY_VALIDATOR_COVERAGE_COMPLETE", sot, StringComparison.Ordinal);
    }

    private static string StripDocComments(string text) =>
        Regex.Replace(text, @"///[^\r\n]*", string.Empty);

    private static IReadOnlyList<string> EndpointFiles() =>
    [
        Path.Combine(StoryRoot(), "Tooba.Story.Endpoints", "Admin", "StoryAdminEndpoints.cs"),
        Path.Combine(StoryRoot(), "Tooba.Story.Endpoints", "Seller", "StorySellerEndpoints.cs"),
        Path.Combine(StoryRoot(), "Tooba.Story.Endpoints", "Storefront", "StoryStorefrontEndpoints.cs"),
    ];

    private static string[] ConstructedRequests(string endpointFile) =>
        Regex.Matches(File.ReadAllText(endpointFile), @"\bnew\s+([A-Za-z0-9_]+(?:Command|Query))\b")
            .Select(m => m.Groups[1].Value)
            .ToArray();

    private static string StoryRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Story");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
