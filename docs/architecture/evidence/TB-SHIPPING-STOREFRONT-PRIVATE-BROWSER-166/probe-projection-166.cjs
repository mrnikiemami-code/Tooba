/**
 * TB-166 — authenticated private-Host probe: build a two-seller cart and read the real shipping
 * projection (gate ON). Uses only product endpoints; no client-side price construction.
 */
const fs = require("fs");
const path = require("path");

const BASE = process.env.TOOBA_166_HOST || "http://127.0.0.1:5421";
const MOBILE = process.env.TOOBA_166_MOBILE || "09121660001";
const OTP = "123456";
const PASSWORD = "Passw0rd!2345";
const SELLER_A = "01a14803-3d1e-7000-a94b-84e8547bc0cc";
const SELLER_B = "01a14803-3fb8-7000-b139-26472635ae4e";
const OFFER_A = "01a14803-3d4a-7000-94d5-5db8871805b3";
const OFFER_B = "01a14803-3fbd-7000-8310-c9260fb93352";
const TRACE = [];

function prop(o, ...names) {
    if (!o || typeof o !== "object") return undefined;
    for (const n of names) if (o[n] !== undefined && o[n] !== null) return o[n];
    return undefined;
}
function unwrap(j) {
    if (j && typeof j === "object") {
        if (j.data !== undefined) return j.data;
        if (j.value !== undefined) return j.value;
    }
    return j;
}
async function call(name, url, init) {
    const res = await fetch(url, init);
    const text = await res.text();
    let json = null;
    try { json = text ? JSON.parse(text) : null; } catch { json = null; }
    TRACE.push({ name, status: res.status, body: text.slice(0, 900) });
    console.log(name, res.status);
    return { status: res.status, json, text };
}

(async () => {
    await call("REGISTER", `${BASE}/v1/auth/register`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ identifierKind: "Phone", identifier: MOBILE, password: PASSWORD }),
    });
    const req = await call("OTP_REQUEST", `${BASE}/v1/auth/otp-login/request`, {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ identifier: MOBILE }),
    });
    const challengeId = prop(unwrap(req.json) || {}, "challengeId");
    const done = await call("OTP_COMPLETE", `${BASE}/v1/auth/otp-login/complete`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ identifier: MOBILE, challengeId, secret: OTP }),
    });
    const accessToken = prop(unwrap(done.json) || {}, "accessToken");
    if (!accessToken) throw new Error("no access token");
    const auth = { Authorization: `Bearer ${accessToken}`, Accept: "application/json" };

    const created = await call("CART_CREATE", `${BASE}/v1/storefront/cart`, {
        method: "POST", headers: { "Content-Type": "application/json" }, body: "{}",
    });
    const guest = unwrap(created.json) || {};
    const guestCartId = prop(guest, "cartId");
    const guestSecret = prop(guest, "guestSecret");
    let guestVersion = prop(guest, "version");
    const gh = () => ({ "Content-Type": "application/json", "X-Tooba-Guest-Secret": guestSecret, "X-Tooba-Cart-Version": String(guestVersion) });
    for (const offerId of [OFFER_A, OFFER_B]) {
        const res = await call("CART_LINE", `${BASE}/v1/storefront/cart/${guestCartId}/lines`, {
            method: "POST", headers: gh(), body: JSON.stringify({ offerId, quantity: 1 }),
        });
        if (res.status >= 400) throw new Error(`cart line failed ${offerId}`);
        guestVersion = prop(unwrap(res.json), "version") ?? guestVersion;
    }
    await call("CART_MERGE", `${BASE}/v1/storefront/cart/merge`, {
        method: "POST", headers: { ...auth, "Content-Type": "application/json", "X-Tooba-Guest-Secret": guestSecret },
        body: JSON.stringify({ cartId: guestCartId }),
    });
    const current = await call("CART_CURRENT", `${BASE}/v1/storefront/cart/current`, { headers: auth });
    const cart = unwrap(current.json) || {};
    const lines = (prop(cart, "lines") || []).map((l) => ({
        lineId: String(prop(l, "lineId")).toLowerCase(),
        offerId: String(prop(l, "offerId") || "").toLowerCase(),
        sellerPartyId: String(prop(l, "sellerPartyId") || "").toLowerCase(),
    }));
    console.log("LINES", JSON.stringify(lines));
    const currentCart = unwrap((await call("CART_FOR_PROJECTION", `${BASE}/v1/storefront/cart/current`, { headers: auth })).json) || {};
    const cartIdForProjection = prop(currentCart, "cartId");
    const proj = await call("PROJECTION", `${BASE}/v1/storefront/shipping/projection`, {
        method: "POST",
        headers: { ...auth, "Content-Type": "application/json" },
        body: JSON.stringify({ cartId: cartIdForProjection, provinceName: "تهران", cityName: "تهران", destination: { countryId: "11111111-1111-1111-1111-111111111111", administrativeAreaId: "22222222-2222-2222-2222-222222222222", cityId: "33333333-3333-3333-3333-333333333333" }, methodCode: null, language: "fa" }),
    });
    const p = unwrap(proj.json) || {};
    const summary = {
        currency: prop(p, "currency"),
        methods: (prop(p, "methods") || []).map((m) => ({ code: prop(m, "methodCode"), price: prop(m, "priceAmount") })),
        unavailableMethods: prop(p, "unavailableMethods") || null,
        horizons: (prop(p, "shippingGroupHorizons") || []).map((h) => ({
            groupKey: prop(h, "groupKey"), seller: prop(h, "sellerPartyId"), method: prop(h, "methodCode"),
            minimum: prop(h, "minimumDeliveryDate"),
        })),
        sellers: [...new Set(lines.map((l) => l.sellerPartyId))],
        sellerCount: prop(p, "sellerCount"),
    };
    const dir = path.join(process.cwd(), "docs", "architecture", "evidence", "TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166");
    fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(path.join(dir, `projection-on-166.json`), JSON.stringify(summary, null, 2) + "\n");
    fs.writeFileSync(path.join(dir, `http-trace-on-166.txt`), TRACE.map((t) => `${t.name} ${t.status}\n${t.body}`).join("\n---\n"));
    console.log(JSON.stringify(summary, null, 2));
    // Session cookie value for the browser leg (opaque session id) is written only to a gitignored temp file.
    const refresh = prop(unwrap(done.json) || {}, "refreshToken");
    fs.writeFileSync(path.join(process.cwd(), ".tmp-166-session.json"), JSON.stringify({ accessToken, refreshToken: refresh ?? null, mobile: MOBILE }));
})().catch((e) => { console.error("FAIL", e.message); process.exit(1); });


