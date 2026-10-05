const fs = require('fs');
const path = require('path');
const root = 'src/backend/Modules/Identity';
function walk(d, out = []) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    if (e.name === 'obj' || e.name === 'bin') continue;
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p, out); else if (e.name.endsWith('.cs')) out.push(p);
  }
  return out;
}
let changed = 0;
for (const f of walk(root)) {
  const src = fs.readFileSync(f, 'utf8');
  const bom = src.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
  const body = bom ? src.slice(1) : src;
  const eol = body.includes('\r\n') ? '\r\n' : '\n';
  const lines = body.split(eol);
  const seen = new Set();
  const out = [];
  let removed = 0;
  for (const line of lines) {
    const m = line.match(/^\s*using\s+([A-Za-z0-9_.]+)\s*;\s*$/);
    if (m) {
      if (seen.has(m[1])) { removed++; continue; }
      seen.add(m[1]);
    }
    out.push(line);
  }
  if (removed > 0) {
    fs.writeFileSync(f, bom + out.join(eol), 'utf8');
    changed++;
    console.log(`dedup ${removed}  ${f}`);
  }
}
console.log('files changed: ' + changed);
