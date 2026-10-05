const fs = require('fs');
const path = require('path');
const roots = ['src/backend/Modules/Identity', 'src/backend/Modules/AccessControl', 'src/backend/Host'];
function walk(d, out = []) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    if (e.name === 'obj' || e.name === 'bin') continue;
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p, out); else if (e.name.endsWith('.cs')) out.push(p);
  }
  return out;
}
const subs = [
  ['Tooba.Identity.Contracts.Problems', 'Tooba.Identity.Contracts.Errors'],
  ['Tooba.Identity.Application.Validators', 'Tooba.Identity.Application.Auth.Validators'],
];
let changed = 0;
for (const r of roots) {
  for (const f of walk(r)) {
    const src = fs.readFileSync(f, 'utf8');
    const bom = src.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
    let body = bom ? src.slice(1) : src;
    let hit = false;
    for (const [from, to] of subs) {
      if (body.includes(from)) { body = body.split(from).join(to); hit = true; }
    }
    if (hit) { fs.writeFileSync(f, bom + body, 'utf8'); changed++; console.log(f); }
  }
}
console.log('files changed: ' + changed);
