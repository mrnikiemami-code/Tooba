using FluentValidation.TestHelper;
using Tooba.BuildingBlocks.Grid;
using Tooba.Settlement.Application.Commands.ProcessAdminPayout;
using Tooba.Settlement.Application.Commands.RequestSellerPayout;
using Tooba.Settlement.Application.Commands.RetryAdminPayout;
using Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;
using Tooba.Settlement.Application.Validators.Admin;
using Tooba.Settlement.Application.Validators.Seller;
using Xunit;

namespace Tooba.Settlement.Tests.Validation;

/// <summary>
/// TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 — direct, in-memory transport-shape
/// tests for the four Settlement validators. No web host, no database.
/// </summary>
public sealed class SettlementValidatorTests
{
    private static readonly Guid Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Actor = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static GridQueryRequest Request(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        IReadOnlyList<GridSortRequest>? sort = null,
        IReadOnlyList<GridFilterRequest>? filters = null) =>
        new(page, pageSize, search, sort ?? [], filters ?? []);

    [Fact]
    public void RequestSellerPayout_transport_shape()
    {
        var validator = new RequestSellerPayoutCommandValidator();

        validator.TestValidate(new RequestSellerPayoutCommand(Id, Actor, 0m, "key"))
            .ShouldHaveValidationErrorFor(x => x.Amount);
        validator.TestValidate(new RequestSellerPayoutCommand(Id, Actor, -1m, "key"))
            .ShouldHaveValidationErrorFor(x => x.Amount);

        validator.TestValidate(new RequestSellerPayoutCommand(Id, Actor, 10m, null!))
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
        validator.TestValidate(new RequestSellerPayoutCommand(Id, Actor, 10m, ""))
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);
        validator.TestValidate(new RequestSellerPayoutCommand(Id, Actor, 10m, "   "))
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);

        validator.TestValidate(new RequestSellerPayoutCommand(Id, Actor, 10m, "key"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RequestSellerPayout_does_not_police_trusted_authorizer_values()
    {
        var validator = new RequestSellerPayoutCommandValidator();

        // SellerPartyId / ActorUserId come from the trusted seller authorization boundary and must
        // not be FluentValidation rules.
        validator.TestValidate(new RequestSellerPayoutCommand(Guid.Empty, Guid.Empty, 10m, "key"))
            .ShouldNotHaveAnyValidationErrors();

        var signature = string.Join(",", typeof(RequestSellerPayoutCommandValidator)
            .GetProperties()
            .Select(p => p.Name));
        Assert.DoesNotContain("SellerPartyId", signature, StringComparison.Ordinal);
        Assert.DoesNotContain("ActorUserId", signature, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessAdminPayout_transport_shape()
    {
        var validator = new ProcessAdminPayoutCommandValidator();

        validator.TestValidate(new ProcessAdminPayoutCommand(Guid.Empty, Actor))
            .ShouldHaveValidationErrorFor(x => x.PayoutRequestId);

        validator.TestValidate(new ProcessAdminPayoutCommand(Id, Actor))
            .ShouldNotHaveAnyValidationErrors();

        // ActorUserId is supplied by the trusted admin authorization boundary: never validated.
        validator.TestValidate(new ProcessAdminPayoutCommand(Id, Guid.Empty))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RetryAdminPayout_transport_shape()
    {
        var validator = new RetryAdminPayoutCommandValidator();

        validator.TestValidate(new RetryAdminPayoutCommand(Guid.Empty, Actor))
            .ShouldHaveValidationErrorFor(x => x.PayoutRequestId);

        validator.TestValidate(new RetryAdminPayoutCommand(Id, Actor))
            .ShouldNotHaveAnyValidationErrors();

        validator.TestValidate(new RetryAdminPayoutCommand(Id, Guid.Empty))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void QueryAdminPayoutGrid_transport_envelope_shape()
    {
        var validator = new QueryAdminPayoutGridQueryValidator();

        validator.TestValidate(new QueryAdminPayoutGridQuery(null!))
            .ShouldHaveValidationErrorFor(x => x.Request);

        validator.TestValidate(new QueryAdminPayoutGridQuery(Request()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void QueryAdminPayoutGrid_does_not_duplicate_grid_policy()
    {
        var validator = new QueryAdminPayoutGridQueryValidator();

        // Deliberately policy-hostile content (paging below one, unknown sort/filter/operator shapes):
        // the validator must accept it because normalization/whitelisting stays with
        // AdminPayoutGridQueryPolicy, not FluentValidation.
        var hostile = new GridQueryRequest(
            0,
            0,
            "search",
            [new GridSortRequest("not-a-real-field", "sideways")],
            [new GridFilterRequest("not-a-real-field", "not-a-real-operator", "x", null, null)]);

        validator.TestValidate(new QueryAdminPayoutGridQuery(hostile))
            .ShouldNotHaveAnyValidationErrors();
    }
}
