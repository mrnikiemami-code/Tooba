using FluentValidation.TestHelper;
using Tooba.Payment.Application.Commands.ConfirmAdminDeposit;
using Tooba.Payment.Application.Commands.ProcessPaymentWebhook;
using Tooba.Payment.Application.Commands.ReconcileAdminPayment;
using Tooba.Payment.Application.Commands.RejectAdminDeposit;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Queries.GetAdminPayment;
using Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid;
using Tooba.Payment.Application.Validators.Admin;
using Tooba.Payment.Application.Validators.Webhooks;
using Xunit;

namespace Tooba.Payment.Tests.Validation;

/// <summary>
/// TB-TMAR-PAYMENT-PRECERT-VALIDATION-002 — direct, in-memory transport-shape tests for the
/// remaining five Admin and one Webhook Payment validators. No web host, no database.
/// </summary>
public sealed class PaymentAdminWebhookValidatorTests
{
    private static readonly Guid Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static AdminPaymentGridQueryInput GridInput(
        int page = 1,
        int pageSize = 20,
        string sortField = "created",
        string sortDirection = "desc",
        IReadOnlyList<AdminPaymentGridFilterInput>? filters = null) =>
        new(null, filters ?? [], sortField, sortDirection, page, pageSize);

    [Fact]
    public void GetAdminPayment_transport_shape()
    {
        var validator = new GetAdminPaymentQueryValidator();

        validator.TestValidate(new GetAdminPaymentQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new GetAdminPaymentQuery(Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReconcileAdminPayment_transport_shape()
    {
        var validator = new ReconcileAdminPaymentCommandValidator();

        validator.TestValidate(new ReconcileAdminPaymentCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new ReconcileAdminPaymentCommand(Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ConfirmAdminDeposit_transport_shape()
    {
        var validator = new ConfirmAdminDepositCommandValidator();

        validator.TestValidate(new ConfirmAdminDepositCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new ConfirmAdminDepositCommand(Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RejectAdminDeposit_transport_shape()
    {
        var validator = new RejectAdminDepositCommandValidator();

        validator.TestValidate(new RejectAdminDepositCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new RejectAdminDepositCommand(Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void QueryAdminPaymentsGrid_transport_envelope_shape()
    {
        var validator = new QueryAdminPaymentsGridQueryValidator();

        validator.TestValidate(new QueryAdminPaymentsGridQuery(null!))
            .ShouldHaveValidationErrorFor(x => x.Input);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(
                GridInput() with { Filters = null! }))
            .ShouldHaveValidationErrorFor(x => x.Input.Filters);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(sortField: "   ")))
            .ShouldHaveValidationErrorFor(x => x.Input.SortField);
        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(sortField: "")))
            .ShouldHaveValidationErrorFor(x => x.Input.SortField);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(sortDirection: "   ")))
            .ShouldHaveValidationErrorFor(x => x.Input.SortDirection);
        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(sortDirection: "")))
            .ShouldHaveValidationErrorFor(x => x.Input.SortDirection);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(page: 0)))
            .ShouldHaveValidationErrorFor(x => x.Input.Page);
        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(page: -1)))
            .ShouldHaveValidationErrorFor(x => x.Input.Page);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(pageSize: 0)))
            .ShouldHaveValidationErrorFor(x => x.Input.PageSize);
        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput(pageSize: -5)))
            .ShouldHaveValidationErrorFor(x => x.Input.PageSize);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(GridInput()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void QueryAdminPaymentsGrid_does_not_police_field_or_operator_whitelists()
    {
        var validator = new QueryAdminPaymentsGridQueryValidator();

        // Unknown field/operator and business-only filter fields are NOT transport concerns;
        // the Payment admin grid normalizer owns that policy.
        var input = GridInput(filters:
        [
            new AdminPaymentGridFilterInput("not-a-real-field", "not-a-real-operator", "x", null, null),
            new AdminPaymentGridFilterInput("supply", "in", "ready", null, ["a", "b"]),
        ]);

        validator.TestValidate(new QueryAdminPaymentsGridQuery(input))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProcessPaymentWebhook_transport_envelope_shape()
    {
        var validator = new ProcessPaymentWebhookCommandValidator();
        var body = "{\"paymentId\":\"00000000-0000-0000-0000-000000000000\"}";

        validator.TestValidate(new ProcessPaymentWebhookCommand("", [1], null, body))
            .ShouldHaveValidationErrorFor(x => x.ProviderCode);
        validator.TestValidate(new ProcessPaymentWebhookCommand("   ", [1], null, body))
            .ShouldHaveValidationErrorFor(x => x.ProviderCode);

        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", null!, null, body))
            .ShouldHaveValidationErrorFor(x => x.RawBody);
        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", [], null, body))
            .ShouldHaveValidationErrorFor(x => x.RawBody);

        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", [1], null, ""))
            .ShouldHaveValidationErrorFor(x => x.BodyText);
        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", [1], null, "   "))
            .ShouldHaveValidationErrorFor(x => x.BodyText);

        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", [1], "   ", body))
            .ShouldHaveValidationErrorFor(x => x.SignatureHeader);

        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", [1], null, body))
            .ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ProcessPaymentWebhookCommand("sandbox", [1], "sig", body))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProcessPaymentWebhook_does_not_parse_json_or_validate_payload_fields()
    {
        var validator = new ProcessPaymentWebhookCommandValidator();

        // Deliberately not JSON, and with no paymentId/attemptId/event fields: the validator must
        // accept it because payload shape stays in the existing handler/verifier boundary.
        var result = validator.TestValidate(
            new ProcessPaymentWebhookCommand("sandbox", [1, 2, 3], null, "not-json-at-all"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
