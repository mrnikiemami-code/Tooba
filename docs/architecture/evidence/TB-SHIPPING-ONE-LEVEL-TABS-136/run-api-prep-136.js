// TB-SHIPPING-ONE-LEVEL-TABS-136 — API prerequisites + readback on the ISOLATED scratch target.
// Read-mostly: checkout-identity policy seam (Development-only), one-level catalog read, and the
// provenance of BOTH live products/offers. The guest cart itself is created IN THE BROWSER by real
// customer storefront actions. No shared environment is touched.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5405';
const EVID = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-136');

const lines = [];
const rec = (k, v) => { lines.push(`${k}=${v}`); };
const redact = (t) => String(t).replace(/"guestSecret"\s*:\s*"[^"]*"/gi, '"guestSecret":"<redacted>"').slice(0, 2000).replace(/\s+/g, ' ');

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

async function main() {
    rec('capturedUtc', new Date().toISOString());
    rec('isolatedBase', BASE);

    const commerce = await call('commerce', `${BASE}/__platform-commerce`);
    rec('commerce.body', redact(commerce.text));
    if (!commerce.text.includes('tenant-136-scratch')) { rec('ABORT', 'provenance is not the scratch reference'); return finish(); }

    const devCtx = await call('dev_context', `${BASE}/v1/admin/dev-context`);
    const adminActor = String(devCtx.json?.actorUserId ?? devCtx.json?.ActorUserId ?? '');
    rec('dev_context.actor', adminActor);

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

    // PRODUCT 1 — the pre-existing live demo product.
    const shirt = await call('product1', `${BASE}/v1/storefront/products/workspace-live-shirt`);
    rec('product1.slug', 'workspace-live-shirt');
    rec('product1.title', String(shirt.json?.title ?? ''));
    rec('product1.primary.seller', String(shirt.json?.primaryOffer?.sellerDisplayName ?? ''));
    rec('product1.primary.sellerSku', String(shirt.json?.primaryOffer?.sellerSku ?? ''));
    const otherSellers = shirt.json?.otherSellers ?? [];
    for (const o of otherSellers) {
        rec(`product1.other[${String(o.sellerDisplayName ?? '')}].offerId`, String(o.offerId ?? ''));
        rec(`product1.other[${String(o.sellerDisplayName ?? '')}].sellerSku`, String(o.sellerSku ?? ''));
        rec(`product1.other[${String(o.sellerDisplayName ?? '')}].amount`, String(o.amountExclusiveOfTax ?? ''));
    }

    // PRODUCT 2 — the TB-136 Development-only companion product.
    const scarf = await call('product2', `${BASE}/v1/storefront/products/workspace-live-scarf`);
    rec('product2.slug', 'workspace-live-scarf');
    rec('product2.title', String(scarf.json?.title ?? ''));
    rec('product2.primary.offerId', String(scarf.json?.primaryOffer?.offerId ?? ''));
    rec('product2.primary.sellerPartyId', String(scarf.json?.primaryOffer?.sellerPartyId ?? ''));
    rec('product2.primary.seller', String(scarf.json?.primaryOffer?.sellerDisplayName ?? ''));
    rec('product2.primary.sellerSku', String(scarf.json?.primaryOffer?.sellerSku ?? ''));
    rec('product2.primary.amount', String(scarf.json?.primaryOffer?.amountExclusiveOfTax ?? ''));
    rec('product2.primary.availableUnits', String(scarf.json?.primaryOffer?.availableUnits ?? ''));
    rec('product2.primary.taxCategory', String(scarf.json?.primaryOffer?.taxCategoryLabel ?? ''));

    fs.writeFileSync(path.join('.tmp-136-run', 'api-prep.json'), JSON.stringify({
        shirtPrimaryOfferId: shirt.json?.primaryOffer?.offerId ?? null,
        shirtSellerAOfferId: otherSellers[0]?.offerId ?? null,
        scarfOfferId: scarf.json?.primaryOffer?.offerId ?? null,
        catalog, capturedUtc: new Date().toISOString(),
    }, null, 2));
    rec('uncommittedSecretsFile', '.tmp-136-run/api-prep.json (never committed)');
    return finish();
}

function finish() {
    fs.writeFileSync(path.join(EVID, 'api-prereq-136.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
