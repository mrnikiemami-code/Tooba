/**
 * TB-SHIPPING-ONE-LEVEL-TABS-140 — API-driven real storefront journey (no browser).
 * Steps 1-4 (cart + lines + shipping projection + shipping selection write) are probed and logged so the
 * exact server-derived method codes/date/window are used instead of guesses. State is persisted to
 * .tmp-140-run/journey-state.json for the next step.
 */
const fs = require('fs');
const path = require('path');

const BASE = process.env.TOOBA_140_HOST || 'http://127.0.0.1:5408';
const OUTDIR = path.join(process.cwd(), '.tmp-140-run');
const STATE = path.join(OUTDIR, 'journey-state.json');
const LOG = [];

const OFFER_SHIRT = '01a143af-9bfe-7000-942c-3a97974c5628';
const OFFER_SCARF = '01a143af-9cee-7000-963b-e52d0d507af5';

function unwrap(json) {
    if (json && typeof json === 'object' && 'data' in json && json.data !== undefined) return json.data;
    if (json && typeof json === 'object' && 'value' in json && json.value !== undefined) return json.value;
    return json;
}

async function call(label, url, options = {}) {
    const res = await fetch(url, options);
    const text = await res.text();
    let json = null;
    try { json = JSON.parse(text); } catch { /* keep raw */ }
    const line = `${label} -> ${res.status} ${text.slice(0, 1200)}`;
    LOG.push(line);
    console.log(line);
    if (!res.ok) {
        throw new Error(`${label} failed with ${res.status}`);
    }
    return { json, headers: res.headers };
}

async function main() {
    const headers = { 'Content-Type': 'application/json' };

    // 1. guest cart
    const cartRes = await call('CART_CREATE', `${BASE}/v1/storefront/cart`, {
        method: 'POST', headers, body: '{}',
    });
    const cart = unwrap(cartRes.json);
    const cartId = cart.cartId || cart.CartId || cart.id;
    let guestSecret = cart.guestSecret || cart.GuestSecret || '';
    if (!guestSecret) {
        const sc = cartRes.headers.getSetCookie ? cartRes.headers.getSetCookie() : [];
        for (const c of sc) {
            const m = /tooba_guest_secret=([^;]+)/.exec(c);
            if (m) guestSecret = m[1];
        }
    }
    let version = cart.version ?? cart.Version ?? 0;
    if (!cartId) throw new Error('cartId missing in cart create response');

    const authHeaders = { ...headers, 'X-Tooba-Guest-Secret': guestSecret, 'X-Tooba-Cart-Version': String(version) };

    // 2. two lines for the SAME seller
    const line1 = unwrap((await call('CART_LINE_1', `${BASE}/v1/storefront/cart/${cartId}/lines`, {
        method: 'POST', headers: authHeaders, body: JSON.stringify({ offerId: OFFER_SHIRT, quantity: 1 }),
    })).json);
    version = line1.version ?? line1.Version ?? version + 1;
    authHeaders['X-Tooba-Cart-Version'] = String(version);
    const line2 = unwrap((await call('CART_LINE_2', `${BASE}/v1/storefront/cart/${cartId}/lines`, {
        method: 'POST', headers: authHeaders, body: JSON.stringify({ offerId: OFFER_SCARF, quantity: 1 }),
    })).json);
    version = line2.version ?? line2.Version ?? version + 1;
    authHeaders['X-Tooba-Cart-Version'] = String(version);

    const lines = (line2.lines || line2.Lines || []).map((l) => ({
        lineId: l.lineId || l.LineId || l.cartLineId || l.CartLineId,
        offerId: l.offerId || l.OfferId,
        title: l.title || l.Title,
    }));

    fs.writeFileSync(STATE, JSON.stringify({
        cartId, guestSecret, version, lines, phase: 'lines',
    }, null, 2));

    // 3. shipping projection (server-derived methods + dates)
    const projection = unwrap((await call('PROJECTION', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST', headers: authHeaders,
        body: JSON.stringify({ cartId, provinceName: 'تهران', cityName: 'تهران', language: 'fa' }),
    })).json);

    fs.writeFileSync(path.join(OUTDIR, 'projection-140.json'), JSON.stringify(projection, null, 2));
    console.log('STATE_SAVED=' + STATE);

    const projectionText = JSON.stringify(projection);
    const methodCodes = [...new Set((projectionText.match(/"[a-z_]+:[a-z_]+"/g) || []))];
    console.log('CANDIDATE_METHOD_CODES=' + JSON.stringify(methodCodes));
    console.log('LINES=' + JSON.stringify(lines));
}

main().catch((err) => {
    console.error('JOURNEY_PROBE_FAILED: ' + err.message);
    fs.writeFileSync(path.join(OUTDIR, 'journey-probe-failure.txt'), LOG.join('\n') + '\n' + err.stack);
    process.exit(1);
});
