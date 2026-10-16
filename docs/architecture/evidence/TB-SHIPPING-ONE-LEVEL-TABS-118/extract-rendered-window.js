// TB-SHIPPING-ONE-LEVEL-TABS-118 — extract a small, redacted HTML window from the read-only
// rendering of GET http://127.0.0.1:3000/fa/shipping so the visible surface can be cited.
// Nothing is mutated; only the already-fetched SSR HTML is inspected.
const fs = require('fs');
const path = require('path');

const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-118');

async function main() {
    const res = await fetch('http://127.0.0.1:3000/fa/shipping', { redirect: 'manual' });
    const html = await res.text();

    const out = [];
    out.push(`capturedUtc=${new Date().toISOString()}`);
    out.push(`GET /fa/shipping status=${res.status} bytes=${html.length}`);
    out.push('');

    const windows = (needle, radius = 320, max = 2) => {
        const found = [];
        let index = html.indexOf(needle);
        while (index !== -1 && found.length < max) {
            found.push(html.slice(Math.max(0, index - radius), Math.min(html.length, index + needle.length + radius)));
            index = html.indexOf(needle, index + 1);
        }
        return found;
    };

    for (const needle of ['shipping-steps', 'shipping-step-2', 'shipping-step-3', 'در حال آماده‌سازی ارسال', 'روش ارسال']) {
        out.push(`---- occurrences of ${JSON.stringify(needle)} ----`);
        const hits = windows(needle);
        if (hits.length === 0) out.push('(none)');
        hits.forEach((h, i) => {
            out.push(`[${i}] ...${h.replace(/\s+/g, ' ')}...`);
            out.push('');
        });
    }

    // Sanity: confirm the client-only sections are NOT in the server render, i.e. the projection
    // (and therefore the one-level display-name mapping) cannot be observed without identity+cart.
    out.push('---- server-render absence check ----');
    for (const needle of ['shipping-methods', 'shipping-method-post', 'shipping-continue', 'shipping-summary', 'shipping-delivery']) {
        out.push(`${needle}=${html.includes(needle) ? 'PRESENT' : 'absent'}`);
    }

    fs.writeFileSync(path.join(OUT, 'rendered-surface-window.txt'), out.join('\n') + '\n', 'utf8');
    console.log(out.join('\n'));
}

main().catch((err) => { console.error(err); process.exit(1); });
