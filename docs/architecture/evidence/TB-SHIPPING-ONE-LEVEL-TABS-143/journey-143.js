/**
 * TB-SHIPPING-ONE-LEVEL-TABS-143 — real API-driven storefront journey (no browser, no bypass).
 * Phases:  auth -> projection -> pay
 * The actor is a real OTP-authenticated customer (Development OTP fixture), the cart is the customer's
 * authenticated cart (`GET /v1/storefront/cart/current`), and every checkout step carries only the
 * bearer session — exactly the authenticated customer flow the identity policy requires.
 * State is kept in .tmp-143-run/journey-state.json (never committed).
 */
const fs = require('fs');
const path = require('path');

const BASE = process.env.TOOBA_143_HOST || 'http://127.0.0.1:5411';
const OUTDIR = path.join(process.cwd(), '.tmp-143-run');
const STATE_PATH = path.join(OUTDIR, 'journey-state.json');
const EVID = path.join(process.cwd(), 'docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-143');
const TRACE = [];

// Two sibling offers of the SAME seeded seller in this scratch fixture; overridable so the evidence
// script never pretends to know a fixture-specific identity.
const OFFER_SHIRT = process.env.TOOBA_143_OFFER_A || '01a143f0-a098-7000-abf6-33b9e2a118c7';
const OFFER_SCARF = process.env.TOOBA_143_OFFER_B || '01a143f0-a184-7000-a701-736ba2fbafa7';
const MOBILE = process.env.TOOBA_143_MOBILE || '09111111111';
const FIXTURE_OTP = '123456';
const FIXTURE_PASSWORD = 'Passw0rd!2345';

const phase = (process.argv[2] || 'all').trim();

function state() {
    return fs.existsSync(STATE_PATH) ? JSON.parse(fs.readFileSync(STATE_PATH, 'utf8')) : {};
}

function save(patch) {
    const merged = { ...state(), ...patch };
    fs.writeFileSync(STATE_PATH, JSON.stringify(merged, null, 2));
    return merged;
}

function unwrap(json) {
    if (json && typeof json === 'object') {
        if ('data' in json && json.data !== undefined) return json.data;
        if ('value' in json && json.value !== undefined) return json.value;
    }
    return json;
}

async function call(label, url, options = {}) {
    const res = await fetch(url, options);
    const text = await res.text();
    let json = null;
    try { json = JSON.parse(text); } catch { /* raw */ }
    TRACE.push(`${label} -> ${res.status} ${text.slice(0, 2000)}`);
    console.log(`${label} -> ${res.status} ${text.slice(0, 1500)}`);
    return { status: res.status, json, text, headers: res.headers };
}

function must(label, res) {
    if (res.status >= 400) {
        fs.writeFileSync(path.join(OUTDIR, `journey-failure-${label}.txt`), TRACE.join('\n'));
        throw new Error(`${label} failed with ${res.status}`);
    }
    return res;
}

function sessionHeaders(s, version) {
    const h = { 'Content-Type': 'application/json', Authorization: `Bearer ${s.accessToken}` };
    if (version !== undefined && version !== null) h['X-Tooba-Cart-Version'] = String(version);
    return h;
}

async function auth() {
    const s = state();
    if (s.accessToken) { console.log('AUTH_CACHED'); return s; }

    await call('AUTH_REGISTER', `${BASE}/v1/auth/register`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifierKind: 'Mobile', identifier: MOBILE, password: FIXTURE_PASSWORD }),
    });

    const req = must('AUTH_OTP_REQUEST', await call('AUTH_OTP_REQUEST', `${BASE}/v1/auth/otp-login/request`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifier: MOBILE }),
    }));
    const challengeId = (unwrap(req.json) || {}).challengeId;

    const done = must('AUTH_OTP_COMPLETE', await call('AUTH_OTP_COMPLETE', `${BASE}/v1/auth/otp-login/complete`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifier: MOBILE, challengeId, secret: FIXTURE_OTP }),
    }));
    const ticket = unwrap(done.json) || {};
    const accessToken = ticket.accessToken;
    if (!accessToken) throw new Error('access token missing in OTP login response');
    const next = save({ accessToken, challengeId });
    console.log('AUTH_OK');
    return next;
}

async function projection() {
    const s = await auth();

    // 1) anonymous cart + two lines for the SAME seller (canonical storefront shopping step)
    const cartRes = must('CART_CREATE', await call('CART_CREATE', `${BASE}/v1/storefront/cart`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{}',
    }));
    const guestCart = unwrap(cartRes.json);
    const guestCartId = guestCart.cartId;
    const guestSecret = guestCart.guestSecret;
    let guestVersion = guestCart.version;
    const guestHeaders = (v) => ({
        'Content-Type': 'application/json',
        'X-Tooba-Guest-Secret': guestSecret,
        'X-Tooba-Cart-Version': String(v),
    });

    const addLine = async (offerId, headers, v) => {
        const res = must('CART_LINE', await call('CART_LINE', `${BASE}/v1/storefront/cart/${guestCartId}/lines`, {
            method: 'POST', headers, body: JSON.stringify({ offerId, quantity: 1 }),
        }));
        return unwrap(res.json);
    };
    guestVersion = (await addLine(OFFER_SHIRT, guestHeaders(guestVersion), guestVersion)).version;
    guestVersion = (await addLine(OFFER_SCARF, guestHeaders(guestVersion), guestVersion)).version;

    // 2) sign-in merge: the anonymous cart becomes the authenticated customer's cart
    const mergeRes = must('CART_MERGE', await call('CART_MERGE', `${BASE}/v1/storefront/cart/merge`, {
        method: 'POST',
        headers: { ...sessionHeaders(s), 'X-Tooba-Guest-Secret': guestSecret },
        body: JSON.stringify({ cartId: guestCartId }),
    }));
    const merged = unwrap(mergeRes.json);
    const cartId = merged.cartId;
    let version = merged.version;
    const lines = (merged.lines || []).map((l) => ({
        lineId: l.lineId, offerId: l.offerId, title: l.title, sellerPartyId: l.sellerPartyId,
    }));
    if (!cartId || lines.length !== 2) throw new Error('merge did not yield the two-line authenticated cart');

    const projRes = must('PROJECTION', await call('PROJECTION', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST', headers: sessionHeaders(s, version),
        body: JSON.stringify({ cartId, provinceName: 'تهران', cityName: 'تهران', language: 'fa' }),
    }));
    const projectionData = unwrap(projRes.json);
    fs.writeFileSync(path.join(EVID, 'projection-143.json'), JSON.stringify(projectionData, null, 2));
    fs.writeFileSync(path.join(OUTDIR, 'projection-143.json'), JSON.stringify(projectionData, null, 2));

    save({
        cartId, version, lines, projection: projectionData,
        guestCartId, guestSecret, mergeCartId: cartId, phase: 'projection',
    });
    console.log('PROJECTION_SAVED');
    console.log('LINES=' + JSON.stringify(lines));
    return { cartId, version, lines };
}

function collectMethods(projection) {
    const found = [];
    const seen = new Set();
    const walk = (node) => {
        if (node === null || node === undefined) return;
        if (Array.isArray(node)) { node.forEach(walk); return; }
        if (typeof node !== 'object') return;
        const code = node.methodCode;
        if (typeof code === 'string' && code.length > 0 && !seen.has(code)) {
            seen.add(code);
            found.push({ methodCode: code, serviceCode: node.serviceCode || null, optionCode: node.optionCode || null });
        }
        for (const value of Object.values(node)) walk(value);
    };
    walk(projection);
    return found;
}

async function pay() {
    let s = state();
    if (!s.cartId || !s.projection) throw new Error('run the projection phase first');

    const methodOptions = collectMethods(s.projection);
    const first = methodOptions.find((m) => m.methodCode === 'post:express') || methodOptions[0];
    const second = methodOptions.find((m) => m.methodCode === 'tipax:standard')
        || methodOptions.find((m) => m.methodCode !== first.methodCode);
    if (!first || !second) throw new Error('two distinct method codes are required for two accepted shipping groups');

    const datedFor = async (methodCode) => {
        const res = must(`PROJECTION_DATED_${methodCode}`, await call(`PROJECTION_DATED_${methodCode}`, `${BASE}/v1/storefront/shipping/projection`, {
            method: 'POST', headers: sessionHeaders(s, s.version),
            body: JSON.stringify({
                cartId: s.cartId, provinceName: 'تهران', cityName: 'تهران', language: 'fa', methodCode,
            }),
        }));
        return unwrap(res.json) || {};
    };
    const datedFirst = await datedFor(first.methodCode);
    const datedSecond = await datedFor(second.methodCode);
    save({ projectionDatedFirst: datedFirst, projectionDatedSecond: datedSecond });

    const minOf = (p) => p.minimumDeliveryDate
        || (p.deliveryDates && p.deliveryDates[0] && (p.deliveryDates[0].value || p.deliveryDates[0]));
    const earliest = [minOf(datedFirst), minOf(datedSecond)].filter(Boolean).sort();
    if (earliest.length === 0) throw new Error('server-derived delivery dates were not returned');
    const deliveryDate = earliest[earliest.length - 1];
    const windowList = datedSecond.deliveryTimeWindows || datedFirst.deliveryTimeWindows || [];
    const window = (windowList[0] && windowList[0].value) || '9-12';
    console.log(`DELIVERY_DATE=${deliveryDate} WINDOW=${window}`);

    const selectionBody = {
        cartId: s.cartId,
        expectedCartVersion: s.version,
        recipientName: 'حمید آزمون',
        contactMobile: '09111111111',
        provinceName: 'تهران',
        cityName: 'تهران',
        postalAddress: 'خیابان آزادی پلاک ۱ واحد ۲',
        postalCode: '1234567890',
        savedAddressId: null,
        shippingMethodCode: first.methodCode,
        selectedDeliveryDate: deliveryDate,
        selectedDeliveryTimeWindow: window,
        customerNote: null,
        firstName: 'حمید',
        lastName: 'آزمون',
        shippingGroups: [
            { shippingMethodCode: first.methodCode, cartLineIds: [s.lines[0].lineId], serviceCode: first.serviceCode, optionCode: first.optionCode },
            { shippingMethodCode: second.methodCode, cartLineIds: [s.lines[1].lineId], serviceCode: second.serviceCode, optionCode: second.optionCode },
        ],
    };

    const sel = must('SELECTION', await call('SELECTION', `${BASE}/v1/storefront/shipping/selection`, {
        method: 'PUT', headers: sessionHeaders(s, s.version), body: JSON.stringify(selectionBody),
    }));
    const selData = unwrap(sel.json);
    let s2 = save({ selection: selData, phase: 'selection' });
    const versionAfterSelection = selData.cartVersion ?? s2.version;

    // The canonical final click is start-payment: the storefront composer commits the checkout itself,
    // so committing through /shipping/commit first would consume the cart and leave it empty.
    const versionForStart = versionAfterSelection;
    const start = must('START_PAYMENT', await call('START_PAYMENT', `${BASE}/v1/storefront/cart/${s2.cartId}/start-payment`, {
        method: 'POST', headers: sessionHeaders(s2, versionForStart),
        body: JSON.stringify({
            expectedCartVersion: versionForStart,
            idempotencyKey: `start-143-${s2.cartId}`,
            useWallet: false,
            providerCode: 'gateway',
        }),
    }));
    const started = unwrap(start.json) || {};
    const redirect = started.redirectUrl || '';
    const refMatch = /ref=([^&]+)/.exec(redirect);
    const providerRequestReference = refMatch ? decodeURIComponent(refMatch[1]) : '';

    s2 = save({
        checkoutId: started.checkoutId,
        paymentId: started.paymentId,
        attemptId: started.attemptId,
        redirectUrl: redirect,
        providerRequestReference,
        paymentStatus: started.paymentStatus,
        amount: started.amount,
        phase: 'started',
    });

    const complete = must('SANDBOX_COMPLETE', await call('SANDBOX_COMPLETE', `${BASE}/v1/storefront/payments/${s2.paymentId}/sandbox/complete`, {
        method: 'POST', headers: sessionHeaders(s2),
        body: JSON.stringify({
            cartId: s2.cartId,
            attemptId: s2.attemptId,
            providerRequestReference: s2.providerRequestReference,
            outcome: 'success',
        }),
    }));
    save({ sandboxComplete: unwrap(complete.json), phase: 'paid' });
    console.log('PAID');
}

async function main() {
    if (phase === 'auth') { await auth(); return; }
    if (phase === 'projection') { await projection(); return; }
    if (phase === 'pay') { await pay(); return; }
    await auth();
    await projection();
    await pay();
}

main().catch((err) => {
    console.error('JOURNEY_FAILED: ' + err.message);
    fs.writeFileSync(path.join(OUTDIR, 'journey-failure.txt'), TRACE.join('\n') + '\n' + err.stack);
    process.exit(1);
});
