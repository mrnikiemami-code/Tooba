const fs = require('fs');
const path = require('path');

const repo = process.argv[2] || '.';
const statePath = path.join(repo, 'docs/architecture/tmar-current-state.json');
const sot = JSON.parse(fs.readFileSync(statePath, 'utf8'));
const w3 = sot.taxAmsc001W3;

w3.guardTestResult = '8_OF_8_PASSED';
w3.w1GuardTestResult = '8_OF_8_PASSED (W1 semantics preserved, not weakened)';
w3.w2GuardTestResult = '8_OF_8_PASSED (repointed to the promoted modules[] entry, same structure record)';
w3.taxTestsResult = '11_OF_11_PASSED';
w3.solutionBuildResult = 'SUCCEEDED_0_ERRORS';
w3.focusedGuardResult = '36_PASSED_1_SKIPPED_0_FAILED';
w3.hostFullSuiteResult = '2304_PASSED_130_SKIPPED_71_FAILED';
w3.hostFullSuiteBaselineResult = '2296_PASSED_130_SKIPPED_71_FAILED at eff3cf5b (isolated git worktree)';
w3.newFailuresIntroduced = 'ZERO';
w3.guardIntegrity = 'GUARDS_ADDED_8_GUARDS_REPOINTED_1_GUARDS_EXTENDED_2_GUARDS_DELETED_NONE_ASSERTIONS_RELAXED_NONE';
w3.baselineFailureNote =
  'The 71 distinct failing test ids are byte-identical at both heads and all pre-exist the task: ' +
  'HostAdminAmcW30PwTaxonomyGuardTests Host/Admin count drift, ProductWorkspaceModuleAmsc001W3R2CertGuardTests, ' +
  'the repository-global TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_' +
  'namespace_alignment failure for the pre-existing Tooba.Catalog.Contracts/Cart + Tooba.Cart.Contracts/' +
  '{Checkout,Presentation} project-level namespace deviation (documented in the gate source as out of scope for a ' +
  'module-local certification), FulfillmentFoundationTests, TmarDurableGuardTests global recovery pins, ' +
  'TmarSourceSizeAndInfraAppTests baseline inventory, ProductHistoryTests and StoryModuleAmsc001W3R1RecoveryGuardTests. ' +
  'No Tax test and no Tax guard fails.';
w3.transientFailureRepaired =
  'The first full-suite run reported 72 failures: StoryModuleAmsc001W3R1RecoveryGuardTests.' +
  'Story_amsc_lineage_is_fully_reconciled_with_real_shas enforces the repository-global rule that no ' +
  'self-referential PENDING_W3_COMMIT placeholder may live in tmar-current-state.json, and this wave had written ' +
  'one. Repaired (not excused): the placeholder fields were removed and replaced by ' +
  'commitState = REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER, the Support/Story convention. ' +
  'The full suite then returned to exactly the 71-failure baseline.';
w3.residualNonBlockingDebt =
  'The repository-global TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_' +
  'namespace_alignment failure belongs to Tooba.Catalog.Contracts/Cart and Tooba.Cart.Contracts/{Checkout,Presentation} ' +
  '(another module surface), is documented in the gate source as out of scope for a module-local certification, and is ' +
  'identical at the W2 head. It is not a Tax defect and was not repaired here.';

fs.writeFileSync(statePath, JSON.stringify(sot, null, 2) + '\n');
console.log('SoT verification fields updated');
