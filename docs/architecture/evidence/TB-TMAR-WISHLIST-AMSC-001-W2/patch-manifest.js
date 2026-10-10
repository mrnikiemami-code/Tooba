// TB-TMAR-WISHLIST-AMSC-001-W2 — persist the Structure wave authority into the durable structure manifest.
// Surgical: only the Wishlist entry is edited (structure-authority keys + disk-accurate forbidden lists).
// Every other module entry is byte-compared before/after so a formatting round-trip cannot silently
// rewrite unrelated modules.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const raw = fs.readFileSync(file, 'utf8');
const bom = raw.charCodeAt(0) === 0xfeff ? '\uFEFF' : '';
const before = JSON.parse(raw.replace(/^\uFEFF/, ''));

if (before.modules.find((m) => m.module === 'Wishlist').structureAuthorityTask) {
  throw new Error('Wishlist structure authority already present — refusing to overwrite');
}

const wishlist = before.modules.find((m) => m.module === 'Wishlist');
wishlist.structureAuthorityTask = 'TB-TMAR-WISHLIST-AMSC-001-W2';
wishlist.structureAuthorityCommit = 'PENDING_W2_COMMIT';
wishlist.structureHandoffState = 'READY_FOR_CERTIFY_CONSUMED_BY_W3';

const infra = wishlist.projects.find((p) => p.projectName === 'Tooba.Wishlist.Infrastructure');
infra.forbiddenTopLevelFolders = ['Migrations', 'Directories', 'Development'];
infra.rootAllowlistJustification =
  'Composition entry WishlistModule.cs only; Adapters/ holds the module-owned Development seed adapter, '
  + 'Persistence/ holds the EF model plus the outbox registration plus the wishlist directory and the single '
  + 'migration under Persistence/Migrations; path<->namespace is exact.';

const application = wishlist.projects.find((p) => p.projectName === 'Tooba.Wishlist.Application');
application.rootAllowlistJustification =
  'Wishlist.Application has no root .cs: the Customer capability tree plus the shared Composition/Models/Ports '
  + 'and the module transport Validation/ seam; path<->namespace exact.';

const content = bom + JSON.stringify(before, null, 2).replace(/\n/g, '\r\n') + '\r\n';

// --- validate: only the Wishlist entry changed ------------------------------------------
const after = JSON.parse(content.replace(/^\uFEFF/, ''));
const beforeOther = before.modules.filter((m) => m.module !== 'Wishlist');
const afterOther = after.modules.filter((m) => m.module !== 'Wishlist');
if (JSON.stringify(beforeOther) !== JSON.stringify(afterOther)) {
  throw new Error('an unrelated module entry was disturbed');
}
if (JSON.stringify(before.uncertifiedHttpOwningModules) !== JSON.stringify(after.uncertifiedHttpOwningModules)
  || JSON.stringify(before.preCertModules) !== JSON.stringify(after.preCertModules)
  || before.version !== after.version
  || before.definitionMarker !== after.definitionMarker
  || JSON.stringify(before.rules) !== JSON.stringify(after.rules)) {
  throw new Error('a manifest-level key was disturbed');
}

const w = after.modules.find((m) => m.module === 'Wishlist');
if (!w.structureCertified) {
  throw new Error('Wishlist structureCertified must stay true (AMSC waves must not de-certify)');
}
if (w.structureAuthorityTask !== 'TB-TMAR-WISHLIST-AMSC-001-W2') {
  throw new Error('structureAuthorityTask not recorded');
}
if (w.projects.length !== 5) {
  throw new Error('Wishlist project count changed');
}
const i = w.projects.find((p) => p.projectName === 'Tooba.Wishlist.Infrastructure');
if (JSON.stringify(i.forbiddenTopLevelFolders) !== JSON.stringify(['Migrations', 'Directories', 'Development'])) {
  throw new Error('Infrastructure forbidden list not updated');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (Wishlist W2 structure authority)');
