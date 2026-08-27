const fs = require("fs");
const path = require("path");

const base = "http://127.0.0.1:5088";
const offerId = "01a0429e-e68d-7000-9139-65f062c1c15d";
const customer = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const sellerActor = "01a03628-3f68-7000-844d-99f1cadb54b0";
const sellerParty = "01a030d1-40cb-7000-8abe-6d31739956c5";
const outFile = path.join(__dirname, "e2e-notification-api.json");

async function req(method, url, { headers = {}, body } = {}) {
  const init = { method, headers: { Accept: "application/json", ...headers } };
  if (body !== undefined) {
    init.headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(body);
  }
  const res = await fetch(url, init);
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: res.status, json, text };
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

(async () => {
  const beforeC = await req("GET", `${base}/v1/customer/notifications/unread-count`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": customer },
  });
  const beforeS = await req("GET", `${base}/v1/seller/notifications/unread-count`, {
    headers: {
      "X-Tooba-Dev-Actor-User-Id": sellerActor,
      "X-Tooba-Seller-Party-Id": sellerParty,
    },
  });
  console.log("BEFORE", beforeC.json, beforeS.json);

  const cartRes = await req("POST", `${base}/v1/storefront/cart`, { body: {} });
  if (cartRes.status !== 200) throw new Error(`cart ${cartRes.status} ${cartRes.text}`);
  const { cartId, guestSecret, version: v0 } = cartRes.json;
  console.log("CART", cartId, "v", v0);

  const lineRes = await req("POST", `${base}/v1/storefront/cart/${cartId}/lines?expectedVersion=${v0}`, {
    headers: { "X-Tooba-Guest-Secret": guestSecret },
    body: { offerId, quantity: 1 },
  });
  if (lineRes.status !== 200) throw new Error(`line ${lineRes.status} ${lineRes.text}`);
  const version = lineRes.json.version;
  console.log("LINE ok version", version);

  const checkoutRes = await req("POST", `${base}/v1/storefront/checkout`, {
    headers: { "X-Tooba-Guest-Secret": guestSecret },
    body: {
      cartId,
      expectedCartVersion: version,
      idempotencyKey: `t023-notif-${Date.now()}`,
      shipping: {
        recipientName: "خریدار تست اعلان",
        contactMobile: "09121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "خیابان تست ۱۲",
        postalCode: "1234567890",
      },
    },
  });
  if (checkoutRes.status !== 200) throw new Error(`checkout ${checkoutRes.status} ${checkoutRes.text}`);
  const checkoutId = checkoutRes.json.checkoutId;
  console.log("CHECKOUT", checkoutId);

  const payRes = await req("POST", `${base}/v1/storefront/checkout/${checkoutId}/payments`, {
    headers: { "X-Tooba-Guest-Secret": guestSecret },
    body: { cartId, idempotencyKey: `t023-pay-${Date.now()}` },
  });
  if (payRes.status !== 200) throw new Error(`pay ${payRes.status} ${payRes.text}`);
  const { paymentId, attemptId, providerRequestReference } = payRes.json;
  console.log("PAY", paymentId, payRes.json.status);

  const doneRes = await req("POST", `${base}/v1/storefront/payments/${paymentId}/sandbox/complete`, {
    headers: { "X-Tooba-Guest-Secret": guestSecret },
    body: {
      cartId,
      attemptId,
      providerRequestReference,
      outcome: "success",
    },
  });
  if (doneRes.status !== 200) throw new Error(`complete ${doneRes.status} ${doneRes.text}`);
  console.log("COMPLETE", doneRes.json.status || doneRes.json);

  let customerPage = null;
  let sellerPage = null;
  let found = false;
  for (let i = 0; i < 30; i++) {
    await sleep(1000);
    const c = await req("GET", `${base}/v1/customer/notifications?take=10&locale=fa`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": customer },
    });
    const s = await req("GET", `${base}/v1/seller/notifications?take=10&locale=fa`, {
      headers: {
        "X-Tooba-Dev-Actor-User-Id": sellerActor,
        "X-Tooba-Seller-Party-Id": sellerParty,
      },
    });
    const cu = await req("GET", `${base}/v1/customer/notifications/unread-count`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": customer },
    });
    const su = await req("GET", `${base}/v1/seller/notifications/unread-count`, {
      headers: {
        "X-Tooba-Dev-Actor-User-Id": sellerActor,
        "X-Tooba-Seller-Party-Id": sellerParty,
      },
    });
    customerPage = c.json;
    sellerPage = s.json;
    console.log(
      `poll${i} cTotal=${customerPage?.totalCount} cUnread=${cu.json?.unreadCount} sTotal=${sellerPage?.totalCount} sUnread=${su.json?.unreadCount}`,
    );
    if ((customerPage?.totalCount ?? 0) > 0 || (sellerPage?.totalCount ?? 0) > 0) {
      found = true;
      break;
    }
  }

  let afterRead = null;
  if (found && customerPage?.items?.length) {
    const nid = customerPage.items[0].notificationId;
    await req("POST", `${base}/v1/customer/notifications/${nid}/read`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": customer },
    });
    afterRead = await req("GET", `${base}/v1/customer/notifications/unread-count`, {
      headers: { "X-Tooba-Dev-Actor-User-Id": customer },
    });
    console.log("AFTER_READ", nid, afterRead.json);
  }

  const payload = {
    at: new Date().toISOString(),
    checkoutId,
    paymentId,
    paymentStatus: doneRes.json?.status ?? doneRes.json,
    found,
    customer: customerPage,
    seller: sellerPage,
    afterRead: afterRead?.json ?? null,
  };
  fs.writeFileSync(outFile, JSON.stringify(payload, null, 2), "utf8");
  console.log("WROTE", outFile, "found=", found);
  process.exit(found ? 0 : 2);
})().catch((err) => {
  console.error(err);
  process.exit(1);
});
