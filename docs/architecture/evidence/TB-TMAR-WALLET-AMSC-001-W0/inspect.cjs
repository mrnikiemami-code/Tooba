// TB-TMAR-WALLET-AMSC-001-W0 — read-only inspection helper.
// Verifies path<->namespace exactness, enumerates the module surface, and audits the raw-fault /
// message-heuristic / coupling baseline. No writes.
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..', '..', '..', '..', 'src', 'backend', 'Modules', 'Wallet');
const projects = fs.readdirSync(root).filter((d) => fs.statSync(path.join(root, d)).isDirectory());

let total = 0;
let mismatches = 0;
const perProject = {};
const rawFaults = [];
const messageHeuristics = [];
const foreignEdges = [];

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

    const code = `${project}/${rel}`;
    for (const m of text.matchAll(/InvalidOperationException\(\s*"([^"]*)"/g)) {
      rawFaults.push(`${code} :: ${m[1]}`);
    }
    if (/\.Message\.(Contains|StartsWith)\(|\.Message\s*==/.test(text)) messageHeuristics.push(code);
    for (const m of text.matchAll(/Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Returns|Support|AddressBook|Payment|StoreContext|Pricing|Offer|Party|Tax|ProductWorkspace)\.(Application|Infrastructure|Domain|Endpoints)/g)) {
      foreignEdges.push(`${code} :: ${m[0]}`);
    }
  }
};

for (const project of projects) walk(path.join(root, project), project);

console.log('projects =', projects.join(', '));
console.log('per-project .cs =', JSON.stringify(perProject));
console.log('total .cs =', total, 'mismatches =', mismatches);
console.log('raw InvalidOperationException("<literal>") faults =', rawFaults.length);
for (const f of rawFaults) console.log('  RAW', f);
console.log('message-heuristic files =', messageHeuristics.length, JSON.stringify(messageHeuristics));
console.log('foreign Application/Infrastructure/Domain/Endpoints edges =', foreignEdges.length);
for (const f of foreignEdges) console.log('  FOREIGN', f);
