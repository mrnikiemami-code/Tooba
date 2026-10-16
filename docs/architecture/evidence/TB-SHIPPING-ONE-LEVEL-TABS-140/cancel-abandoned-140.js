/**
 * TB-SHIPPING-ONE-LEVEL-TABS-140 — cancel the abandoned PendingPayment checkouts created by the earlier
 * (failed) acceptance attempts through the canonical storefront cancel route. No DB writes, no bypass.
 */
const fs = require('fs');
const path = require('path');

const BASE = process.env.TOOBA_140_HOST || 'http://127.0.0.1:5408';
const state = JSON.parse(fs.readFileSync(path.join(process.cwd(), '.tmp-140-run', 'journey-state.json'), 'utf8'));
const token = state.accessToken;

const targets = [
    { checkoutId: '01a143b5-0099-7000-8e61-7dad1a6ad736', cartId: '01a143b4-ff95-7000-a730-f0fe6b2db254' },
    { checkoutId: '01a143b5-5b81-7000-b02a-9551eca8e443', cartId: '01a143b5-5aab-7000-a3e7-302474189761' },
];

(async () => {
    for (const t of targets) {
        const res = await fetch(`${BASE}/v1/storefront/checkout/${t.checkoutId}/cancel?cartId=${t.cartId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
            body: '{}',
        });
        console.log(`CANCEL ${t.checkoutId} -> ${res.status} ${(await res.text()).slice(0, 300)}`);
    }
})();
