/**
 * TB-SHIPPING-METHOD-COVERAGE-BROWSER-174 — authenticated customer strict-quote legs on the private
 * isolated Host (127.0.0.1:5428) against the disposable scratch PostgreSQL (127.0.0.1:55494).
 *
 * The Admin coverage writes are performed by the REAL Admin browser session (screenshots + DOM), not
 * by this script. This script only proves what the authenticated customer sees for the same cart:
 *
 *   baseline    : no declaration -> both real options priced at destination 1.
 *   restricted  : standard Restricted + own grant for destination 1 -> destination 1 keeps standard,
 *                 destination 2 (fully rated, NOT granted) excludes standard while express is
 *                 unchanged at its own amount; an unsupported selection is rejected 4xx and the
 *                 previously accepted draft is unchanged.
 *   unrestricted: standard restored -> destination 2 prices standard again; the historical grant is
 *                 retained and never inherited by the sibling.
 *
 * Usage: node acceptance-174.js baseline|restricted|unrestricted
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("node:child_process");

const PHASE = (process.argv[2] || "baseline").toLowerCase();
const BASE = process.env.TOOBA_174_HOST || "http://127.0.0.1:5428";
const EVIDENCE = __dirname;
const OUT_DIR = path.join(process.cwd(), ".tmp-174-run");
// Fixture credentials are supplied by the environment so that no OTP/password is committed.
// The run used a disposable Development scratch fixture that no longer exists.
const MOBILE = process.env.TOOBA_174_CUSTOMER_MOBILE || "";
const OTP = process.env.TOOBA_174_CUSTOMER_OTP || "";
const PASSWORD = process.env.TOOBA_174_CUSTOMER_PASSWORD || "";
if (!MOBILE || !OTP || !PASSWORD) {
    console.error("ASSERT_FAIL: TOOBA_174_CUSTOMER_MOBILE / _OTP / _PASSWORD must come from the environment");
    process.exit(2);
}
const SERVICE = "tipax";
const STANDARD = "standard";
const EXPRESS = "express";
const D1 = {
    countryId: "11111111-1111-1111-1111-111111111111",
    administrativeAreaId: "22222222-2222-2222-2222-222222222222",
    cityId: "33333333-3333-3333-3333-333333333333",
    provinceName: "تهران",
    cityName: "تهران",
};
const D2 = {
    countryId: "11111111-1111-1111-1111-111111111111",
    administrativeAreaId: "55555555-5555-5555-5555-555555555555",
    cityId: "66666666-6666-6666-6666-666666666666",
    provinceName: "البرز",
    cityName: "کرج",
};

const checks = [];
const trace = [];
const record = (id, ok, detail = "") => {
    checks.push({ id, ok, detail });
    console.log(`${ok ? "PASS" : "FAIL"} ${id} ${detail}`);
};
const prop = (o, ...names) => {
    if (!o || typeof o !== "object") return undefined;
    for (const n of names) if (o[n] !== undefined && o[n] !== null) return o[n];
    return undefined;
};
const unwrap = (j) => (j && typeof j === "object" && j.data !== undefined ? j.data : j);
const errorCode = (p) => (p && typeof p === "object" ? p.errorCode || p.code || (p.error && p.error.code) || null : null);

async function call(name, url, init) {
    const res = await fetch(`${BASE}${url}`, init);
    const text = await res.text();
    let json = null;
    try { json = text ? JSON.parse(text) : null; } catch { }
    trace.push(`${name} ${url} -> ${res.status} :: ${text.slice(0, 900).replace(/\s+/g, " ")}`);
    return { status: res.status, json, text };
}

function psql(sql) {
    const container = process.env.TOOBA_174_CONTAINER || "tooba-tb174-pg";
    const user = process.env.TOOBA_174_USER || "scratch174";
    const database = process.env.TOOBA_174_DATABASE || "tooba_174_scratch";
    const password = process.env.TOOBA_174_SCRATCH_PASSWORD || "";
    return execFileSync(
        "docker",
        ["exec", "-e", `PGPASSWORD=${password}`, container, "psql", "-U", user, "-d", database, "-tAc", sql],
        { encoding: "utf8" }).trim();
}

function methodList(projection) {
    return (prop(projection, "methods") || []).map((m) => ({
        code: prop(m, "methodCode"),
        price: prop(m, "priceAmount"),
        leadDays: prop(m, "leadDays"),
        isFree: prop(m, "isFree"),
        serviceCode: prop(m, "serviceCode"),
        optionCode: prop(m, "optionCode"),
    }));
}

const projectionBody = (cartId, d) => ({
    cartId,
    provinceName: d.provinceName,
    cityName: d.cityName,
    destination: { countryId: d.countryId, administrativeAreaId: d.administrativeAreaId, cityId: d.cityId },
    methodCode: null,
    language: "fa",
});

async function project(auth, cartId, d, label) {
    const res = await call(`PROJECTION_${label}`, "/v1/storefront/shipping/projection", {
        method: "POST",
        headers: { ...auth, "Content-Type": "application/json" },
        body: JSON.stringify(projectionBody(cartId, d)),
    });
    const body = unwrap(res.json) || {};
    return { res, body, methods: methodList(body) };
}

const findOption = (methods, option) => methods.find((m) => m.optionCode === option || m.code === `${SERVICE}:${option}`);

async function login() {
    await call("REGISTER", "/v1/auth/register", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ identifierKind: "Phone", identifier: MOBILE, password: PASSWORD }),
    });
    const req = await call("OTP_REQUEST", "/v1/auth/otp-login/request", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ identifier: MOBILE }),
    });
    const challengeId = prop(unwrap(req.json) || {}, "challengeId");
    const done = await call("OTP_COMPLETE", "/v1/auth/otp-login/complete", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ identifier: MOBILE, challengeId, secret: OTP }),
    });
    const accessToken = prop(unwrap(done.json) || {}, "accessToken");
    record("authenticated_customer_session", done.status === 200 && !!accessToken, `http=${done.status}`);
    return { Authorization: `Bearer ${accessToken}`, Accept: "application/json" };
}

function saveSession(data) {
    fs.mkdirSync(OUT_DIR, { recursive: true });
    fs.writeFileSync(path.join(OUT_DIR, "session-174.json"), JSON.stringify(data, null, 2));
}
function readSession() {
    return JSON.parse(fs.readFileSync(path.join(OUT_DIR, "session-174.json"), "utf8"));
}

(async () => {
    const auth = await login();
    const token = auth.Authorization.replace("Bearer ", "");

    if (PHASE === "baseline") {
        const sellers = psql("select seller_party_id||'|'||offer_id from offer.offers order by seller_party_id, offer_id")
            .split("\n").filter(Boolean).map((l) => l.split("|"));
        const seller = sellers[0][0];
        const offerIds = sellers.filter((s) => s[0] === seller).slice(0, 2).map((s) => s[1]);
        record("seeded_single_seller_offers_read", offerIds.length === 2, `seller=${seller} offers=${offerIds.join(",")}`);

        const created = await call("CART_CREATE", "/v1/storefront/cart", {
            method: "POST", headers: { "Content-Type": "application/json" }, body: "{}",
        });
        const guest = unwrap(created.json) || {};
        const guestCartId = prop(guest, "cartId");
        const guestSecret = prop(guest, "guestSecret");
        let guestVersion = prop(guest, "version");
        for (const offerId of offerIds) {
            const res = await call("CART_LINE", `/v1/storefront/cart/${guestCartId}/lines`, {
                method: "POST",
                headers: { "Content-Type": "application/json", "X-Tooba-Guest-Secret": guestSecret, "X-Tooba-Cart-Version": String(guestVersion) },
                body: JSON.stringify({ offerId, quantity: 1 }),
            });
            if (res.status >= 400) throw new Error(`cart line failed for ${offerId}: ${res.status} ${res.text.slice(0, 300)}`);
            guestVersion = prop(unwrap(res.json), "version") ?? guestVersion;
        }
        await call("CART_MERGE", "/v1/storefront/cart/merge", {
            method: "POST",
            headers: { ...auth, "Content-Type": "application/json", "X-Tooba-Guest-Secret": guestSecret },
            body: JSON.stringify({ cartId: guestCartId }),
        });
        const currentRes = await call("CART_CURRENT", "/v1/storefront/cart/current", { headers: auth });
        const cart = unwrap(currentRes.json) || {};
        const cartId = prop(cart, "cartId");
        const lineCount = (prop(cart, "lines") || []).length;
        record("authenticated_single_seller_cart_ready", currentRes.status === 200 && !!cartId && lineCount === 2,
            `cartId=${cartId} lines=${lineCount}`);

        const d1 = await project(auth, cartId, D1, "BASELINE_D1");
        const std1 = findOption(d1.methods, STANDARD);
        const exp1 = findOption(d1.methods, EXPRESS);
        record("baseline_d1_both_owned_options_priced",
            d1.res.status === 200 && !!std1 && std1.price === 205000 && !!exp1 && exp1.price === 480000,
            `standard=${std1 && std1.price} express=${exp1 && exp1.price}`);

        const d2 = await project(auth, cartId, D2, "BASELINE_D2");
        const std2 = findOption(d2.methods, STANDARD);
        const exp2 = findOption(d2.methods, EXPRESS);
        record("baseline_d2_both_owned_options_priced",
            d2.res.status === 200 && !!std2 && std2.price === 210000 && !!exp2 && exp2.price === 490000,
            `standard=${std2 && std2.price} express=${exp2 && exp2.price}`);

        // Accept ONE real selection first (express at destination 1) so later rejected writes can be
        // proven to leave the accepted draft untouched.
        const horizon = (prop(d1.body, "shippingGroupHorizons") || []).find((h) => prop(h, "methodCode") === `tipax:express`);
        const date = prop(horizon, "minimumDeliveryDate") || prop(d1.body, "minimumDeliveryDate");
        const windows = prop(horizon, "eligibleTimeWindows") || prop(d1.body, "deliveryTimeWindows") || [];
        const window = prop(windows[0], "value", "Value") || prop(windows[0], "code");
        record("server_returned_schedule_available", !!date && !!window, `date=${date} window=${window}`);
        const selection = await call("SAVE_EXPRESS_SELECTION", "/v1/storefront/shipping/selection", {
            method: "PUT", headers: { ...auth, "Content-Type": "application/json" },
            body: JSON.stringify({
                cartId,
                expectedCartVersion: prop(d1.body, "cartVersion"),
                recipientName: "مشتری آزمون ۱۷۴",
                contactMobile: MOBILE,
                provinceName: D1.provinceName,
                cityName: D1.cityName,
                postalAddress: "خیابان آزمون، پلاک ۱۷۴",
                postalCode: "1234567890",
                savedAddressId: null,
                shippingMethodCode: `tipax:express`,
                selectedDeliveryDate: date,
                selectedDeliveryTimeWindow: window,
                customerNote: null,
                firstName: "مشتری",
                lastName: "آزمون",
            }),
        });
        record("accepted_selection_saved", selection.status >= 200 && selection.status < 300,
            `http=${selection.status} errorCode=${errorCode(selection.json)}`);
        const reread = await project(auth, cartId, D1, "BASELINE_SELECTED");
        record("accepted_draft_persisted",
            prop(reread.body, "selectedMethodCode") === `tipax:express` && prop(reread.body, "selectedShippingAmount") === 480000,
            `selected=${prop(reread.body, "selectedMethodCode")} amount=${prop(reread.body, "selectedShippingAmount")}`);

        saveSession({ cartId, token, seller, offerIds, date, window });
        fs.writeFileSync(path.join(EVIDENCE, "projection-baseline-174.json"), JSON.stringify({
            d1: { methods: d1.methods, selected: prop(reread.body, "selectedMethodCode"), amount: prop(reread.body, "selectedShippingAmount") },
            d2: { methods: d2.methods },
        }, null, 2) + "\n");
    } else {
        const session = readSession();
        const cartId = session.cartId;
        const d1 = await project(auth, cartId, D1, `${PHASE.toUpperCase()}_D1`);
        const d2 = await project(auth, cartId, D2, `${PHASE.toUpperCase()}_D2`);
        const std1 = findOption(d1.methods, STANDARD);
        const exp1 = findOption(d1.methods, EXPRESS);
        const std2 = findOption(d2.methods, STANDARD);
        const exp2 = findOption(d2.methods, EXPRESS);

        if (PHASE === "restricted") {
            record("restricted_d1_allowed_method_kept",
                d1.res.status === 200 && !!std1 && std1.price === 205000,
                `standard=${std1 && std1.price}`);
            record("restricted_d1_sibling_unchanged", !!exp1 && exp1.price === 480000, `express=${exp1 && exp1.price}`);
            record("restricted_d2_not_granted_method_excluded",
                d2.res.status === 200 && !std2,
                `present=${!!std2} visible=${d2.methods.map((m) => m.code).join(",")}`);
            record("restricted_d2_sibling_still_owned_priced",
                !!exp2 && exp2.price === 490000 && !d2.methods.some((m) => m.price === 0),
                `express=${exp2 && exp2.price} zeroPriced=${d2.methods.filter((m) => m.price === 0).map((m) => m.code).join(",") || "none"}`);
            record("exclusion_is_coverage_not_missing_rate",
                !std2 && psql(`select count(*) from shipping.rate_policies where service_code='tipax' and option_code='standard' and administrative_area_id='55555555-5555-5555-5555-555555555555' and is_active`) === "1",
                "rated destination 2 still hides standard while Restricted");

            // Rejected unsupported selection must not corrupt the accepted express draft.
            const bad = await call("INVALID_SELECTION_UNGRANTED_DESTINATION", "/v1/storefront/shipping/selection", {
                method: "PUT", headers: { ...auth, "Content-Type": "application/json" },
                body: JSON.stringify({
                    cartId,
                    expectedCartVersion: prop(d2.body, "cartVersion"),
                    recipientName: "مشتری آزمون ۱۷۴",
                    contactMobile: MOBILE,
                    provinceName: D2.provinceName,
                    cityName: D2.cityName,
                    postalAddress: "خیابان آزمون، پلاک ۱۷۴",
                    postalCode: "1234567890",
                    savedAddressId: null,
                    shippingMethodCode: `tipax:${STANDARD}`,
                    selectedDeliveryDate: session.date,
                    selectedDeliveryTimeWindow: session.window,
                    customerNote: null,
                    firstName: "مشتری",
                    lastName: "آزمون",
                }),
            });
            record("unsupported_method_selection_rejected", bad.status >= 400 && bad.status < 500,
                `http=${bad.status} errorCode=${errorCode(bad.json)}`);
            const after = await project(auth, cartId, D1, "RESTRICTED_AFTER_REJECT");
            record("prior_accepted_draft_unchanged",
                prop(after.body, "selectedMethodCode") === `tipax:express` && prop(after.body, "selectedShippingAmount") === 480000,
                `selected=${prop(after.body, "selectedMethodCode")} amount=${prop(after.body, "selectedShippingAmount")}`);
            record("declaration_and_grant_row_exactly_one",
                psql(`select count(*) from shipping.method_coverages where service_code='tipax' and option_code='standard'`) === "1"
                && psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='standard' and is_active`) === "1"
                && psql(`select count(*) from shipping.method_coverages where service_code='tipax' and option_code='express'`) === "0",
                `decl=${psql(`select count(*) from shipping.method_coverages where service_code='tipax'`)} own_grants=${psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='standard'`)} sibling_grants=${psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='express'`)}`);
            fs.writeFileSync(path.join(EVIDENCE, "projection-restricted-174.json"), JSON.stringify({
                d1: { methods: d1.methods },
                d2: { methods: d2.methods },
                afterReject: { selected: prop(after.body, "selectedMethodCode"), amount: prop(after.body, "selectedShippingAmount") },
            }, null, 2) + "\n");
        } else {
            record("unrestricted_d1_still_priced", !!std1 && std1.price === 205000, `standard=${std1 && std1.price}`);
            record("unrestricted_d2_method_priced_again", !!std2 && std2.price === 210000,
                `standard=${std2 && std2.price}`);
            record("unrestricted_d2_sibling_unchanged", !!exp2 && exp2.price === 490000, `express=${exp2 && exp2.price}`);
            record("historical_grant_retained_not_inherited",
                psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='standard'`) === "1"
                && psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='express'`) === "0",
                `own=${psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='standard'`)} sibling=${psql(`select count(*) from shipping.coverage_grants where service_code='tipax' and option_code='express'`)}`);
            fs.writeFileSync(path.join(EVIDENCE, "projection-unrestricted-174.json"), JSON.stringify({
                d1: { methods: d1.methods },
                d2: { methods: d2.methods },
                declaration: psql(`select mode from shipping.method_coverages where service_code='tipax' and option_code='standard'`),
            }, null, 2) + "\n");
        }
    }

    const passed = checks.filter((c) => c.ok).length;
    fs.writeFileSync(path.join(EVIDENCE, `assertion-matrix-${PHASE}-174.json`), JSON.stringify({
        generatedAtUtc: new Date().toISOString(), phase: PHASE, host: BASE,
        total: checks.length, passed, failed: checks.length - passed, checks,
    }, null, 2) + "\n");
    fs.writeFileSync(path.join(EVIDENCE, `http-trace-${PHASE}-174.txt`), trace.join("\n") + "\n");
    console.log(`PHASE=${PHASE} TOTAL=${checks.length} PASSED=${passed} FAILED=${checks.length - passed}`);
    if (passed !== checks.length) process.exitCode = 1;
})().catch((err) => {
    console.error("HARNESS_ERROR", err && err.stack);
    fs.writeFileSync(path.join(EVIDENCE, `http-trace-${PHASE}-174.txt`), trace.join("\n") + "\n");
    process.exitCode = 2;
});
