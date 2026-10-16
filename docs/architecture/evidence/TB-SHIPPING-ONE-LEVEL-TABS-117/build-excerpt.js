// TB-SHIPPING-ONE-LEVEL-TABS-117 — build a secrets-free redacted diagnostic excerpt.
const fs = require('fs');
const path = require('path');

const OUT = path.join('docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-117');
const stdoutPath = path.join('.tmp-117-run', 'isolated-host-117-stdout.txt');
const stderrPath = path.join('.tmp-117-run', 'isolated-host-117-stderr.txt');

const raw = fs.readFileSync(stdoutPath, 'utf8');
const lines = raw.split(/\r?\n/).filter((l) => l.length > 0);
const stderrRaw = fs.existsSync(stderrPath) ? fs.readFileSync(stderrPath, 'utf8') : '';

const occurrences = (needle) => raw.split(needle).length - 1;

const categories = new Map();
for (const line of lines) {
    const m = line.match(/"Category":"([^"]+)"/);
    if (m) categories.set(m[1], (categories.get(m[1]) ?? 0) + 1);
}
const categorySummary = [...categories.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([name, count]) => `${name}=${count}`)
    .join(' ');

const messageOf = (line) => {
    const m = line.match(/"Message":"([^"]+)"/);
    return m ? m[1].replace(/\\r\\n/g, ' ').slice(0, 240) : line.slice(0, 240);
};

const warnings = [];
lines.forEach((line, index) => {
    if (!line.includes('"LogLevel":"Warning"')) return;
    const message = line.match(/"Message":"([^"]*)"/);
    const errorType = line.match(/"ErrorType":"([^"]*)"/);
    warnings.push(`line ${index + 1}: WARNING: ${message ? message[1] : ''}${errorType ? ` [ErrorType=${errorType[1]}]` : ''}`);
});

const lifecycle = [];
lines.forEach((line, index) => {
    if (!/Now listening on|Application started|Hosting environment|Content root path/.test(line)) return;
    lifecycle.push(`line ${index + 1}: ${messageOf(line)}`);
});

const stageMarkers = [
    'INSERT INTO catalog.template_products',
    'INSERT INTO catalog.store_templates',
    'INSERT INTO catalog.store_landing_pages',
    'INSERT INTO catalog.store_menus',
];

const out = [];
out.push('# TB-117 REDACTED DIAGNOSTIC EXCERPT (no credentials, no connection strings, no customer identifiers)');
out.push('');
out.push(`stdout_total_lines=${lines.length}`);
out.push(`stderr_total_lines=${stderrRaw.split(/\r?\n/).filter((l) => l.length > 0).length}`);
out.push(`stdout_categories=${categorySummary}`);
out.push('');
out.push('== marker counts over the whole stdout ==');
for (const marker of [
    'platform.edition.unconfigured',
    'Cannot resolve scoped service',
    'from root provider',
    'catalog.attribute.code.duplicate',
    'catalog-dev.attribute.incompatible-reuse',
    'Development seed requires Active tenant',
    'Now listening on',
    'Application started',
]) {
    out.push(`OCC[${marker}]=${occurrences(marker)}`);
}
out.push('');
out.push('== lifecycle lines (message text only) ==');
out.push(...lifecycle);
out.push('');
out.push('== non-information lines in the whole run ==');
if (warnings.length === 0) out.push('none');
out.push(...warnings);
out.push('');
out.push('== legacy bootstrap stage evidence (statement shapes only; parameters never printed) ==');
for (const marker of stageMarkers) {
    const index = lines.findIndex((l) => l.includes(marker));
    out.push(`${marker}: ${index === -1 ? 'not found' : `first seen at stdout line ${index + 1}`} (occurrences: ${occurrences(marker)})`);
}
out.push('');
out.push('== stderr verbatim (empty means no unhandled exception) ==');
out.push(stderrRaw.trim().length === 0 ? '(empty)' : stderrRaw.trim().slice(0, 800));

fs.writeFileSync(path.join(OUT, 'redacted-diagnostic-excerpt.txt'), out.join('\n') + '\n', 'utf8');
console.log(out.join('\n'));
