const fs = require('fs');
const path = require('path');

const root = 'src/backend/Modules/Media';
const files = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) {
      if (e.name === 'obj' || e.name === 'bin') continue;
      walk(p);
    } else if (e.name.endsWith('.cs')) {
      files.push(p.split(path.sep).join('/'));
    }
  }
})(root);

let bad = 0;
for (const f of files) {
  const parts = f.split('/');
  const idx = parts.indexOf('Media');
  const proj = parts[idx + 1];
  const dirs = parts.slice(idx + 2, -1);
  const expected = [proj, ...dirs].join('.');
  const txt = fs.readFileSync(f, 'utf8');
  const m = txt.match(/^\s*namespace\s+([A-Za-z0-9_.]+)\s*[;{]/m);
  const ns = m ? m[1] : '(none)';
  if (ns !== expected) {
    bad++;
    console.log('MISMATCH', f, 'ns=' + ns, 'expected=' + expected);
  }
}
console.log('files=' + files.length, 'mismatches=' + bad);
