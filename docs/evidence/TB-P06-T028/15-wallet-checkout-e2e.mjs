import http from "node:http";
import { randomUUID } from "node:crypto";
import { writeFileSync } from "node:fs";

const HOST = "127.0.0.1";
const PORT = 5088;
const CUSTOMER = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const SELLER = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_PARTY = "01a030d1-40cb-7000-8abe-6d31739956c5";
const OFFER = "01a04402-7a75-7000-b614-6f093cb072ac"; // book demo ~350k + tax
const ADDRESS = "aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1";

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
          resolve({ status: res.statusCode, headers: res.headers, json, raw });
        });
      },
    );
    r.on("error", reject);
    if (payload) r.write(payload);
    r.end();
  });
}

function cookieJar(setCookie) {
  const list = Array.isArray(setCookie) ? setCookie : setCookie ? [setCookie] : [];
  return list.map((c) => c.split(";")[0]).join("; ");
}

function pick(obj, ...keys) {
  for (const k of keys) {
    if (obj && obj[k] != null) return obj[k];
  }
  return undefined;
}

const evidence = { steps: [] };
function step(name, data) {
  evidence.steps.push({ name, ...data });
  console.log(JSON.stringify({ name, status: data.status, ok: data.ok }, null, 0));
}

async function main() {
  const bal0 = await req("GET", "/v1/customer/wallet", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
  });
  step("wallet-balance-before", {
    status: bal0.status,
    ok: bal0.status === 200,
    balance: pick(bal0.json, "balance", "Balance"),
  });

  const cartCreate = await req("POST", "/v1/storefront/cart");
  const cartId = pick(cartCreate.json, "cartId", "CartId");
  const guestCookie = cookieJar(cartCreate.headers["set-cookie"]);
  const guestSecret =
    pick(cartCreate.json, "guestSecret", "GuestSecret") ||
    guestCookie
      .split(";")
      .map((p) => p.trim())
      .find((p) => p.startsWith("tooba_guest_secret="))
      ?.split("=")[1] ||
    null;
  const cartVersion0 = pick(cartCreate.json, "version", "Version") ?? 0;
  step("create-cart", {
    status: cartCreate.status,
    ok: !!cartId && !!guestSecret,
    cartId,
    cartVersion0,
    guestSecret: !!guestSecret,
  });

  const cartHeaders = {
    ...(guestCookie ? { Cookie: guestCookie } : {}),
    ...(guestSecret ? { "X-Tooba-Guest-Secret": guestSecret } : {}),
  };

  const add = await req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${cartVersion0}`, {
    headers: { ...cartHeaders, "X-Tooba-Cart-Version": String(cartVersion0) },
    body: { offerId: OFFER, quantity: 1 },
  });
  const version = pick(add.json, "version", "Version") ?? cartVersion0 + 1;
  step("add-line", {
    status: add.status,
    ok: add.status === 200 || add.status === 201,
    version,
    error: add.status >= 400 ? add.json : undefined,
  });

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
  const payable = pick(submit.json, "payableAmount", "PayableAmount", "grandTotal", "GrandTotal");
  step("submit-checkout", {
    status: submit.status,
    ok: !!checkoutId,
    checkoutId,
    payable,
    sellerOrderId: pick(sellerOrders[0] || {}, "sellerOrderId", "SellerOrderId"),
    orderStatus: pick(sellerOrders[0] || {}, "status", "Status"),
    error: submit.status >= 400 ? submit.json : undefined,
  });
  if (!checkoutId) {
    writeFileSync(new URL("./15-wallet-checkout-e2e.json", import.meta.url), JSON.stringify(evidence, null, 2));
    process.exit(1);
  }

  const quote = await req(
    "GET",
    `/v1/storefront/checkout/${checkoutId}/wallet-quote?cartId=${encodeURIComponent(cartId)}`,
    {
      headers: {
        ...cartHeaders,
        "X-Tooba-Dev-Actor-User-Id": CUSTOMER,
      },
    },
  );
  step("wallet-quote", {
    status: quote.status,
    ok: quote.status === 200 && pick(quote.json, "canPayFullyWithWallet", "CanPayFullyWithWallet") === true,
    quote: quote.json,
  });

  const pay = await req("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    headers: {
      ...cartHeaders,
      "X-Tooba-Dev-Actor-User-Id": CUSTOMER,
    },
    body: {
      cartId,
      idempotencyKey: randomUUID(),
      providerCode: "wallet",
      useWallet: true,
    },
  });
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  step("wallet-pay", {
    status: pay.status,
    ok: pay.status === 200 || pay.status === 201,
    paymentId,
    providerCode: pick(pay.json, "providerCode", "ProviderCode"),
    requiresPspRedirect: pick(pay.json, "requiresPspRedirect", "RequiresPspRedirect"),
    paymentStatus: pick(pay.json, "status", "Status"),
    error: pay.status >= 400 ? pay.json : undefined,
  });

  // Wait briefly for outbox Paid + fulfillment handoff
  await new Promise((r) => setTimeout(r, 2500));
  const checkoutAfter = await req(
    "GET",
    `/v1/storefront/checkout/${checkoutId}?cartId=${encodeURIComponent(cartId)}`,
    { headers: cartHeaders },
  );
  const ordersAfter = pick(checkoutAfter.json, "sellerOrders", "SellerOrders") || [];
  step("checkout-after-pay", {
    status: checkoutAfter.status,
    ok: Array.isArray(ordersAfter) && ordersAfter.some((o) => pick(o, "status", "Status") === "Paid"),
    statuses: ordersAfter.map((o) => pick(o, "status", "Status")),
  });

  const ledger = await req("GET", "/v1/customer/wallet/ledger?take=20", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
  });
  const items = pick(ledger.json, "items", "Items") || [];
  const debit = items.find(
    (x) =>
      String(pick(x, "type", "Type")).includes("OrderPaymentDebit") &&
      String(pick(x, "sourceId", "SourceId")).toLowerCase() === String(paymentId).toLowerCase(),
  );
  step("ledger-debit", {
    status: ledger.status,
    ok: !!debit,
    debit,
    balance: pick(
      (
        await req("GET", "/v1/customer/wallet", {
          headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
        })
      ).json,
      "balance",
      "Balance",
    ),
  });

  const sellerOrderId = pick(ordersAfter[0] || sellerOrders[0] || {}, "sellerOrderId", "SellerOrderId");
  const sellerHeaders = {
    "X-Tooba-Dev-Actor-User-Id": SELLER,
    "X-Tooba-Seller-Party-Id": SELLER_PARTY,
  };

  let fulfillment = null;
  for (let i = 0; i < 10 && !fulfillment; i++) {
    const list = await req("GET", `/v1/customer/orders/${checkoutId}/fulfillments`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    });
    const rows = Array.isArray(list.json) ? list.json : pick(list.json, "items", "Items", "value") || [];
    fulfillment = rows.find((f) => pick(f, "sellerOrderId", "SellerOrderId") === sellerOrderId) || rows[0] || null;
    if (!fulfillment) await new Promise((r) => setTimeout(r, 500));
  }
  const fulfillmentId = pick(fulfillment || {}, "fulfillmentId", "FulfillmentId");
  const fulfillmentItems = pick(fulfillment || {}, "items", "Items") || [];
  const lineId = pick(fulfillmentItems[0] || {}, "orderLineId", "OrderLineId");
  step("fulfillment-handoff", {
    status: fulfillment ? 200 : 404,
    ok: !!fulfillmentId && !!lineId,
    fulfillmentId,
    lineId,
    statusName: pick(fulfillment || {}, "status", "Status"),
  });

  if (fulfillmentId && lineId) {
    const processing = await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/processing`, {
      headers: sellerHeaders,
      body: {},
    });
    const packed = await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/packed`, {
      headers: sellerHeaders,
      body: {},
    });
    const ship = await req("POST", `/v1/seller/fulfillments/${fulfillmentId}/shipments`, {
      headers: sellerHeaders,
      body: {
        carrierDisplayName: "Post T028 Wallet",
        items: [{ orderLineId: lineId, quantity: 1 }],
      },
    });
    const shipments = pick(ship.json, "shipments", "Shipments") || [];
    const shipmentId = pick(shipments[0] || {}, "shipmentId", "ShipmentId");
    const tracking = await req(
      "POST",
      `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/tracking`,
      { headers: sellerHeaders, body: { trackingReference: `TRK-T028-${Date.now()}` } },
    );
    const dispatch = await req(
      "POST",
      `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/dispatch`,
      { headers: sellerHeaders, body: {} },
    );
    const deliver = await req(
      "POST",
      `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/deliver`,
      { headers: sellerHeaders, body: {} },
    );
    step("fulfillment-deliver", {
      status: deliver.status,
      ok: deliver.status === 200,
      processing: processing.status,
      packed: packed.status,
      ship: ship.status,
      tracking: tracking.status,
      dispatch: dispatch.status,
      shipmentId,
      error: deliver.status >= 400 ? deliver.json : undefined,
    });
  }

  const createRet = await req("POST", "/v1/customer/returns", {
    headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    body: {
      sellerOrderId,
      idempotencyKey: randomUUID(),
      reason: "TB-P06-T028 wallet refund e2e",
      destination: "Wallet",
      refundDestination: "Wallet",
      items: lineId ? [{ orderLineId: lineId, quantity: 1 }] : [],
    },
  });
  const returnRequestId = pick(createRet.json, "returnRequestId", "ReturnRequestId");
  step("create-return-wallet", {
    status: createRet.status,
    ok: !!returnRequestId,
    returnRequestId,
    destination: pick(createRet.json, "refundDestination", "RefundDestination", "destination", "Destination"),
    error: createRet.status >= 400 ? createRet.json : undefined,
  });

  if (returnRequestId) {
    const approve = await req("POST", `/v1/seller/returns/${returnRequestId}/approve`, {
      headers: sellerHeaders,
      body: { destination: "Wallet", refundDestination: "Wallet" },
    });
    step("approve-return-wallet", {
      status: approve.status,
      ok: approve.status === 200 || approve.status === 201,
      statusName: pick(approve.json, "status", "Status"),
      destination: pick(approve.json, "refundDestination", "RefundDestination"),
      error: approve.status >= 400 ? approve.json : undefined,
    });

    await new Promise((r) => setTimeout(r, 1500));
    const ledger2 = await req("GET", "/v1/customer/wallet/ledger?take=30", {
      headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    });
    const items2 = pick(ledger2.json, "items", "Items") || [];
    const credits = items2.filter(
      (x) =>
        String(pick(x, "type", "Type")).includes("RefundCredit") &&
        String(pick(x, "sourceId", "SourceId")).toLowerCase() === String(returnRequestId).toLowerCase(),
    );
    const approve2 = await req("POST", `/v1/seller/returns/${returnRequestId}/approve`, {
      headers: sellerHeaders,
      body: { destination: "Wallet", refundDestination: "Wallet" },
    });
    const ledger3 = await req("GET", "/v1/customer/wallet/ledger?take=30", {
      headers: { "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    });
    const items3 = pick(ledger3.json, "items", "Items") || [];
    const creditsAfter = items3.filter(
      (x) =>
        String(pick(x, "type", "Type")).includes("RefundCredit") &&
        String(pick(x, "sourceId", "SourceId")).toLowerCase() === String(returnRequestId).toLowerCase(),
    );
    step("refund-credit-once", {
      status: ledger2.status,
      ok: credits.length === 1 && creditsAfter.length === 1,
      credits: credits.length,
      creditsAfterRetry: creditsAfter.length,
      approveRetryStatus: approve2.status,
      creditAmount: pick(credits[0] || {}, "amount", "Amount"),
    });
  }

  step("providerCode-accepted-on-initiate", {
    status: pay.status,
    ok: pick(pay.json, "providerCode", "ProviderCode") === "wallet",
    note: "FE providerCode=wallet mapped via Host WantsWallet",
  });

  writeFileSync(new URL("./15-wallet-checkout-e2e.json", import.meta.url), JSON.stringify(evidence, null, 2));
  const failed = evidence.steps.filter((s) => s.ok === false);
  console.log(failed.length ? `FAILED ${failed.length}` : "ALL_OK");
  process.exit(failed.length ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
