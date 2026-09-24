using FluentValidation;
using FluentValidation.TestHelper;
using Tooba.Payment.Application.Commands.CompleteSandboxPayment;
using Tooba.Payment.Application.Commands.InitiateStorefrontPayment;
using Tooba.Payment.Application.Commands.RetryManualPayment;
using Tooba.Payment.Application.Commands.RetryUnpaidPayment;
using Tooba.Payment.Application.Commands.SubmitManualPaymentEvidence;
using Tooba.Payment.Application.Commands.UploadManualPaymentProof;
using Tooba.Payment.Application.Queries.GetStorefrontPayment;
using Tooba.Payment.Application.Queries.GetStorefrontPaymentSandboxContext;
using Tooba.Payment.Application.Queries.GetStorefrontWalletQuote;
using Tooba.Payment.Application.Validators.Storefront;
using Xunit;

namespace Tooba.Payment.Tests.Validation;

/// <summary>
/// TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 — direct, in-memory transport-shape tests for the nine
/// storefront Payment validators. No web host, no database, no business-rule assertions.
/// </summary>
public sealed class PaymentStorefrontValidatorTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void InitiateStorefrontPayment_transport_shape()
    {
        var validator = new InitiateStorefrontPaymentCommandValidator();

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Guid.Empty, Id, null, "idem-1", true, null, null))
            .ShouldHaveValidationErrorFor(x => x.CheckoutId);

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Guid.Empty, null, "idem-1", true, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.CartId);

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Id, null, "   ", true, null, null))
            .ShouldHaveValidationErrorFor(x => x.IdempotencyKey);

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Id, null, "idem-1", true, "   ", null))
            .ShouldHaveValidationErrorFor(x => x.ProviderCode);

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Id, "  ", "idem-1", true, null, null))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Id, null, "idem-1", true, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Id, null, "idem-1", true, null, null))
            .ShouldNotHaveAnyValidationErrors();

        validator.TestValidate(new InitiateStorefrontPaymentCommand(
            Id, Id, "guest", "  idem-1  ", true, "  provider  ", Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetStorefrontWalletQuote_transport_shape()
    {
        var validator = new GetStorefrontWalletQuoteQueryValidator();

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Guid.Empty, Id, null, null))
            .ShouldHaveValidationErrorFor(x => x.CheckoutId);
        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Guid.Empty, null, null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Id, "  ", null))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);
        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Id, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);
        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Id, null, null))
            .ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Id, "guest", Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetStorefrontPayment_transport_shape()
    {
        var validator = new GetStorefrontPaymentQueryValidator();

        validator.TestValidate(new GetStorefrontPaymentQuery(Guid.Empty, Id, null, null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new GetStorefrontPaymentQuery(Id, Guid.Empty, null, null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new GetStorefrontPaymentQuery(Id, Id, "  ", null))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);
        validator.TestValidate(new GetStorefrontPaymentQuery(Id, Id, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);
        validator.TestValidate(new GetStorefrontPaymentQuery(Id, Id, null, null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetStorefrontPaymentSandboxContext_transport_shape()
    {
        var validator = new GetStorefrontPaymentSandboxContextQueryValidator();

        validator.TestValidate(new GetStorefrontPaymentSandboxContextQuery(Guid.Empty, Id, null, null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new GetStorefrontPaymentSandboxContextQuery(Id, Guid.Empty, null, null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new GetStorefrontPaymentSandboxContextQuery(Id, Id, "  ", null))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);
        validator.TestValidate(new GetStorefrontPaymentSandboxContextQuery(Id, Id, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);
        validator.TestValidate(new GetStorefrontPaymentSandboxContextQuery(Id, Id, null, Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CompleteSandboxPayment_transport_shape()
    {
        var validator = new CompleteSandboxPaymentCommandValidator();

        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Guid.Empty, Id, null, Id, "ref", "succeeded", null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Id, Guid.Empty, null, Id, "ref", "succeeded", null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Id, Id, null, Guid.Empty, "ref", "succeeded", null))
            .ShouldHaveValidationErrorFor(x => x.AttemptId);
        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Id, Id, null, Id, "   ", "succeeded", null))
            .ShouldHaveValidationErrorFor(x => x.ProviderRequestReference);
        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Id, Id, null, Id, "ref", "   ", null))
            .ShouldHaveValidationErrorFor(x => x.Outcome);
        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Id, Id, "  ", Id, "ref", "succeeded", Guid.Empty))
            .ShouldHaveAnyValidationError();

        validator.TestValidate(new CompleteSandboxPaymentCommand(
            Id, Id, null, Id, "ref", "succeeded", Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SubmitManualPaymentEvidence_transport_shape()
    {
        var validator = new SubmitManualPaymentEvidenceCommandValidator();

        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Guid.Empty, Id, null, "tr", null, null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Id, Guid.Empty, null, "tr", null, null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Id, Id, null, "   ", null, null))
            .ShouldHaveValidationErrorFor(x => x.TransferReference);
        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Id, Id, null, "tr", Guid.Empty, null))
            .ShouldHaveValidationErrorFor(x => x.ProofMediaAssetId);
        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Id, Id, "  ", "tr", null, Guid.Empty))
            .ShouldHaveAnyValidationError();

        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Id, Id, null, "tr", null, null))
            .ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new SubmitManualPaymentEvidenceCommand(
            Id, Id, "guest", "tr", Id, Id))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RetryManualPayment_transport_shape()
    {
        var validator = new RetryManualPaymentCommandValidator();

        validator.TestValidate(new RetryManualPaymentCommand(Guid.Empty, Id, null, null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new RetryManualPaymentCommand(Id, Guid.Empty, null, null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new RetryManualPaymentCommand(Id, Id, "  ", null))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);
        validator.TestValidate(new RetryManualPaymentCommand(Id, Id, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);
        validator.TestValidate(new RetryManualPaymentCommand(Id, Id, null, null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RetryUnpaidPayment_transport_shape()
    {
        var validator = new RetryUnpaidPaymentCommandValidator();

        validator.TestValidate(new RetryUnpaidPaymentCommand(Guid.Empty, Id, null, null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new RetryUnpaidPaymentCommand(Id, Guid.Empty, null, null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new RetryUnpaidPaymentCommand(Id, Id, "  ", null))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);
        validator.TestValidate(new RetryUnpaidPaymentCommand(Id, Id, null, Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);
        validator.TestValidate(new RetryUnpaidPaymentCommand(Id, Id, null, null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UploadManualPaymentProof_transport_shape_without_reading_stream()
    {
        var validator = new UploadManualPaymentProofCommandValidator();
        var stream = new NonSeekableStream();

        validator.TestValidate(new UploadManualPaymentProofCommand(
            Guid.Empty, Id, null, stream, "proof.png", "image/png", null))
            .ShouldHaveValidationErrorFor(x => x.PaymentId);
        validator.TestValidate(new UploadManualPaymentProofCommand(
            Id, Guid.Empty, null, stream, "proof.png", "image/png", null))
            .ShouldHaveValidationErrorFor(x => x.CartId);
        validator.TestValidate(new UploadManualPaymentProofCommand(
            Id, Id, null, stream, "   ", "image/png", null))
            .ShouldHaveValidationErrorFor(x => x.FileName);
        validator.TestValidate(new UploadManualPaymentProofCommand(
            Id, Id, null, stream, "proof.png", "   ", null))
            .ShouldHaveValidationErrorFor(x => x.ContentType);
        validator.TestValidate(new UploadManualPaymentProofCommand(
            Id, Id, "  ", stream, "proof.png", "image/png", Guid.Empty))
            .ShouldHaveAnyValidationError();

        var result = validator.TestValidate(new UploadManualPaymentProofCommand(
            Id, Id, null, stream, "proof.png", "image/png", null));
        result.ShouldNotHaveAnyValidationErrors();
        Assert.Equal(0, stream.ReadCount);
        Assert.Equal(0, stream.SeekCount);
    }

    [Fact]
    public void UploadManualPaymentProof_rejects_null_content_without_touching_stream()
    {
        var validator = new UploadManualPaymentProofCommandValidator();

        var result = validator.TestValidate(new UploadManualPaymentProofCommand(
            Id, Id, null, null!, "proof.png", "image/png", null));

        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    /// <summary>Non-seekable stream that records whether validation read or sought it.</summary>
    private sealed class NonSeekableStream : Stream
    {
        public int ReadCount { get; private set; }

        public int SeekCount { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            throw new InvalidOperationException("validator must not read the proof stream");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            SeekCount++;
            throw new InvalidOperationException("validator must not seek the proof stream");
        }

        public override void Flush() => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
