// TB-184 stage 3 — attribute EF CommandExecuted events to Home requests by W3C TraceId.
// Reads the diagnostic JSONL produced with Logging__LogLevel__Microsoft.EntityFrameworkCore=Information.
const fs = require('fs');
const path = require('path');
const readline = require('readline');

const repo = process.cwd();
const runDir = path.join(repo, '.tmp-184-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-STOREFRONT-PERF-PROMOTION-BATCH-184');
const logFile = process.env.TB184_LOG || path.join(runDir, 'h-diag.jsonl');
const requests = JSON.parse(fs.readFileSync(path.join(runDir, 'requests-184.json'), 'utf8').replace(/^\uFEFF/, ''));
const wanted = new Map(requests.map((r) => [r.traceId, r]));
const per = new Map();
for (const r of requests) per.set(r.traceId, { commands: 0, commandMs: 0, logBytes: 0, requestPathLines: 0 });

let totalEf = 0;
let totalLines = 0;
let totalLogBytes = 0;
let attributedEf = 0;
let unattributedEf = 0;
const unattributedByCategory = new Map();
const unattributedSamples = [];
let firstRequestTs = null;
let backgroundDuringRequests = 0;

const EF_CATEGORY = 'Microsoft.EntityFrameworkCore.Database.Command';

function extractMs(msg) {
    const m = /^Executed DbCommand \((\d+)ms\)/.exec(msg || '');
    return m ? Number(m[1]) : 0;
}

(async () => {
    const stream = fs.createReadStream(logFile, { encoding: 'utf8' });
    const rl = readline.createInterface({ input: stream, crlfDelay: Infinity });
    for await (const line of rl) {
        if (!line) continue;
        totalLines++;
        const bytes = Buffer.byteLength(line, 'utf8') + 1;
        totalLogBytes += bytes;
        const isEf = line.includes('"' + EF_CATEGORY + '"');
        let traceId = null;
        const sc = line.indexOf('"Scopes"');
        if (sc >= 0) {
            const m = /"TraceId":"([0-9a-f]{32})"/.exec(line.slice(sc));
            if (m) traceId = m[1];
        }
        if (isEf) totalEf++;
        if (traceId && wanted.has(traceId)) {
            const bucket = per.get(traceId);
            bucket.logBytes += bytes;
            if (isEf) {
                bucket.commands++;
                attributedEf++;
                try {
                    const obj = JSON.parse(line);
                    bucket.commandMs += extractMs(obj.Message);
                } catch { /* keep counts, skip ms */ }
            }
            if (line.includes('RequestPath:')) bucket.requestPathLines++;
        } else if (isEf) {
            unattributedEf++;
            let ts = null;
            try { const o = JSON.parse(line); ts = Date.parse(o.Timestamp); } catch {}
            if (ts && firstRequestTs !== null && ts >= firstRequestTs) backgroundDuringRequests++;
            // coarse classification by first table hint
            let cls = 'other';
            const t = line.toLowerCase();
            if (t.includes('from catalog.')) cls = 'catalog';
            else if (t.includes('from pricing.')) cls = 'pricing';
            else if (t.includes('from inventory.')) cls = 'inventory';
            else if (t.includes('from offer.')) cls = 'offer';
            else if (t.includes('from party.')) cls = 'party';
            else if (t.includes('pg_catalog') || t.includes('information_schema')) cls = 'schema';
            else if (t.includes('outbox') || t.includes('transport.')) cls = 'messaging';
            unattributedByCategory.set(cls, (unattributedByCategory.get(cls) || 0) + 1);
            if (unattributedSamples.length < 6 && ts) unattributedSamples.push({ ts: new Date(ts).toISOString(), cls, msg: (line.match(/"Message":"([^"]{0,140})/) || [])[1] });
        }
        if (firstRequestTs === null && traceId && wanted.has(traceId)) {
            try { const o = JSON.parse(line); firstRequestTs = Date.parse(o.Timestamp); } catch {}
        }
    }

    const rows = requests.map((r) => {
        const b = per.get(r.traceId);
        return {
            tag: r.tag, phase: r.phase, traceId: r.traceId, status: r.status, wallMs: r.wallMs,
            payloadBytes: r.payloadBytes, bodySha256: r.bodySha256,
            dbCommands: b.commands, dbCommandMs: b.commandMs, boundedLogBytes: b.logBytes,
        };
    });
    const measured = rows.filter((r) => r.phase !== 'warmup');
    const byTag = {};
    for (const tag of ['size-a', 'size-b']) {
        const rs = measured.filter((r) => r.tag === tag);
        const cmds = rs.map((r) => r.dbCommands);
        byTag[tag] = {
            requests: rs.length,
            dbCommandsPerRequest: cmds,
            meanDbCommands: Math.round((cmds.reduce((a, b) => a + b, 0) / Math.max(1, cmds.length)) * 100) / 100,
            meanDbCommandMs: Math.round((rs.reduce((a, b) => a + b.dbCommandMs, 0) / Math.max(1, rs.length)) * 100) / 100,
            meanWallMs: Math.round((rs.reduce((a, b) => a + b.wallMs, 0) / Math.max(1, rs.length)) * 10) / 10,
            payloadBytes: rs[0] && rs[0].payloadBytes,
            boundedLogBytesMean: Math.round(rs.reduce((a, b) => a + b.boundedLogBytes, 0) / Math.max(1, rs.length)),
        };
    }
    const out = {
        logFile: path.basename(logFile),
        logTotalBytes: totalLogBytes,
        logTotalLines: totalLines,
        totalEfCommandExecuted: totalEf,
        attributedEfCommands: attributedEf,
        unattributedEfCommands: unattributedEf,
        unattributedDuringRequestWindow: backgroundDuringRequests,
        unattributedTopClasses: Object.fromEntries([...unattributedByCategory.entries()].sort((a, b) => b[1] - a[1])),
        unattributedSamples,
        perRequest: rows,
        summary: byTag,
    };
    fs.writeFileSync(path.join(evidenceDir, 'ef-command-counts-184.json'), JSON.stringify(out, null, 2));
    console.log(JSON.stringify({ a: byTag['size-a'], b: byTag['size-b'], totalEf, attributedEf, unattributedEf, logMB: Math.round(totalLogBytes / 1e6) }, null, 2));
})();
