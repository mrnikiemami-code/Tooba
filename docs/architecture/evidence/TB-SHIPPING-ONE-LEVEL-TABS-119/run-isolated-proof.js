// TB-SHIPPING-ONE-LEVEL-TABS-119 — ONE bounded authorized proof against the ISOLATED Host on :5399.
// Writes performed are ONLY through existing authorized application APIs on the task-owned scratch DB:
//   1) PUT /v1/admin/settings/checkout-identity  (the canonical Catalog policy seam; Development admin
//      actor taken from the canonical GET /v1/admin/dev-context seam — no fake header invented)
//   2) POST /v1/storefront/cart                  (canonical guest cart creation)
//   3) POST /v1/storefront/cart/{id}/lines       (canonical cart line, real scratch offer)
// Everything else is read-only. No SQL, no commit, no order, no payment, no flag toggle in shared config.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5399';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-119');
fs.mkdirSync(OUT, { recursive: true });

const lines = [];
const rec = (k, v) => { lines.push(`${k}=${v}`); };
let adminActor = '';

function redact(text) {
    return String(text)
        .replace(/"guestSecret"\s*:\s*"[^"]*"/gi, '"guestSecret":"<redacted>"')
        .replace(/"GuestSecret"\s*:\s*"[^"]*"/g, '"GuestSecret":"<redacted>"')
        .slice(0, 1600)
        .replace(/\s+/g, ' ');
}

async function call(label, url, init = {}) {
    try {
        const res = await fetch(url, { redirect: 'manual', ...init });
        const text = await res.text();
        rec(`${label}.status`, res.status);
        return { status: res.status, text, json: (() => { try { return JSON.parse(text); } catch { return null; } })() };
    } catch (err) {
        rec(`${label}.status`, 'ERROR');
        rec(`${label}.error`, err.message);
        return { status: 'ERROR', text: '', json: null };
    }
}

const headers = (extra = {}) => ({ 'content-type': 'application/json', ...extra });
const adminHeaders = () => headers(adminActor ? { 'x-tooba-dev-actor-user-id': adminActor } : {});

async function main() {
    rec('capturedUtc', new Date().toISOString());
    rec('isolatedBase', BASE);

    // 0. provenance first: this must be the scratch target
    const commerce = await call('commerce', `${BASE}/__platform-commerce`);
    rec('commerce.body', redact(commerce.text));
    if (!commerce.text.includes('tenant-119-scratch')) {
        rec('ABORT', 'provenance is not the scratch reference; no proof probe attempted');
        fs.writeFileSync(path.join(OUT, 'isolated-proof.txt'), lines.join('\n') + '\n', 'utf8');
        console.log(lines.join('\n'));
        return;
    }

    // 1. canonical Development admin actor seam
    const devCtx = await call('dev_context', `${BASE}/v1/admin/dev-context`);
    adminActor = String(devCtx.json?.actorUserId ?? devCtx.json?.ActorUserId ?? '');
    rec('dev_context.actor', adminActor);
    rec('dev_context.label', String(devCtx.json?.actorLabel ?? devCtx.json?.ActorLabel ?? ''));

    // 2. canonical admin policy seam: allow guest checkout so the identity gate can pass legitimately
    const policyBefore = await call('policy_before', `${BASE}/v1/storefront/checkout-identity-policy`);
    rec('policy_before.body', redact(policyBefore.text));
    const policyPut = await call('policy_put', `${BASE}/v1/admin/settings/checkout-identity`, {
        method: 'PUT',
        headers: adminHeaders(),
        body: JSON.stringify({ policy: 'GuestAllowed' }),
    });
    rec('policy_put.body', redact(policyPut.text));
    const policyAfter = await call('policy_after', `${BASE}/v1/storefront/checkout-identity-policy`);
    rec('policy_after.body', redact(policyAfter.text));

    // 3. one-level admin read on scratch with the gate ENABLED in the isolated overlay
    const oneLevel = await call('one_level_admin', `${BASE}/v1/admin/shipping-services/shipping-methods/one-level?language=fa`, {
        headers: adminHeaders(),
    });
    rec('one_level_admin.body', redact(oneLevel.text));

    // 4. real scratch offer discovery through the public storefront catalog
    const listing = await call('products', `${BASE}/v1/storefront/products?take=5`);
    rec('products.body', redact(listing.text));

    // 5. canonical guest cart
    const cart = await call('cart_create', `${BASE}/v1/storefront/cart`, { method: 'POST', headers: headers(), body: '{}' });
    const cartId = String(cart.json?.cartId ?? cart.json?.CartId ?? '');
    const guestSecret = String(cart.json?.guestSecret ?? cart.json?.GuestSecret ?? '');
    rec('cart_create.cartId', cartId);
    rec('cart_create.guestSecret', guestSecret ? '<redacted>' : '<absent>');
    if (!cartId) {
        rec('ABORT', 'no scratch cart could be created; projection proof not attempted');
        fs.writeFileSync(path.join(OUT, 'isolated-proof.txt'), lines.join('\n') + '\n', 'utf8');
        console.log(lines.join('\n'));
        return;
    }

    // 6. canonical cart line using a real offer from the scratch catalog, if one was discoverable
    const offers = [];
    const detail = await call('product_detail', `${BASE}/v1/storefront/products/schema-mobile-demo-phone`);
    rec('product_detail.body', redact(detail.text));
    for (const source of [detail.json, listing.json]) {
        const items = Array.isArray(source?.items) ? source.items : Array.isArray(source?.products) ? source.products : (source ? [source] : []);
        for (const item of items) {
            for (const offer of (item?.offers ?? item?.Offers ?? [])) {
                const id = offer?.offerId ?? offer?.OfferId;
                if (id) offers.push(String(id));
            }
        }
    }
    rec('offers_found', offers.length);
    let lineAdded = false;
    if (offers.length > 0) {
        const line = await call('cart_line', `${BASE}/v1/storefront/cart/${cartId}/lines`, {
            method: 'POST',
            headers: headers({ 'x-tooba-guest-secret': guestSecret, 'x-tooba-cart-version': '1' }),
            body: JSON.stringify({ offerId: offers[0], quantity: 1 }),
        });
        rec('cart_line.body', redact(line.text));
        lineAdded = line.status === 200;
    }

    // 7. THE proof: authorized projection on scratch, with and without a destination
    const projection = await call('projection_no_dest', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST',
        headers: headers({ 'x-tooba-guest-secret': guestSecret }),
        body: JSON.stringify({ cartId, language: 'fa' }),
    });
    rec('projection_no_dest.body', redact(projection.text));

    const provinces = Array.isArray(projection.json?.provinces) ? projection.json.provinces : [];
    rec('projection.provinces_count', provinces.length);
    if (provinces.length > 0 && projection.status === 200) {
        const provinceName = String(provinces[0].label ?? provinces[0].Label ?? '');
        rec('projection.province_used', provinceName);
        const withDest = await call('projection_with_dest', `${BASE}/v1/storefront/shipping/projection`, {
            method: 'POST',
            headers: headers({ 'x-tooba-guest-secret': guestSecret }),
            body: JSON.stringify({ cartId, language: 'fa', provinceName }),
        });
        rec('projection_with_dest.body', redact(withDest.text));
    }

    // 8. normalization of the observed methods for the report
    const methods = Array.isArray(projection.json?.methods) ? projection.json.methods : [];
    rec('projection.methods_count', methods.length);
    for (const m of methods.slice(0, 12)) {
        rec(`method[${m.methodCode}]`, `label="${m.label}" serviceCode=${m.serviceCode} optionCode=${m.optionCode ?? m.OptionCode ?? ''} price=${m.priceAmount} leadDays=${m.leadDays}`);
    }
    rec('cart_line_added', lineAdded);
    rec('gate_enabled_in_isolated_overlay', 'true');
    rec('one_level_rows_present', oneLevel.json ? String(Array.isArray(oneLevel.json.methods ?? oneLevel.json.Methods) ? (oneLevel.json.methods ?? oneLevel.json.Methods).length : 'unknown') : 'unknown');

    fs.writeFileSync(path.join(OUT, 'isolated-proof.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
