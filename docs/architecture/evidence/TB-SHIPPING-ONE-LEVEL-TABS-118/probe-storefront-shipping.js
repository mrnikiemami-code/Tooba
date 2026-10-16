// TB-SHIPPING-ONE-LEVEL-TABS-118 — READ-ONLY storefront shipping surface probe.
// GET-only plus one identity-less POST that is rejected by the Host identity gate BEFORE any handler
// runs (CheckoutIdentityGate is a pure read of the Catalog policy). No cookie, no session, no cartId,
// no guest secret, no X-Tooba-Dev-Actor-User-Id header, no mutation, no flag toggle.
const fs = require('fs');
const path = require('path');

const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-118');
fs.mkdirSync(OUT, { recursive: true });

const lines = [];
const record = (label, value) => { lines.push(`${label}=${value}`); };

async function probe(label, url, init = {}) {
    try {
        const res = await fetch(url, { redirect: 'manual', ...init });
        const text = await res.text();
        return { label, status: res.status, location: res.headers.get('location') || '', text };
    } catch (err) {
        return { label, status: 'ERROR', location: '', text: err.message };
    }
}

const summarise = (r) => `${r.status}${r.location ? ` location=${r.location}` : ''} bytes=${r.text.length}`;

async function main() {
    record('capturedUtc', new Date().toISOString());

    // --- shared identity (read-only) -------------------------------------------------------------
    const health = await probe('health', 'http://127.0.0.1:5088/health');
    const commerce = await probe('commerce', 'http://127.0.0.1:5088/__platform-commerce');
    record('host_health', summarise(health));
    record('host_commerce', summarise(commerce));
    record('host_commerce_body', commerce.text.slice(0, 400).replace(/\s+/g, ' '));

    // --- storefront shipping page rendering (direct read-only UI observation) ---------------------
    const page = await probe('page', 'http://127.0.0.1:3000/fa/shipping');
    const pageRoot = await probe('page_root', 'http://127.0.0.1:3000/shipping');
    record('next_fa_shipping', summarise(page));
    record('next_root_shipping', summarise(pageRoot));

    const markers = [
        'data-testid="shipping-page"',
        'data-testid="shipping-steps"',
        'data-testid="shipping-step-1"',
        'data-testid="shipping-step-2"',
        'data-testid="shipping-step-3"',
        'data-testid="shipping-address-section"',
        'data-testid="shipping-methods"',
        'data-testid="shipping-methods-empty"',
        'data-testid="shipping-method-',
        'data-testid="shipping-delivery"',
        'data-testid="shipping-continue"',
        'data-testid="shipping-summary"',
        'اطلاعات ارسال',
        'روش ارسال',
        'ادامه به پرداخت',
        'سبد خرید پیدا نشد',
        'در حال آماده‌سازی ارسال',
    ];
    for (const marker of markers) {
        record(`page_marker[${marker}]`, page.text.includes(marker) ? 'PRESENT' : 'absent');
    }

    // --- Host storefront shipping endpoints: route existence without invoking the write path ------
    const projectionGet = await probe('projection_get', 'http://127.0.0.1:5088/v1/storefront/shipping/projection');
    const selectionGet = await probe('selection_get', 'http://127.0.0.1:5088/v1/storefront/shipping/selection');
    const commitGet = await probe('commit_get', 'http://127.0.0.1:5088/v1/storefront/shipping/commit');
    record('GET /v1/storefront/shipping/projection', summarise(projectionGet));
    record('GET /v1/storefront/shipping/selection', summarise(selectionGet));
    record('GET /v1/storefront/shipping/commit', summarise(commitGet));

    // --- the exact gate: identity-less POST/PUT is rejected by CheckoutIdentityGate (read-only) ---
    const projectionPost = await probe('projection_post', 'http://127.0.0.1:5088/v1/storefront/shipping/projection', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ cartId: '00000000-0000-0000-0000-000000000000', language: 'fa' }),
    });
    record('POST /v1/storefront/shipping/projection (no identity)', summarise(projectionPost));
    record('POST projection body', projectionPost.text.slice(0, 300).replace(/\s+/g, ' '));

    // --- admin one-level read surface: does it exist and is it authorized? -----------------------
    const adminRead = await probe('admin_read', 'http://127.0.0.1:5088/v1/admin/shipping-services/shipping-methods/one-level?language=fa');
    record('GET /v1/admin/shipping-services/shipping-methods/one-level', summarise(adminRead));
    record('admin read body', adminRead.text.slice(0, 300).replace(/\s+/g, ' '));

    // --- admin one-level activation gate check: route existence only (no PUT) ---------------------
    const activationGet = await probe('activation_get', 'http://127.0.0.1:5088/v1/admin/shipping-services/shipping-methods/one-level/post/activation');
    record('GET .../one-level/post/activation', summarise(activationGet));

    const text = lines.join('\n') + '\n';
    fs.writeFileSync(path.join(OUT, 'storefront-probe.txt'), text, 'utf8');
    console.log(text);
}

main().catch((err) => { console.error(err); process.exit(1); });
