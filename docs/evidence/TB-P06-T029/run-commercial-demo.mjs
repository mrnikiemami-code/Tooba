/**
 * TB-P06-T029 — commercial demo journey via Host APIs only (no DB mutation).
 * Covers: cart→checkout→wallet pay→fulfill→return Wallet→ticket smoke.
 */
import http from "node:http";
import { randomUUID } from "node:crypto";
import { writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const HOST = "127.0.0.1";
const PORT = 5088;
const FE = "http://localhost:3000";
const CUSTOMER = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const SELLER = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_PARTY = "01a030d1-40cb-7000-8abe-6d31739956c5";
const OFFER = "01a04402-7a75-7000-b614-6f093cb072ac";
const ADDRESS = "aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1";
const outDir = dirname(fileURLToPath(import.meta.url));

async function resolveAdminActor() {
  const ctx = await req("GET", "/v1/admin/dev-context");
  return pick(ctx.json, "actorUserId", "ActorUserId");
}

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
  for (const k of keys) if (obj && obj[k] != null) return obj[k];
  return undefined;
}

async function main() {
  mkdirSync(outDir, { recursive: true });
  const report = { createdAt: new Date().toISOString(), directDbMutation: false, steps: [] };

  const adminActor = await resolveAdminActor();
  report.adminActor = adminActor;
  if (adminActor) {
    const topUp = await req("POST", `/v1/admin/wallets/${CUSTOMER}/adjustments`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": adminActor },
      body: {
        amount: 1_000_000,
        direction: "Credit",
        reason: "TB-P06-T029 commercial preview top-up",
        idempotencyKey: randomUUID(),
      },
    });
    report.steps.push({ walletTopUp: topUp.status });
  }

  const wallet0 = await req("GET", "/v1/customer/wallet", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
  });
  report.walletBefore = pick(wallet0.json, "balance", "Balance");
  report.steps.push({ walletRead: wallet0.status });

  const cartCreate = await req("POST", "/v1/storefront/cart");
  const cartId = pick(cartCreate.json, "cartId", "CartId");
  const guestSecret = pick(cartCreate.json, "guestSecret", "GuestSecret");
  let version = pick(cartCreate.json, "version", "Version") ?? 1;
  const cartHeaders = { "X-Tooba-Guest-Secret": guestSecret };

  const add = await req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    headers: { ...cartHeaders, "X-Tooba-Cart-Version": String(version) },
    body: { offerId: OFFER, quantity: 1 },
  });
  version = pick(add.json, "version", "Version") ?? version + 1;
  report.steps.push({ addLine: add.status });

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
  const sellerOrderId = pick((pick(submit.json, "sellerOrders", "SellerOrders") || [])[0] || {}, "sellerOrderId", "SellerOrderId");
  const payable = pick(submit.json, "payableAmount", "PayableAmount");
  report.steps.push({ checkout: submit.status, checkoutId, payable });

  const quote = await req(
    "GET",
    `/v1/storefront/checkout/${checkoutId}/wallet-quote?cartId=${encodeURIComponent(cartId)}`,
    { headers: { ...cartHeaders, "X-Tooba-Dev-Actor-User-Id": CUSTOMER } },
  );
  report.quote = quote.json;
  report.steps.push({ walletQuote: quote.status });

  const pay = await req("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    headers: { ...cartHeaders, "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    body: { cartId, idempotencyKey: randomUUID(), providerCode: "wallet", useWallet: true },
  });
  report.payment = {
    status: pay.status,
    paymentId: pick(pay.json, "paymentId", "PaymentId"),
    providerCode: pick(pay.json, "providerCode", "ProviderCode"),
    requiresPspRedirect: pick(pay.json, "requiresPspRedirect", "RequiresPspRedirect"),
    paymentStatus: pick(pay.json, "status", "Status"),
  };
  report.steps.push({ pay: pay.status });

  const sellerHeaders = {
    "X-Tooba-Dev-Actor-User-Id": SELLER,
    "X-Tooba-Seller-Party-Id": SELLER_PARTY,
  };

  let fulfillment = null;
  for (let i = 0; i < 20 && !fulfillment; i++) {
    await new Promise((r) => setTimeout(r, 500));
    const list = await req("GET", `/v1/customer/orders/${checkoutId}/fulfillments`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    });
    const rows = Array.isArray(list.json) ? list.json : pick(list.json, "items", "Items", "value") || [];
    fulfillment = rows[0] || null;
  }
  const fulfillmentId = pick(fulfillment || {}, "fulfillmentId", "FulfillmentId");
  const lineId = pick((pick(fulfillment || {}, "items", "Items") || [])[0] || {}, "orderLineId", "OrderLineId");
  report.fulfillmentId = fulfillmentId;
  report.lineId = lineId;

  if (fulfillmentId && lineId) {
    await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/processing`, { headers: sellerHeaders, body: {} });
    await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/packed`, { headers: sellerHeaders, body: {} });
    const ship = await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments`, {
      headers: sellerHeaders,
      body: { carrierDisplayName: "Post T029", items: [{ orderLineId: lineId, quantity: 1 }] },
    });
    const shipmentId = pick((pick(ship.json, "shipments", "Shipments") || [])[0] || {}, "shipmentId", "ShipmentId");
    await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/tracking`, {
      headers: sellerHeaders,
      body: { trackingReference: `TRK-T029-${Date.now()}` },
    });
    await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/dispatch`, {
      headers: sellerHeaders,
      body: {},
    });
    await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/deliver`, {
      headers: sellerHeaders,
      body: {},
    });
    report.steps.push({ fulfillDeliver: true, shipmentId });

    const createRet = await req("POST", "/v1/customer/returns", {
      headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
      body: {
        sellerOrderId,
        idempotencyKey: randomUUID(),
        reason: "TB-P06-T029 commercial return",
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
    report.return = {
      createStatus: createRet.status,
      returnRequestId,
      approveStatus: approve.status,
      approveRetryStatus: approve2.status,
    };
    report.steps.push({ returnApprove: approve.status, retry: approve2.status });
  } else {
    report.steps.push({ fulfillDeliver: false, error: "fulfillment missing" });
  }

  const ticket = await req("POST", "/v1/customer/support/tickets", {
    headers: {
      "X-Tooba-Dev-Actor-User-Id": CUSTOMER,
      "Idempotency-Key": randomUUID(),
    },
    body: {
      subject: "TB-P06-T029 support smoke",
      category: "Order",
      priority: "Normal",
      body: "Commercial readiness support journey seed",
    },
  });
  report.ticket = {
    status: ticket.status,
    ticketId: pick(ticket.json, "ticketId", "TicketId", "id", "Id"),
    errorCode: pick(ticket.json, "errorCode", "ErrorCode"),
  };

  if (adminActor && report.ticket.ticketId) {
    const reply = await req("POST", `/v1/admin/support/tickets/${report.ticket.ticketId}/replies`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": adminActor },
      body: { body: "T029 admin reply", isInternalNote: false },
    });
    report.ticket.adminReplyStatus = reply.status;
  }

  const wallet1 = await req("GET", "/v1/customer/wallet", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
  });
  report.walletAfter = pick(wallet1.json, "balance", "Balance");

  report.urls = {
    home: `${FE}/fa`,
    products: `${FE}/fa/products`,
    cart: `${FE}/fa/cart`,
    checkout: `${FE}/fa/checkout`,
    blogs: `${FE}/fa/blogs`,
    order: `${FE}/customer-panel/orders/${checkoutId}`,
    wallet: `${FE}/customer-panel/wallet`,
    tickets: `${FE}/customer-panel/tickets`,
    notifications: `${FE}/customer-panel/notifications`,
    sellerOrder: `${FE}/vendor-panel/orders?sellerPartyId=${SELLER_PARTY}`,
    sellerReturn: report.return?.returnRequestId
      ? `${FE}/vendor-panel/returns/${report.return.returnRequestId}?sellerPartyId=${SELLER_PARTY}`
      : null,
    adminTickets: `${FE}/admin/tickets`,
  };

  report.ok =
    submit.status === 200 &&
    pay.status === 200 &&
    report.payment.requiresPspRedirect === false &&
    !!report.return?.returnRequestId &&
    (report.return.approveStatus === 200 || report.return.approveStatus === 201) &&
    report.return.approveRetryStatus === 400;

  writeFileSync(join(outDir, "commercial-demo.json"), JSON.stringify(report, null, 2), "utf8");
  console.log(JSON.stringify({ ok: report.ok, checkoutId, payment: report.payment, ret: report.return, ticket: report.ticket }, null, 2));
  process.exit(report.ok ? 0 : 1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
