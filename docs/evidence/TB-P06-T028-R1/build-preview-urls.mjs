/**
 * TB-P06-T028-R1 — builds deterministic Development preview URLs via Host APIs only.
 * Writes preview-urls.json for evidence / USER-PREVIEW.
 */
import http from "node:http";
import { randomUUID } from "node:crypto";
import { writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const HOST = "127.0.0.1";
const PORT = 5088;
const FE = "http://localhost:3000";
const SHOPEIVA = "http://127.0.0.1:3001";
const CUSTOMER = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const SELLER = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_PARTY = "01a030d1-40cb-7000-8abe-6d31739956c5";
const OFFER = "01a04402-7a75-7000-b614-6f093cb072ac";
const ADDRESS = "aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1";

const outDir = dirname(fileURLToPath(import.meta.url));

function req(method, path, { headers = {}, body } = {}) {
  return new Promise((resolve, reject) => {
    const payload = body == null ? null : Buffer.from(JSON.stringify(body), "utf8");
    const r = http.request(
      {
        host: HOST,
        port: PORT,
        path,
        method,
        headers: {
          Accept: "application/json",
          ...(payload ? { "Content-Type": "application/json", "Content-Length": payload.length } : {}),
          ...headers,
        },
      },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          const raw = Buffer.concat(chunks).toString("utf8");
          let json = null;
          try {
            json = raw ? JSON.parse(raw) : null;
          } catch {
            json = raw;
          }
          resolve({ status: res.statusCode, json, raw });
        });
      },
    );
    r.on("error", reject);
    if (payload) r.write(payload);
    r.end();
  });
}

function pick(obj, ...keys) {
  for (const k of keys) {
    if (obj && obj[k] != null) return obj[k];
  }
  return undefined;
}

function confirmationUrl({ checkoutId, cartId, guestSecret }) {
  const q = new URLSearchParams({
    checkoutId,
    cartId,
    guestSecret,
    actor: CUSTOMER,
  });
  // Primary concrete User-Preview URL (customer-panel; same confirmation UI).
  return `${FE}/customer-panel/dev/wallet-checkout?${q.toString()}`;
}

async function createPendingCheckout() {
  const cartCreate = await req("POST", "/v1/storefront/cart");
  const cartId = pick(cartCreate.json, "cartId", "CartId");
  const guestSecret = pick(cartCreate.json, "guestSecret", "GuestSecret");
  const version0 = pick(cartCreate.json, "version", "Version") ?? 1;
  const cartHeaders = { "X-Tooba-Guest-Secret": guestSecret };

  const add = await req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version0}`, {
    headers: { ...cartHeaders, "X-Tooba-Cart-Version": String(version0) },
    body: { offerId: OFFER, quantity: 1 },
  });
  const version = pick(add.json, "version", "Version") ?? version0 + 1;

  const submit = await req("POST", "/v1/storefront/checkout", {
    headers: {
      ...cartHeaders,
      "X-Tooba-Dev-Actor-User-Id": CUSTOMER,
      "X-Tooba-Cart-Version": String(version),
    },
    body: {
      cartId,
      expectedCartVersion: version,
      idempotencyKey: randomUUID(),
      shipping: {
        recipientName: "گیرندهٔ نمایشی توبا",
        contactMobile: "+989120000014",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "خیابان نمونه، پلاک ۱۴",
        postalCode: "19199",
        savedAddressId: ADDRESS,
      },
    },
  });
  const checkoutId = pick(submit.json, "checkoutId", "CheckoutId");
  const sellerOrders = pick(submit.json, "sellerOrders", "SellerOrders") || [];
  const payable = pick(submit.json, "payableAmount", "PayableAmount");
  const quote = await req(
    "GET",
    `/v1/storefront/checkout/${checkoutId}/wallet-quote?cartId=${encodeURIComponent(cartId)}`,
    { headers: { ...cartHeaders, "X-Tooba-Dev-Actor-User-Id": CUSTOMER } },
  );

  return {
    cartId,
    guestSecret,
    checkoutId,
    payable,
    sellerOrderId: pick(sellerOrders[0] || {}, "sellerOrderId", "SellerOrderId"),
    quote: quote.json,
    confirmationUrl: confirmationUrl({ checkoutId, cartId, guestSecret }),
    submitStatus: submit.status,
    quoteStatus: quote.status,
  };
}

async function payWithWallet(pending) {
  const pay = await req("POST", `/v1/storefront/checkout/${pending.checkoutId}/payments`, {
    headers: {
      "X-Tooba-Guest-Secret": pending.guestSecret,
      "X-Tooba-Dev-Actor-User-Id": CUSTOMER,
    },
    body: {
      cartId: pending.cartId,
      idempotencyKey: randomUUID(),
      providerCode: "wallet",
      useWallet: true,
    },
  });
  return {
    status: pay.status,
    paymentId: pick(pay.json, "paymentId", "PaymentId"),
    providerCode: pick(pay.json, "providerCode", "ProviderCode"),
    requiresPspRedirect: pick(pay.json, "requiresPspRedirect", "RequiresPspRedirect"),
    paymentStatus: pick(pay.json, "status", "Status"),
    orderUrl: `${FE}/customer-panel/orders/${pending.checkoutId}`,
  };
}

async function deliverAndRefund(pending, payment) {
  await new Promise((r) => setTimeout(r, 2000));
  const sellerHeaders = {
    "X-Tooba-Dev-Actor-User-Id": SELLER,
    "X-Tooba-Seller-Party-Id": SELLER_PARTY,
  };
  let fulfillment = null;
  for (let i = 0; i < 12 && !fulfillment; i++) {
    const list = await req("GET", `/v1/customer/orders/${pending.checkoutId}/fulfillments`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    });
    const rows = Array.isArray(list.json) ? list.json : pick(list.json, "items", "Items", "value") || [];
    fulfillment = rows[0] || null;
    if (!fulfillment) await new Promise((r) => setTimeout(r, 400));
  }
  const fulfillmentId = pick(fulfillment || {}, "fulfillmentId", "FulfillmentId");
  const lineId = pick((pick(fulfillment || {}, "items", "Items") || [])[0] || {}, "orderLineId", "OrderLineId");
  if (!fulfillmentId || !lineId) {
    return { ok: false, error: "fulfillment missing", fulfillment };
  }

  await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/processing`, { headers: sellerHeaders, body: {} });
  await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/packed`, { headers: sellerHeaders, body: {} });
  const ship = await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments`, {
    headers: sellerHeaders,
    body: { carrierDisplayName: "Post T028-R1", items: [{ orderLineId: lineId, quantity: 1 }] },
  });
  const shipmentId = pick((pick(ship.json, "shipments", "Shipments") || [])[0] || {}, "shipmentId", "ShipmentId");
  await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/tracking`, {
    headers: sellerHeaders,
    body: { trackingReference: `TRK-R1-${Date.now()}` },
  });
  await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/dispatch`, {
    headers: sellerHeaders,
    body: {},
  });
  await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/deliver`, {
    headers: sellerHeaders,
    body: {},
  });

  const createRet = await req("POST", "/v1/customer/returns", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    body: {
      sellerOrderId: pending.sellerOrderId,
      idempotencyKey: randomUUID(),
      reason: "TB-P06-T028-R1 preview refund",
      destination: "Wallet",
      refundDestination: "Wallet",
      items: [{ orderLineId: lineId, quantity: 1 }],
    },
  });
  const returnRequestId = pick(createRet.json, "returnRequestId", "ReturnRequestId");
  const approve = await req("POST", `/v1/seller/returns/${returnRequestId}/approve`, {
    headers: sellerHeaders,
    body: { destination: "Wallet", refundDestination: "Wallet" },
  });
  const approve2 = await req("POST", `/v1/seller/returns/${returnRequestId}/approve`, {
    headers: sellerHeaders,
    body: { destination: "Wallet", refundDestination: "Wallet" },
  });

  return {
    ok: !!returnRequestId && (approve.status === 200 || approve.status === 201),
    fulfillmentId,
    lineId,
    returnRequestId,
    approveStatus: approve.status,
    approveRetryStatus: approve2.status,
    customerOrderUrl: `${FE}/customer-panel/orders/${pending.checkoutId}`,
    sellerReturnUrl: `${FE}/vendor-panel/returns/${returnRequestId}?sellerPartyId=${SELLER_PARTY}`,
    sellerReturnsListUrl: `${FE}/vendor-panel/returns?sellerPartyId=${SELLER_PARTY}`,
    walletUrl: `${FE}/customer-panel/wallet`,
    notificationInboxUrl: `${FE}/customer-panel/notifications`,
  };
}

async function main() {
  mkdirSync(outDir, { recursive: true });
  const wallet = await req("GET", "/v1/customer/wallet", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
  });
  const demo = await req("GET", "/v1/admin/wallet/demo-preview");

  const pending = await createPendingCheckout();
  const paid = await payWithWallet(pending);
  const refund = await deliverAndRefund(pending, paid);

  // Second pending checkout left unpaid for browser-open wallet pay proof
  const unpaid = await createPendingCheckout();

  const preview = {
    createdAt: new Date().toISOString(),
    directDbMutation: false,
    identity: {
      customerActorUserId: CUSTOMER,
      sellerActorUserId: SELLER,
      sellerPartyId: SELLER_PARTY,
      offerId: OFFER,
      addressId: ADDRESS,
    },
    walletBalanceBeforeScenario: pick(wallet.json, "balance", "Balance"),
    demoPreview: demo.json,
    unpaidCheckoutForBrowserPay: {
      ...unpaid,
      steps: [
        "Open confirmationUrl in browser (cartId/guestSecret/actor query bootstrap session).",
        "Confirm Wallet method selected when canPayFullyWithWallet.",
        "Submit pay — expect no PSP redirect.",
      ],
    },
    paidCheckoutScenario: {
      pending,
      payment: paid,
      refund,
    },
    urls: {
      customerCheckoutConfirmation: unpaid.confirmationUrl,
      customerWallet: `${FE}/customer-panel/wallet`,
      walletPaidOrder: `${FE}/customer-panel/orders/${pending.checkoutId}`,
      customerRefundEntry: `${FE}/customer-panel/orders/${pending.checkoutId}`,
      sellerRefundOperation: refund.sellerReturnUrl,
      walletAfterRefund: `${FE}/customer-panel/wallet`,
      notificationInbox: `${FE}/customer-panel/notifications`,
      shopeivaCheckout: `${SHOPEIVA}/payment`,
      shopeivaCart: `${SHOPEIVA}/cart`,
      shopeivaUserPanel: `${SHOPEIVA}/user-panel`,
      shopeivaGiftCards: `${SHOPEIVA}/user-panel/gift-cards`,
      shopeivaWalletClosest: `${SHOPEIVA}/user-panel/wallet`,
      shopeivaOrders: `${SHOPEIVA}/user-panel/orders`,
    },
  };

  writeFileSync(join(outDir, "preview-urls.json"), JSON.stringify(preview, null, 2), "utf8");
  console.log(
    JSON.stringify(
      {
        unpaidConfirmation: unpaid.confirmationUrl,
        paidOrder: preview.urls.walletPaidOrder,
        sellerReturn: refund.sellerReturnUrl,
        ok: unpaid.submitStatus === 200 && paid.status === 200 && refund.ok,
      },
      null,
      2,
    ),
  );
  process.exit(unpaid.submitStatus === 200 && paid.status === 200 && refund.ok ? 0 : 1);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
