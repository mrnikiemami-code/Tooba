using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Wallet.Contracts.Payments;

namespace Tooba.Payment.Application.Models;

/// <summary>Application-owned storefront payment orchestration (former Host StorefrontPaymentComposer).</summary>
public sealed class StorefrontPaymentOrchestrator
{
    private readonly IStorefrontCheckoutPaymentAccessPort _checkouts;
    private readonly IPaymentDirectory _payments;
    private readonly IOrderPaymentProjection _orderPayments;
    private readonly IWalletOrderPaymentPort _wallets;
    private readonly IPaymentGatewayCatalogPort _catalog;
    private readonly IPaymentProofMediaPort _media;
    private readonly IPaymentUnpaidRetrySupplyPort _supply;
    private readonly IPaymentExpiryDirectory _expiry;
    private readonly IClock _clock;
    private readonly ILogger<StorefrontPaymentOrchestrator> _logger;

    public StorefrontPaymentOrchestrator(
        IStorefrontCheckoutPaymentAccessPort checkouts,
        IPaymentDirectory payments,
        IOrderPaymentProjection orderPayments,
        IWalletOrderPaymentPort wallets,
        IPaymentGatewayCatalogPort catalog,
        IPaymentProofMediaPort media,
        IPaymentUnpaidRetrySupplyPort supply,
        IPaymentExpiryDirectory expiry,
        IClock clock,
        ILogger<StorefrontPaymentOrchestrator> logger)
    {
        _checkouts = checkouts;
        _payments = payments;
        _orderPayments = orderPayments;
        _wallets = wallets;
        _catalog = catalog;
        _media = media;
        _supply = supply;
        _expiry = expiry;
        _clock = clock;
        _logger = logger;
    }

    public static Guid ResolvePaymentActor(Guid? authenticatedUserId) =>
        authenticatedUserId is { } userId && userId != Guid.Empty
            ? userId
            : PaymentStorefrontActors.GuestActorId;

    public async Task<StorefrontWalletQuoteDto> GetWalletQuoteAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        await _checkouts.EnsureCheckoutActorAsync(cancellationToken);
        var checkout = await _checkouts.GetForMutationAsync(checkoutId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);

        var actor = ResolvePaymentActor(authenticatedUserId);
        var quote = await _wallets.QuoteForPayableAsync(
            actor,
            checkout.PayableAmount,
            checkout.Currency,
            cancellationToken);
        return new StorefrontWalletQuoteDto(
            checkout.CheckoutId,
            quote.WalletBalance,
            quote.MaxUsable,
            quote.RemainingPayable,
            quote.CanPayFullyWithWallet,
            quote.Currency,
            MixedTenderDeferred: true,
            ManualCardToCardEnabled: _catalog.ManualCardToCardEnabled);
    }

    public StorefrontPaymentMethodsDto ListPaymentMethods()
    {
        var methods = new List<StorefrontPaymentMethodOptionDto>();
        if (_catalog.IsOnlineGatewayOffered())
            methods.Add(new(PaymentProviderCodes.GatewayCatalog, "درگاه بانکی", "پرداخت آنلاین از طریق درگاه"));

        if (_catalog.ManualCardToCardEnabled)
        {
            methods.Add(new(
                PaymentProviderCodes.Manual,
                "کارت به کارت",
                "پرداخت دستی؛ سفارش پس از تأیید واریز توسط فروشگاه تکمیل می‌شود"));
        }

        return new StorefrontPaymentMethodsDto(
            methods,
            _catalog.ManualCardToCardEnabled,
            _catalog.ManualProofRequirement,
            _catalog.ManualPaymentInstructions);
    }

    public async Task<StorefrontPaymentInitiationDto> InitiateAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        string idempotencyKey,
        bool useWallet,
        string? providerCodeOverride,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        await _checkouts.EnsureCheckoutActorAsync(cancellationToken);
        var checkout = await _checkouts.GetForMutationAsync(checkoutId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);

        var providerCode = _catalog.DefaultProvider;
        var actor = ResolvePaymentActor(authenticatedUserId);
        if (useWallet)
        {
            var quote = await _wallets.QuoteForPayableAsync(
                actor,
                checkout.PayableAmount,
                checkout.Currency,
                cancellationToken);
            if (!quote.CanPayFullyWithWallet || quote.RemainingPayable > 0)
                throw new InvalidOperationException(PaymentErrorCodes.WalletMixedDeferred);

            providerCode = PaymentProviderCodes.Wallet;
        }
        else if (!string.IsNullOrWhiteSpace(providerCodeOverride)
            && PaymentProviderCodes.IsManual(providerCodeOverride))
        {
            if (!_catalog.ManualCardToCardEnabled)
                throw new InvalidOperationException(PaymentErrorCodes.MethodUnavailable);

            providerCode = PaymentProviderCodes.Manual;
        }
        else if (!string.IsNullOrWhiteSpace(providerCodeOverride)
            && providerCodeOverride.Trim().Equals(PaymentProviderCodes.GatewayCatalog, StringComparison.OrdinalIgnoreCase))
        {
            if (!_catalog.IsOnlineGatewayOffered())
                throw new InvalidOperationException(PaymentErrorCodes.MethodUnavailable);
            providerCode = _catalog.DefaultProvider;
        }
        else if (!useWallet && string.IsNullOrWhiteSpace(providerCodeOverride))
        {
            if (!_catalog.IsOnlineGatewayOffered())
                throw new InvalidOperationException(PaymentErrorCodes.MethodUnavailable);
        }

        var initiated = await _payments.InitiateAsync(
            new InitiatePaymentCommand(
                checkout.CheckoutId,
                actor,
                null,
                idempotencyKey,
                providerCode),
            cancellationToken);

        if (PaymentProviderCodes.IsWallet(initiated.ProviderCode)
            && initiated.Status != PaymentStatus.Succeeded)
        {
            var verified = await _payments.VerifyAsync(
                new VerifyPaymentCommand(
                    initiated.PaymentId,
                    initiated.AttemptId,
                    initiated.ProviderRequestReference,
                    true),
                cancellationToken);
            _logger.LogInformation(
                "Storefront wallet payment verified immediately. CheckoutId={CheckoutId} PaymentId={PaymentId} Status={Status} NewlySucceeded={NewlySucceeded}",
                checkout.CheckoutId,
                verified.PaymentId,
                verified.Status,
                verified.NewlySucceeded);

            var after = await _payments.GetAsync(initiated.PaymentId, actor, null, cancellationToken)
                ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);

            return new StorefrontPaymentInitiationDto(
                after.PaymentId,
                initiated.AttemptId,
                checkout.CheckoutId,
                after.Status.ToString(),
                after.ProviderCode,
                initiated.ProviderRequestReference,
                RedirectUrl: $"/payment/result?checkoutId={checkout.CheckoutId:D}&paymentId={after.PaymentId:D}",
                after.Amount,
                after.Currency,
                RequiresPspRedirect: false);
        }

        if (PaymentProviderCodes.IsManual(initiated.ProviderCode))
        {
            return new StorefrontPaymentInitiationDto(
                initiated.PaymentId,
                initiated.AttemptId,
                checkout.CheckoutId,
                initiated.Status.ToString(),
                initiated.ProviderCode,
                initiated.ProviderRequestReference,
                RedirectUrl: $"/payment/result?checkoutId={checkout.CheckoutId:D}&paymentId={initiated.PaymentId:D}&awaitingManual=1",
                initiated.Amount,
                initiated.Currency,
                RequiresPspRedirect: false);
        }

        var redirect = string.IsNullOrWhiteSpace(initiated.RedirectUrl)
            ? "/payment/sandbox"
            : initiated.RedirectUrl;
        if (!redirect.Contains("checkoutId=", StringComparison.Ordinal))
        {
            redirect += (redirect.Contains('?', StringComparison.Ordinal) ? "&" : "?")
                + "checkoutId=" + checkout.CheckoutId.ToString("D");
        }

        _logger.LogInformation(
            "Storefront payment initiated. CheckoutId={CheckoutId} PaymentId={PaymentId} Amount={Amount} Currency={Currency}",
            checkout.CheckoutId,
            initiated.PaymentId,
            initiated.Amount,
            initiated.Currency);

        return new StorefrontPaymentInitiationDto(
            initiated.PaymentId,
            initiated.AttemptId,
            checkout.CheckoutId,
            initiated.Status.ToString(),
            initiated.ProviderCode,
            initiated.ProviderRequestReference,
            redirect,
            initiated.Amount,
            initiated.Currency,
            RequiresPspRedirect: true);
    }

    public async Task<StorefrontPaymentDto?> GetAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        _ = cartId;
        var actor = ResolvePaymentActor(authenticatedUserId);
        var payment = await _payments.GetAsync(paymentId, actor, null, cancellationToken);
        if (payment is null)
            return null;

        var checkout = await _checkouts.GetOwnedForPaymentResultAsync(
            payment.CheckoutId,
            guestSecret,
            cancellationToken);
        if (checkout is null)
            throw new InvalidOperationException(PaymentErrorCodes.GuestInvalid);

        return MapPaymentPage(payment, checkout);
    }

    public async Task<StorefrontSandboxContextDto> GetSandboxContextAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        if (!_catalog.IsSandboxSimulatorEnabled())
            throw new InvalidOperationException(PaymentErrorCodes.SandboxUnavailable);

        var payment = await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        var checkout = await _checkouts.GetOwnedAsync(payment.CheckoutId, cartId, guestSecret, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        var orderNumber = checkout.OrderNumber
            ?? checkout.CheckoutId.ToString("N")[..12];
        return new StorefrontSandboxContextDto(
            payment.PaymentId,
            payment.CheckoutId,
            _catalog.StoreDisplayName,
            orderNumber,
            payment.Amount,
            payment.Currency,
            "درگاه بانکی (آزمایشی)",
            Sandbox: true);
    }

    public async Task<StorefrontPaymentDto> CompleteSandboxAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Guid attemptId,
        string providerRequestReference,
        string outcome,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        if (!_catalog.IsSandboxSimulatorEnabled())
            throw new InvalidOperationException(PaymentErrorCodes.SandboxUnavailable);

        _ = await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);

        var successRequested = string.Equals(outcome, "success", StringComparison.OrdinalIgnoreCase);
        if (!successRequested)
            _catalog.MarkSandboxDecline(providerRequestReference);

        var verified = await _payments.VerifyAsync(
            new VerifyPaymentCommand(paymentId, attemptId, providerRequestReference, successRequested),
            cancellationToken);

        _logger.LogInformation(
            "Storefront sandbox payment verified. PaymentId={PaymentId} Status={Status} NewlySucceeded={NewlySucceeded} Duplicate={Duplicate}",
            verified.PaymentId,
            verified.Status,
            verified.NewlySucceeded,
            !verified.NewlySucceeded && string.Equals(verified.Status.ToString(), "Succeeded", StringComparison.Ordinal));

        return await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
    }

    public async Task<Guid> UploadManualProofAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Stream stream,
        string fileName,
        string contentType,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        _ = await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        var actor = ResolvePaymentActor(authenticatedUserId);
        var mediaAssetId = await _media.UploadAsync(stream, fileName, contentType, actor, cancellationToken);
        await _payments.RegisterProofAssetAsync(paymentId, actor, null, mediaAssetId, cancellationToken);
        return mediaAssetId;
    }

    public async Task<StorefrontPaymentDto> SubmitManualEvidenceAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        string transferReference,
        Guid? proofMediaAssetId,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var opened = await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        if (string.Equals(opened.Status, "Succeeded", StringComparison.Ordinal)
            || await _payments.HasSucceededPaymentForCheckoutAsync(opened.CheckoutId, cancellationToken))
        {
            throw new InvalidOperationException(PaymentErrorCodes.AlreadySucceeded);
        }

        var requirement = _catalog.ManualProofRequirement;
        if (requirement.Equals("Required", StringComparison.OrdinalIgnoreCase) && proofMediaAssetId is null)
            throw new InvalidOperationException(PaymentErrorCodes.ProofRequired);

        if (requirement.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            proofMediaAssetId = null;

        var actor = ResolvePaymentActor(authenticatedUserId);
        await _payments.SubmitManualEvidenceAsync(
            paymentId,
            actor,
            null,
            transferReference,
            proofMediaAssetId,
            cancellationToken);

        var reviewExpiresAt = _clock.UtcNow.AddHours(_catalog.ManualPaymentReviewHoldHours);
        var payment = await _payments.GetAsync(paymentId, actor, null, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        await _orderPayments.PromoteReservationsForManualPaymentReviewAsync(
            payment.CheckoutId,
            reviewExpiresAt,
            cancellationToken);

        return await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
    }

    public async Task<StorefrontPaymentDto> RetryManualAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var current = await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        if (string.Equals(current.Status, "Succeeded", StringComparison.Ordinal)
            || await _payments.HasSucceededPaymentForCheckoutAsync(current.CheckoutId, cancellationToken))
        {
            throw new InvalidOperationException(PaymentErrorCodes.AlreadySucceeded);
        }

        await _payments.RetryManualAfterRejectionAsync(
            paymentId,
            ResolvePaymentActor(authenticatedUserId),
            null,
            cancellationToken);
        return await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
    }

    public async Task<StorefrontPaymentDto> RetryUnpaidAsync(
        Guid paymentId,
        Guid cartId,
        string? guestSecret,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var page = await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
        if (string.Equals(page.Status, "Succeeded", StringComparison.Ordinal)
            || await _payments.HasSucceededPaymentForCheckoutAsync(page.CheckoutId, cancellationToken))
        {
            throw new InvalidOperationException(PaymentErrorCodes.AlreadySucceeded);
        }

        await RetryUnpaidCoreAsync(paymentId, page.CheckoutId, page.Status, authenticatedUserId, cancellationToken);
        return await GetAsync(paymentId, cartId, guestSecret, authenticatedUserId, cancellationToken)
            ?? throw new InvalidOperationException(PaymentErrorCodes.Missing);
    }

    public async Task RetryUnpaidCoreAsync(
        Guid paymentId,
        Guid checkoutId,
        string? paymentStatus,
        Guid? authenticatedUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _supply.EnsureRetrySupplyAsync(checkoutId, cancellationToken);
        }
        catch (InvalidOperationException ex) when (PaymentExceptionMapper.TryMapExact(ex.Message, out _))
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(PaymentErrorCodes.UnpaidSupplyUnavailable);
        }

        if (string.Equals(paymentStatus, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            await _expiry.ReopenExpiredForRetryAsync(
                paymentId,
                ResolvePaymentActor(authenticatedUserId),
                null,
                cancellationToken);
        }
    }

    private StorefrontPaymentDto MapPaymentPage(
        PaymentSnapshot payment,
        StorefrontCheckoutPaymentAccessDto checkout)
    {
        var history = (payment.EvidenceHistory ?? [])
            .Select(x => new StorefrontManualEvidenceHistoryDto(
                x.AttemptId,
                x.AttemptStatus.ToString(),
                x.CustomerTransferReference,
                x.ProofMediaAssetId,
                x.EvidenceSubmittedAt,
                x.FailureCode))
            .ToArray();
        var manual = PaymentProviderCodes.IsManual(payment.ProviderCode);
        var succeeded = payment.Status == PaymentStatus.Succeeded;
        var canSubmit = !succeeded
            && manual
            && payment.Status == PaymentStatus.Pending
            && payment.EvidenceSubmittedAt is null;
        var canRetry = !succeeded && manual && payment.Status == PaymentStatus.Failed;
        var canRetryUnpaid = !succeeded && payment.Status == PaymentStatus.Expired;
        return new StorefrontPaymentDto(
            payment.PaymentId,
            payment.CheckoutId,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.ProviderCode,
            payment.Allocations
                .Select(x => new StorefrontPaymentAllocationDto(
                    x.SellerOrderId,
                    x.AllocatedAmount,
                    x.Currency,
                    x.TargetKind.ToString()))
                .ToArray(),
            payment.CustomerTransferReference,
            payment.ProofMediaAssetId,
            payment.EvidenceSubmittedAt,
            checkout.OrderNumber,
            _catalog.ManualProofRequirement,
            _catalog.ManualPaymentInstructions,
            canSubmit,
            canRetry,
            history,
            canRetryUnpaid);
    }
}
