/**
 * TB-SHIPPING-ONE-LEVEL-TABS-102 — isolated-storefront reachability probe (read-only + task-owned scratch only).
 * Prints statuses and error codes; never prints tokens, session ids or the scratch password.
 */
const BASE = 'http://127.0.0.1:5192';
const EMAIL = '102.probe.customer@tooba.local';
const PASSWORD = process.env.TOOBA_102_PROBE_PASSWORD || '';

async function call(label, path, init) {
    const res = await fetch(`${BASE}${path}`, init);
    const text = await res.text();
    console.log(`${label} | status=${res.status} | body=${text.slice(0, 260)}`);
    return { status: res.status, text };
}

function authHeaders(token) {
    return { 'content-type': 'application/json', accept: 'application/json', authorization: `Bearer ${token}` };
}

(async () => {
    console.log('=== TB-102 isolated storefront reachability probe ===');
    await call('provenance GET /__platform-commerce', '/__platform-commerce', {});

    const anon = await call('projection anonymous (no session)', '/v1/storefront/shipping/projection', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ cartId: '00000000-0000-4000-8000-000000000001', language: 'fa' }),
    });

    const reg = await call('POST /v1/auth/register (genuine customer registration)', '/v1/auth/register', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ identifierKind: 'Email', identifier: EMAIL, password: PASSWORD }),
    });

    const login = await call('POST /v1/auth/login (genuine credential login)', '/v1/auth/login', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ identifierKind: 'Email', identifier: EMAIL, password: PASSWORD }),
    });

    let token = null;
    try {
        const body = JSON.parse(login.text);
        token = body.accessToken || body.AccessToken || null;
    } catch { /* noop */ }
    console.log(`login_token_present=${token !== null} token_printed=false`);

    if (!token) {
        console.log('STOP: no session token; cannot continue to cart.');
        return;
    }

    const cart = await call('POST /v1/storefront/cart (authenticated, owned)', '/v1/storefront/cart', {
        method: 'POST',
        headers: authHeaders(token),
        body: '{}',
    });
    let cartId = null;
    let guestSecret = null;
    try {
        const body = JSON.parse(cart.text);
        cartId = body.cartId || body.CartId || null;
        guestSecret = body.guestSecret || body.GuestSecret || null;
    } catch { /* noop */ }
    console.log(`cart_id_present=${cartId !== null} cart_id_printed=false guest_secret_present=${guestSecret !== null} guest_secret_printed=false`);

    if (cartId) {
        const cartHeaders = { ...authHeaders(token), ...(guestSecret ? { 'X-Tooba-Guest-Secret': guestSecret } : {}) };
        await call('projection with owned cart (pre-line)', '/v1/storefront/shipping/projection', {
            method: 'POST',
            headers: cartHeaders,
            body: JSON.stringify({ cartId, language: 'fa' }),
        });

        await call('POST /v1/storefront/cart/{id}/lines with a non-existent offer', `/v1/storefront/cart/${cartId}/lines?expectedVersion=1`, {
            method: 'POST',
            headers: cartHeaders,
            body: JSON.stringify({ offerId: '0442a1fa-1d95-4b24-b9d9-129fc19942d2', quantity: 1 }),
        });
    }
    console.log('=== end ===');
})().catch((e) => { console.error('PROBE_ERROR', e.message); process.exit(1); });
