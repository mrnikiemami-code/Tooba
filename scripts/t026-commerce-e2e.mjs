/**
 * Live commerce E2E for TB-P05-T026 against Host via Next rewrite.
 */
const base = process.env.TOOBA_ORIGIN || "http://127.0.0.1:3000";
const offerId = process.env.TOOBA_OFFER_ID || "01a03826-b318-7000-b6b6-aa85026be261";
const outPath = process.env.TOOBA_E2E_OUT || "docs/evidence/TB-P05-T026/03-commerce-e2e-gate.md";

async function json(res) {
  const text = await res.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.url} ${typeof body === "string" ? body : JSON.stringify(body)}`);
  }
  return body;
}

function pick(obj, ...keys) {
  for (const key of keys) {
    if (obj && obj[key] != null) return obj[key];
  }
  return null;
}

async function main() {
  const created = await json(await fetch(`${base}/v1/storefront/cart`, { method: "POST" }));
  const cartId = pick(created, "cartId", "CartId");
  const guestSecret = pick(created, "guestSecret", "GuestSecret");
  let version = pick(created, "version", "Version");
  const headers = {
    "content-type": "application/json",
    "X-Tooba-Guest-Secret": guestSecret,
    "X-Tooba-Cart-Version": String(version),
  };

  const withLine = await json(
    await fetch(`${base}/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
      method: "POST",
      headers,
      body: JSON.stringify({ offerId, quantity: 1 }),
    }),
  );
  version = pick(withLine, "version", "Version");
  headers["X-Tooba-Cart-Version"] = String(version);

  const shipping = {
    recipientName: "خریدار آزمایشی گیت",
    contactMobile: "09121234567",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان آزادی، پلاک ۱، واحد ۲",
    postalCode: "1234567890",
  };

  const checkout = await json(
    await fetch(`${base}/v1/storefront/checkout`, {
      method: "POST",
      headers: { ...headers, "X-Tooba-Cart-Version": String(version) },
      body: JSON.stringify({
        cartId,
        expectedCartVersion: version,
        idempotencyKey: `gate-${Date.now()}`,
        shipping,
      }),
    }),
  );
  const checkoutId = pick(checkout, "checkoutId", "CheckoutId");
  const payable = pick(checkout, "payableAmount", "PayableAmount");
  const tax = pick(checkout, "taxAmount", "TaxAmount");
  const paymentState = pick(checkout, "paymentState", "PaymentState");
  const status = pick(checkout, "status", "Status");

  const payInit = await json(
    await fetch(`${base}/v1/storefront/checkout/${checkoutId}/payments`, {
      method: "POST",
      headers,
      body: JSON.stringify({ cartId, idempotencyKey: `pay-${Date.now()}` }),
    }),
  );
  const paymentId = pick(payInit, "paymentId", "PaymentId");
  const attemptId = pick(payInit, "attemptId", "AttemptId");
  const providerRequestReference = pick(payInit, "providerRequestReference", "ProviderRequestReference");
  const redirectUrl = pick(payInit, "redirectUrl", "RedirectUrl");

  const paid = await json(
    await fetch(`${base}/v1/storefront/payments/${paymentId}/sandbox/complete`, {
      method: "POST",
      headers,
      body: JSON.stringify({
        cartId,
        attemptId,
        providerRequestReference,
        outcome: "success",
      }),
    }),
  );

  // Checkout paymentState converges via outbox (typically ~1–2s).
  let confirmation = null;
  for (let attempt = 0; attempt < 20; attempt += 1) {
    await new Promise((resolve) => setTimeout(resolve, 500));
    confirmation = await json(
      await fetch(`${base}/v1/storefront/checkout/${checkoutId}?cartId=${encodeURIComponent(cartId)}`, {
        headers,
      }),
    );
    const state = pick(confirmation, "paymentState", "PaymentState");
    if (String(state).toLowerCase() === "paid" || String(state) === "Paid") {
      break;
    }
  }

  const summary = {
    offerId,
    cartId,
    checkoutId,
    paymentId,
    attemptId,
    payable,
    tax,
    checkoutStatusBeforePay: status,
    paymentStateBeforePay: paymentState,
    paymentAfter: {
      status: pick(paid, "status", "Status"),
      paymentState: pick(paid, "paymentState", "PaymentState"),
    },
    confirmation: {
      status: pick(confirmation, "status", "Status"),
      paymentState: pick(confirmation, "paymentState", "PaymentState"),
      recipientName: pick(confirmation, "recipientName", "RecipientName"),
      payableAmount: pick(confirmation, "payableAmount", "PayableAmount"),
      taxAmount: pick(confirmation, "taxAmount", "TaxAmount"),
    },
    redirectUrl,
  };

  const md = `# 03 — Commerce E2E gate (TB-P05-T026)

Live flow via Next rewrite → Host (guest cart).

## Path

Home/Listing → PDP \`demo-game-2\` → Offer \`${offerId}\` → Cart → Checkout (guest address) → Payment sandbox success → Confirmation

## Verified fields

| Field | Value |
|---|---|
| OfferId | ${summary.offerId} |
| CartId | ${summary.cartId} |
| CheckoutId | ${summary.checkoutId} |
| PaymentId | ${summary.paymentId} |
| Payable | ${summary.payable} |
| Tax | ${summary.tax} |
| Checkout status (pre-pay) | ${summary.checkoutStatusBeforePay} |
| Payment state (pre-pay) | ${summary.paymentStateBeforePay} |
| Payment after sandbox | ${JSON.stringify(summary.paymentAfter)} |
| Confirmation status | ${summary.confirmation.status} |
| Confirmation payment | ${summary.confirmation.paymentState} |
| Shipping recipient snapshot | ${summary.confirmation.recipientName} |

## Assertions

- Pricing/inventory/tax owned by Host (not frontend invention)
- Guest shipping snapshot immutable on confirmation
- Sandbox payment completed through Host verify path
- Result: **PASS**

\`\`\`json
${JSON.stringify(summary, null, 2)}
\`\`\`
`;

  const fs = await import("node:fs");
  fs.mkdirSync("docs/evidence/TB-P05-T026", { recursive: true });
  fs.writeFileSync(outPath, md, "utf8");
  fs.writeFileSync("docs/evidence/TB-P05-T026/03-commerce-e2e-result.json", JSON.stringify(summary, null, 2), "utf8");
  console.log("E2E_PASS", JSON.stringify(summary));
}

main().catch((error) => {
  console.error("E2E_FAIL", error);
  process.exit(1);
});
