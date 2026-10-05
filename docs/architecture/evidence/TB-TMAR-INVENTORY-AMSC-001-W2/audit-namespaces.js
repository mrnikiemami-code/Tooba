// TB-TMAR-INVENTORY-AMSC-001-W2 — one-shot path<->namespace alignment audit for the Inventory module.
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..', '..', '..', '..', 'src', 'backend', 'Modules', 'Inventory');
const projects = [
  'Tooba.Inventory.Contracts',
  'Tooba.Inventory.Domain',
  'Tooba.Inventory.Application',
  'Tooba.Inventory.Infrastructure',
];

let bad = 0;
function walk(project, dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === 'bin' || entry.name === 'obj') continue;
      walk(project, full);
      continue;
    }
    if (!entry.name.endsWith('.cs')) continue;
    if (entry.name.startsWith('GlobalUsings')) continue;
    const rel = path.relative(path.join(root, project), full).replace(/\\/g, '/');
    const dirPart = path.dirname(rel);
    const expected = dirPart === '.' ? project : project + '.' + dirPart.replace(/\//g, '.');
    const text = fs.readFileSync(full, 'utf8');
    const match = text.match(/^namespace\s+([A-Za-z0-9_.]+)/m);
    const ns = match ? match[1] : '(none)';
    if (ns !== expected) {
      console.log('MISMATCH', rel, 'ns=' + ns, 'expected=' + expected);
      bad++;
    }
  }
}

for (const project of projects) {
  walk(project, path.join(root, project));
}

console.log('path<->namespace mismatches:', bad);
