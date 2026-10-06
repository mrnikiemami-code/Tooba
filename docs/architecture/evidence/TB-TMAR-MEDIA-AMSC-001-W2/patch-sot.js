const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);

j.mediaModuleAmsc001W2 = {
  task: 'TB-TMAR-MEDIA-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-structure',
  target: 'src/backend/Modules/Media/Tooba.Media.*',
  parentTask: 'TB-TMAR-MEDIA-AMSC-001-W1',
  startingHead: '0b0fde0a',
  state: 'STRUCTURE_READY_FOR_CERTIFY',
  verdict: 'READY_FOR_CERTIFY',
  lockVersion: 'ARCH-COMPLETE-002',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  pathNamespaceState: 'EXACT',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  fileCohesionState: 'COHESIVE',
  structureState: 'READY_FOR_CERTIFY',
  capabilityFirstApplication: 'Assets/{Commands,Queries,Validators,Models} under shared Composition/Models/Ports',
  technicalAxisFirstRequestTree: 'NONE',
  singleFileRequestLeafFolder: 'NONE',
  rootDumpState: 'NONE',
  productionFileCount: 38,
  namespaceMismatchCount: 0,
  solutionGroup: '/Modules/Media/ (5 projects, all entries resolve on disk)',
  migrationsPlacement: 'Infrastructure/Persistence/Migrations (no root Migrations folder)',
  staleOrDuplicateCopyState: 'NONE',
  manifestChanged: false,
  artifactsFoldersFinding: 'WITHDRAWN_UNTRACKED_GITIGNORED_REPO_WIDE_CONVENTION_ZERO_TRACKED_FILES',
  productionChange: 'ZERO_PRODUCTION_CHANGE_THIS_WAVE_GUARD_AND_EVIDENCE_ONLY',
  durableGuard: 'src/backend/Host/Tooba.Host.Tests/Architecture/MediaModuleAmsc001W2StructureGuardTests.cs',
  focusedValidation: 'dotnet test --filter FullyQualifiedName~MediaModuleAmsc001 = 23 passed/0 failed; --filter FullyQualifiedName~Media = 75 passed/3 docker-skipped/0 failed; Architecture namespace 43 pre-existing unrelated failures unchanged with/without the new guard; TmarDurableGuardTests 2 pre-existing repo-global failures reproduced with the W2 SoT block stashed',
  hostFinalClosureState: 'PRESERVED',
  hostTouched: false,
  frontendTouched: false,
  certHandoffState: 'REQUIRED',
  commit: 'PENDING',
  evidence: 'docs/architecture/evidence/TB-TMAR-MEDIA-AMSC-001-W2/structure.md'
};

fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W2 SoT block written');
