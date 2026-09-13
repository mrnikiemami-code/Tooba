import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const MOBILE = "09111111111";
const OTP = "123456";
const results = {};

function rec(name, ok, detail) {
  results[name] = { ok: !!ok, detail };
  console.log(`${ok ? "PASS" : "FAIL"} ${name}: ${detail}`);
}

function pick(obj, ...names) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const n of names) if (obj[n] !== undefined) return obj[n];
  return undefined;
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

async function req(method, path, { body, extra } = {}) {
  const headers = { Host: "alpha.localhost", Accept: "application/json", ...(extra ?? {}) };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const res = await fetch(HOST + path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: res.status, json, text };
}

function bearer(token) {
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function otpLogin() {
  const request = await req("POST", "/v1/auth/otp-login/request", { body: { identifier: MOBILE } });
  const challengeId = pick(request.json, "challengeId", "ChallengeId");
  const complete = await req("POST", "/v1/auth/otp-login/complete", {
    body: { identifier: MOBILE, challengeId, secret: OTP },
  });
  return {
    token: pick(complete.json, "accessToken", "AccessToken"),
    userId: pick(complete.json, "userId", "UserId"),
    status: complete.status,
    text: complete.text,
  };
}

try {
  rec("host-health", (await req("GET", "/health")).status === 200, "5088");
  rec("fe", (await fetch(`${FE}/fa/shipping`)).status === 200, "3000 /fa/shipping");

  const login = await otpLogin();
  rec("A-login", login.status === 200 && !!login.token, `${login.status} ${login.userId || login.text.slice(0, 120)}`);
  if (!login.token) throw new Error("login failed");
  const auth = bearer(login.token);
  const admin = { "X-Tooba-Dev-Actor-User-Id": ADMIN };

  const pending = sql(
    `select c.checkout_id from "order".checkouts c join "order".seller_orders s on s.checkout_id = c.checkout_id where c.placed_by_user_id = '${login.userId}' and s.status in ('PendingPayment','Submitted');`,
  );
  for (const id of pending.split("\n").map((x) => x.trim()).filter(Boolean)) {
    await req("POST", `/v1/storefront/checkout/${id}/cancel`, { extra: auth });
  }

  const me = await req("GET", "/v1/storefront/cart/current", { extra: auth });
  rec("B-cart-current", me.status === 200 || me.status === 404, `${me.status} items=${pick(me.json, "itemCount", "ItemCount") ?? "none"}`);

  const guest = await req("POST", "/v1/storefront/cart");
  const guestId = pick(guest.json, "cartId", "CartId");
  const secret = pick(guest.json, "guestSecret", "GuestSecret");
  let version = pick(guest.json, "version", "Version") ?? 0;
  const offerId = sql(
    `select o.offer_id::text from offer.offers o join inventory.stock_positions s on s.offer_id = o.offer_id where s.on_hand - s.reserved >= 1 limit 1;`,
  );
  const added = await req("POST", `/v1/storefront/cart/${guestId}/lines?expectedVersion=${version}`, {
    body: { offerId, quantity: 1 },
    extra: { "X-Tooba-Guest-Secret": secret },
  });
  version = pick(added.json, "version", "Version") ?? version;
  rec("B-guest-line", added.status === 200, `${added.status} offer=${offerId}`);

  const merged = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId: guestId },
    extra: { ...auth, "X-Tooba-Guest-Secret": secret },
  });
  const cartId = pick(merged.json, "cartId", "CartId") ?? guestId;
  version = pick(merged.json, "version", "Version") ?? version;
  const afterMerge = await req("GET", `/v1/storefront/cart/${cartId}`, { extra: auth });
  const lines = (pick(afterMerge.json, "lines", "Lines") ?? []).length;
  rec("B-cart-populated", merged.status === 200 && lines >= 1, `${merged.status} lines=${lines} cart=${cartId}`);

  const listed = await req("GET", "/v1/customer/addresses", { extra: auth });
  const addresses = Array.isArray(listed.json) ? listed.json : pick(listed.json, "items", "Items") ?? [];
  let legacy = addresses.find((a) =>
    (pick(a, "recipientName", "RecipientName") === "محمد لمامی")
    && !pick(a, "firstName", "FirstName")
    && !pick(a, "lastName", "LastName"),
  );
  if (!legacy) {
    const created = await req("POST", "/v1/customer/addresses", {
      extra: auth,
      body: {
        recipientName: "محمد لمامی",
        contactMobile: "09121111444",
        country: "IR",
        provinceName: "تهران",
        cityName: "تهران",
        postalCode: "1111111111",
        postalAddress: "نشانی قدیمی لمامی",
        label: "legacy-r24r1r1",
        isDefault: false,
      },
    });
    rec("D-legacy-create", created.status === 201 || created.status === 200, `${created.status}`);
    legacy = created.json;
  } else {
    rec("D-legacy-create", true, "existing محمد لمامی");
  }
  const savedAddressId = pick(legacy, "addressId", "AddressId");
  rec("D-legacy-readable", !!savedAddressId && pick(legacy, "recipientName", "RecipientName") === "محمد لمامی", `${savedAddressId}`);

  const proj = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    extra: auth,
  });
  const min = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  rec("C-shipping-proj", proj.status === 200 && !!min, `${proj.status} min=${min}`);

  const save = await req("PUT", "/v1/storefront/shipping/selection", {
    extra: auth,
    body: {
      cartId,
      expectedCartVersion: version,
      recipientName: "محمد لمامی",
      firstName: "محمد",
      lastName: "امامی",
      contactMobile: pick(legacy, "contactMobile", "ContactMobile") || "09121111444",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: pick(legacy, "postalAddress", "PostalAddress") || "نشانی قدیمی لمامی",
      postalCode: pick(legacy, "postalCode", "PostalCode") || "1111111111",
      savedAddressId,
      shippingMethodCode: "post:express",
      selectedDeliveryDate: min,
      selectedDeliveryTimeWindow: "9-12",
    },
  });
  version = pick(save.json, "cartVersion", "CartVersion") ?? version;
  const draftName = pick(save.json, "recipientName", "RecipientName");
  const draftFirst = pick(save.json, "firstName", "FirstName");
  const draftLast = pick(save.json, "lastName", "LastName");
  rec(
    "E-explicit-save",
    save.status === 200 && draftName === "محمد امامی" && draftFirst === "محمد" && draftLast === "امامی",
    `${save.status} ${draftName} ${draftFirst} ${draftLast} ${save.text.slice(0, 180)}`,
  );

  const commit = await req("POST", "/v1/storefront/shipping/commit", {
    extra: auth,
    body: { cartId, expectedCartVersion: version, idempotencyKey: crypto.randomUUID() },
  });
  const checkoutId = pick(commit.json, "checkoutId", "CheckoutId");
  const payName = pick(commit.json, "recipientName", "RecipientName");
  const payFirst = pick(commit.json, "firstName", "FirstName");
  const payLast = pick(commit.json, "lastName", "LastName");
  rec(
    "F-commit",
    commit.status === 200 && !!checkoutId,
    `${commit.status} ${checkoutId} ${commit.text.slice(0, 160)}`,
  );
  rec(
    "G-payment-summary",
    payName === "محمد امامی" && payFirst === "محمد" && payLast === "امامی" && payName !== "محمد لمامی",
    `name=${payName} first=${payFirst} last=${payLast}`,
  );

  const page = await req("GET", `/v1/storefront/checkout/${checkoutId}?cartId=${cartId}`, { extra: auth });
  rec(
    "G-payment-get",
    pick(page.json, "recipientName", "RecipientName") === "محمد امامی",
    `${page.status} ${pick(page.json, "recipientName", "RecipientName")}`,
  );

  const customer = await req("GET", `/v1/customer/orders/${checkoutId}`, { extra: auth });
  rec(
    "H-customer-order",
    pick(customer.json, "recipientName", "RecipientName") === "محمد امامی",
    `${customer.status} ${pick(customer.json, "recipientName", "RecipientName")}`,
  );

  const adminOrder = await req("GET", `/v1/admin/orders/${checkoutId}`, { extra: admin });
  rec(
    "I-admin-order",
    pick(adminOrder.json, "recipientName", "RecipientName") === "محمد امامی",
    `${adminOrder.status} ${pick(adminOrder.json, "recipientName", "RecipientName")} ${adminOrder.text.slice(0, 120)}`,
  );

  const snap = sql(
    `select recipient_name||'|'||coalesce(recipient_first_name,'')||'|'||coalesce(recipient_last_name,'') from "order".checkouts where checkout_id = '${checkoutId}';`,
  );
  rec("snapshot-independent", snap === "محمد امامی|محمد|امامی", snap);

  const historical = sql(
    `select checkout_id||'|'||recipient_name from "order".checkouts where coalesce(recipient_first_name,'') = '' and coalesce(recipient_last_name,'') = '' and recipient_name <> '' order by submitted_at desc limit 1;`,
  );
  if (historical.includes("|")) {
    const [histId, histName] = historical.split("|");
    const histAdmin = await req("GET", `/v1/admin/orders/${histId}`, { extra: admin });
    rec(
      "J-historical-fallback",
      pick(histAdmin.json, "recipientName", "RecipientName") === histName && histName.length > 0,
      `${histId} ${pick(histAdmin.json, "recipientName", "RecipientName")}`,
    );
  } else {
    const untouched = await req("GET", `/v1/customer/addresses/${savedAddressId}`, { extra: auth });
    rec(
      "J-historical-fallback",
      pick(untouched.json, "recipientName", "RecipientName") === "محمد لمامی"
        && !pick(untouched.json, "firstName", "FirstName")
        && !pick(untouched.json, "lastName", "LastName"),
      `address ${savedAddressId} still محمد لمامی`,
    );
  }

  const current = await req("GET", "/v1/storefront/cart/current", { extra: auth });
  rec(
    "K-cart-after-commit",
    current.status === 404 || Number(pick(current.json, "itemCount", "ItemCount") ?? 0) === 0,
    `${current.status} ${pick(current.json, "itemCount", "ItemCount")}`,
  );
  rec("K-header-fa", (await fetch(`${FE}/fa`)).status === 200, "home 200");
  rec("K-header-en", (await fetch(`${FE}/en`)).status === 200, "en 200");

  const failed = Object.entries(results).filter(([, v]) => !v.ok);
  writeFileSync(
    "docs/evidence/TB-P10-T004/_r24-r1-r1-runtime-raw.json",
    JSON.stringify({ checkoutId, cartId, savedAddressId, results }, null, 2),
  );
  if (failed.length) {
    console.error("FAILED", failed);
    process.exit(1);
  }
  console.log("ALL_RUNTIME_API_PASS", checkoutId);
} catch (error) {
  console.error(error);
  writeFileSync(
    "docs/evidence/TB-P10-T004/_r24-r1-r1-runtime-raw.json",
    JSON.stringify({ error: String(error), results }, null, 2),
  );
  process.exit(1);
}
