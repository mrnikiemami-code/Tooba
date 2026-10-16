/**
 * TB-SHIPPING-GROUP-ID-CONTINUITY-153 — real authenticated HTTP acceptance against the
 * task-owned PRIVATE Host (:5413) and its disposable scratch database only.
 *
 * Scenario: two-seller cart whose lines select the SAME canonical method tipax:standard while each
 * seller has its own preparation days (A = 2, B = 3), so the two customer-visible promises must be
 * independent. Closes TB-151 gap (A) selection/persistence and gap (B) read-only payment draft after
 * the minimum scratch tax fixture was added, and records gap (D) repeated-save ShippingGroupId identity.
 *
 * Phases: auth -> cart -> horizons -> save/re-read/edit/fail-closed -> payment draft -> identity(D) -> integrity
 * State is kept in .tmp-153-run/journey-state.json (never committed).
 */
const fs = require('fs');
const path = require('path');

const BASE = process.env.TOOBA_153_HOST || 'http://127.0.0.1:5414';
const OUTDIR = path.join(process.cwd(), '.tmp-153-run');
const STATE_PATH = path.join(OUTDIR, 'journey-state.json');
const EVID = path.join(process.cwd(), 'docs', 'architecture', 'evidence', 'TB-SHIPPING-GROUP-ID-CONTINUITY-153');
const TRACE = [];
const CHECKS = [];

// TB-153: the disposable scratch bootstrap mints fresh offer/seller ids on every seed, so the two
// task-owned offers and their sellers are injected from that DB read-back instead of being hard-coded.
function requiredEnv(name) {
    const value = process.env[name];
    if (!value) throw new Error(`missing required env ${name} (read it back from the scratch DB)`);
    return value.trim().toLowerCase();
}

const SELLER_A = requiredEnv('TOOBA_153_SELLER_A'); // preparation days 2 (scratch overlay)
const SELLER_B = requiredEnv('TOOBA_153_SELLER_B'); // preparation days 3 (scratch overlay)
const OFFER_A = requiredEnv('TOOBA_153_OFFER_A');   // first line of seller A
const OFFER_A2 = process.env.TOOBA_153_OFFER_A2 ? process.env.TOOBA_153_OFFER_A2.trim().toLowerCase() : null;
const OFFER_B = requiredEnv('TOOBA_153_OFFER_B');   // first line of seller B
const CANONICAL_METHOD = 'tipax:standard';
const MOBILE = process.env.TOOBA_153_MOBILE || '09111111111';
const FIXTURE_OTP = '123456';
const FIXTURE_PASSWORD = 'Passw0rd!2345';

fs.mkdirSync(OUTDIR, { recursive: true });
fs.mkdirSync(EVID, { recursive: true });

function check(name, condition, detail) {
    CHECKS.push({ name, result: condition ? 'PASS' : 'FAIL', detail: detail === undefined ? null : detail });
    console.log(`CHECK ${condition ? 'PASS' : 'FAIL'} :: ${name}${detail === undefined ? '' : ' :: ' + JSON.stringify(detail)}`);
    return !!condition;
}
function observe(name, detail) {
    CHECKS.push({ name, result: 'OBSERVED', detail: detail === undefined ? null : detail });
    console.log(`OBSERVED :: ${name} :: ${JSON.stringify(detail)}`);
}
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
function prop(obj, ...names) {
    if (!obj || typeof obj !== 'object') return undefined;
    for (const n of names) if (obj[n] !== undefined && obj[n] !== null) return obj[n];
    return undefined;
}
function redact(text) {
    return String(text)
        .replace(/("(?:accessToken|refreshToken|guestSecret|secret)"\s*:\s*")[^"]*(")/g, '$1REDACTED$2')
        .replace(/Bearer\s+[A-Za-z0-9._-]+/g, 'Bearer REDACTED');
}
async function call(label, url, options = {}) {
    const started = Date.now();
    const res = await fetch(url, options);
    const text = await res.text();
    const ms = Date.now() - started;
    let json = null;
    try { json = JSON.parse(text); } catch { /* raw */ }
    TRACE.push(`### ${label} ${options.method || 'GET'} ${url} -> ${res.status} in ${ms}ms\n${redact(text).slice(0, 4000)}`);
    console.log(`${label} -> ${res.status} (${ms}ms) ${redact(text).slice(0, 500)}`);
    return { status: res.status, json, text, headers: res.headers, ms };
}
function sessionHeaders(s, version) {
    const h = { 'Content-Type': 'application/json', Authorization: `Bearer ${s.accessToken}` };
    if (version !== undefined && version !== null) h['X-Tooba-Cart-Version'] = String(version);
    return h;
}
function dayOffset(iso, days) {
    const d = new Date(`${iso}T00:00:00Z`);
    d.setUTCDate(d.getUTCDate() + days);
    return d.toISOString().slice(0, 10);
}

async function auth() {
    const s = state();
    if (s.accessToken) { console.log('AUTH_CACHED'); return s; }
    await call('AUTH_REGISTER', `${BASE}/v1/auth/register`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifierKind: 'Mobile', identifier: MOBILE, password: FIXTURE_PASSWORD }),
    });
    const req = await call('AUTH_OTP_REQUEST', `${BASE}/v1/auth/otp-login/request`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifier: MOBILE }),
    });
    if (req.status >= 400) throw new Error(`otp request failed ${req.status}`);
    const challengeId = prop(unwrap(req.json) || {}, 'challengeId');
    const done = await call('AUTH_OTP_COMPLETE', `${BASE}/v1/auth/otp-login/complete`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifier: MOBILE, challengeId, secret: FIXTURE_OTP }),
    });
    if (done.status >= 400) throw new Error(`otp complete failed ${done.status}`);
    const accessToken = prop(unwrap(done.json) || {}, 'accessToken');
    if (!accessToken) throw new Error('access token missing in OTP login response');
    console.log('AUTH_OK');
    return save({ accessToken, challengeId, mobile: MOBILE });
}

async function buildCart(s) {
    const created = await call('CART_CREATE', `${BASE}/v1/storefront/cart`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{}',
    });
    if (created.status >= 400) throw new Error(`cart create failed ${created.status}`);
    const guest = unwrap(created.json);
    const guestCartId = prop(guest, 'cartId');
    const guestSecret = prop(guest, 'guestSecret');
    let guestVersion = prop(guest, 'version');
    const guestHeaders = (v) => ({
        'Content-Type': 'application/json', 'X-Tooba-Guest-Secret': guestSecret, 'X-Tooba-Cart-Version': String(v),
    });
    for (const offerId of [OFFER_A, OFFER_B]) {
        const res = await call('CART_LINE', `${BASE}/v1/storefront/cart/${guestCartId}/lines`, {
            method: 'POST', headers: guestHeaders(guestVersion), body: JSON.stringify({ offerId, quantity: 1 }),
        });
        if (res.status >= 400) throw new Error(`cart line failed for ${offerId}: ${res.status}`);
        guestVersion = prop(unwrap(res.json), 'version') ?? guestVersion;
    }
    const merge = await call('CART_MERGE', `${BASE}/v1/storefront/cart/merge`, {
        method: 'POST',
        headers: { ...sessionHeaders(s), 'X-Tooba-Guest-Secret': guestSecret },
        body: JSON.stringify({ cartId: guestCartId }),
    });
    if (merge.status >= 400) throw new Error(`cart merge failed ${merge.status}`);
    const merged = unwrap(merge.json);
    const cartId = prop(merged, 'cartId');
    let version = prop(merged, 'version');
    // Normalise the disposable cart to exactly the two task-owned offers (idempotent; no other cart touched).
    const wanted = [OFFER_A, OFFER_B];
    let linesNow = (prop(merged, 'lines') || []).map((l) => ({
        lineId: prop(l, 'lineId'), offerId: String(prop(l, 'offerId') || '').toLowerCase(),
    }));
    for (const line of linesNow.filter((l) => !wanted.includes(l.offerId))) {
        const del = await call('CART_LINE_NORMALISE_REMOVE', `${BASE}/v1/storefront/cart/${cartId}/lines/${line.lineId}`, {
            method: 'DELETE', headers: sessionHeaders(s, version),
        });
        version = prop(unwrap(del.json), 'version') ?? version;
    }
    for (const offerId of wanted.filter((o) => !linesNow.some((l) => l.offerId === o))) {
        const add = await call('CART_LINE_NORMALISE_ADD', `${BASE}/v1/storefront/cart/${cartId}/lines`, {
            method: 'POST', headers: sessionHeaders(s, version), body: JSON.stringify({ offerId, quantity: 1 }),
        });
        version = prop(unwrap(add.json), 'version') ?? version;
    }
    const current = await call('CART_CURRENT_NORMALISED', `${BASE}/v1/storefront/cart/current`, {
        method: 'GET', headers: sessionHeaders(s, version),
    });
    const cartNow = unwrap(current.json) || {};
    version = prop(cartNow, 'version') ?? version;
    const lines = (prop(cartNow, 'lines') || []).map((l) => ({
        lineId: prop(l, 'lineId'), offerId: prop(l, 'offerId'), sellerPartyId: prop(l, 'sellerPartyId'),
    }));
    check('two authenticated cart lines exist', lines.length === 2, lines);
    check('two distinct sellers own the two lines',
        new Set(lines.map((l) => String(l.sellerPartyId).toLowerCase())).size === 2,
        lines.map((l) => l.sellerPartyId));
    check('neither cart line was mutated before checkout (offer membership as submitted)',
        lines.map((l) => String(l.offerId).toLowerCase()).sort().join(',') === wanted.slice().sort().join(','),
        lines.map((l) => l.offerId));
    return save({ cartId, version, lines, guestCartId, guestSecret });
}

async function projection(s, methodCode) {
    const body = { cartId: s.cartId, provinceName: 'تهران', cityName: 'تهران', language: 'fa' };
    if (methodCode) body.methodCode = methodCode;
    const res = await call('PROJECTION' + (methodCode ? `_${methodCode}` : ''),
        `${BASE}/v1/storefront/shipping/projection`,
        { method: 'POST', headers: sessionHeaders(s, s.version), body: JSON.stringify(body) });
    if (res.status >= 400) throw new Error(`projection failed ${res.status}`);
    return unwrap(res.json) || {};
}
function horizonsOf(p) { return prop(p, 'shippingGroupHorizons') || []; }
function routeOf(h) {
    return {
        groupKey: prop(h, 'groupKey'),
        shippingGroupId: prop(h, 'shippingGroupId') || null,
        sellerPartyId: String(prop(h, 'sellerPartyId') || '').toLowerCase(),
        methodCode: prop(h, 'methodCode'),
        minimumDeliveryDate: prop(h, 'minimumDeliveryDate'),
        selectedDeliveryDate: prop(h, 'selectedDeliveryDate') || null,
        selectedDeliveryTimeWindow: prop(h, 'selectedDeliveryTimeWindow') || null,
        leadDays: prop(h, 'leadDays'),
        sellerPreparationDays: prop(h, 'sellerPreparationDays'),
        windows: (prop(h, 'eligibleTimeWindows') || []).map((w) => prop(w, 'value')),
        dates: (prop(h, 'eligibleDates') || []).map((d) => prop(d, 'value')),
    };
}
function decodeGroups(container) {
    return (prop(container, 'shippingGroups', 'ShippingGroups') || []).map((g) => ({
        shippingGroupId: prop(g, 'shippingGroupId', 'ShippingGroupId'),
        sellerPartyId: String(prop(g, 'sellerPartyId', 'SellerPartyId') || '').toLowerCase(),
        methodCode: prop(g, 'methodCode', 'MethodCode'),
        methodLabel: prop(g, 'methodLabel', 'MethodLabel'),
        selectedDeliveryDate: prop(g, 'selectedDeliveryDate', 'SelectedDeliveryDate') || null,
        selectedDeliveryTimeWindow: prop(g, 'selectedDeliveryTimeWindow', 'SelectedDeliveryTimeWindow') || null,
        shippingAmount: prop(g, 'shippingAmount', 'ShippingAmount'),
        minimumDeliveryDate: prop(g, 'minimumDeliveryDate', 'MinimumDeliveryDate') || null,
        currency: prop(g, 'currency', 'Currency') || null,
        leadDays: prop(g, 'leadDays', 'LeadDays'),
        lineCount: prop(g, 'lineCount', 'LineCount'),
        cartLineIds: (prop(g, 'cartLineIds', 'CartLineIds') || []).map((x) => String(x).toLowerCase()),
    }));
}

async function phaseHorizons(s) {
    const p = await projection(s);
    fs.writeFileSync(path.join(EVID, 'projection-before-save-153.json'), JSON.stringify(p, null, 2));
    const canon = horizonsOf(p).map(routeOf).filter((h) => h.methodCode === CANONICAL_METHOD);
    check('a canonical tipax:standard promise exists for both sellers',
        canon.length === 2 && new Set(canon.map((h) => h.sellerPartyId)).size === 2, canon);
    check('both canonical routes share the SAME method code', canon.every((h) => h.methodCode === CANONICAL_METHOD),
        canon.map((h) => h.methodCode));
    check('the two canonical group keys are distinct before the first save',
        new Set(canon.map((h) => h.groupKey)).size === 2, canon.map((h) => h.groupKey));
    const byA = canon.find((h) => h.sellerPartyId === SELLER_A);
    const byB = canon.find((h) => h.sellerPartyId === SELLER_B);
    check('seller A route resolves the seller-A preparation (2 days)', byA && byA.sellerPreparationDays === 2,
        byA && byA.sellerPreparationDays);
    check('seller B route resolves the seller-B preparation (3 days)', byB && byB.sellerPreparationDays === 3,
        byB && byB.sellerPreparationDays);
    check('both canonical routes share the same method lead days (3)',
        byA && byB && byA.leadDays === 3 && byB.leadDays === 3, byA && byB && [byA.leadDays, byB.leadDays]);
    check('the two authoritative earliest dates are independent and differ by the preparation delta',
        byA && byB && byA.minimumDeliveryDate !== byB.minimumDeliveryDate
        && dayOffset(byA.minimumDeliveryDate, 1) === byB.minimumDeliveryDate,
        byA && byB && [byA.minimumDeliveryDate, byB.minimumDeliveryDate]);
    save({ horizons: canon, routeA: byA, routeB: byB });
    return { canon, byA, byB };
}

function selectionBody(s, groups) {
    return {
        cartId: s.cartId,
        expectedCartVersion: s.version,
        recipientName: 'حمید آزمون',
        contactMobile: MOBILE,
        provinceName: 'تهران',
        cityName: 'تهران',
        postalAddress: 'خیابان آزادی پلاک ۱ واحد ۲',
        postalCode: '1234567890',
        savedAddressId: null,
        shippingMethodCode: CANONICAL_METHOD,
        selectedDeliveryDate: groups[0].deliveryDate,
        selectedDeliveryTimeWindow: groups[0].window,
        customerNote: null,
        firstName: 'حمید',
        lastName: 'آزمون',
        shippingGroups: groups.map((g) => ({
            shippingMethodCode: CANONICAL_METHOD,
            cartLineIds: [g.lineId],
            serviceCode: g.serviceCode || null,
            optionCode: g.optionCode || null,
            selectedDeliveryDate: g.deliveryDate,
            selectedDeliveryTimeWindow: g.window,
        })),
    };
}
async function putSelection(s, body, label = 'SELECTION') {
    return call(label, `${BASE}/v1/storefront/shipping/selection`, {
        method: 'PUT', headers: sessionHeaders(s, s.version), body: JSON.stringify(body),
    });
}

async function phaseSave(s, hz) {
    const lineA = s.lines.find((l) => String(l.sellerPartyId).toLowerCase() === SELLER_A).lineId;
    const lineB = s.lines.find((l) => String(l.sellerPartyId).toLowerCase() === SELLER_B).lineId;
    const groups = [
        { lineId: lineA, deliveryDate: hz.byA.minimumDeliveryDate, window: hz.byA.windows[0] },
        { lineId: lineB, deliveryDate: hz.byB.minimumDeliveryDate, window: hz.byB.windows[0] },
    ];
    const res = await putSelection(s, selectionBody(s, groups));
    if (res.status >= 400) throw new Error(`selection save failed ${res.status} ${res.text.slice(0, 300)}`);
    const saved = unwrap(res.json) || {};
    fs.writeFileSync(path.join(EVID, 'selection-save-153.json'),
        JSON.stringify({ request: selectionBody(s, groups), response: saved }, null, 2));
    const savedGroups = decodeGroups(saved);
    check('the save returned two distinct stable ShippingGroupIds', savedGroups.length === 2
        && new Set(savedGroups.map((g) => String(g.shippingGroupId))).size === 2, savedGroups);
    check('saved groups keep the two distinct sellers with the SAME canonical method',
        new Set(savedGroups.map((g) => g.sellerPartyId)).size === 2
        && savedGroups.every((g) => g.methodCode === CANONICAL_METHOD),
        savedGroups.map((g) => `${g.sellerPartyId}:${g.methodCode}`));
    check('each saved group retained its own selected date and window',
        savedGroups.every((g) => g.selectedDeliveryDate && g.selectedDeliveryTimeWindow)
        && new Set(savedGroups.map((g) => g.selectedDeliveryDate)).size === 2,
        savedGroups.map((g) => `${g.selectedDeliveryDate}/${g.selectedDeliveryTimeWindow}`));
    let next = save({ version: prop(saved, 'cartVersion') ?? s.version, savedGroups });

    const reRead = await projection(next);
    fs.writeFileSync(path.join(EVID, 'projection-after-save-153.json'), JSON.stringify(reRead, null, 2));
    const after = horizonsOf(reRead).map(routeOf).filter((h) => h.methodCode === CANONICAL_METHOD);
    const afterA = after.find((h) => h.sellerPartyId === SELLER_A);
    const afterB = after.find((h) => h.sellerPartyId === SELLER_B);
    check('each re-read route carries its OWN stable ShippingGroupId and accepted schedule',
        afterA && afterB && afterA.shippingGroupId && afterB.shippingGroupId
        && afterA.shippingGroupId !== afterB.shippingGroupId
        && afterA.selectedDeliveryDate === groups[0].deliveryDate
        && afterB.selectedDeliveryDate === groups[1].deliveryDate
        && afterA.selectedDeliveryTimeWindow === groups[0].window
        && afterB.selectedDeliveryTimeWindow === groups[1].window, { afterA, afterB });

    const refresh = await projection(next);
    const afterRefresh = horizonsOf(refresh).map(routeOf).filter((h) => h.methodCode === CANONICAL_METHOD);
    check('reload/back re-read is identical (no client drift, no server recalculation)',
        JSON.stringify(afterRefresh) === JSON.stringify(after), afterRefresh);

    const editedWindow = hz.byA.windows[1];
    const editRes = await putSelection(next, selectionBody(next, [
        { lineId: lineA, deliveryDate: hz.byA.minimumDeliveryDate, window: editedWindow },
        { lineId: lineB, deliveryDate: groups[1].deliveryDate, window: groups[1].window },
    ]), 'SELECTION_EDIT_A');
    if (editRes.status >= 400) throw new Error(`edit A failed ${editRes.status}`);
    const edited = unwrap(editRes.json) || {};
    const editedGroups = decodeGroups(edited);
    const editedA = editedGroups.find((g) => g.sellerPartyId === SELLER_A);
    const editedB = editedGroups.find((g) => g.sellerPartyId === SELLER_B);
    check('changing group A persists the new window on A',
        editedA && editedA.selectedDeliveryTimeWindow === editedWindow, editedA);
    check('changing group A leaves group B schedule and line membership unchanged',
        editedB && editedB.selectedDeliveryDate === afterB.selectedDeliveryDate
        && editedB.selectedDeliveryTimeWindow === afterB.selectedDeliveryTimeWindow
        && editedB.cartLineIds.length === 1 && editedB.cartLineIds[0] === lineB.toLowerCase(),
        { before: afterB, after: editedB });
    next = save({ version: prop(edited, 'cartVersion') ?? next.version, editedGroups, editedWindow });
    fs.writeFileSync(path.join(EVID, 'selection-edit-a-153.json'),
        JSON.stringify({ request: selectionBody(next, [
            { lineId: lineA, deliveryDate: hz.byA.minimumDeliveryDate, window: editedWindow },
            { lineId: lineB, deliveryDate: groups[1].deliveryDate, window: groups[1].window },
        ]), response: edited }, null, 2));

    // (D) repeated save identity continuity probe — interleaved with the fail-closed probes.
    const invalidDate = dayOffset(hz.byA.minimumDeliveryDate, -1);
    const dateRes = await putSelection(next, selectionBody(next, [
        { lineId: lineA, deliveryDate: invalidDate, window: editedWindow },
        { lineId: lineB, deliveryDate: groups[1].deliveryDate, window: groups[1].window },
    ]), 'SELECTION_INVALID_DATE');
    check('an invalid (past) date for group A fails closed with a 4xx',
        dateRes.status >= 400 && dateRes.status < 500, { status: dateRes.status, body: prop(unwrap(dateRes.json), 'errorCode') });

    const winRes = await putSelection(next, selectionBody(next, [
        { lineId: lineA, deliveryDate: hz.byA.minimumDeliveryDate, window: '23-24' },
        { lineId: lineB, deliveryDate: groups[1].deliveryDate, window: groups[1].window },
    ]), 'SELECTION_INVALID_WINDOW');
    check('an invalid time window for group A fails closed with a 4xx',
        winRes.status >= 400 && winRes.status < 500, { status: winRes.status, body: prop(unwrap(winRes.json), 'errorCode') });

    const afterInvalid = await projection(next);
    const safe = horizonsOf(afterInvalid).map(routeOf).filter((h) => h.methodCode === CANONICAL_METHOD);
    const safeA = safe.find((h) => h.sellerPartyId === SELLER_A);
    const safeB = safe.find((h) => h.sellerPartyId === SELLER_B);
    check('a rejected write never overwrites the accepted selection (A and B keep the last accepted schedule)',
        safeA && safeB && safeA.selectedDeliveryTimeWindow === editedWindow
        && safeB.selectedDeliveryDate === editedB.selectedDeliveryDate
        && safeB.selectedDeliveryTimeWindow === editedB.selectedDeliveryTimeWindow, { safeA, safeB });

    // (D) repeat the identical accepted PUT and compare group identity.
    const repeatRes = await putSelection(next, selectionBody(next, [
        { lineId: lineA, deliveryDate: hz.byA.minimumDeliveryDate, window: editedWindow },
        { lineId: lineB, deliveryDate: groups[1].deliveryDate, window: groups[1].window },
    ]), 'SELECTION_REPEAT_ACCEPTED');
    const repeatGroups = decodeGroups(unwrap(repeatRes.json) || {});
    const repeatedA = repeatGroups.find((g) => g.sellerPartyId === SELLER_A);
    const repeatedB = repeatGroups.find((g) => g.sellerPartyId === SELLER_B);
    // TB-153 invariant: the edit that touched ONLY group A must not have moved either id, and the
    // identical repeated save must be a strict no-op on identity.
    const firstA = savedGroups.find((g) => g.sellerPartyId === SELLER_A);
    const firstB = savedGroups.find((g) => g.sellerPartyId === SELLER_B);
    const samePromise = (a, b) => !!a && !!b
        && a.shippingGroupId === b.shippingGroupId
        && a.methodCode === b.methodCode
        && a.shippingAmount === b.shippingAmount
        && a.minimumDeliveryDate === b.minimumDeliveryDate
        && a.selectedDeliveryDate === b.selectedDeliveryDate
        && a.selectedDeliveryTimeWindow === b.selectedDeliveryTimeWindow
        && a.cartLineIds.join(',') === b.cartLineIds.join(',');
    const editKeptIds = !!firstA && !!firstB && !!editedA && !!editedB
        && editedA.shippingGroupId === firstA.shippingGroupId
        && editedB.shippingGroupId === firstB.shippingGroupId;
    const idsStable = !!repeatedA && !!repeatedB && !!editedA && !!editedB
        && repeatedA.shippingGroupId === editedA.shippingGroupId
        && repeatedB.shippingGroupId === editedB.shippingGroupId;
    const schedulesStable = !!(repeatedA && repeatedA.selectedDeliveryTimeWindow === editedWindow
        && repeatedB && repeatedB.selectedDeliveryTimeWindow === editedB.selectedDeliveryTimeWindow);
    const bUntouchedByEdit = samePromise(firstB, editedB);
    const aKeptPromise = !!editedA && !!firstA
        && editedA.shippingGroupId === firstA.shippingGroupId
        && editedA.shippingAmount === firstA.shippingAmount
        && editedA.minimumDeliveryDate === firstA.minimumDeliveryDate
        && editedA.cartLineIds.join(',') === firstA.cartLineIds.join(',');
    const identity = {
        stableAcrossRepeatedSave: idsStable,
        editKeptBothIds: editKeptIds,
        groupBByteStableAcrossEdit: bUntouchedByEdit,
        schedulesStable,
        firstSave: { a: firstA && firstA.shippingGroupId, b: firstB && firstB.shippingGroupId },
        afterEditA: { a: editedA && editedA.shippingGroupId, b: editedB && editedB.shippingGroupId },
        afterRepeatSave: { a: repeatedA && repeatedA.shippingGroupId, b: repeatedB && repeatedB.shippingGroupId },
    };
    check('(D) first save allocated two distinct ShippingGroupIds',
        !!firstA && !!firstB && firstA.shippingGroupId !== firstB.shippingGroupId, identity.firstSave);
    check('(D) editing ONLY group A keeps BOTH persisted ShippingGroupIds stable', editKeptIds, identity);
    check('(D) editing only group A leaves group A promise identity/fee/minimum/lines unchanged',
        aKeptPromise, { before: firstA, after: editedA });
    check('(D) editing only group A leaves group B promise byte-identical (id/fee/minimum/date/window/lines)',
        bUntouchedByEdit, { before: firstB, after: editedB });
    check('(D) identical repeated save is idempotent on BOTH ShippingGroupIds', idsStable, identity);
    check('(D) repeated save preserves the accepted per-group schedules', schedulesStable, identity);
    const identityState = { version: prop(unwrap(repeatRes.json) || {}, 'cartVersion') ?? next.version, repeatGroups,
        identityStable: idsStable, identity };
    fs.writeFileSync(path.join(EVID, 'selection-repeat-save-153.json'),
        JSON.stringify({ stableAcrossRepeatedSave: idsStable, identity, groups: repeatGroups }, null, 2));
    return save({ phase: 'save', groups, afterA, afterB, editedA, editedB, safeState: { safeA, safeB }, ...identityState });
}

async function phasePaymentDraft(s) {
    const res = await call('PAYMENT_DRAFT', `${BASE}/v1/storefront/cart/${s.cartId}/payment-draft`, {
        method: 'GET', headers: sessionHeaders(s, s.version),
    });
    const draft = unwrap(res.json) || {};
    fs.writeFileSync(path.join(EVID, 'payment-draft-153.json'), JSON.stringify({ status: res.status, body: draft }, null, 2));
    check('read-only payment-draft endpoint answers HTTP 200 (not 409)', res.status === 200,
        { status: res.status, errorCode: prop(draft, 'errorCode') });
    if (res.status !== 200) return save({ paymentDraftStatus: res.status });

    const draftGroups = decodeGroups(draft);
    check('payment draft exposes exactly two groups mapped to the two sellers on the SAME method',
        draftGroups.length === 2 && new Set(draftGroups.map((g) => g.sellerPartyId)).size === 2
        && draftGroups.every((g) => g.methodCode === CANONICAL_METHOD),
        draftGroups.map((g) => `${g.sellerPartyId}:${g.methodCode}:${g.shippingGroupId}`));
    check('payment draft group identities match the latest accepted save',
        draftGroups.every((g) => s.repeatGroups.some((r) => r.shippingGroupId === g.shippingGroupId)),
        { draft: draftGroups.map((g) => g.shippingGroupId), accepted: s.repeatGroups.map((g) => g.shippingGroupId) });
    check('payment draft exposes each group line mapping (one line each, no shared line)',
        draftGroups.every((g) => g.lineCount === 1 && g.cartLineIds.length === 1)
        && new Set(draftGroups.flatMap((g) => g.cartLineIds)).size === 2, draftGroups.map((g) => g.cartLineIds));
    check('payment draft preserves the requested per-group schedules',
        draftGroups.find((g) => g.sellerPartyId === SELLER_A)?.selectedDeliveryDate === s.groups[0].deliveryDate
        && draftGroups.find((g) => g.sellerPartyId === SELLER_A)?.selectedDeliveryTimeWindow === s.editedWindow
        && draftGroups.find((g) => g.sellerPartyId === SELLER_B)?.selectedDeliveryDate === s.groups[1].deliveryDate
        && draftGroups.find((g) => g.sellerPartyId === SELLER_B)?.selectedDeliveryTimeWindow === s.groups[1].window,
        draftGroups.map((g) => `${g.sellerPartyId}:${g.selectedDeliveryDate}/${g.selectedDeliveryTimeWindow}`));

    const fees = draftGroups.map((g) => Number(g.shippingAmount));
    const draftShipping = Number(prop(draft, 'shippingAmount'));
    check('each group is charged its own fee exactly once and the draft aggregates them',
        fees.length === 2 && fees.every((f) => Number.isFinite(f) && f > 0)
        && Math.abs(draftShipping - fees.reduce((a, b) => a + b, 0)) < 0.0001,
        { groupFees: fees, draftShippingAmount: draftShipping });

    const subtotal = Number(prop(draft, 'subtotalExclusiveOfTax'));
    const tax = Number(prop(draft, 'taxAmount'));
    const discount = Number(prop(draft, 'discountAmount'));
    const payable = Number(prop(draft, 'payableAmount'));
    check('authoritative tax is calculated (> 0) for the two-seller cart', Number.isFinite(tax) && tax > 0, tax);
    check('payable = subtotal - discount + tax + shipping (authoritative arithmetic, no invented share)',
        Math.abs(payable - (subtotal - discount + tax + draftShipping)) < 0.01,
        { subtotal, discount, tax, draftShipping, payable });
    check('payment draft creates no checkout/order (read-only seam)',
        prop(draft, 'checkoutId') === null || prop(draft, 'checkoutId') === undefined, prop(draft, 'checkoutId'));
    observe('(B) authoritative payment draft amounts', {
        subtotalExclusiveOfTax: subtotal, discountAmount: discount, taxAmount: tax,
        shippingAmount: draftShipping, payableAmount: payable,
        groupFees: draftGroups.map((g) => ({ sellerPartyId: g.sellerPartyId, fee: g.shippingAmount })),
    });
    return save({ paymentDraftStatus: res.status, draftGroups, paymentTotals: { subtotal, tax, discount, payable, draftShipping } });
}

async function phaseIntegrity(s) {
    const res = await call('CART_CURRENT', `${BASE}/v1/storefront/cart/current`, {
        method: 'GET', headers: sessionHeaders(s, s.version),
    });
    const cart = unwrap(res.json) || {};
    const lines = (prop(cart, 'lines') || []).map((l) => String(prop(l, 'offerId')).toLowerCase());
    check('cart line membership is unchanged after the whole shipping/draft flow',
        lines.length === 2 && lines.sort().join(',') === [OFFER_A, OFFER_B].sort().join(','), lines);
    fs.writeFileSync(path.join(EVID, 'cart-after-flow-153.json'), JSON.stringify(cart, null, 2));
    return save({ cartAfterFlowLines: lines });
}

async function main() {
    const s = await auth();
    const cartState = await buildCart(s);
    const hz = await phaseHorizons(cartState);
    const saved = await phaseSave(cartState, hz);
    const drafted = await phasePaymentDraft(saved);
    await phaseIntegrity(drafted);

    fs.writeFileSync(path.join(EVID, 'journey-153-trace.txt'), TRACE.join('\n\n'));
    fs.writeFileSync(path.join(OUTDIR, 'journey-153-trace.txt'), TRACE.join('\n\n'));
    const failures = CHECKS.filter((c) => c.result === 'FAIL');
    const blocked = CHECKS.filter((c) => c.result === 'BLOCKED_ENV');
    const observed = CHECKS.filter((c) => c.result === 'OBSERVED');
    const passed = CHECKS.filter((c) => c.result === 'PASS');
    const matrix = [
        'TASK=TB-SHIPPING-GROUP-ID-CONTINUITY-153',
        `BASE=${BASE}`,
        `CANONICAL_METHOD=${CANONICAL_METHOD}`,
        `CHECKS_TOTAL=${CHECKS.length}`,
        `CHECKS_PASS=${passed.length}`,
        `CHECKS_FAIL=${failures.length}`,
        `CHECKS_BLOCKED_ENV=${blocked.length}`,
        `CHECKS_OBSERVED=${observed.length}`,
        '',
        ...CHECKS.map((c) => `${c.result}\t${c.name}\t${c.detail === null ? '' : JSON.stringify(c.detail)}`),
    ].join('\n');
    fs.writeFileSync(path.join(EVID, 'assertion-matrix-153.txt'), matrix + '\n');
    console.log('JOURNEY=' + (failures.length === 0 ? 'PASS' : 'FAIL'));
    if (failures.length > 0) process.exit(4);
}

main().catch((err) => {
    fs.writeFileSync(path.join(EVID, 'journey-153-trace.txt'), TRACE.join('\n\n') + `\n\nFATAL: ${err && err.stack ? err.stack : err}\n`);
    fs.writeFileSync(path.join(EVID, 'assertion-matrix-153.txt'),
        ['TASK=TB-SHIPPING-GROUP-ID-CONTINUITY-153', 'JOURNEY=FATAL',
            String(err && err.message ? err.message : err), '',
            ...CHECKS.map((c) => `${c.result}\t${c.name}`)].join('\n') + '\n');
    console.error('JOURNEY=FATAL ' + (err && err.message ? err.message : err));
    process.exit(5);
});
