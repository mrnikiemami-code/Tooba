/**
 * TB-SHIPPING-ONE-LEVEL-TABS-103 — official Development demo bootstrap on the isolated scratch host.
 * Uses only supported HTTP surfaces: admin credential login + the authorized Catalog demo seed API.
 * Never prints tokens or scratch credentials.
 */
const BASE = 'http://127.0.0.1:5292';
const ADMIN_EMAIL = 'admin-actor@tooba.local';
const ADMIN_PASSWORD = process.env.TB103_ADMIN_PASSWORD || ''; // repository Development bootstrap credential, never committed
if (!ADMIN_PASSWORD) { console.error('MISSING TB103_ADMIN_PASSWORD'); process.exit(1); }

async function call(label, path, init) {
    const started = Date.now();
    const res = await fetch(`${BASE}${path}`, init);
    const text = await res.text();
    console.log(`${label} | status=${res.status} | ms=${Date.now() - started} | body=${text.slice(0, 700)}`);
    return { status: res.status, text };
}

(async () => {
    console.log('=== TB-103 isolated official demo bootstrap ===');
    const login = await call('admin login', '/v1/auth/login', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ identifierKind: 'Email', identifier: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    });
    let token = null;
    try { token = JSON.parse(login.text).accessToken || null; } catch { /* noop */ }
    console.log(`admin_token_present=${token !== null}`);

    if (!token) {
        console.log('STOP: admin login failed');
        return;
    }

    const headers = { 'content-type': 'application/json', accept: 'application/json', authorization: `Bearer ${token}` };
    await call('GET demo status (pre)', '/v1/admin/catalog/demo/status', { headers });

    await call(
        'POST demo reset-and-seed (official authorized seed API)',
        '/v1/admin/catalog/demo/reset-and-seed',
        { method: 'POST', headers, body: '{}' });

    await call('GET demo status (post)', '/v1/admin/catalog/demo/status', { headers });
    console.log('=== end ===');
})().catch((e) => { console.error('BOOTSTRAP_ERROR', e.message); process.exit(1); });
