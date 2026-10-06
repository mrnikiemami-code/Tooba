const fs = require('fs');
const path = require('path');

const root = 'src/backend/Modules/Media';
const projects = fs.readdirSync(root, { withFileTypes: true })
  .filter(e => e.isDirectory() && e.name.startsWith('Tooba.Media.'))
  .map(e => e.name).sort();

function tree(dir, prefix, out) {
  const entries = fs.readdirSync(dir, { withFileTypes: true })
    .filter(e => !['obj', 'bin', 'artifacts'].includes(e.name))
    .sort((a, b) => (a.isDirectory() === b.isDirectory() ? a.name.localeCompare(b.name) : a.isDirectory() ? -1 : 1));
  entries.forEach((e, i) => {
    const last = i === entries.length - 1;
    out.push(prefix + (last ? '`-- ' : '|-- ') + e.name + (e.isDirectory() ? '/' : ''));
    if (e.isDirectory()) tree(path.join(dir, e.name), prefix + (last ? '    ' : '|   '), out);
  });
}

const out = [];
for (const p of projects) {
  out.push(p + '/');
  tree(path.join(root, p), '', out);
  out.push('');
}
console.log(out.join('\n'));
