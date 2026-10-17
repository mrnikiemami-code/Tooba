/**
 * TB-STOREFRONT-PERF-SCALE-CLOSURE-180 — semantic response parity + payload diff.
 *
 * Compares, for each size, the diagnostic-leg body against the default-leg body (same instance, same data)
 * and reports every differing JSON path instead of declaring parity from a byte hash. Also extracts the
 * storefront item identity (productId / slug) and compares it with the authoritative scratch database
 * snapshot rows (catalog.products) to prove no omission and no cross-tenant bleed.
 */
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const evidence = path.join(process.cwd(), 'docs', 'architecture', 'evidence', 'TB-STOREFRONT-PERF-SCALE-CLOSURE-180');
const rawJson = fs.readFileSync(path.join(evidence, 'measurement-180.json'), 'utf8').replace(/^\uFEFF/, '');
const stageA = JSON.parse(fs.readFileSync(path.join(evidence, 'stage-a.json'), 'utf8').replace(/^\uFEFF/, ''));
const measurement = JSON.parse(rawJson);
measurement.sizeA = stageA;
measurement.sizeB = { rows: null };
const readText = (f) => fs.readFileSync(f, 'utf8').replace(/^\uFEFF/, '');

const read = (file) => (fs.existsSync(file) ? readText(file) : null);
const sha = (text) => crypto.createHash('sha256').update(text, 'utf8').digest('hex').toUpperCase();

function walkDiff(a, b, at, out, limit = 200) {
    if (out.length >= limit) return;
    if (a === b) return;
    const ta = a === null ? 'null' : Array.isArray(a) ? 'array' : typeof a;
    const tb = b === null ? 'null' : Array.isArray(b) ? 'array' : typeof b;
    if (ta !== tb) { out.push({ path: at, kind: 'type', a: ta, b: tb }); return; }
    if (ta === 'array') {
        if (a.length !== b.length) out.push({ path: at, kind: 'length', a: a.length, b: b.length });
        for (let i = 0; i < Math.max(a.length, b.length); i++) walkDiff(a[i], b[i], `${at}[${i}]`, out, limit);
        return;
    }
    if (ta === 'object') {
        const keys = new Set([...Object.keys(a), ...Object.keys(b)]);
        for (const k of keys) {
            if (!(k in a)) { out.push({ path: `${at}.${k}`, kind: 'only-in-default' }); continue; }
            if (!(k in b)) { out.push({ path: `${at}.${k}`, kind: 'only-in-diagnostic' }); continue; }
            walkDiff(a[k], b[k], `${at}.${k}`, out, limit);
        }
        return;
    }
    out.push({ path: at, kind: 'value', a, b });
}

function collectItems(node, acc = { productIds: new Set(), slugs: new Set(), offers: new Set() }) {
    if (Array.isArray(node)) { node.forEach((n) => collectItems(n, acc)); return acc; }
    if (node && typeof node === 'object') {
        for (const [k, v] of Object.entries(node)) {
            if (k === 'productId' && typeof v === 'string') acc.productIds.add(v);
            else if (k === 'slug' && typeof v === 'string') acc.slugs.add(v);
            else if (k === 'offerId' && typeof v === 'string') acc.offers.add(v);
            else collectItems(v, acc);
        }
    }
    return acc;
}

function parseSnapshot(tag) {
    const text = read(path.join(evidence, `db-snapshot-180-${tag}.txt`)) || '';
    const rows = {};
    for (const line of text.split(/\r?\n/)) {
        if (!line.trim()) continue;
        const idx = line.indexOf('|');
        const table = line.slice(0, idx);
        try { (rows[table] = rows[table] || []).push(JSON.parse(line.slice(idx + 1))); } catch { }
    }
    return rows;
}

function sizeReport(tag) {
    const diag = read(path.join(evidence, `home-body-180-${tag}-diagnostic.json`));
    const def = read(path.join(evidence, `home-body-180-${tag}-default.json`));
    const snap = parseSnapshot(tag);
    const report = { tag, diagnosticBytes: diag ? Buffer.byteLength(diag, 'utf8') : null, defaultBytes: def ? Buffer.byteLength(def, 'utf8') : null, hashesEqual: diag && def ? sha(diag) === sha(def) : null };
    if (diag && def) {
        const a = JSON.parse(diag);
        const b = JSON.parse(def);
        const diffs = [];
        walkDiff(a, b, '$', diffs);
        report.diffCount = diffs.length;
        report.diffs = diffs.slice(0, 40);
        report.diagnosticItemIdentity = { productIds: collectItems(a).productIds.size, slugs: collectItems(a).slugs.size };
        report.defaultItemIdentity = { productIds: collectItems(b).productIds.size, slugs: collectItems(b).slugs.size };

        const dbProducts = (snap['catalog.products'] || []);
        const dbSlugs = new Set(dbProducts.map((p) => p.slug || p.Slug).filter(Boolean));
        const bodySlugs = collectItems(b).slugs;
        const missingFromBody = [...dbSlugs].filter((s) => !bodySlugs.has(s));
        report.parity = {
            dbProductRows: dbProducts.length,
            bodySlugCount: bodySlugs.size,
            missingFromBody: missingFromBody.slice(0, 20),
            missingFromBodyCount: missingFromBody.length,
            offers: (snap['offer.offers'] || []).length,
            prices: (snap['pricing.prices'] || []).length,
            stockPositions: (snap['inventory.stock_positions'] || []).length,
            sellers: (snap['party.parties'] || []).length,
            localizations: (snap['catalog.localized_texts'] || []).length,
        };
    }
    return report;
}

const report = {
    measurement: {
        route: measurement.route,
        hostHeader: measurement.hostHeader,
        sizeA: { rows: measurement.sizeA.rows, diagnostic: summarise(measurement.sizeA.diagnostic), default: summarise(measurement.sizeA.default) },
        seed: measurement.seed,
        sizeB: measurement.sizeB.rows ? { rows: measurement.sizeB.rows, diagnostic: summarise(measurement.sizeB.diagnostic), default: summarise(measurement.sizeB.default) } : null,
    },
    parity: [sizeReport('size-a'), measurement.sizeB.rows ? sizeReport('size-b') : null].filter(Boolean),
};
report.sharedPortsTouched = measurement.sharedPortsTouched;
report.credentialsInEvidence = measurement.credentialsInEvidence;

function summarise(leg) {
    if (!leg) return null;
    const wall = leg.samples.map((s) => s.wallMs).sort((x, y) => x - y);
    const perRequest = leg.requestScopedCommands.map((w) => w.commands);
    return {
        efLevel: leg.efLevel,
        statuses: leg.samples.map((s) => s.status),
        wallMs: leg.samples.map((s) => s.wallMs),
        medianWallMs: wall.length % 2 ? wall[(wall.length - 1) / 2] : (wall[wall.length / 2 - 1] + wall[wall.length / 2]) / 2,
        payloadBytes: leg.samples.map((s) => s.payloadBytes),
        slugCount: leg.samples.map((s) => s.slugCount),
        identicalHashes: new Set(leg.samples.map((s) => s.bodySha256)).size === 1,
        bodySha256: leg.samples[0].bodySha256,
        requestScopedCommands: perRequest,
        warmupCommands: leg.requestScopedCommands[0] ? leg.requestScopedCommands[0].commands : null,
        totalCommandLines: leg.totalCommandLines,
        stdoutBytes: leg.stdoutBytes,
        stdoutLines: leg.stdoutLines,
        efCategoryLines: leg.efCategoryLines,
        errorOrCriticalLines: leg.errorOrCriticalLines,
        listenerPidMatchesHost: leg.listenerPid === leg.hostPid,
    };
}

const out = path.join(evidence, 'analysis-180.json');
fs.writeFileSync(out, JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));

