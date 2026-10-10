// TB-TMAR-WISHLIST-AMSC-001-W0 — deterministic structural inspection (read-only).
// Emits the machine-verifiable facts the Analyze wave records: production file inventory, exact
// path<->namespace alignment, module route/dispatch inventory, root allowlist state, single-file
// leaves, bilingual resource keys and the stable-code / typed-fault inventory.
const fs = require('fs');
const path = require('path');

const repo = path.resolve(__dirname, '..', '..', '..', '..');
const moduleRoot = path.join(repo, 'src', 'backend', 'Modules', 'Wishlist');
const projects = fs.readdirSync(moduleRoot)
  .filter((d) => fs.statSync(path.join(moduleRoot, d)).isDirectory());

const SKIP = new Set(['bin', 'obj']);

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      return SKIP.has(entry.name) ? [] : walk(full);
    }
    return full.endsWith('.cs') ? [full] : [];
  });
}

function nsOf(text) {
  const m = text.match(/^\s*namespace\s+([A-Za-z0-9_.]+)\s*;/m);
  if (m) return m[1];
  const b = text.match(/^\s*namespace\s+([A-Za-z0-9_.]+)\s*\{/m);
  return b ? b[1] : null;
}

const inventory = [];
const mismatches = [];
for (const project of projects) {
  const root = path.join(moduleRoot, project);
  for (const file of walk(root)) {
    const rel = path.relative(root, file).split(path.sep);
    const fileName = rel.pop();
    const expected = [project, ...rel].join('.');
    const text = fs.readFileSync(file, 'utf8');
    const actual = nsOf(text);
    const isMigration = rel.join('/').includes('Migrations') || fileName.endsWith('ModelSnapshot.cs');
    inventory.push({
      project,
      relative: [project, ...rel, fileName].join('/'),
      namespace: actual,
      expectedNamespace: expected,
      isMigration,
    });
    if (!isMigration && actual !== expected) {
      mismatches.push({ file: [project, ...rel, fileName].join('/'), actual, expected });
    }
  }
}

const countsByProject = {};
for (const entry of inventory) {
  countsByProject[entry.project] = (countsByProject[entry.project] || 0) + 1;
}

// Root-level .cs per project (root allowlist evidence).
const rootFiles = {};
for (const project of projects) {
  rootFiles[project] = fs.readdirSync(path.join(moduleRoot, project))
    .filter((f) => f.endsWith('.cs'));
}

// Single-file leaf folders (folder granularity evidence).
function foldersWithOneSource(project) {
  const root = path.join(moduleRoot, project);
  const out = [];
  const visit = (dir) => {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      if (!entry.isDirectory()) continue;
      if (SKIP.has(entry.name)) continue;
      const full = path.join(dir, entry.name);
      const sources = walk(full);
      const direct = fs.readdirSync(full).filter((f) => f.endsWith('.cs'));
      if (sources.length === 1) {
        out.push({
          folder: path.relative(root, full).split(path.sep).join('/'),
          sourceCount: sources.length,
          directFileCount: direct.length,
        });
      }
      visit(full);
    }
  };
  visit(root);
  return out;
}

// Route + dispatch inventory derived from the endpoint source (group.Map*, ISender.Send).
const endpointModuleFile = path.join(moduleRoot, 'Tooba.Wishlist.Endpoints', 'WishlistEndpointModule.cs');
const customerFile = path.join(moduleRoot, 'Tooba.Wishlist.Endpoints', 'Customer', 'WishlistCustomerEndpoints.cs');
const moduleSource = fs.readFileSync(endpointModuleFile, 'utf8');
const endpointSource = fs.readFileSync(customerFile, 'utf8');

const groupPrefix = (moduleSource.match(/MapGroup\("([^"]+)"\)/) || [])[1] || null;
const routes = [...endpointSource.matchAll(
  /group\.Map(Get|Post|Put|Patch|Delete)\("([^"]*)"\s*,\s*([A-Za-z_][A-Za-z0-9_]*)\)/g,
)].map((m) => ({
  verb: m[1],
  path: m[2],
  fullPath: (groupPrefix || '') + m[2],
  handler: m[3],
}));
const sends = [...endpointSource.matchAll(/sender\.Send\(\s*new\s+([A-Za-z_][A-Za-z0-9_]*)/g)]
  .map((m) => m[1]);

// Bilingual resource keys.
const resx = fs.readFileSync(path.join(moduleRoot, 'Tooba.Wishlist.Contracts', 'Resources', 'WishlistErrors.resx'), 'utf8');
const resxFa = fs.readFileSync(path.join(moduleRoot, 'Tooba.Wishlist.Contracts', 'Resources', 'WishlistErrors.fa.resx'), 'utf8');
const keys = (t) => [...t.matchAll(/data name="([^"]+)"/g)].map((m) => m[1]);

// Stable-code + typed-fault inventory.
const codesFile = fs.readFileSync(path.join(moduleRoot, 'Tooba.Wishlist.Contracts', 'Errors', 'WishlistErrorCodes.cs'), 'utf8');
const declaredCodes = [...codesFile.matchAll(/public const string (\w+)\s*=\s*"([^"]+)"/g)]
  .map((m) => ({ name: m[1], code: m[2] }));

const productionSources = inventory
  .filter((i) => !i.isMigration)
  .map((i) => path.join(moduleRoot, i.relative));
const rawInvalidOperation = [];
const semanticFaults = [];
const messageClassification = [];
for (const file of productionSources) {
  const text = fs.readFileSync(file, 'utf8');
  const rel = path.relative(moduleRoot, file).split(path.sep).join('/');
  for (const m of text.matchAll(/InvalidOperationException\(\s*"([^"]*)"/g)) {
    rawInvalidOperation.push({ file: rel, literal: m[1] });
  }
  for (const m of text.matchAll(/new SemanticException\(\s*new SemanticError\(([^)]*)\)/g)) {
    semanticFaults.push({ file: rel, code: m[1].trim() });
  }
  if (/\.Message\.(Contains|StartsWith)\(|\.Message\s*==/.test(text)) {
    messageClassification.push(rel);
  }
}

const report = {
  task: 'TB-TMAR-WISHLIST-AMSC-001-W0',
  projects,
  productionCsCount: inventory.length,
  countsByProject,
  pathNamespaceMismatches: mismatches,
  rootFiles,
  singleSourceFolders: Object.fromEntries(
    projects.map((p) => [p, foldersWithOneSource(p)]),
  ),
  routeGroupPrefix: groupPrefix,
  moduleOwnedRouteCount: routes.length,
  routes,
  dispatchedRequests: sends,
  resxKeys: { en: keys(resx), fa: keys(resxFa) },
  declaredCodes,
  rawInvalidOperationFaults: rawInvalidOperation,
  semanticFaults,
  messageTextClassificationSites: messageClassification,
  inventory: inventory.map((i) => i.relative),
};

process.stdout.write(JSON.stringify(report, null, 2) + '\n');
