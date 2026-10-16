// TB-SHIPPING-ONE-LEVEL-TABS-123 — API prerequisites on the ISOLATED scratch target (single pass).
// Uses ONLY canonical existing APIs: Development admin actor, checkout-identity policy seam, one-level
// catalog read, guest cart creation, one genuine offer line, and one authorized projection WITH a
// methodCode so the delivery-date horizon the UI needs is exercised. The cart secret stays in an
// UNCOMMITTED run file only.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5402';
const EVID = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-123');
const RUN = path.join('.tmp-123-run');
fs.mkdirSync(EVID, { recursive: true });
fs.mkdirSync(RUN, { recursive: true });

const lines = [];
const rec = (k, v) => { lines.push(`${k}=${v}`); };

const redact = (t) => String(t)
    .replace(/"guestSecret"\s*:\s*"[^"]*"/gi, '"guestSecret":"<redacted>"')
    .replace(/"GuestSecret"\s*:\s*"[^"]*"/g, '"GuestSecret":"<redacted>"')
    .slice(0, 2500)
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
    if (!commerce.text.includes('tenant-123-scratch')) { rec('ABORT', 'provenance is not the scratch reference'); return finish(); }

    const devCtx = await call('dev_context', `${BASE}/v1/admin/dev-context`);
    const adminActor = String(devCtx.json?.actorUserId ?? devCtx.json?.ActorUserId ?? '');
    rec('dev_context.actor', adminActor);

    const policyBefore = await call('policy_before', `${BASE}/v1/storefront/checkout-identity-policy`);
    rec('policy_before.body', redact(policyBefore.text));
    const policyPut = await call('policy_put', `${BASE}/v1/admin/settings/checkout-identity`, {
        method: 'PUT', headers: headers({ 'x-tooba-dev-actor-user-id': adminActor }),
        body: JSON.stringify({ policy: 'GuestAllowed' }),
    });
    rec('policy_put.status', policyPut.status);
    const policyAfter = await call('policy_after', `${BASE}/v1/storefront/checkout-identity-policy`);
    rec('policy_after.body', redact(policyAfter.text));

    const oneLevel = await call('one_level_admin', `${BASE}/v1/admin/shipping-services/shipping-methods/one-level?language=fa`, {
        headers: headers({ 'x-tooba-dev-actor-user-id': adminActor }),
    });
    const catalog = (oneLevel.json?.methods ?? oneLevel.json?.Methods ?? []).map((m) => ({
        methodCode: String(m.methodCode ?? m.MethodCode ?? ''),
        name: String(m.name ?? m.Name ?? ''),
        isActive: Boolean(m.isActive ?? m.IsActive),
    }));
    rec('catalog.gate', String(oneLevel.json?.gate ?? oneLevel.json?.Gate ?? ''));
    rec('catalog.ready', String(oneLevel.json?.ready ?? oneLevel.json?.Ready ?? ''));
    rec('catalog.rowCount', catalog.length);
    for (const c of catalog) rec(`catalog.row[${c.methodCode}]`, `name="${c.name}" isActive=${c.isActive}`);

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
    rec('cart_line.status', line.status);

    // Projection WITHOUT method: documents the empty deliveryDates horizon the UI starts from.
    const projNoMethod = await call('projection_no_method', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST', headers: headers({ 'x-tooba-guest-secret': guestSecret }),
        body: JSON.stringify({ cartId, language: 'fa' }),
    });
    rec('projection_no_method.methodsCount', (projNoMethod.json?.methods ?? []).length);
    rec('projection_no_method.deliveryDatesCount', (projNoMethod.json?.deliveryDates ?? []).length);
    rec('projection_no_method.deliveryTimeWindowsCount', (projNoMethod.json?.deliveryTimeWindows ?? []).length);

    // Projection WITH the representative method: this is the horizon the UI needs to render dates.
    const METHOD = 'tipax:standard';
    const projWithMethod = await call('projection_with_method', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST', headers: headers({ 'x-tooba-guest-secret': guestSecret }),
        body: JSON.stringify({ cartId, language: 'fa', methodCode: METHOD, provinceName: 'تهران', cityName: 'تهران' }),
    });
    const methods = projWithMethod.json?.methods ?? [];
    rec('projection_with_method.methodsCount', methods.length);
    const chosen = methods.find((m) => String(m.methodCode) === METHOD);
    rec('projection_with_method.chosen',
        chosen ? `label="${chosen.label}" serviceCode=${chosen.serviceCode} optionCode=${chosen.optionCode} price=${chosen.priceAmount}` : 'NOT FOUND');
    rec('projection_with_method.deliveryDatesCount', (projWithMethod.json?.deliveryDates ?? []).length);
    rec('projection_with_method.minimumDeliveryDate', String(projWithMethod.json?.minimumDeliveryDate ?? ''));
    rec('projection_with_method.deliveryTimeWindowsCount', (projWithMethod.json?.deliveryTimeWindows ?? []).length);
    for (const d of (projWithMethod.json?.deliveryDates ?? []).slice(0, 10)) rec('projection_with_method.date', String(d.value ?? d.Value ?? ''));

    fs.writeFileSync(path.join(RUN, 'api-prep.json'), JSON.stringify({
        cartId, guestSecret, catalog, chosenMethod: METHOD, capturedUtc: new Date().toISOString(),
    }, null, 2));
    rec('uncommittedSecretsFile', '.tmp-123-run/api-prep.json (never committed)');
    return finish();
}

function finish() {
    fs.writeFileSync(path.join(EVID, 'api-prereq-123.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
