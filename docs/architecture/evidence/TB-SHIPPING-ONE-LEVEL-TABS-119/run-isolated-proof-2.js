// TB-SHIPPING-ONE-LEVEL-TABS-119 — correcting pass against the SAME single isolated Host launch on :5399
// (no relaunch). Adds ONE canonical cart line using a real scratch offer discovered from the product
// detail payload (variants[].primaryOffer.offerId), then performs the authorized projection proof.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5399';
const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-119');

const lines = [];
const rec = (k, v) => { lines.push(`${k}=${v}`); };

const redact = (t) => String(t)
    .replace(/"guestSecret"\s*:\s*"[^"]*"/gi, '"guestSecret":"<redacted>"')
    .replace(/"GuestSecret"\s*:\s*"[^"]*"/g, '"GuestSecret":"<redacted>"')
    .slice(0, 2200).replace(/\s+/g, ' ');

async function call(label, url, init = {}) {
    const res = await fetch(url, { redirect: 'manual', ...init });
    const text = await res.text();
    rec(`${label}.status`, res.status);
    let json = null; try { json = JSON.parse(text); } catch { }
    return { status: res.status, text, json };
}

async function main() {
    rec('capturedUtc', new Date().toISOString());
    rec('sameSingleLaunch', 'true');

    const commerce = await call('commerce', `${BASE}/__platform-commerce`);
    rec('commerce.body', redact(commerce.text));

    const detail = await call('product_detail', `${BASE}/v1/storefront/products/schema-mobile-demo-phone`);
    const offerIds = [];
    const push = (o) => { const id = o?.offerId ?? o?.OfferId; if (id) offerIds.push(String(id)); };
    for (const v of (detail.json?.variants ?? [])) push(v?.primaryOffer);
    if (offerIds.length === 0 && detail.json?.primaryOffer) push(detail.json.primaryOffer);
    rec('offer_ids_found', offerIds.length);

    const cart = await call('cart_create', `${BASE}/v1/storefront/cart`, {
        method: 'POST', headers: { 'content-type': 'application/json' }, body: '{}',
    });
    const cartId = String(cart.json?.cartId ?? '');
    const guestSecret = String(cart.json?.guestSecret ?? '');
    rec('cart_create.cartId', cartId);
    rec('cart_create.guestSecret', guestSecret ? '<redacted>' : '<absent>');

    for (let i = 0; i < offerIds.length && i < 2; i++) {
        const line = await call(`cart_line_${i}`, `${BASE}/v1/storefront/cart/${cartId}/lines`, {
            method: 'POST',
            headers: { 'content-type': 'application/json', 'x-tooba-guest-secret': guestSecret, 'x-tooba-cart-version': '1' },
            body: JSON.stringify({ offerId: offerIds[i], quantity: 1 }),
        });
        rec(`cart_line_${i}.body`, redact(line.text));
    }

    const projection = await call('projection_no_dest', `${BASE}/v1/storefront/shipping/projection`, {
        method: 'POST',
        headers: { 'content-type': 'application/json', 'x-tooba-guest-secret': guestSecret },
        body: JSON.stringify({ cartId, language: 'fa' }),
    });
    rec('projection_no_dest.body', redact(projection.text));

    const provinces = Array.isArray(projection.json?.provinces) ? projection.json.provinces : [];
    rec('provinces_count', provinces.length);
    const methods = Array.isArray(projection.json?.methods) ? projection.json.methods : [];
    rec('methods_count', methods.length);
    for (const m of methods) {
        rec(`method[${m.methodCode}]`, `label="${m.label}" serviceCode=${m.serviceCode} optionCode=${m.optionCode ?? ''} price=${m.priceAmount} leadDays=${m.leadDays} isFree=${m.isFree}`);
    }
    rec('oneLevelOverlayGateEnabled', 'true');
    rec('observedLabelsAreOneLevelCatalogLabels', 'false (catalog has zero rows on scratch)');

    if (provinces.length > 0) {
        const provinceName = String(provinces[0].label ?? provinces[0].Label ?? '');
        rec('province_used', provinceName);
        const withDest = await call('projection_with_dest', `${BASE}/v1/storefront/shipping/projection`, {
            method: 'POST',
            headers: { 'content-type': 'application/json', 'x-tooba-guest-secret': guestSecret },
            body: JSON.stringify({ cartId, language: 'fa', provinceName }),
        });
        rec('projection_with_dest.body', redact(withDest.text));
        const destMethods = Array.isArray(withDest.json?.methods) ? withDest.json.methods : [];
        rec('dest_methods_count', destMethods.length);
        for (const m of destMethods) rec(`dest_method[${m.methodCode}]`, `label="${m.label}" price=${m.priceAmount}`);
    }

    fs.writeFileSync(path.join(OUT, 'isolated-proof-2.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
