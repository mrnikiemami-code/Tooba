const fs = require('fs');
const p = 'docs/architecture/tmar-current-state.json';
let raw = fs.readFileSync(p, 'utf8');
const bom = raw.charCodeAt(0) === 0xFEFF ? '\uFEFF' : '';
if (bom) raw = raw.slice(1);
const j = JSON.parse(raw);
j.localizationModuleAmsc001W2 = {
  task: 'TB-TMAR-LOCALIZATION-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_AMSC',
  skill: 'tooba-architecture-structure',
  target: 'src/backend/Modules/Localization/Tooba.Localization.*',
  parentTask: 'TB-TMAR-LOCALIZATION-AMSC-001-W1',
  startingHead: '074fc3a4',
  state: 'STRUCTURE_READY_FOR_CERTIFY',
  verdict: 'READY_FOR_CERTIFY',
  structureState: 'READY_FOR_CERTIFY',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  solutionFolder: '/Modules/Localization/',
  solutionProjectEntries: 5,
  pathNamespaceState: 'EXACT',
  pathNamespaceFilesVerified: 33,
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  rootAllowlistManifestDiskReconciliation: 'EXACT_FOR_ALL_5_PROJECTS',
  forbiddenRootsState: 'ABSENT',
  fileCohesionState: 'COHESIVE',
  technicalAxisFirstState: 'ZERO',
  singleFileRequestLeafState: 'ZERO',
  rootDumpState: 'ZERO',
  aliasWorkaround: 'NONE',
  typeForwardedToWorkaround: 'ZERO',
  migrationPlacementState: 'CORRECT_PERSISTENCE_MIGRATIONS',
  sharedApplicationBucketsAllowed: ['Composition', 'Models', 'Ports'],
  artifactsFolderFindingWithdrawn: 'Repo-wide git-ignored scratch convention present in ~70 projects including certified modules; zero tracked files; cannot affect rootAllowlist or Solution Explorer; no Localization-specific action.',
  filesMoved: 0,
  filesRenamed: 0,
  productionFilesChanged: 0,
  manifestChanged: false,
  manifestChangeRationale: 'Existing modules[] entry for Localization already declares structureCertified=true, ARCH-COMPLETE-002 and accurate allowlists/forbidden lists for all five projects; re-verified against disk and matching exactly. No preCertModules duplicate was created because Localization is already a modules[] member.',
  hostFinalClosureState: 'PRESERVED',
  hostFinalClosureRegression: 'NONE',
  sinkFolderRegressionState: 'ZERO',
  durableGuards: [
    'LocalizationModuleAmsc001W2StructureGuardTests'
  ],
  focusedValidation: 'LocalizationModuleAmsc001 filter 16 passed / 0 failed (7 W1 + 9 W2); Tooba.Host.Tests build 0 errors',
  structureHandoffState: 'READY_FOR_CERTIFY',
  commit: 'PENDING',
  evidence: 'docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMSC-001-W2/structure.md'
};
fs.writeFileSync(p, bom + JSON.stringify(j, null, 2) + '\n');
console.log('W2 SoT block written');
