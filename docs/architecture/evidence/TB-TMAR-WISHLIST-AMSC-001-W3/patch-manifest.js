// TB-TMAR-WISHLIST-AMSC-001-W3 — promote the Certify wave truth into the durable structure manifest.
// Surgical: only the Wishlist entry is annotated (certification keys + structure-authority commit).
// Every other module entry is byte-compared before/after so a formatting round-trip cannot silently
// rewrite unrelated modules.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
const raw = fs.readFileSync(file, 'utf8');
const bom = raw.charCodeAt(0) === 0xfeff ? '\uFEFF' : '';
const before = JSON.parse(raw.replace(/^\uFEFF/, ''));

const wishlist = before.modules.find((m) => m.module === 'Wishlist');
if (!wishlist) {
  throw new Error('Wishlist module entry not found');
}
if (wishlist.currentCertificationTask === 'TB-TMAR-WISHLIST-AMSC-001-W3') {
  throw new Error('Wishlist W3 certification already recorded — refusing to overwrite');
}
if (wishlist.structureAuthorityTask !== 'TB-TMAR-WISHLIST-AMSC-001-W2') {
  throw new Error('Wishlist structure authority is not the W2 wave');
}

wishlist.certificationNote =
  'CERTIFIED by TB-TMAR-WISHLIST-AMSC-001-W3 (tooba-architecture-certify) after the Structure wave W2 '
  + '(03871df6). Wave lineage W0 e80045ca (analyze) -> W1 d7fa18f7 (migrate) -> W2 03871df6 (structure) -> '
  + 'W3 (this certify wave). Wishlist is HTTP_OWNING: the W0 applicability gate and the certify 6a hard '
  + 'blocker re-derived from disk prove exactly 4 module-owned HTTP routes under the single group '
  + '/v1/customer/wishlist (GET list, POST /{productId:guid}, DELETE /{productId:guid}, POST /membership) '
  + 'with Host owning ZERO Wishlist routes; 4 endpoint-reachable requests each mapped 1:1 to exactly one '
  + 'MediatR request record and one IRequestHandler and all routed through the WishlistOperation seam (4/4); '
  + 'the exhaustive validator matrix is 3 VALIDATOR_REQUIRED (AddWishlistItemCommand, '
  + 'RemoveWishlistItemCommand, GetWishlistMembershipQuery) + 1 NO_VALIDATOR_REQUIRED (ListWishlistPageQuery, '
  + 'whose only argument is the server-derived actor and whose route binds no client-controlled shape); the '
  + 'single canonical Contracts stable-code home declares 4 Wishlist-owned codes plus the shared '
  + 'Foundation-owned customer.session.required with a KnownCodes/IsKnown declared-code guard, 4 descriptors '
  + 'registered exactly once by WishlistErrorCatalogContributor and 4 EN + 4 FA bilingual resources owned once '
  + 'by WishlistErrorResourceSet with explicit locked LogicalName values; the typed WishlistOperation seam '
  + 'classifies by typed code only (ContractOperationException + SemanticException) with zero message-prose '
  + 'classification and zero raw InvalidOperationException literals; PROFESSIONAL_SHALLOW capability-first '
  + 'structure with exact path<->namespace and enforced root allowlists across all five production projects '
  + 'under the canonical /Modules/Wishlist/ solution folder; Contracts-only boundaries (zero foreign '
  + 'Application/Infrastructure/Domain edge, zero cross-module persistence, zero cross-module join; the only '
  + 'foreign edges are the Catalog.Contracts lookup seams and the Order.Contracts.Fulfillment session seam); '
  + 'the wishlist schema and the single migration 20260825162434_InitialWishlist are byte-identical to the W0 '
  + 'baseline; and the HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED / HOST_ROOT_FINAL_CERTIFIED checkpoints '
  + 'are preserved. Verdict COMPLETE_REFERENCE_PATTERN; structureCertified true; microserviceExtractable = '
  + 'TRUE_CONTRACTS_ONLY_SELF_CONTAINED_ERROR_SURFACE_EXACT_PATH_NAMESPACE.';
wishlist.certificationState = 'WISHLIST_AMSC_001_CERTIFIED';
wishlist.currentCertificationTask = 'TB-TMAR-WISHLIST-AMSC-001-W3';
wishlist.currentVerdict = 'COMPLETE_REFERENCE_PATTERN';
wishlist.currentValidatorMatrixState = 'EXHAUSTIVE_3_VALIDATOR_REQUIRED_1_NO_VALIDATOR_REQUIRED';
wishlist.evidence = 'docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W3/certification.md';
wishlist.structureAuthorityCommit = '03871df68d3204214680afec9aae7992dc481b2e';

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
if (!w.structureCertified || w.lockVersion !== 'ARCH-COMPLETE-002') {
  throw new Error('Wishlist certification invariants broken');
}
if (w.currentCertificationTask !== 'TB-TMAR-WISHLIST-AMSC-001-W3'
  || w.currentVerdict !== 'COMPLETE_REFERENCE_PATTERN'
  || w.certificationState !== 'WISHLIST_AMSC_001_CERTIFIED') {
  throw new Error('Wishlist certification truth not recorded');
}
if (w.structureAuthorityCommit !== '03871df68d3204214680afec9aae7992dc481b2e') {
  throw new Error('structureAuthorityCommit not recorded');
}
if (w.projects.length !== 5) {
  throw new Error('Wishlist project count changed');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/tmar-module-structure-manifests.json (Wishlist W3 certification)');
