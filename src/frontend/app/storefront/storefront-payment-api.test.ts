import assert from "node:assert/strict";
import test from "node:test";
import {
  mapStorefrontPayment,
  mapStorefrontPaymentInitiation,
  mapStorefrontWalletQuote,
  requiresProviderRedirect,
  toCustomerPaymentMessage,
  WALLET_PROVIDER_CODE,
} from "./storefront-payment-api.ts";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";

test("payment initiation mapper keeps host redirect and amount", () => {
  const page = mapStorefrontPaymentInitiation({
    paymentId: "pay-1",
    attemptId: "att-1",
    checkoutId: "chk-1",
    status: "Pending",
    providerCode: "fake",
    providerRequestReference: "fake-abc",
    redirectUrl: "/payment/sandbox?paymentId=pay-1",
    amount: 1951100,
    currency: "IRR",
  });
  assert.equal(page?.paymentId, "pay-1");
  assert.equal(page?.amount, 1951100);
  assert.match(page?.redirectUrl ?? "", /sandbox/);
  assert.equal(page?.status, "Pending");
  assert.equal(requiresProviderRedirect(page!), true);
});

test("wallet initiation allows empty redirect and skips provider redirect", () => {
  const page = mapStorefrontPaymentInitiation({
    PaymentId: "pay-wallet",
    AttemptId: "att-w",
    CheckoutId: "chk-w",
    Status: "Succeeded",
    ProviderCode: WALLET_PROVIDER_CODE,
    ProviderRequestReference: "wallet-ref",
    RedirectUrl: "",
    Amount: 500000,
    Currency: "IRR",
  });
  assert.ok(page);
  assert.equal(page?.providerCode, "wallet");
  assert.equal(page?.redirectUrl, "");
  assert.equal(requiresProviderRedirect(page!), false);
});

test("non-wallet initiation without redirect is rejected by mapper", () => {
  const page = mapStorefrontPaymentInitiation({
    paymentId: "pay-2",
    providerCode: "fake",
    redirectUrl: "",
    status: "Pending",
  });
  assert.equal(page, null);
});

test("wallet quote mapper keeps host-calculated balances and deferred mixed flag", () => {
  const quote = mapStorefrontWalletQuote({
    CheckoutId: "chk-1",
    CartId: "cart-1",
    Currency: "IRR",
    Balance: 2_000_000,
    MaxUsableAmount: 1_500_000,
    SelectedWalletAmount: 1_500_000,
    RemainingPayable: 0,
    PayableAmount: 1_500_000,
    CanPayFullyWithWallet: true,
    MixedTenderAvailable: false,
  });
  assert.ok(quote);
  assert.equal(quote?.canPayFullyWithWallet, true);
  assert.equal(quote?.remainingPayable, 0);
  assert.equal(quote?.mixedTenderAvailable, false);
  assert.equal(quote?.balance, 2_000_000);
});

test("wallet quote mapper accepts Host wallet-quote field names without cartId", () => {
  const quote = mapStorefrontWalletQuote({
    checkoutId: "chk-2",
    walletBalance: 724000,
    maxUsable: 381500,
    remainingPayable: 0,
    canPayFullyWithWallet: true,
    currency: "IRR",
    mixedTenderDeferred: true,
  });
  assert.ok(quote);
  assert.equal(quote?.balance, 724000);
  assert.equal(quote?.maxUsableAmount, 381500);
  assert.equal(quote?.canPayFullyWithWallet, true);
  assert.equal(quote?.mixedTenderAvailable, false);
});

test("payment mapper does not invent succeeded", () => {
  const page = mapStorefrontPayment({
    paymentId: "pay-1",
    checkoutId: "chk-1",
    amount: 100,
    currency: "IRR",
    status: "Pending",
    providerCode: "fake",
  });
  assert.equal(page?.status, "Pending");
});

test("customer payment message hides gateway codes", () => {
  const hidden = toCustomerPaymentMessage(
    new StorefrontCartApiError(400, "payment.rejected", "GATEWAY_REJECTED"),
  );
  assert.equal(hidden.includes("GATEWAY_"), false);
});

test("customer payment message maps wallet insufficient", () => {
  const msg = toCustomerPaymentMessage(
    new StorefrontCartApiError(400, "payment.wallet.insufficient", "INSUFFICIENT"),
  );
  assert.match(msg, /موجودی/);
});
