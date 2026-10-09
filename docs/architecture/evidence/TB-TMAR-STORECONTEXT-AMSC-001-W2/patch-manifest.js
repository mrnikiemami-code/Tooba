// TB-TMAR-STORECONTEXT-AMSC-001-W2 — structure normalization of the StoreContext manifest entry.
// Text-anchor replacement of exactly one block, so every other module record and the file formatting
// (2-space record indent, 6-space project indent, CRLF) stay byte-identical.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const before = [
  '    {',
  '      "module": "StoreContext",',
  '      "structureCertified": true,',
  '      "lockVersion": "ARCH-COMPLETE-002",',
  '      "projects": [',
  '        {',
  '          "projectName": "Tooba.StoreContext.Contracts",',
  '          "rootAllowlist": [],',
  '          "forbiddenRootFiles": [',
  '            "StoreCommerceContext.cs"',
  '          ],',
  '          "forbiddenTopLevelFolders": []',
  '        },',
  '        {',
  '          "projectName": "Tooba.StoreContext.Infrastructure",',
  '          "rootAllowlist": [',
  '            "StoreContextModule.cs"',
  '          ],',
  '          "forbiddenRootFiles": [',
  '            "StoreCommerceContextAccessor.cs"',
  '          ],',
  '          "forbiddenTopLevelFolders": []',
  '        }',
  '      ]',
  '    },',
].join(NL);

const after = [
  '    {',
  '      "module": "StoreContext",',
  '      "structureCertified": true,',
  '      "lockVersion": "ARCH-COMPLETE-002",',
  '      "projects": [',
  '        {',
  '          "projectName": "Tooba.StoreContext.Contracts",',
  '          "rootAllowlist": [],',
  '          "rootAllowlistJustification": "Boundary semantics only: the capability folder Current/ carries the effective store commerce context record plus the read, assign and worker-factory seams. No root .cs remains and namespace alignment is exact path-derived equality.",',
  '          "forbiddenRootFiles": [',
  '            "StoreCommerceContext.cs"',
  '          ],',
  '          "forbiddenTopLevelFolders": []',
  '        },',
  '        {',
  '          "projectName": "Tooba.StoreContext.Infrastructure",',
  '          "rootAllowlist": [],',
  '          "rootAllowlistJustification": "TB-TMAR-STORECONTEXT-AMSC-001-W2 aligned the module composition entry with the newest ARCH-COMPLETE-002 certified precedent: StoreContextModule.cs moved from the project root to DependencyInjection/ (namespace Tooba.StoreContext.Infrastructure.DependencyInjection), so the Infrastructure root now has zero .cs while Current/ keeps the scoped StoreCommerceContextAccessor. Exact path-derived namespace equality holds for both files.",',
  '          "forbiddenRootFiles": [',
  '            "StoreContextModule.cs",',
  '            "StoreCommerceContextAccessor.cs"',
  '          ],',
  '          "forbiddenTopLevelFolders": []',
  '        }',
  '      ]',
  '    },',
].join(NL);

const occurrences = original.split(before).length - 1;
if (occurrences !== 1) {
  throw new Error(`expected exactly one StoreContext block, found ${occurrences}`);
}

const content = original.replace(before, after);

// --- validate: single module entry, no other module record disturbed ---------
const parsedBefore = JSON.parse(original);
const parsedAfter = JSON.parse(content);

const modulesBefore = parsedBefore.modules.map((m) => m.module);
const modulesAfter = parsedAfter.modules.map((m) => m.module);
if (JSON.stringify(modulesBefore) !== JSON.stringify(modulesAfter)) {
  throw new Error('the certified module list changed');
}
if (modulesAfter.filter((m) => m === 'StoreContext').length !== 1) {
  throw new Error('StoreContext must appear exactly once');
}
if (JSON.stringify(parsedBefore.modules.filter((m) => m.module !== 'StoreContext'))
    !== JSON.stringify(parsedAfter.modules.filter((m) => m.module !== 'StoreContext'))) {
  throw new Error('an unrelated module record changed');
}

const entry = parsedAfter.modules.find((m) => m.module === 'StoreContext');
if (entry.structureCertified !== true || entry.lockVersion !== 'ARCH-COMPLETE-002') {
  throw new Error('StoreContext certification flags changed');
}
if (entry.projects.length !== 2) {
  throw new Error('StoreContext project count changed');
}
const infra = entry.projects.find((p) => p.projectName === 'Tooba.StoreContext.Infrastructure');
if (!infra || infra.rootAllowlist.length !== 0) {
  throw new Error('StoreContext Infrastructure rootAllowlist must be empty after the move');
}
if (!infra.forbiddenRootFiles.includes('StoreContextModule.cs')
  || !infra.forbiddenRootFiles.includes('StoreCommerceContextAccessor.cs')) {
  throw new Error('StoreContext Infrastructure forbiddenRootFiles must block both root regressions');
}
const contracts = entry.projects.find((p) => p.projectName === 'Tooba.StoreContext.Contracts');
if (!contracts || contracts.rootAllowlist.length !== 0) {
  throw new Error('StoreContext Contracts rootAllowlist must stay empty');
}

if (parsedAfter.preCertModules.some((m) => m.module === 'StoreContext')) {
  throw new Error('StoreContext must not appear in preCertModules');
}
if (content === original) {
  throw new Error('no change produced');
}
if (!content.endsWith(NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (StoreContext W2 structure)');
