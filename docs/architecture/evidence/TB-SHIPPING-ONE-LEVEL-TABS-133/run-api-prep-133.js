// TB-SHIPPING-ONE-LEVEL-TABS-133 — API prerequisites on the ISOLATED scratch target.
// Read-mostly: checkout-identity policy seam (Development-only), one-level catalog read, product/offer
// provenance. The guest cart itself is created IN THE BROWSER by real customer storefront actions.
const fs = require('fs');
const path = require('path');

const BASE = 'http://127.0.0.1:5403';
const EVID = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-133');

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
    if (!commerce.text.includes('tenant-133-scratch')) { rec('ABORT', 'provenance is not the scratch reference'); return finish(); }

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

    // PRODUCT + BOTH SELLER OFFERS (provenance for the multi-seller grouping proof).
    const detail = await call('product_detail', `${BASE}/v1/storefront/products/workspace-live-shirt`);
    const primary = detail.json?.primaryOffer ?? detail.json?.PrimaryOffer ?? null;
    const others = detail.json?.otherSellers ?? detail.json?.OtherSellers ?? [];
    rec('product.slug', 'workspace-live-shirt');
    rec('product.title', String(detail.json?.title ?? ''));
    rec('product.taxCategoryLabel', String(primary?.taxCategoryLabel ?? primary?.TaxCategoryLabel ?? ''));
    rec('offer.primary.offerId', String(primary?.offerId ?? ''));
    rec('offer.primary.sellerPartyId', String(primary?.sellerPartyId ?? ''));
    rec('offer.primary.sellerDisplayName', String(primary?.sellerDisplayName ?? ''));
    rec('offer.primary.sellerSku', String(primary?.sellerSku ?? ''));
    rec('offer.primary.amount', String(primary?.amountExclusiveOfTax ?? ''));
    rec('offer.alternate.count', others.length);
    for (const o of others) {
        rec(`offer.alternate[${String(o.sellerDisplayName ?? '')}].offerId`, String(o.offerId ?? ''));
        rec(`offer.alternate[${String(o.sellerDisplayName ?? '')}].amount`, String(o.amountExclusiveOfTax ?? ''));
        rec(`offer.alternate[${String(o.sellerDisplayName ?? '')}].inStock`, String(o.inStock ?? ''));
    }
    const distinctSellers = new Set([String(primary?.sellerPartyId ?? ''), ...others.map((o) => String(o.sellerPartyId ?? ''))].filter(Boolean));
    rec('product.distinctSellerPartyIds', distinctSellers.size);

    fs.writeFileSync(path.join('.tmp-133-run', 'api-prep.json'), JSON.stringify({
        primaryOfferId: primary?.offerId ?? null,
        alternateOfferIds: others.map((o) => o.offerId ?? null),
        catalog, capturedUtc: new Date().toISOString(),
    }, null, 2));
    rec('uncommittedSecretsFile', '.tmp-133-run/api-prep.json (never committed)');
    return finish();
}

function finish() {
    fs.writeFileSync(path.join(EVID, 'api-prereq-133.txt'), lines.join('\n') + '\n', 'utf8');
    console.log(lines.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
