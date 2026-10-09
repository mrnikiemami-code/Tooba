// TB-TMAR-STORY-AMSC-001-W3-R2 — bounded route-to-dispatch certification guard repair (test/evidence only).
//   Appends the additive storyAmsc001W3R2 record to the SoT. Zero production, manifest, schema, frontend or
//   unrelated-module change. The W3 certification truth, the W3-R1 recovery record, the historical AMC-001
//   lineage and the repository-global Host root checkpoint are preserved exactly.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-current-state.json');
const original = fs.readFileSync(file, 'utf8');
const NL = original.includes('\r\n') ? '\r\n' : '\n';

const W3R1_COMMIT = '47284caad070b28ae79e803946dd19aed4b1678c';

if (original.includes('"storyAmsc001W3R2"')) {
  throw new Error('storyAmsc001W3R2 already present — refusing to overwrite');
}

const record = {
  task: 'TB-TMAR-STORY-AMSC-001-W3-R2',
  parentTask: 'TB-TMAR-STORY-AMSC-001-W3-R1',
  mode: 'BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF',
  skill: 'tooba-architecture-certify',
  target: 'src/backend/Modules/Story (test/evidence only)',
  startingHead: W3R1_COMMIT,
  state: 'STORY_AMSC_001_W3_R2_SET_EQUALITY_PROVEN',
  verdict: 'COMPLETE_REFERENCE_PATTERN',
  lockVersion: 'ARCH-COMPLETE-002',
  structureCertified: true,
  productionCodeChanged: false,
  productionScopeState: 'TEST_EVIDENCE_ONLY_ZERO_PRODUCTION_CHANGE',
  routeRequestExactMapState: 'EXACT_25_ROUTES_25_SENDS_25_REQUESTS_25_HANDLERS',
  routeRequestExactMapDetail: 'The full audience + verb + full route + handler -> actual dispatched request map is re-derived from the endpoint source (MapGroup prefix per audience, inline Send(new T(...)) and traced variable Send) and asserted row by row: storefront 1, seller 9, admin 15 = 25, with 25 distinct dispatched request types, exactly one IRequestHandler<,> and exactly one record declaration per request, zero orphan/duplicate/unmapped route and zero Host-owned Story route.',
  requestValidatorMatrixState: 'EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED_CONCRETE_ABSTRACTVALIDATOR_DI_VERIFIED',
  concreteValidatorClassCount: 16,
  validatorMatrixDetail: 'The 25 dispatched requests partition into 16 VALIDATOR_REQUIRED + 9 NO_VALIDATOR_REQUIRED; the concrete AbstractValidator<T> targets read from Stories/Validators/StoryValidators.cs (16 classes) equal the required half one-to-one; a real ServiceCollection built via AddToobaCqrsFoundation(typeof(IStoryDirectory).Assembly) resolves each required request to its expected validator type, resolves null for each of the nine exemptions and installs ValidationBehavior<,> exactly once.',
  exemptionProvenanceState: 'NON_CIRCULAR_ALL_NINE_ROUTE_GUID_SERVER_DERIVED_ACTOR_NO_CLIENT_INPUT',
  exemptionProvenanceDetail: 'For all nine exemptions the guard verifies on disk that every route {..} segment carries the :guid constraint, that TenantId is resolved fail-closed through StoryHttpErrors.ResolveTenantId(ICurrentTenant tenant), that actor and sellerPartyId come only from auth.RequireAuthorizedAsync(...), that no X-Tooba-Dev-Actor-User-Id or Request.Headers shortcut exists, and that every constructor argument of every exempt request is a route-bound identifier or a server-derived value rather than a client-bound body.',
  callerControlledOptionalInputState: 'GetPublicStoriesQuery_LOCALE_MARKET_VALIDATOR_REQUIRED_NOT_EXEMPTED',
  mutationNegativeGuardState: 'IN_MEMORY_WITHIN_SET_SWAP_PRESERVES_ROUTE_AND_SEND_COUNTS_AND_TYPE_SET_EXACT_MAP_FAILS',
  mutationNegativeGuardDetail: 'In-memory mutation A remaps the admin /reorder route to AdminUpdateAsync: route count, sender.Send count and the distinct constructed-name set are all preserved, yet the exact route -> request map fails (the route now dispatches UpdateAdminStoryCommand and AdminReorderAsync is orphaned). Mutation B replaces a seller dispatch: counts are preserved while the constructed-name set changes. The shipped source is re-read afterwards and still passes the exact map, so no production byte is touched or committed and no on-disk mutation is required.',
  failClosedState: 'ABSENT_SEND_MULTI_SEND_UNRECOGNISED_EXPRESSION_UNTRACEABLE_VARIABLE_UNKNOWN_MAP_SYNTAX_ALL_THROW',
  legacyGuardBlindSpotState: 'DEMONSTRATED_COUNT_ONLY_AND_SET_ONLY_CHECKS_PASS_ON_THE_MUTATED_SOURCE',
  guardsAdded: 'StoryModuleAmsc001W3R2SetEqualityGuardTests (10 facts)',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  manifestState: 'NOT_TOUCHED',
  schemaMigrationState: 'UNCHANGED',
  frontendState: 'FROZEN_UNTOUCHED',
  crossModuleBoundaryState: 'NONE_SELF_CONTAINED',
  foreignAppInfraDomainCoupling: 'ZERO',
  microserviceExtractable: true,
  certifiedTruthPreservedState: 'STORY_AMSC_001_W3_AND_W3_R1_UNCHANGED',
  historicalLineageState: 'PRESERVED_HISTORICAL_NOT_REWRITTEN',
  globalHostCheckpointState: 'PRESERVED',
  jsonParseState: 'PASS',
  workflowStop: 'USER_REVIEW_STORY_AMSC_001_W3_R2',
  automaticNextImplementationTask: 'NONE',
  commitState: 'REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER',
  evidence: 'docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R2/route-request-set-equality.md',
};

const recordText = JSON.stringify(record, null, 2)
  .split('\n')
  .map((line, index) => (index === 0 ? `  "storyAmsc001W3R2": ${line}` : `  ${line}`))
  .join(NL);

const terminator = `  }${NL}}${NL}`;
const lastIndex = original.lastIndexOf(terminator);
if (lastIndex < 0) {
  throw new Error('record terminator not found');
}

const content = original.slice(0, lastIndex) + `  },${NL}` + recordText + NL + `}${NL}`;

// --- validate ------------------------------------------------------------
const parsed = JSON.parse(content);

const w3 = parsed.storyAmsc001W3;
const r1 = parsed.storyAmsc001W3R1;
const r2 = parsed.storyAmsc001W3R2;

if (!w3 || w3.state !== 'STORY_AMSC_001_CERTIFIED' || w3.verdict !== 'COMPLETE_REFERENCE_PATTERN'
  || w3.lockVersion !== 'ARCH-COMPLETE-002' || w3.structureState !== 'CERTIFIED'
  || w3.httpApplicability !== 'HTTP_OWNING' || w3.endpointReachableRequests !== 25
  || w3.validatorCoverageState !== 'EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED'
  || w3.commit !== '39ab324e9c57e342516202bdd8df95953dda6439') {
  throw new Error('storyAmsc001W3 certification truth disturbed');
}
if (!r1 || r1.state !== 'STORY_AMSC_001_RECOVERY_RECONCILED'
  || r1.certifiedCommit !== '39ab324e9c57e342516202bdd8df95953dda6439') {
  throw new Error('storyAmsc001W3R1 recovery record disturbed');
}
if (!r2 || r2.state !== 'STORY_AMSC_001_W3_R2_SET_EQUALITY_PROVEN'
  || r2.parentTask !== 'TB-TMAR-STORY-AMSC-001-W3-R1'
  || r2.mode !== 'BOUNDED_DURABLE_ROUTE_REQUEST_SET_EQUALITY_PROOF'
  || r2.productionCodeChanged !== false || r2.structureCertified !== true
  || Object.prototype.hasOwnProperty.call(r2, 'commit')) {
  throw new Error('storyAmsc001W3R2 record missing or wrong state');
}

const certified = parsed.structureLock.certifiedModules;
if (certified.filter((m) => m === 'Story').length !== 1) {
  throw new Error('structureLock.certifiedModules promotion disturbed');
}

for (const key of ['storyModuleAmc001', 'storyModuleAmc001W1', 'storyModuleAmc001W2Cert',
  'storyModuleAmc001W3', 'storyModuleAmc001W4', 'storyModuleAmc001W5', 'storyModuleAmc001W6Cert',
  'storyAmsc001W0', 'storyAmsc001W1', 'storyAmsc001W2']) {
  if (!Object.prototype.hasOwnProperty.call(parsed, key)) {
    throw new Error(`record ${key} was disturbed`);
  }
}

if (parsed.lastAcceptedTask !== 'TB-TMAR-HOST-ROOT-FINAL-CERT-001'
  || parsed.currentHostCheckpoint !== 'HOST_ROOT_FINAL_CERTIFIED'
  || parsed.workflowStop !== 'USER_REVIEW_HOST_ROOT_FINAL_CERT_001'
  || parsed.automaticNextImplementationTask !== 'NONE') {
  throw new Error('repository-global Host root checkpoint was disturbed');
}

if (content.includes('PENDING_W3R2_COMMIT')) {
  throw new Error('a self-referential PENDING_W3R2_COMMIT placeholder must not be written');
}
if (!content.includes('REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER')) {
  throw new Error('expected the R2 commitState marker');
}
if (!content.endsWith('}' + NL)) {
  throw new Error('unexpected trailing bytes');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-current-state.json (storyAmsc001W3R2 appended)');
