// TB-TMAR-USERPREFERENCE-AMSC-001-W0 — read-only inspection helper.
// Verifies path<->namespace exactness and enumerates the module surface. No writes.
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..', '..', '..', '..', 'src', 'backend', 'Modules', 'UserPreference');
const projects = fs.readdirSync(root).filter((d) => fs.statSync(path.join(root, d)).isDirectory());

let total = 0;
let mismatches = 0;
const perProject = {};

const walk = (dir, project) => {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === 'obj' || entry.name === 'bin') continue;
      walk(full, project);
      continue;
    }
    if (!entry.name.endsWith('.cs')) continue;
    const rel = path.relative(path.join(root, project), full).split(path.sep).join('/');
    const dirPart = path.posix.dirname(rel);
    const expected = dirPart === '.' ? project : `${project}.${dirPart.split('/').join('.')}`;
    const text = fs.readFileSync(full, 'utf8').replace(/^\uFEFF/, '');
    const match = text.match(/^namespace\s+([A-Za-z0-9_.]+)/m);
    total += 1;
    perProject[project] = (perProject[project] || 0) + 1;
    if (!match || match[1] !== expected) {
      mismatches += 1;
      console.log(`MISMATCH ${project}/${rel} got=${match ? match[1] : '(none)'} expected=${expected}`);
    }
  }
};

for (const project of projects) walk(path.join(root, project), project);

console.log('projects =', projects.join(', '));
console.log('per-project .cs =', JSON.stringify(perProject));
console.log('total .cs =', total, 'mismatches =', mismatches);
