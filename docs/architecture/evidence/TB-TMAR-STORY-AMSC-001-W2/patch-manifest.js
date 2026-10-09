// TB-TMAR-STORY-AMSC-001-W2 — update the Story manifest entry for the structure wave.
// Rewrites only the Story module entry's allowlists / justification / certificationNote; every other
// module entry and the file's formatting (CRLF, 2-space indent) are preserved.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';
const I10 = ' '.repeat(10);
const I6 = ' '.repeat(6);

if (!original.includes('"module": "Story"')) {
  throw new Error('Story manifest entry not found');
}
if (original.includes('DependencyInjection/StoryModule.cs')) {
  throw new Error('Story manifest already updated — refusing to double-apply');
}

const pairs = [];

// Application justification — the shared fault seam moves from Stories/Composition to the shallow root.
pairs.push([
  `${I10}"rootAllowlistJustification": "Capability-first Stories/{Commands,Queries,Models,Ports,Presentation,Validators,Composition}; StoryFailureMapper lives under Stories/.",`,
  `${I10}"rootAllowlistJustification": "Capability-first Stories/{Commands,Queries,Models,Ports,Presentation,Validators}; the shared fault seam lives at the shallow Composition/ root (21-module precedent); StoryFailureMapper lives under Stories/.",`
]);

// Infrastructure — root allowlist emptied; composition entry and outbox registration are foldered.
pairs.push([
  `${I10}"rootAllowlist": [${NL}${I10}  "StoryModule.cs"${NL}${I10}],${NL}${I10}"rootAllowlistJustification": "Composition entry StoryModule.cs only; Directory/Development/Grid/Adapters/Persistence hold integrations.",`,
  `${I10}"rootAllowlist": [],${NL}${I10}"rootAllowlistJustification": "No root .cs: the composition entry lives in DependencyInjection/ (21-module precedent), the outbox registration in Messaging/, and Development/Grid/Adapters/Persistence/Directories hold integrations.",`
]);

// certificationNote — record the AMSC-001 re-verification lineage additively.
pairs.push([
  `${I6}"certificationNote": "Certified by TB-TMAR-STORY-AMC-001-W6 under COMPLETE_REFERENCE_PATTERN. Capability-first Stories foldering; Result<T>+ApiResponseFactory; exhaustive 25-request validator matrix; Contracts-only foreign boundary (ModuleContracts+BuildingBlocks platform only); Host Story CLOSED_HOST_ZERO; /Modules/Story/ solution group; microservice-extractable with zero foreign Application/Infrastructure/Domain coupling.",`,
  `${I6}"certificationNote": "Certified by TB-TMAR-STORY-AMC-001-W6 under COMPLETE_REFERENCE_PATTERN and re-verified by the AMSC-001 wave line (W0 analyze -> W1 migrate -> W2 structure -> W3 certify). Capability-first shallow Stories foldering; shared fault seam at Application/Composition/; Infrastructure composition entry at DependencyInjection/StoryModule.cs with the outbox registration split to Messaging/StoryOutboxRegistration.cs and Directories/ plural; Result<T>+ApiResponseFactory; exhaustive 25-request validator matrix with all 10 validation codes bilingually localized; Contracts-only foreign boundary (ModuleContracts+BuildingBlocks platform only); Host Story CLOSED_HOST_ZERO; /Modules/Story/ solution group; microservice-extractable with zero foreign Application/Infrastructure/Domain coupling.",`
]);

let content = original;
for (const [b, a] of pairs) {
  if (!content.includes(b)) {
    throw new Error(`manifest anchor not found: ${JSON.stringify(b.slice(0, 80))}`);
  }
  content = content.replace(b, a);
}

const parsed = JSON.parse(content);
const entries = parsed.modules.filter((m) => m.module === 'Story');
if (entries.length !== 1) {
  throw new Error('Story entry count changed');
}
const story = entries[0];
if (story.structureCertified !== true || story.lockVersion !== 'ARCH-COMPLETE-002') {
  throw new Error('Story certification flags were disturbed');
}
const infra = story.projects.find((p) => p.projectName === 'Tooba.Story.Infrastructure');
if (!infra || infra.rootAllowlist.length !== 0) {
  throw new Error('Infrastructure root allowlist not emptied');
}
if (!infra.forbiddenRootFiles.includes('StoryDirectory.cs') || !infra.forbiddenTopLevelFolders.includes('Migrations')) {
  throw new Error('Infrastructure forbidden lists were disturbed');
}
const app = story.projects.find((p) => p.projectName === 'Tooba.Story.Application');
if (!app || !app.rootAllowlistJustification.includes('Composition/')) {
  throw new Error('Application justification not updated');
}
if (app.rootAllowlist.length !== 0 || app.forbiddenTopLevelFolders.length !== 4) {
  throw new Error('Application allowlist/forbidden list were disturbed');
}
if (!story.certificationNote.includes('AMSC-001')) {
  throw new Error('certificationNote not updated');
}
if (parsed.modules.length !== 29) {
  throw new Error(`module count changed: ${parsed.modules.length}`);
}
if (parsed.version !== 'ARCH-COMPLETE-002') {
  throw new Error('manifest version changed');
}
if (content === original) {
  throw new Error('no change produced');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (Story entry)');
