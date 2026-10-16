// TB-SHIPPING-ONE-LEVEL-TABS-121 — ONE bounded authorized observation pass against the ISOLATED Host
// on :5400 (single launch, scratch provenance). All writes go through existing authorized application
// APIs on the task-owned scratch DB only; no SQL, no activation, no order/payment/checkout commit.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5400';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-121');
fs.mkdirSync(OUT, { recursive: true });

const lines = [];
const rec = (k, v) => { lines.push(`${k}=${v}`); };
let adminActor = '';

const redact = (t) => String(t)
    .replace(/"guestSecret"\s*:\s*"[^"]*"/gi, '"guestSecret":"<redacted>"')
    .replace(/"GuestSecret"\s*:\s*"[^"]*"/g, '"GuestSecret":"<redacted>"')
    .slice(0, 4000)
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
const adminHeaders = () => headers(adminActor ? { 'x-tooba-dev-actor-user-id': adminActor } : {});

async function waitForHealth(limitSeconds) {
    const deadline = Date.now() + limitSeconds * 1000;
    while (Date.now() < deadline) {
        try {
            const res = await fetch(`${BASE}/health`, { redirect: 'manual' });
            if (res.status === 200) { return true; }
        } catch { }
        await new Promise((r) => setTimeout(r, 2000));
    }
    return false;
}

async function main() {
    rec('capturedUtc', new Date().toISOString());
    rec('isolatedBase', BASE);

    const healthy = await waitForHealth(150);
    rec('health_reached_within_150s', String(healthy));
    if (!healthy) {
        rec('ABORT', 'isolated Host never reached /health 200 within the bounded wait');
        fs.writeFileSync(path.join(OUT, 'isolated-proof-121.txt'), lines.join('\n') + '\n', 'utf8');
        console.log(lines.join('\n'));
        return;
    }

    // 0. provenance first: this must be the scratch target
    const commerce = await call('commerce', `${BASE}/__platform-commerce`);
    rec('commerce.body', redact(commerce.text));
    if (!commerce.text.includes('tenant-121-scratch')) {
        rec('ABORT', 'provenance is not the scratch reference; no proof probe attempted');
        fs.writeFileSync(path.join(OUT, 'isolated-proof-121.txt'), lines.join('\n') + '\n', 'utf8');
        console.log(lines.join('\n'));
        return;
    }

    // 1. canonical Development admin actor seam (no invented header)
    const devCtx = await call('dev_context', `${BASE}/v1/admin/dev-context`);
    adminActor = String(devCtx.json?.actorUserId ?? devCtx.json?.ActorUserId ?? '');
    rec('dev_context.actor', adminActor);
    rec('dev_context.label', String(devCtx.json?.actorLabel ?? devCtx.json?.ActorLabel ?? ''));

    // 2. THE catalog proof: enabled one-level catalog on a freshly booted scratch DB.
    const oneLevel = await call('one_level_admin', `${BASE}/v1/admin/shipping-services/shipping-methods/one-level?language=fa`, {
        headers: adminHeaders(),
    });
    rec('one_level_admin.body', redact(oneLevel.text));

    const methods = Array.isArray(oneLevel.json?.methods) ? oneLevel.json.methods
        : Array.isArray(oneLevel.json?.Methods) ? oneLevel.json.Methods : [];
    rec('one_level.gate', String(oneLevel.json?.gate ?? oneLevel.json?.Gate ?? ''));
    rec('one_level.catalogReady', String(oneLevel.json?.catalogReady ?? oneLevel.json?.CatalogReady ?? ''));
    rec('one_level.languageId', String(oneLevel.json?.languageId ?? oneLevel.json?.LanguageId ?? ''));
    rec('one_level.rows_count', methods.length);

    const catalogByCode = new Map();
    for (const m of methods) {
        const code = String(m.methodCode ?? m.MethodCode ?? '');
        const service = String(m.legacyServiceCode ?? m.LegacyServiceCode ?? '');
        const option = m.legacyOptionCode ?? m.LegacyOptionCode ?? '';
        const active = String(m.isActive ?? m.IsActive ?? '');
        const translations = m.translations ?? m.Translations ?? [];
        const fa = translations.find((t) => String(t.languageId ?? t.LanguageId ?? '') !== '' &&
            String(t.languageId ?? t.LanguageId ?? '').toLowerCase() ===
            String(oneLevel.json?.languageId ?? oneLevel.json?.LanguageId ?? '').toLowerCase());
        const name = String(fa?.name ?? fa?.Name ?? (translations[0]?.name ?? translations[0]?.Name) ?? '');
        rec(`catalog_row[${code}]`, `serviceCode=${service} optionCode=${option} isActive=${active} name="${name}"`);
        catalogByCode.set(code, { service, option, active, name });
    }

    // 3. canonical admin policy seam: allow guest checkout so the identity gate passes legitimately
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

    // 4. canonical guest cart + one real scratch offer line
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
    if (!cartId || offerIds.length === 0) {
        rec('ABORT', 'cart or offer prerequisite unavailable; projection proof not attempted');
        fs.writeFileSync(path.join(OUT, 'isolated-proof-121.txt'), lines.join('\n') + '\n', 'utf8');
        console.log(lines.join('\n'));
        return;
    }

    const line = await call('cart_line', `${BASE}/v1/storefront/cart/${cartId}/lines`, {
        method: 'POST',
        headers: headers({ 'x-tooba-guest-secret': guestSecret, 'x-tooba-cart-version': '1' }),
        body: JSON.stringify({ offerId: offerIds[0], quantity: 1 }),
    });
    rec('cart_line.body', redact(line.text));

    // 5. THE projection proof (ONE bounded call): are the labels sourced from the one-level catalog?
    const projection = await call('projection', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST',
        headers: headers({ 'x-tooba-guest-secret': guestSecret }),
        body: JSON.stringify({ cartId, language: 'fa' }),
    });
    rec('projection.body', redact(projection.text));

    const projected = Array.isArray(projection.json?.methods) ? projection.json.methods : [];
    rec('projection.methods_count', projected.length);
    let catalogSourced = 0;
    let mismatched = 0;
    for (const m of projected) {
        const code = String(m.methodCode ?? '');
        const label = String(m.label ?? '');
        const row = catalogByCode.get(code);
        const matches = row ? row.name : '';
        const isSourced = row && row.name === label;
        if (isSourced) catalogSourced++; else mismatched++;
        rec(`projected[${code}]`,
            `label="${label}" serviceCode=${m.serviceCode} optionCode=${m.optionCode ?? ''} price=${m.priceAmount}` +
            ` | catalogName="${matches}" catalogMatch=${isSourced}`);
    }
    rec('projected_labels_matching_one_level_catalog', catalogSourced);
    rec('projected_labels_not_matching', mismatched);
    rec('gate_enabled_in_isolated_overlay', 'true');

    fs.writeFileSync(path.join(OUT, 'isolated-proof-121.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
