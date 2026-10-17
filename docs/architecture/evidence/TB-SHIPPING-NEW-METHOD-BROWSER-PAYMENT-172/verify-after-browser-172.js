/**
 * TB-172 — post-browser read-only verification for the same authenticated customer/cart.
 * Proves the state the browser produced is server-authoritative: projection, saved selection,
 * read-only payment draft (identity + arithmetic), and DB membership exactly once.
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("node:child_process");

const BASE = process.env.TOOBA_172_HOST || "http://127.0.0.1:5426";
const EVIDENCE = __dirname;
const MOBILE = "09111111111";
const CART_ID = process.env.TOOBA_172_CART_ID || "";
const METHOD = "tipax:scratch172";
const OWNED_AMOUNT = 185000;

const prop = (o, ...names) => {
    if (!o || typeof o !== "object") return undefined;
    for (const n of names) if (o[n] !== undefined && o[n] !== null) return o[n];
    return undefined;
};
const unwrap = (j) => (j && typeof j === "object" && j.data !== undefined ? j.data : j);
const checks = [];

function psql(sql) {
    return execFileSync("docker", ["exec", "-e", `PGPASSWORD=${process.env.TOOBA_172_SCRATCH_PASSWORD || ""}`,
        "tooba-tb172-pg", "psql", "-U", "scratch172", "-d", "tooba_172_scratch", "-tAc", sql], { encoding: "utf8" }).trim();
}

(async () => {
    const req = await (await fetch(`${BASE}/v1/auth/otp-login/request`, {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ identifier: MOBILE }),
    })).json();
    const challengeId = prop(unwrap(req) || {}, "challengeId");
    const done = await (await fetch(`${BASE}/v1/auth/otp-login/complete`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ identifier: MOBILE, challengeId, secret: "123456" }),
    })).json();
    const token = prop(unwrap(done) || {}, "accessToken");
    const auth = { Authorization: `Bearer ${token}`, Accept: "application/json" };

    const current = unwrap(await (await fetch(`${BASE}/v1/storefront/cart/current`, { headers: auth })).json()) || {};
    const cartId = CART_ID || prop(current, "cartId");

    const projection = unwrap(await (await fetch(`${BASE}/v1/storefront/shipping/projection`, {
        method: "POST", headers: { ...auth, "Content-Type": "application/json" },
        body: JSON.stringify({ cartId, provinceName: "تهران", cityName: "تهران", language: "fa" }),
    })).json()) || {};
    const methods = (prop(projection, "methods") || []).map((m) => ({
        code: prop(m, "methodCode"), price: prop(m, "priceAmount"), leadDays: prop(m, "leadDays"),
    }));
    const draftGroups = (prop(prop(projection, "draft") || {}, "shippingGroups") || []).map((g) => ({
        groupId: prop(g, "shippingGroupId"), method: prop(g, "methodCode"), amount: prop(g, "shippingAmount"),
        date: prop(g, "selectedDeliveryDate"), window: prop(g, "selectedDeliveryTimeWindow"),
    }));

    const payRes = await fetch(`${BASE}/v1/storefront/cart/${cartId}/payment-draft`, { headers: auth });
    const pay = unwrap(await payRes.json()) || {};
    const payGroups = (prop(pay, "shippingGroups") || []).map((g) => ({
        groupId: prop(g, "shippingGroupId"), method: prop(g, "methodCode"),
        amount: prop(g, "shippingAmount"), service: prop(g, "serviceCode"), option: prop(g, "optionCode"),
    }));

    const record = (id, ok, detail) => { checks.push({ id, ok, detail }); console.log(`${ok ? "PASS" : "FAIL"} ${id} ${detail}`); };
    const visible = methods.find((m) => m.code === METHOD);
    record("after_browser_new_method_still_quoted", !!visible && visible.price === OWNED_AMOUNT,
        `price=${visible && visible.price} leadDays=${visible && visible.leadDays}`);
    record("after_browser_selection_persisted",
        prop(projection, "selectedMethodCode") === METHOD && draftGroups.length === 1
        && draftGroups[0].amount === OWNED_AMOUNT && !!draftGroups[0].date && !!draftGroups[0].window,
        `selected=${prop(projection, "selectedMethodCode")} group=${JSON.stringify(draftGroups)}`);
    record("payment_draft_http_200", payRes.status === 200, `http=${payRes.status}`);
    record("payment_draft_method_and_group_identity",
        prop(pay, "shippingMethodCode") === METHOD && payGroups.length === 1
        && payGroups[0].groupId === draftGroups[0].groupId,
        `method=${prop(pay, "shippingMethodCode")} payGroup=${payGroups[0] && payGroups[0].groupId} draftGroup=${draftGroups[0] && draftGroups[0].groupId}`);
    const subtotal = Number(prop(pay, "subtotalExclusiveOfTax") || 0);
    const discount = Number(prop(pay, "discountAmount") || 0);
    const tax = Number(prop(pay, "taxAmount") || 0);
    const shipping = Number(prop(pay, "shippingAmount") || 0);
    const payable = Number(prop(pay, "payableAmount") || 0);
    record("payment_draft_arithmetic", payable === subtotal - discount + tax + shipping && shipping === OWNED_AMOUNT,
        `subtotal=${subtotal} tax=${tax} shipping=${shipping} payable=${payable}`);
    record("no_checkout_or_payment_created",
        psql('select count(*) from "order".checkouts') === "0" && psql("select count(*) from payment.payments") === "0",
        `checkouts=${psql('select count(*) from "order".checkouts')} payments=${psql("select count(*) from payment.payments")}`);

    const membership = psql(`select count(*) from "order".cart_shipping_groups g join "order".cart_shipping_group_lines l on l.shipping_group_id=g.shipping_group_id where g.cart_id='${cartId}' and g.method_code='${METHOD}'`);
    const groupCount = psql(`select count(*) from "order".cart_shipping_groups where cart_id='${cartId}' and method_code='${METHOD}'`);
    record("db_membership_once", membership === "2" && groupCount === "1", `lines_in_group=${membership} groups=${groupCount}`);

    const out = {
        cartId, methods, draftGroups, paymentDraft: {
            shippingMethodCode: prop(pay, "shippingMethodCode"), subtotal, discount, tax, shipping, payable,
            shippingGroups: payGroups,
        }, checks,
    };
    fs.writeFileSync(path.join(EVIDENCE, "http-after-browser-172.json"), JSON.stringify(out, null, 2) + "\n");
    fs.writeFileSync(path.join(EVIDENCE, "db-readback-172.txt"),
        `# scratch DB readback (disposable 127.0.0.1:55491 / tooba_172_scratch); credentials not recorded\n` +
        `CART=${cartId}\nGROUPS=${groupCount}\nLINES_IN_GROUP=${membership}\n` +
        `CHECKOUTS=${psql('select count(*) from "order".checkouts')}\nPAYMENTS=${psql("select count(*) from payment.payments")}\n` +
        `METHOD_ROWS=${psql(`select count(*) from fulfillment.shipping_methods where method_code='${METHOD}'`)}\n` +
        `LEGACY_OPTION_ROWS=${psql("select count(*) from fulfillment.shipping_service_options o join fulfillment.shipping_services s on s.shipping_service_id=o.shipping_service_id where s.code='tipax' and o.code='scratch172'")}\n`);
    const failed = checks.filter((c) => !c.ok).length;
    console.log(`TOTAL=${checks.length} FAILED=${failed}`);
    if (failed) process.exitCode = 1;
})().catch((e) => { console.error("VERIFY_ERROR", e.stack); process.exit(2); });
