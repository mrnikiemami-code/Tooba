// TB-TMAR-STORY-AMSC-001-W3 — replace the manifest Story certificationNote with the AMSC-001 certification
// truth. Text-anchor insertion: only the certificationNote line is rewritten, so the manifest formatting
// (6-space module-property indent, CRLF) is preserved byte for byte and no other module entry is touched.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const original = fs.readFileSync(file, 'utf8');
const NL = '\r\n';

const anchor = '      "module": "Story",' + NL
  + '      "structureCertified": true,' + NL
  + '      "lockVersion": "ARCH-COMPLETE-002",' + NL;

if (original.split(anchor).length !== 2) {
  throw new Error('Story manifest anchor not found exactly once');
}

const noteMatch = /"certificationNote": "((?:[^"\\]|\\.)*)",/.exec(
  original.slice(original.indexOf(anchor) + anchor.length));
if (!noteMatch) {
  throw new Error('Story certificationNote not found');
}
const oldNote = noteMatch[1];
if (!oldNote.includes('AMSC-001')) {
  throw new Error('Story certificationNote does not yet record the AMSC-001 wave line');
}

const certificationNote = 'TB-TMAR-STORY-AMC-001-W6 certified Story under COMPLETE_REFERENCE_PATTERN and the '
  + 'AMSC-001 wave line re-verified and re-certified it (W0 analyze -> W1 migrate -> W2 structure -> W3 certify): '
  + 'verdict COMPLETE_REFERENCE_PATTERN, structureState CERTIFIED, structureCertified true, lockVersion '
  + 'ARCH-COMPLETE-002. HTTP_OWNING module: endpointOwnership MODULE_OWNED with 25 module routes over '
  + '/v1/admin/stories, /v1/seller/stories and /v1/storefront/stories and Host-owned route count ZERO; CQRS is 25 '
  + 'real IRequest/IRequestHandler dispatched through ISender (MediatR 12.5); the validator matrix is exhaustive at '
  + '16 VALIDATOR_REQUIRED + 9 NO_VALIDATOR_REQUIRED with 17 discoverable AbstractValidator classes and zero '
  + 'unmapped endpoint-reachable request. Structure: capability-first shallow Stories foldering; the shared fault '
  + 'seam sits at the shallow Application/Composition/; the Infrastructure composition entry is '
  + 'DependencyInjection/StoryModule.cs with the outbox registration split to Messaging/StoryOutboxRegistration.cs '
  + 'and the directory capability at Directories/StoryDirectory.cs; root allowlists are empty on four projects with '
  + 'a single allowlisted Endpoints composition entry; path<->namespace EXACT (0 mismatches over 40 production .cs); '
  + '/Modules/Story/ solution group holds the five projects exactly once; no over-foldering, no use-case-named '
  + 'single-file request leaf, no technical-axis-first request tree, no stale duplicate and no god file '
  + '(StoryDirectory.cs 672 LOC stays WATCH, single responsibility below the 800 ceiling). Canonical mechanisms: '
  + 'Result<T> + ApiResponseFactory From/Created only with zero ad-hoc Results.*/ProblemDetails and zero '
  + 'message-text classification; all 10 story.* validation codes and all 5 stable error codes are bilingually '
  + 'resourced in StoryErrors.resx/StoryErrors.fa.resx with exactly one descriptor owner for the 5 stable codes and '
  + 'no first/last-wins suppression; logging is canonical with zero ILogger/Console/Debug; OpenTelemetry and '
  + 'correlation are canonical with no parallel correlation; the Persian code documentation standard '
  + '(docs/architecture/32-persian-code-documentation-standard.md) is satisfied. Boundaries: Contracts holds error '
  + 'codes only with no Application contracts dump and no duplicate CQRS shape; foreign module '
  + 'Application/Infrastructure/Domain/Endpoints edge ZERO; cross-module persistence and join NONE; namespace '
  + 'alias/type-forwarding workaround NONE; one StoryDbContext on schema story with the two accepted migrations '
  + 'unchanged and no migration regenerated. Microservice extractability: ARCHITECTURAL_READY - the module '
  + 'references only BuildingBlocks, ModuleContracts and Persistence, never Tooba.Host, so /Modules/Story/ can be '
  + 'dropped into its own host behind a Contracts-shaped inbound seam. Host closure PRESERVED: Host keeps only the '
  + 'composition root and the legitimate Host/Security/Seller/HostStorySellerAuthorizer.cs platform adapter; '
  + 'src/backend/Host/Tooba.Host/Story remains ABSENT and hostIllegalAuthorityState is ZERO. Durable guards: '
  + 'StoryModuleAmsc001W1MigrateGuardTests, StoryModuleAmsc001W2StructureGuardTests and '
  + 'StoryModuleAmsc001W3CertGuardTests. This wave changed no production file; guards weakened NONE; baselines '
  + 'widened NONE; the repository-global Host root checkpoint (lastAcceptedTask TB-TMAR-HOST-ROOT-FINAL-CERT-001, '
  + 'currentHostCheckpoint HOST_ROOT_FINAL_CERTIFIED, workflowStop USER_REVIEW_HOST_ROOT_FINAL_CERT_001, '
  + 'automaticNextImplementationTask NONE) is preserved exactly. Stop gate USER_REVIEW_STORY_AMSC_001_W3. Evidence '
  + 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3/certification.md.';

if (certificationNote.includes('"') || certificationNote.includes('\\')) {
  throw new Error('certificationNote must not contain a quote or backslash');
}

const content = original.replace(
  anchor + `      "certificationNote": "${oldNote}",`,
  anchor + `      "certificationNote": "${certificationNote}",`);

if (content === original) {
  throw new Error('no change produced');
}

// --- validate: only the Story note changed --------------------------------
const parsed = JSON.parse(content);
const before = JSON.parse(original);
if (parsed.modules.length !== before.modules.length) {
  throw new Error('manifest module count changed');
}
if (JSON.stringify(parsed.modules.map((m) => m.module)) !== JSON.stringify(before.modules.map((m) => m.module))) {
  throw new Error('manifest module order changed');
}
const story = parsed.modules.find((m) => m.module === 'Story');
if (!story || story.structureCertified !== true || story.lockVersion !== 'ARCH-COMPLETE-002') {
  throw new Error('Story certification flags disturbed');
}
if (!story.certificationNote.includes('TB-TMAR-STORY-AMSC-001-W3')
  || !story.certificationNote.includes('HTTP_OWNING')
  || !story.certificationNote.includes('COMPLETE_REFERENCE_PATTERN')) {
  throw new Error('Story certificationNote is not the AMSC certification truth');
}
if (story.projects.length !== 5) {
  throw new Error('Story project count changed');
}
const allow = Object.fromEntries(story.projects.map((p) => [p.projectName, p.rootAllowlist]));
if (JSON.stringify(allow['Tooba.Story.Endpoints']) !== JSON.stringify(['StoryEndpointModule.cs'])) {
  throw new Error('Endpoints allowlist changed');
}
for (const project of ['Tooba.Story.Contracts', 'Tooba.Story.Domain', 'Tooba.Story.Application', 'Tooba.Story.Infrastructure']) {
  if (allow[project].length !== 0) {
    throw new Error(`${project} rootAllowlist is not empty`);
  }
}
if (parsed.preCertModules.length !== 0) {
  throw new Error('preCertModules is not empty');
}
if (JSON.stringify(parsed.uncertifiedHttpOwningModules) !== JSON.stringify(before.uncertifiedHttpOwningModules)) {
  throw new Error('uncertifiedHttpOwningModules changed');
}
if (content.split('"certificationNote"').length !== original.split('"certificationNote"').length) {
  throw new Error('a certificationNote was added or removed');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (Story AMSC-001 certificationNote)');
