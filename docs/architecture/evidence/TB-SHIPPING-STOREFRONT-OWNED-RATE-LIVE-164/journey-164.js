/**
 * TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164 — real authenticated HTTP acceptance against the
 * task-owned PRIVATE Host (:5419) and its disposable scratch database only.
 *
 * Proves the TB-163 default-off strict owned-rate gate when enabled ONLY in the scratch overlay:
 * projection -> two-seller save -> re-read -> read-only payment draft priced from Shipping-owned
 * exact-option policies, plus fail-closed negatives and provider/sibling non-inheritance.
 */
const fs = require('fs');
const path = require('path');

const BASE = process.env.TOOBA_164_HOST || 'http://127.0.0.1:5419';
const OUTDIR = path.join(process.cwd(), '.tmp-164-run');
const STATE_PATH = path.join(OUTDIR, 'journey-state.json');
const EVID = path.join(process.cwd(), 'docs', 'architecture', 'evidence', 'TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164');
const TRACE = [];
const CHECKS = [];

const SELLER_A = process.env.TOOBA_164_SELLER_A.trim().toLowerCase();
const SELLER_B = process.env.TOOBA_164_SELLER_B.trim().toLowerCase();
const OFFER_A = process.env.TOOBA_164_OFFER_A.trim().toLowerCase();
const OFFER_B = process.env.TOOBA_164_OFFER_B.trim().toLowerCase();
const METHOD = 'tipax:standard';
const EXPRESS = 'tipax:express';
const OWNER_STANDARD = 205000;
const OWNER_EXPRESS = 480000;
const LEGACY_STANDARD = 180000;
const LEGACY_EXPRESS = 260000;
const TIMES = Number(process.env.TOOBA_164_REPEATS || 1);
const MOBILE = process.env.TOOBA_164_MOBILE || '09121111111';
const FIXTURE_OTP = '123456';
const FIXTURE_PASSWORD = 'Passw0rd!2345';

fs.mkdirSync(OUTDIR, { recursive: true });
fs.mkdirSync(EVID, { recursive: true });

function check(name, ok, detail) {
    CHECKS.push({ name, result: ok ? 'PASS' : 'FAIL', detail: detail === undefined ? null : detail });
    console.log(`CHECK ${ok ? 'PASS' : 'FAIL'} :: ${name}${detail === undefined ? '' : ' :: ' + JSON.stringify(detail)}`);
    return !!ok;
}
function observe(name, detail) {
    CHECKS.push({ name, result: 'OBSERVED', detail });
    console.log(`OBSERVED :: ${name} :: ${JSON.stringify(detail)}`);
}
function state() { return fs.existsSync(STATE_PATH) ? JSON.parse(fs.readFileSync(STATE_PATH, 'utf8')) : {}; }
function save(patch) { const m = { ...state(), ...patch }; fs.writeFileSync(STATE_PATH, JSON.stringify(m, null, 2)); return m; }
function unwrap(j) { if (j && typeof j === 'object') { if (j.data !== undefined) return j.data; if (j.value !== undefined) return j.value; } return j; }
function prop(o, ...n) { if (!o || typeof o !== 'object') return undefined; for (const k of n) if (o[k] !== undefined && o[k] !== null) return o[k]; return undefined; }
function redact(t) {
    return String(t)
        .replace(/("(?:accessToken|refreshToken|guestSecret|secret|token)"\s*:\s*")[^"]*(")/g, '$1REDACTED$2')
        .slice(0, 4000);
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function call(label, url, options = {}) {
    const started = Date.now();
    const res = await fetch(url, options);
    const text = await res.text();
    let json = null;
    try { json = text ? JSON.parse(text) : null; } catch { }
    const ms = Date.now() - started;
    TRACE.push(`### ${label} ${options.method || 'GET'} ${url} -> ${res.status} in ${ms}ms\n${redact(text)}`);
    console.log(`${label} -> ${res.status} (${ms}ms) ${redact(text).slice(0, 260)}`);
    return { status: res.status, json, text, ms };
}
function headers(s, version) {
    const h = { 'Content-Type': 'application/json', Authorization: `Bearer ${s.accessToken}` };
    if (version !== undefined && version !== null) h['X-Tooba-Cart-Version'] = String(version);
    return h;
}
function methodsOf(p) {
    return (prop(p, 'methods') || []).map((m) => ({
        methodCode: prop(m, 'methodCode'),
        serviceCode: prop(m, 'serviceCode'),
        optionCode: prop(m, 'optionCode') || null,
        priceAmount: Number(prop(m, 'priceAmount')),
        isFree: prop(m, 'isFree'),
        label: prop(m, 'label'),
    }));
}
function horizonsOf(p) {
    return (prop(p, 'shippingGroupHorizons') || []).map((h) => ({
        groupKey: prop(h, 'groupKey'),
        shippingGroupId: prop(h, 'shippingGroupId') || null,
        sellerPartyId: String(prop(h, 'sellerPartyId') || '').toLowerCase(),
        methodCode: prop(h, 'methodCode'),
        minimumDeliveryDate: prop(h, 'minimumDeliveryDate'),
        windows: (prop(h, 'eligibleTimeWindows') || []).map((w) => prop(w, 'value')),
    }));
}
function groupsOf(c) {
    return (prop(c, 'shippingGroups', 'ShippingGroups') || []).map((g) => ({
        shippingGroupId: prop(g, 'shippingGroupId', 'ShippingGroupId'),
        sellerPartyId: String(prop(g, 'sellerPartyId', 'SellerPartyId') || '').toLowerCase(),
        methodCode: prop(g, 'methodCode', 'MethodCode'),
        shippingAmount: Number(prop(g, 'shippingAmount', 'ShippingAmount')),
        currency: prop(g, 'currency', 'Currency'),
        selectedDeliveryDate: prop(g, 'selectedDeliveryDate', 'SelectedDeliveryDate') || null,
        selectedDeliveryTimeWindow: prop(g, 'selectedDeliveryTimeWindow', 'SelectedDeliveryTimeWindow') || null,
        lineCount: prop(g, 'lineCount', 'LineCount'),
        cartLineIds: (prop(g, 'cartLineIds', 'CartLineIds') || []).map((x) => String(x).toLowerCase()),
    }));
}

async function auth() {
    const s = state();
    if (s.accessToken) { console.log('AUTH_CACHED'); return s; }
    await call('AUTH_REGISTER', `${BASE}/v1/auth/register`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifierKind: 'Phone', identifier: MOBILE, password: FIXTURE_PASSWORD }),
    });
    const req = await call('AUTH_OTP_REQUEST', `${BASE}/v1/auth/otp-login/request`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ identifier: MOBILE }),
    });
    if (req.status >= 400) throw new Error(`otp request failed ${req.status}`);
    const challengeId = prop(unwrap(req.json) || {}, 'challengeId');
    const done = await call('AUTH_OTP_COMPLETE', `${BASE}/v1/auth/otp-login/complete`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifier: MOBILE, challengeId, secret: FIXTURE_OTP }),
    });
    if (done.status >= 400) throw new Error(`otp complete failed ${done.status}`);
    const accessToken = prop(unwrap(done.json) || {}, 'accessToken');
    if (!accessToken) throw new Error('access token missing');
    console.log('AUTH_OK');
    return save({ accessToken, mobile: MOBILE });
}

async function buildCart(s) {
    const created = await call('CART_CREATE', `${BASE}/v1/storefront/cart`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{}',
    });
    const guest = unwrap(created.json);
    const guestCartId = prop(guest, 'cartId');
    const guestSecret = prop(guest, 'guestSecret');
    let guestVersion = prop(guest, 'version');
    const gh = (v) => ({ 'Content-Type': 'application/json', 'X-Tooba-Guest-Secret': guestSecret, 'X-Tooba-Cart-Version': String(v) });
    for (const offerId of [OFFER_A, OFFER_B]) {
        const res = await call('CART_LINE', `${BASE}/v1/storefront/cart/${guestCartId}/lines`, {
            method: 'POST', headers: gh(guestVersion), body: JSON.stringify({ offerId, quantity: 1 }),
        });
        if (res.status >= 400) throw new Error(`cart line failed ${offerId}: ${res.status} ${res.text.slice(0, 200)}`);
        guestVersion = prop(unwrap(res.json), 'version') ?? guestVersion;
    }
    const merge = await call('CART_MERGE', `${BASE}/v1/storefront/cart/merge`, {
        method: 'POST', headers: { ...headers(s), 'X-Tooba-Guest-Secret': guestSecret },
        body: JSON.stringify({ cartId: guestCartId }),
    });
    if (merge.status >= 400) throw new Error(`cart merge failed ${merge.status}`);
    const merged = unwrap(merge.json);
    const cartId = prop(merged, 'cartId');
    let version = prop(merged, 'version');
    const current = await call('CART_CURRENT', `${BASE}/v1/storefront/cart/current`, { method: 'GET', headers: headers(s, version) });
    const cart = unwrap(current.json) || {};
    version = prop(cart, 'version') ?? version;
    const lines = (prop(cart, 'lines') || []).map((l) => ({
        lineId: String(prop(l, 'lineId')).toLowerCase(),
        offerId: String(prop(l, 'offerId') || '').toLowerCase(),
        sellerPartyId: String(prop(l, 'sellerPartyId') || '').toLowerCase(),
    }));
    check('two authenticated cart lines exist (one per seller)', lines.length === 2, lines);
    check('two distinct sellers own the two lines', new Set(lines.map((l) => l.sellerPartyId)).size === 2);
    return save({ cartId, version, lines });
}

async function projection(s, methodCode) {
    const body = { cartId: s.cartId, provinceName: 'تهران', cityName: 'تهران', language: 'fa' };
    if (methodCode) body.methodCode = methodCode;
    const res = await call('PROJECTION', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST', headers: headers(s, s.version), body: JSON.stringify(body),
    });
    if (res.status >= 400) throw new Error(`projection failed ${res.status} ${res.text.slice(0, 300)}`);
    return unwrap(res.json) || {};
}

async function phaseOn() {
    const s = await auth();
    const built = await buildCart(s);

    const p1 = await projection(built);
    fs.writeFileSync(path.join(EVID, 'projection-on-164.json'), JSON.stringify(p1, null, 2));
    const methods = methodsOf(p1);
    const standard = methods.find((m) => m.methodCode === METHOD);
    const express = methods.find((m) => m.methodCode === EXPRESS);
    check('ON: tipax:standard is projectable', !!standard, standard);
    check('ON: tipax:standard price is the owner exact-option amount (205000 IRR)',
        standard && standard.priceAmount === OWNER_STANDARD, standard && standard.priceAmount);
    check('ON: the legacy configured amount (180000) never reaches the customer quote',
        standard && standard.priceAmount !== LEGACY_STANDARD, standard && standard.priceAmount);
    check('ON: sibling tipax:express keeps its own owner amount (480000), never standard/provider',
        express && express.priceAmount === OWNER_EXPRESS, express && express.priceAmount);
    check('ON: provider-only 99000 row never prices a strict option',
        methods.filter((m) => m.methodCode === METHOD || m.methodCode === EXPRESS).every((m) => m.priceAmount !== 99000),
        methods.map((m) => [m.methodCode, m.priceAmount]));
    check('ON: no strict method is presented as free/zero', methods.every((m) => !m.isFree && m.priceAmount > 0),
        methods.map((m) => [m.methodCode, m.priceAmount, m.isFree]));

    const canon = horizonsOf(p1).filter((h) => h.methodCode === METHOD);
    check('ON: a canonical promise exists for both sellers', canon.length === 2 && new Set(canon.map((h) => h.sellerPartyId)).size === 2, canon);
    const byA = canon.find((h) => h.sellerPartyId === SELLER_A);
    const byB = canon.find((h) => h.sellerPartyId === SELLER_B);
    if (!byA || !byB) throw new Error('canonical horizons missing for a seller');
    save({ routeA: byA, routeB: byB });

    const lineA = built.lines.find((l) => l.sellerPartyId === SELLER_A).lineId;
    const lineB = built.lines.find((l) => l.sellerPartyId === SELLER_B).lineId;
    const body = {
        cartId: built.cartId,
        expectedCartVersion: built.version,
        recipientName: 'حمید آزمون',
        contactMobile: MOBILE,
        provinceName: 'تهران',
        cityName: 'تهران',
        postalAddress: 'خیابان آزادی پلاک ۱ واحد ۲',
        postalCode: '1234567890',
        savedAddressId: null,
        shippingMethodCode: METHOD,
        selectedDeliveryDate: byA.minimumDeliveryDate,
        selectedDeliveryTimeWindow: byA.windows[0],
        customerNote: null,
        firstName: 'حمید',
        lastName: 'آزمون',
        shippingGroups: [
            { shippingMethodCode: METHOD, cartLineIds: [lineA], selectedDeliveryDate: byA.minimumDeliveryDate, selectedDeliveryTimeWindow: byA.windows[0] },
            { shippingMethodCode: METHOD, cartLineIds: [lineB], selectedDeliveryDate: byB.minimumDeliveryDate, selectedDeliveryTimeWindow: byB.windows[0] },
        ],
    };
    const saveRes = await call('SELECTION_SAVE', `${BASE}/v1/storefront/shipping/selection`, {
        method: 'PUT', headers: headers(built, built.version), body: JSON.stringify(body),
    });
    fs.writeFileSync(path.join(EVID, 'selection-save-164.json'), JSON.stringify({ status: saveRes.status, body: unwrap(saveRes.json) }, null, 2));
    check('ON: two-group selection save succeeds', saveRes.status === 200, saveRes.status);
    if (saveRes.status !== 200) return save({ stoppedAt: 'save' });
    const savedGroups = groupsOf(unwrap(saveRes.json));
    check('ON: persisted groups carry the owner amount 205000 each (not 180000)',
        savedGroups.length === 2 && savedGroups.every((g) => g.shippingAmount === OWNER_STANDARD),
        savedGroups.map((g) => [g.methodCode, g.shippingAmount, g.currency]));

    const st = state();
    const p2 = await projection(st);
    fs.writeFileSync(path.join(EVID, 'projection-on-reread-164.json'), JSON.stringify(p2, null, 2));
    const draft2 = prop(p2, 'draft') || {};
    const groups2 = groupsOf(draft2);
    check('ON: re-read draft still has exactly two groups with owner amounts',
        groups2.length === 2 && groups2.every((g) => g.shippingAmount === OWNER_STANDARD),
        groups2.map((g) => [g.shippingAmount, g.currency]));
    check('ON: group identities are stable across save and re-read',
        groups2.map((g) => g.shippingGroupId).sort().join(',') === savedGroups.map((g) => g.shippingGroupId).sort().join(','),
        { saved: savedGroups.map((g) => g.shippingGroupId), reread: groups2.map((g) => g.shippingGroupId) });
    check('ON: line membership is one line per group and disjoint',
        groups2.every((g) => g.lineCount === 1 && g.cartLineIds.length === 1)
        && new Set(groups2.flatMap((g) => g.cartLineIds)).size === 2);

    const pay = await call('PAYMENT_DRAFT', `${BASE}/v1/storefront/cart/${built.cartId}/payment-draft`, {
        method: 'GET', headers: headers(built, built.version),
    });
    const draft = unwrap(pay.json) || {};
    fs.writeFileSync(path.join(EVID, 'payment-draft-on-164.json'), JSON.stringify({ status: pay.status, body: draft }, null, 2));
    check('ON: read-only payment draft answers 200', pay.status === 200, pay.status);
    if (pay.status === 200) {
        const payGroups = groupsOf(draft);
        const shipping = Number(prop(draft, 'shippingAmount'));
        check('ON: payment draft has two groups at 205000 each',
            payGroups.length === 2 && payGroups.every((g) => g.shippingAmount === OWNER_STANDARD),
            payGroups.map((g) => g.shippingAmount));
        check('ON: payment shipping aggregate is exactly 410000 (group charge once)', shipping === 410000, shipping);
        check('ON: payment group identities match the accepted save',
            payGroups.map((g) => g.shippingGroupId).sort().join(',') === savedGroups.map((g) => g.shippingGroupId).sort().join(','));
        const subtotal = Number(prop(draft, 'subtotalExclusiveOfTax'));
        const tax = Number(prop(draft, 'taxAmount'));
        const discount = Number(prop(draft, 'discountAmount', 'discount') || 0);
        const payable = Number(prop(draft, 'payableAmount'));
        check('ON: payable arithmetic is authoritative (subtotal - discount + tax + shipping)',
            Number.isFinite(payable) && payable === subtotal - discount + tax + shipping,
            { subtotal, tax, discount, shipping, payable });
        check('ON: payment draft is read-only (no checkout id)', prop(draft, 'checkoutId') === undefined || prop(draft, 'checkoutId') === null);
    }

    // ---- Negative A: express loses its owned rule -> excluded with honest reason, selection rejected ----
    // The negative fixture is applied out-of-band (scratch SQL) after this phase; see RESULT.md.
    return save({ stoppedAt: 'on-phase-complete' });
}

async function phaseNegative() {
    const s = state();
    const p = await projection(s);
    fs.writeFileSync(path.join(EVID, 'projection-negative-164.json'), JSON.stringify(p, null, 2));
    const methods = methodsOf(p);
    const express = methods.find((m) => m.methodCode === EXPRESS);
    check('NEGATIVE: tipax:express is excluded from the selectable list', !express, methods.map((m) => m.methodCode));
    const unavailable = (prop(p, 'unavailableMethods') || []).map((u) => ({ methodCode: prop(u, 'methodCode'), reasonCode: prop(u, 'reasonCode') }));
    check('NEGATIVE: the exclusion is honest and explicit (stable owner reason, no fabricated fee)',
        unavailable.some((u) => u.methodCode === EXPRESS
            && (u.reasonCode === 'shipping.coverage.method_restricted' || u.reasonCode === 'shipping.quote.owned_option_not_priced')), unavailable);
    check('NEGATIVE: the remaining standard quote is still the owner amount',
        methods.filter((m) => m.methodCode === METHOD).every((m) => m.priceAmount === OWNER_STANDARD));

    const lineA = s.lines.find((l) => l.sellerPartyId === SELLER_A).lineId;
    const lineB = s.lines.find((l) => l.sellerPartyId === SELLER_B).lineId;
    const res = await call('NEGATIVE_EXPRESS_SELECTION', `${BASE}/v1/storefront/shipping/selection`, {
        method: 'PUT', headers: headers(s, s.version), body: JSON.stringify({
            cartId: s.cartId, expectedCartVersion: s.version,
            recipientName: 'حمید آزمون', contactMobile: MOBILE, provinceName: 'تهران', cityName: 'تهران',
            postalAddress: 'خیابان آزادی پلاک ۱ واحد ۲', postalCode: '1234567890', savedAddressId: null,
            shippingMethodCode: EXPRESS, selectedDeliveryDate: s.routeA.minimumDeliveryDate,
            selectedDeliveryTimeWindow: s.routeA.windows[0], customerNote: null, firstName: 'حمید', lastName: 'آزمون',
            shippingGroups: [
                { shippingMethodCode: EXPRESS, cartLineIds: [lineA], selectedDeliveryDate: s.routeA.minimumDeliveryDate, selectedDeliveryTimeWindow: s.routeA.windows[0] },
                { shippingMethodCode: EXPRESS, cartLineIds: [lineB], selectedDeliveryDate: s.routeB.minimumDeliveryDate, selectedDeliveryTimeWindow: s.routeB.windows[0] },
            ],
        }),
    });
    check('NEGATIVE: selecting the unquotable method fails CLOSED (4xx, no fabricated fee)',
        res.status >= 400 && res.status < 500, { status: res.status, body: redact(res.text) });

    const after = await projection(s);
    const draft = prop(after, 'draft') || {};
    const groups = groupsOf(draft);
    check('NEGATIVE: rejected selection did not corrupt the previously saved draft',
        groups.length === 2 && groups.every((g) => g.shippingAmount === OWNER_STANDARD && g.methodCode === METHOD),
        groups.map((g) => [g.methodCode, g.shippingAmount]));
    return save({ stoppedAt: 'negative-complete' });
}

async function main() {
    const phase = process.env.TOOBA_164_PHASE || 'on';
    if (phase === 'on') await phaseOn();
    else if (phase === 'negative') await phaseNegative();
    fs.writeFileSync(path.join(EVID, `checks-${phase}-164.json`), JSON.stringify(CHECKS, null, 2));
    fs.writeFileSync(path.join(EVID, `trace-${phase}-164.txt`), TRACE.map(redact).join('\n\n'));
    const failed = CHECKS.filter((c) => c.result === 'FAIL');
    console.log(`SUMMARY phase=${phase} pass=${CHECKS.filter((c) => c.result === 'PASS').length} fail=${failed.length} observed=${CHECKS.filter((c) => c.result === 'OBSERVED').length}`);
    if (failed.length > 0) process.exit(4);
}

main().catch((e) => { console.error('JOURNEY_ERROR', e.message); process.exit(5); });
