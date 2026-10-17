/**
 * TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171 — real authenticated customer acceptance
 * on the private isolated Host (127.0.0.1:5425) and the disposable scratch PostgreSQL (127.0.0.1:55490).
 *
 * Phase "on"  : create -> activate -> owned rate -> coverage through the real owner Admin APIs, then
 *               authenticate a genuine customer and read the real storefront shipping projection twice
 *               (before/after financial readiness), save a selection with server-returned date/window,
 *               re-read it, and force the fail-closed negative control.
 * Phase "off" : same cart/session against a Host relaunched with Tooba:ShippingOwnedRateAuthority=false.
 *
 * Usage: node acceptance-171.js on|off
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("node:child_process");

const PHASE = (process.argv[2] || "on").toLowerCase();
const BASE = process.env.TOOBA_171_HOST || "http://127.0.0.1:5425";
const EVIDENCE = __dirname;
const OUT_DIR = path.join(process.cwd(), ".tmp-171-run");
const MOBILE = process.env.TOOBA_171_MOBILE || "09111111111";
const OTP = "123456";
const PASSWORD = "Passw0rd!2345";
const METHOD = "tipax:scratch171";
const SERVICE = "tipax";
const OPTION = "scratch171";
const OWNED_AMOUNT = 185000;
const OWNED_LEAD_DAYS = 3;
const DESTINATION = {
    countryId: "11111111-1111-1111-1111-111111111111",
    administrativeAreaId: "22222222-2222-2222-2222-222222222222",
    cityId: "33333333-3333-3333-3333-333333333333",
};
// The storefront selection step resolves the address by province/city NAMES, which stops at
// administrative-area level in this chain. The owned rate is therefore declared at
// country+administrative-area scope (a strictly wider, still real, owner scope) so the same rule
// matches both the explicit-destination projection and the name-based selection.
const RATE_SCOPE = {
    countryId: DESTINATION.countryId,
    administrativeAreaId: DESTINATION.administrativeAreaId,
};
const UNKNOWN_ACTOR = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";

const checks = [];
const trace = [];
const record = (id, ok, detail) => {
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
    const container = process.env.TOOBA_171_CONTAINER || "tooba-tb171-pg";
    const user = process.env.TOOBA_171_USER || "scratch171";
    const database = process.env.TOOBA_171_DATABASE || "tooba_171_scratch";
    const password = process.env.TOOBA_171_SCRATCH_PASSWORD || "";
    return execFileSync(
        "docker",
        ["exec", "-e", `PGPASSWORD=${password}`, container, "psql", "-U", user, "-d", database, "-tAc", sql],
        { encoding: "utf8" }).trim();
}

const adminHeaders = (actorId) => ({
    Accept: "application/json",
    "Accept-Language": "fa-IR",
    "X-Tooba-Dev-Actor-User-Id": actorId,
    "Content-Type": "application/json",
});

function methods(projection) {
    const list = prop(projection, "methods") || [];
    return list.map((m) => ({
        code: prop(m, "methodCode"),
        label: prop(m, "label"),
        price: prop(m, "priceAmount"),
        leadDays: prop(m, "leadDays"),
        isFree: prop(m, "isFree"),
        serviceCode: prop(m, "serviceCode"),
        optionCode: prop(m, "optionCode"),
    }));
}

const projectionBody = (cartId) => ({
    cartId,
    provinceName: "تهران",
    cityName: "تهران",
    destination: DESTINATION,
    methodCode: null,
    language: "fa",
});

async function project(auth, cartId, label) {
    const res = await call(`PROJECTION_${label}`, "/v1/storefront/shipping/projection", {
        method: "POST",
        headers: { ...auth, "Content-Type": "application/json" },
        body: JSON.stringify(projectionBody(cartId)),
    });
    const body = unwrap(res.json) || {};
    return { res, body, methods: methods(body) };
}

(async () => {
    // ---------- Admin owner pathway (phase ON only) ----------
    let actorUserId = null;
    let countryRateCreated = false;
    if (PHASE === "on") {
        const dev = await call("DEV_CONTEXT", "/v1/admin/dev-context", { headers: { Accept: "application/json" } });
        actorUserId = prop(unwrap(dev.json) || {}, "actorUserId");
        record("dev_context_available", dev.status === 200 && !!actorUserId, `http=${dev.status}`);

        await call("BOOTSTRAP", "/v1/admin/access-control/bootstrap", {
            method: "POST", headers: adminHeaders(actorUserId), body: "{}",
        });
        const caps = await call("CAPABILITIES", "/v1/admin/access-control/me/capabilities", { headers: adminHeaders(actorUserId) });
        const perms = ((unwrap(caps.json) || {}).permissions || []).map((p) => (typeof p === "string" ? p : prop(p, "permissionId")));
        record("fulfillment_manage_granted", perms.includes("fulfillment.manage"), `granted=${perms.length}`);

        const langs = await call("LANGUAGES", "/v1/admin/languages", { headers: adminHeaders(actorUserId) });
        const langRows = (unwrap(langs.json) || {}).items || unwrap(langs.json) || [];
        const fa = (Array.isArray(langRows) ? langRows : []).map((l) => ({ id: prop(l, "languageId", "id"), code: prop(l, "code") }))
            .find((l) => l.code === "fa-IR");
        record("real_language_id_read", langs.status === 200 && !!fa, `fa-IR=${fa && fa.id}`);

        // Baseline catalog rows (7 pre-existing methods).
        const before = await call("CATALOG_BEFORE", "/v1/admin/shipping-services/shipping-methods/one-level?language=fa-IR",
            { headers: adminHeaders(actorUserId) });
        const beforeRows = ((unwrap(before.json) || {}).methods) || [];
        record("baseline_catalog_seven_methods", before.status === 200 && beforeRows.length === 7,
            `rows=${beforeRows.length}`);

        // Create the new canonical method (unique option => no legacy option row exists).
        const create = await call("CREATE_METHOD", "/v1/admin/shipping-services/shipping-methods/one-level", {
            method: "POST", headers: adminHeaders(actorUserId),
            body: JSON.stringify({ methodCode: METHOD, providerKind: SERVICE, translations: [{ languageId: fa.id, name: "روش تازهٔ ۱۷۱" }] }),
        });
        record("canonical_method_created", create.status >= 200 && create.status < 300,
            `http=${create.status} errorCode=${errorCode(create.json)}`);

        const legacyOptionRows = psql(
            `select count(*) from fulfillment.shipping_service_options o join fulfillment.shipping_services s on s.shipping_service_id=o.shipping_service_id where s.code='${SERVICE}' and o.code='${OPTION}'`);
        const rateRowsBefore = psql(`select count(*) from shipping.rate_policies where service_code='${SERVICE}' and option_code='${OPTION}'`);
        record("no_fabricated_legacy_or_price_at_create", legacyOptionRows === "0" && rateRowsBefore === "0",
            `legacy_option_rows=${legacyOptionRows} rate_rows=${rateRowsBefore}`);

        // Activate through the supported Fulfillment endpoint.
        const activate = await call("ACTIVATE", `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(METHOD)}/activation`, {
            method: "PUT", headers: adminHeaders(actorUserId), body: JSON.stringify({ isActive: true }),
        });
        const actBody = unwrap(activate.json) || {};
        record("activation_applied_canonical_only",
            activate.status >= 200 && activate.status < 300
            && prop(actBody, "isActive") === true && prop(actBody, "changed") === true && prop(actBody, "canonicalOnly") === true,
            `http=${activate.status} isActive=${prop(actBody, "isActive")} canonicalOnly=${prop(actBody, "canonicalOnly")}`);

        const legacyAfterActivate = psql(
            `select count(*) from fulfillment.shipping_service_options o join fulfillment.shipping_services s on s.shipping_service_id=o.shipping_service_id where s.code='${SERVICE}' and o.code='${OPTION}'`);
        record("activation_created_no_legacy_option_row", legacyAfterActivate === "0", `rows=${legacyAfterActivate}`);
    }

    // ---------- Genuine authenticated customer + cart ----------
    const sellers = psql("select seller_party_id||'|'||offer_id from offer.offers order by seller_party_id, offer_id")
        .split("\n").filter(Boolean).map((l) => l.split("|"));
    const seller = sellers[0][0];
    const offerIds = sellers.filter((s) => s[0] === seller).slice(0, 2).map((s) => s[1]);
    record("seeded_single_seller_offers_read", offerIds.length === 2, `seller=${seller} offers=${offerIds.join(",")}`);

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
    const auth = { Authorization: `Bearer ${accessToken}`, Accept: "application/json" };

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
    const cartSellers = [...new Set((prop(cart, "lines") || []).map((l) => prop(l, "sellerPartyId")))];
    record("authenticated_single_seller_cart_ready", currentRes.status === 200 && !!cartId && lineCount === 2 && cartSellers.length === 1,
        `cartId=${cartId} lines=${lineCount} sellers=${cartSellers.length}`);

    // ---------- Storefront projection ----------
    const first = await project(auth, cartId, PHASE === "on" ? "ON_BASELINE" : "OFF");
    record("projection_http_ok", first.res.status === 200, `http=${first.res.status} methods=${first.methods.length}`);
    const newMethod = first.methods.find((m) => m.code === METHOD);

    if (PHASE === "on") {
        // Financial readiness is separate from activation: activated but unrated must NOT appear.
        record("activated_but_unrated_method_absent",
            first.methods.length > 0 && !newMethod,
            `present=${!!newMethod} visibleCodes=${first.methods.map((m) => m.code).join(",")}`);
        record("baseline_legacy_methods_priced_by_owner",
            first.methods.some((m) => m.code === "tipax:standard" && m.price === 205000)
            && first.methods.some((m) => m.code === "tipax:express" && m.price === 480000),
            `legacy=${JSON.stringify(first.methods.filter((m) => m.code.startsWith("tipax:")))}`);

        // Owner-written rate + explicit Unrestricted declaration (supported Admin APIs).
        const rate = await call("CREATE_RATE", `/v1/admin/shipping-services/${SERVICE}/rates`, {
            method: "POST", headers: adminHeaders(actorUserId),
            body: JSON.stringify({
                serviceCode: SERVICE,
                scope: RATE_SCOPE,
                chargeAmount: OWNED_AMOUNT,
                currencyCode: "IRR",
                preparationDays: 2,
                leadDays: OWNED_LEAD_DAYS,
                optionCode: OPTION,
            }),
        });
        record("owned_rate_created_for_new_method", rate.status >= 200 && rate.status < 300,
            `http=${rate.status} errorCode=${errorCode(rate.json)}`);

        const coverage = await call("SET_COVERAGE_UNRESTRICTED", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
            method: "PUT", headers: adminHeaders(actorUserId),
            body: JSON.stringify({ optionCode: OPTION, mode: 0 }),
        });
        record("coverage_declaration_unrestricted", coverage.status >= 200 && coverage.status < 300,
            `http=${coverage.status} errorCode=${errorCode(coverage.json)}`);

        const second = await project(auth, cartId, "ON_READY");
        const visible = second.methods.find((m) => m.code === METHOD);
        record("new_method_visible_at_owned_rate",
            second.res.status === 200 && !!visible
            && visible.price === OWNED_AMOUNT && visible.leadDays === OWNED_LEAD_DAYS && visible.isFree === false,
            `present=${!!visible} price=${visible && visible.price} leadDays=${visible && visible.leadDays} isFree=${visible && visible.isFree}`);
        record("new_method_identity_canonical",
            !!visible && visible.serviceCode === SERVICE && visible.optionCode === OPTION,
            `serviceCode=${visible && visible.serviceCode} optionCode=${visible && visible.optionCode}`);
        record("no_sibling_or_provider_amount_inherited",
            !!visible && visible.price === OWNED_AMOUNT && visible.price !== 205000 && visible.price !== 480000,
            `price=${visible && visible.price}`);

        // Selection with the server-returned date/window, then authoritative re-read.
        const horizon = (prop(second.body, "shippingGroupHorizons") || [])
            .find((h) => prop(h, "methodCode") === METHOD);
        const date = prop(horizon, "minimumDeliveryDate") || prop(second.body, "minimumDeliveryDate");
        const windows = prop(horizon, "eligibleTimeWindows") || prop(second.body, "deliveryTimeWindows") || [];
        const window = prop(windows[0], "value", "Value") || prop(windows[0], "code");
        record("server_returned_schedule_available", !!date && !!window, `date=${date} window=${window}`);

        const selection = await call("SAVE_SELECTION", "/v1/storefront/shipping/selection", {
            method: "PUT", headers: { ...auth, "Content-Type": "application/json" },
            body: JSON.stringify({
                cartId,
                expectedCartVersion: prop(second.body, "cartVersion"),
                recipientName: "مشتری آزمون ۱۷۱",
                contactMobile: MOBILE,
                provinceName: "تهران",
                cityName: "تهران",
                postalAddress: "خیابان آزمون، پلاک ۱۷۱",
                postalCode: "1234567890",
                savedAddressId: null,
                shippingMethodCode: METHOD,
                selectedDeliveryDate: date,
                selectedDeliveryTimeWindow: window,
                customerNote: null,
                firstName: "مشتری",
                lastName: "آزمون",
            }),
        });
        record("selection_saved_with_server_schedule", selection.status >= 200 && selection.status < 300,
            `http=${selection.status} errorCode=${errorCode(selection.json)}`);

        const reread = await project(auth, cartId, "ON_SELECTED");
        const selected = prop(reread.body, "selectedMethodCode");
        const selectedAmount = prop(reread.body, "selectedShippingAmount");
        record("selection_persisted_and_priced_once",
            selected === METHOD && selectedAmount === OWNED_AMOUNT,
            `selected=${selected} amount=${selectedAmount}`);

        const draftGroups = prop(prop(reread.body, "draft") || {}, "shippingGroups") || [];
        const draftGroup = draftGroups.find((g) => prop(g, "methodCode") === METHOD);
        record("draft_group_holds_owned_amount_once",
            !!draftGroup && prop(draftGroup, "shippingAmount") === OWNED_AMOUNT,
            `groups=${draftGroups.length} amount=${draftGroup && prop(draftGroup, "shippingAmount")}`);

        // No premature order/payment.
        const orderCount = psql("select count(*) from \"order\".checkouts");
        const paymentCount = psql("select count(*) from payment.payments");
        const sellerOrderCount = psql("select count(*) from \"order\".seller_orders");
        record("no_premature_order_or_payment",
            orderCount === "0" && paymentCount === "0" && sellerOrderCount === "0",
            `checkouts=${orderCount} payments=${paymentCount} seller_orders=${sellerOrderCount}`);

        // Negative control A: an invalid selection (schedule earlier than the authoritative minimum) is
        // rejected and cannot corrupt the accepted draft, which must still read back at the owned amount.
        const badDateSelection = await call("INVALID_SELECTION_SCHEDULE", "/v1/storefront/shipping/selection", {
            method: "PUT", headers: { ...auth, "Content-Type": "application/json" },
            body: JSON.stringify({
                cartId,
                expectedCartVersion: prop(reread.body, "cartVersion"),
                recipientName: "مشتری آزمون ۱۷۱",
                contactMobile: MOBILE,
                provinceName: "تهران",
                cityName: "تهران",
                postalAddress: "خیابان آزمون، پلاک ۱۷۱",
                postalCode: "1234567890",
                savedAddressId: null,
                shippingMethodCode: METHOD,
                selectedDeliveryDate: "2020-01-01",
                selectedDeliveryTimeWindow: window,
                customerNote: null,
                firstName: "مشتری",
                lastName: "آزمون",
            }),
        });
        record("invalid_schedule_selection_rejected", badDateSelection.status >= 400 && badDateSelection.status < 500,
            `http=${badDateSelection.status} errorCode=${errorCode(badDateSelection.json)}`);

        const afterBadSchedule = await project(auth, cartId, "ON_AFTER_INVALID_SCHEDULE");
        record("invalid_selection_cannot_corrupt_accepted_draft",
            prop(afterBadSchedule.body, "selectedMethodCode") === METHOD
            && prop(afterBadSchedule.body, "selectedShippingAmount") === OWNED_AMOUNT,
            `selected=${prop(afterBadSchedule.body, "selectedMethodCode")} amount=${prop(afterBadSchedule.body, "selectedShippingAmount")}`);

        // Negative control B: Restricted declaration without an eligible own grant => fail closed.
        const restrict = await call("SET_COVERAGE_RESTRICTED", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
            method: "PUT", headers: adminHeaders(actorUserId),
            body: JSON.stringify({ optionCode: OPTION, mode: 1 }),
        });
        record("coverage_switched_to_restricted", restrict.status >= 200 && restrict.status < 300, `http=${restrict.status}`);

        const restricted = await project(auth, cartId, "ON_RESTRICTED");
        const stillVisible = restricted.methods.find((m) => m.code === METHOD);
        record("restricted_without_own_grant_fails_closed",
            !stillVisible && restricted.methods.every((m) => m.price !== 0),
            `present=${!!stillVisible} zeroPriced=${restricted.methods.filter((m) => m.price === 0).map((m) => m.code).join(",") || "none"}`);

        const badSelection = await call("INVALID_SELECTION_UNAVAILABLE", "/v1/storefront/shipping/selection", {
            method: "PUT", headers: { ...auth, "Content-Type": "application/json" },
            body: JSON.stringify({
                cartId,
                expectedCartVersion: prop(restricted.body, "cartVersion"),
                recipientName: "مشتری آزمون ۱۷۱",
                contactMobile: MOBILE,
                provinceName: "تهران",
                cityName: "تهران",
                postalAddress: "خیابان آزمون، پلاک ۱۷۱",
                postalCode: "1234567890",
                savedAddressId: null,
                shippingMethodCode: METHOD,
                selectedDeliveryDate: date,
                selectedDeliveryTimeWindow: window,
                customerNote: null,
                firstName: "مشتری",
                lastName: "آزمون",
            }),
        });
        record("invalid_selection_rejected", badSelection.status >= 400 && badSelection.status < 500,
            `http=${badSelection.status} errorCode=${errorCode(badSelection.json)}`);

        const afterBad = await project(auth, cartId, "ON_AFTER_REJECT");
        const zeroGroups = (prop(prop(afterBad.body, "draft") || {}, "shippingGroups") || [])
            .filter((g) => prop(g, "methodCode") === METHOD && prop(g, "shippingAmount") === 0);
        record("rejected_selection_never_stores_zero_or_legacy_amount",
            zeroGroups.length === 0 && !restricted.methods.some((m) => m.code === METHOD),
            `zeroGroups=${zeroGroups.length} revalidation=${prop(afterBad.body, "revalidationMessage") ? "present" : "absent"}`);

        // Restore the truthful ready state for the OFF comparison.
        await call("SET_COVERAGE_RESTORED", `/v1/admin/shipping-services/${SERVICE}/method-coverage`, {
            method: "PUT", headers: adminHeaders(actorUserId),
            body: JSON.stringify({ optionCode: OPTION, mode: 0 }),
        });
        const restored = await project(auth, cartId, "ON_RESTORED");
        record("restored_after_negative_control",
            restored.methods.some((m) => m.code === METHOD && m.price === OWNED_AMOUNT),
            `visible=${restored.methods.some((m) => m.code === METHOD)}`);

        fs.writeFileSync(path.join(EVIDENCE, "projection-on-171.json"), JSON.stringify({
            baseline: { methods: first.methods, unavailable: prop(first.body, "unavailableMethods") },
            ready: { methods: second.methods, selected: prop(second.body, "selectedMethodCode") },
            selected: { methods: reread.methods, selected, selectedAmount, draft: prop(reread.body, "draft") },
            afterInvalidSchedule: {
                selected: prop(afterBadSchedule.body, "selectedMethodCode"),
                amount: prop(afterBadSchedule.body, "selectedShippingAmount"),
            },
            restricted: { methods: restricted.methods, revalidation: prop(afterBad.body, "revalidationMessage") },
        }, null, 2) + "\n");
        fs.writeFileSync(path.join(OUT_DIR, "cart-and-session-171.json"),
            JSON.stringify({ cartId, seller, offerIds, lineCount, actorUserId }, null, 2));
    } else {
        // OFF control: canonical-only method must be absent, legacy methods keep committed legacy pricing.
        record("off_new_method_absent", !newMethod, `present=${!!newMethod} visibleCodes=${first.methods.map((m) => m.code).join(",")}`);
        const legacy = first.methods.filter((m) => m.code.startsWith("tipax:"));
        const unavailable = prop(first.body, "unavailableMethods");
        record("off_legacy_methods_keep_prior_pricing",
            first.methods.length >= 2 && legacy.length >= 1 && (unavailable === null || unavailable === undefined),
            `legacy=${JSON.stringify(legacy)} unavailable=${JSON.stringify(unavailable)}`);
        fs.writeFileSync(path.join(EVIDENCE, "projection-off-171.json"), JSON.stringify({
            methods: first.methods,
            unavailableMethods: unavailable ?? null,
            selectedMethodCode: prop(first.body, "selectedMethodCode"),
            selectedShippingAmount: prop(first.body, "selectedShippingAmount"),
        }, null, 2) + "\n");
    }

    const passed = checks.filter((c) => c.ok).length;
    const matrix = {
        generatedAtUtc: new Date().toISOString(),
        phase: PHASE,
        host: BASE,
        methodCode: METHOD,
        total: checks.length,
        passed,
        failed: checks.length - passed,
        checks,
    };
    fs.writeFileSync(path.join(EVIDENCE, `assertion-matrix-${PHASE}-171.json`), JSON.stringify(matrix, null, 2) + "\n");
    fs.writeFileSync(path.join(EVIDENCE, `http-trace-${PHASE}-171.txt`), trace.join("\n") + "\n");
    console.log(`PHASE=${PHASE} TOTAL=${checks.length} PASSED=${passed} FAILED=${checks.length - passed}`);
    if (passed !== checks.length) process.exitCode = 1;
})().catch((err) => {
    console.error("HARNESS_ERROR", err && err.stack);
    fs.writeFileSync(path.join(EVIDENCE, `http-trace-${PHASE}-171.txt`), trace.join("\n") + "\n");
    process.exitCode = 2;
});
