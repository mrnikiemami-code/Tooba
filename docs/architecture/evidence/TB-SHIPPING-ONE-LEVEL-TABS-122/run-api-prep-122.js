// TB-SHIPPING-ONE-LEVEL-TABS-122 — API prerequisites on the ISOLATED scratch target (single pass).
// Uses ONLY canonical existing APIs: Development admin actor, checkout-identity policy seam, storefront
// catalog read, guest cart creation, one genuine offer line, one authorized shipping projection.
// The cart secret is written to an UNCOMMITTED run file only and is redacted everywhere else.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5401';
const EVID = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-122');
const RUN = path.join('.tmp-122-run');
fs.mkdirSync(EVID, { recursive: true });
fs.mkdirSync(RUN, { recursive: true });

const lines = [];
const rec = (k, v) => { lines.push(`${k}=${v}`); };
let adminActor = '';

const redact = (t) => String(t)
    .replace(/"guestSecret"\s*:\s*"[^"]*"/gi, '"guestSecret":"<redacted>"')
    .replace(/"GuestSecret"\s*:\s*"[^"]*"/g, '"GuestSecret":"<redacted>"')
    .slice(0, 4500)
    .replace(/\s+/g, ' ');

async function call(label, url, init = {}) {
    try {
        const res = await fetch(url, { redirect: 'manual', ...init });
        const text = await res.text();
        rec(`${label}.status`, res.status);
        let json = null; try { json = JSON.parse(text); } catch { }
        return { status: res.status, text, json };
    } catch (err) {
        rec(`${label}.status`, 'ERROR');
        rec(`${label}.error`, err.message);
        return { status: 'ERROR', text: '', json: null };
    }
}

const headers = (extra = {}) => ({ 'content-type': 'application/json', ...extra });

async function waitForHealth(limitSeconds) {
    const deadline = Date.now() + limitSeconds * 1000;
    while (Date.now() < deadline) {
        try { const r = await fetch(`${BASE}/health`, { redirect: 'manual' }); if (r.status === 200) return true; } catch { }
        await new Promise((r) => setTimeout(r, 2000));
    }
    return false;
}

async function main() {
    rec('capturedUtc', new Date().toISOString());
    rec('isolatedBase', BASE);
    const healthy = await waitForHealth(150);
    rec('health_reached_within_150s', String(healthy));
    if (!healthy) { rec('ABORT', 'isolated Host never reached /health 200'); return finish(); }

    const commerce = await call('commerce', `${BASE}/__platform-commerce`);
    rec('commerce.body', redact(commerce.text));
    if (!commerce.text.includes('tenant-122-scratch')) { rec('ABORT', 'provenance is not the scratch reference'); return finish(); }

    const devCtx = await call('dev_context', `${BASE}/v1/admin/dev-context`);
    adminActor = String(devCtx.json?.actorUserId ?? devCtx.json?.ActorUserId ?? '');
    rec('dev_context.actor', adminActor);

    const policyBefore = await call('policy_before', `${BASE}/v1/storefront/checkout-identity-policy`);
    rec('policy_before.body', redact(policyBefore.text));
    const policyPut = await call('policy_put', `${BASE}/v1/admin/settings/checkout-identity`, {
        method: 'PUT', headers: headers({ 'x-tooba-dev-actor-user-id': adminActor }),
        body: JSON.stringify({ policy: 'GuestAllowed' }),
    });
    rec('policy_put.body', redact(policyPut.text));
    const policyAfter = await call('policy_after', `${BASE}/v1/storefront/checkout-identity-policy`);
    rec('policy_after.body', redact(policyAfter.text));

    const oneLevel = await call('one_level_admin', `${BASE}/v1/admin/shipping-services/shipping-methods/one-level?language=fa`, {
        headers: headers({ 'x-tooba-dev-actor-user-id': adminActor }),
    });
    rec('one_level_admin.body', redact(oneLevel.text));
    const catalog = (oneLevel.json?.methods ?? oneLevel.json?.Methods ?? []).map((m) => ({
        methodCode: String(m.methodCode ?? m.MethodCode ?? ''),
        name: String(m.name ?? m.Name ?? ''),
        isActive: Boolean(m.isActive ?? m.IsActive),
        sortOrder: Number(m.sortOrder ?? m.SortOrder ?? 0),
    }));
    rec('catalog.rowCount', catalog.length);

    const detail = await call('product_detail', `${BASE}/v1/storefront/products/schema-mobile-demo-phone`);
    const offerIds = [];
    for (const v of (detail.json?.variants ?? [])) {
        const id = v?.primaryOffer?.offerId ?? v?.primaryOffer?.OfferId;
        if (id) offerIds.push(String(id));
    }
    rec('offer_ids_found', offerIds.length);

    const cart = await call('cart_create', `${BASE}/v1/storefront/cart`, { method: 'POST', headers: headers(), body: '{}' });
    const cartId = String(cart.json?.cartId ?? cart.json?.CartId ?? '');
    const guestSecret = String(cart.json?.guestSecret ?? cart.json?.GuestSecret ?? '');
    rec('cart_create.cartId', cartId);
    rec('cart_create.guestSecret', guestSecret ? '<redacted>' : '<absent>');
    if (!cartId || !guestSecret || offerIds.length === 0) { rec('ABORT', 'cart/offer prerequisite unavailable'); return finish(); }

    const line = await call('cart_line', `${BASE}/v1/storefront/cart/${cartId}/lines`, {
        method: 'POST',
        headers: headers({ 'x-tooba-guest-secret': guestSecret, 'x-tooba-cart-version': '1' }),
        body: JSON.stringify({ offerId: offerIds[0], quantity: 1 }),
    });
    rec('cart_line.status_body', `${line.status} ${redact(line.text)}`);

    const projection = await call('projection', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST',
        headers: headers({ 'x-tooba-guest-secret': guestSecret }),
        body: JSON.stringify({ cartId, language: 'fa' }),
    });
    rec('projection.body', redact(projection.text));
    const methods = (projection.json?.methods ?? []).map((m) => ({
        methodCode: String(m.methodCode ?? ''),
        label: String(m.label ?? ''),
        serviceCode: String(m.serviceCode ?? ''),
        optionCode: String(m.optionCode ?? ''),
        priceAmount: m.priceAmount ?? null,
        leadDays: m.leadDays ?? null,
        isFree: Boolean(m.isFree),
    }));
    rec('projection.methodsCount', methods.length);
    for (const m of methods) {
        rec(`projection.method[${m.methodCode}]`,
            `label="${m.label}" serviceCode=${m.serviceCode} optionCode=${m.optionCode} price=${m.priceAmount} leadDays=${m.leadDays} isFree=${m.isFree}`);
    }

    fs.writeFileSync(path.join(RUN, 'api-prep.json'), JSON.stringify({
        cartId, guestSecret, catalog, projectionMethods: methods, capturedUtc: new Date().toISOString(),
    }, null, 2));
    rec('uncommittedSecretsFile', '.tmp-122-run/api-prep.json (never committed)');
    return finish();
}

function finish() {
    fs.writeFileSync(path.join(EVID, 'api-prereq-122.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
