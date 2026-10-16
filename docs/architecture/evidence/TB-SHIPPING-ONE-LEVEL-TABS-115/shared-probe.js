// TB-SHIPPING-ONE-LEVEL-TABS-115 — READ-ONLY shared baseline / postflight probe.
// Never stops, restarts or mutates anything. GET requests only.
const fs = require('fs');
const path = require('path');

const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-115');
const label = process.argv[2] === 'post' ? 'postflight' : 'preflight';

async function get(url) {
    try {
        const res = await fetch(url, { redirect: 'manual' });
        const text = await res.text();
        return `${res.status} ${text.slice(0, 400).replace(/\s+/g, ' ')}`;
    } catch (err) {
        return `ERROR ${err.message}`;
    }
}

async function main() {
    fs.mkdirSync(OUT, { recursive: true });
    const lines = [];
    lines.push(`capturedUtc=${new Date().toISOString()}`);
    lines.push(`shared_health_5088=${await get('http://127.0.0.1:5088/health')}`);
    lines.push(`shared_platform_commerce=${await get('http://127.0.0.1:5088/__platform-commerce')}`);
    lines.push(`shared_next_3000=${await get('http://127.0.0.1:3000/')}`);
    lines.push(`isolated_port_listeners_expected_zero=5397`);
    const text = lines.join('\n') + '\n';
    fs.writeFileSync(path.join(OUT, `shared-${label}.txt`), text, 'utf8');
    console.log(text);
}

main().catch((err) => { console.error(err); process.exit(1); });
