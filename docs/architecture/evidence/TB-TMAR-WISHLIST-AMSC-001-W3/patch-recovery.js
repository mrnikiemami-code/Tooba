// TB-TMAR-WISHLIST-AMSC-001-W3 — append the Wishlist certification checkpoint to Master Recovery.
// Additive only: appends one section at EOF (CRLF preserved), no existing text rewritten.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'TOOBA-TMAR-MASTER-RECOVERY.md');
const original = fs.readFileSync(file, 'utf8');

const anchor = 'Wishlist AMSC module recovery checkpoint W3 (authoritative, module-local)';
if (original.includes(anchor)) {
  throw new Error('Wishlist W3 checkpoint already present — refusing to overwrite');
}
if (!original.endsWith('\r\n')) {
  throw new Error('unexpected file ending');
}

const section = [
  '---',
  '',
  anchor,
  '',
  'Recorded by `TB-TMAR-WISHLIST-AMSC-001-W3` (`tooba-architecture-certify`), the fourth and final wave of the AMSC re-standardization of `src/backend/Modules/Wishlist` over the structure authority of W2. Wave lineage: W0 `e80045ca` Analyze -> W1 `d7fa18f7` Migrate -> W2 `03871df6` Structure -> W3 (this wave, starting head `03871df68d3204214680afec9aae7992dc481b2e`, `HEAD == origin/main`). Final objective stated up front: Wishlist must be extractable as an independent microservice, so the certification gates are **zero invalid coupling** and an **exact path<->namespace** boundary. Wishlist was already a member of `structureLock.certifiedModules` (**32**) from the earlier `TB-TMAR-WISHLIST-AMC-001` lineage, so W3 **refreshes that entry in place** (never duplicates it) with the AMSC-001 W0->W3 truth and leaves the global lock count unchanged.',
  '- **Scope**: `src/backend/Modules/Wishlist` — `Tooba.Wishlist.Contracts` / `.Domain` / `.Application` / `.Infrastructure` / `.Endpoints`; behavior-preserving, structure/architecture only.',
  '- **W0 (Analyze)** commit `e80045ca`: classified `Wishlist` as `HTTP_OWNING`; audited **4 HTTP-reachable routes** under the single group `/v1/customer/wishlist`; CQRS **4/4**; `VALIDATOR_REQUIRED` = 3 (`AddWishlistItemCommand`, `RemoveWishlistItemCommand`, `GetWishlistMembershipQuery`), `NO_VALIDATOR_REQUIRED` = 1 (`ListWishlistPageQuery`); 3 client-observable codes plus one raw outbox fault literal.',
  '- **W1 (Migrate)** commit `d7fa18f7`: canonical Contracts error home `WishlistErrorCodes.cs` with the `KnownCodes`/`IsKnown` declared-code guard; typed-fault seam `WishlistOperation.cs` (`ContractOperationException` + `SemanticException`); retired the raw `InvalidOperationException` literal in `WishlistOutboxRegistration` for the declared `wishlist.outbox.unmapped_event_type` code; bilingual `.resx` (4 EN + 4 FA) with explicit locked `LogicalName` values. `productionCodeChanged = true` (wave-scoped), `guardsWeakened = NONE`, `baselinesWidened = NONE`.',
  '- **W2 (Structure)** commit `03871df68d3204214680afec9aae7992dc481b2e`: capability-first shallow tree; moved `WishlistDirectory.cs` `Directories/` -> `Persistence/` and `WishlistDevelopmentSeed.cs` `Development/` -> `Adapters/`; split the merged validator bundle into one cohesive file per request under `Application/Validation/`; exact path↔namespace for all 27 production files; manifest structure authority record with forbidden `Migrations`/`Directories`/`Development`. Zero single-file request leaves, zero over-foldering.',
  '- **W3 (Certify)** commit reported in the Bridge Result only (Architect reconciles the final SHA separately): refreshed the existing `modules[Wishlist]` entry in place with `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`, `certificationState = WISHLIST_AMSC_001_CERTIFIED`, `currentCertificationTask = TB-TMAR-WISHLIST-AMSC-001-W3`, `currentVerdict = COMPLETE_REFERENCE_PATTERN` and `structureAuthorityCommit 03871df6`; added the `wishlistAmsc001W3` state record; new durable guard `WishlistModuleAmsc001W3CertGuardTests.cs` (manifest/SoT truth, HTTP ownership, §6a source-derived route→request provenance set-equality, validator discovery + matrix, canonical result/typed-fault/error-catalog mechanisms, bilingual localization, Contracts-only microservice extractability, schema preservation, Master Recovery logging).',
  '- **§6a route/request/provenance set equality**: `EXACT_SOURCE_DERIVED_4_OF_4_ENFORCED`; 4 routes ↔ 4 `ISender.Send` call sites ↔ 4 MediatR requests ↔ 4 handlers, 1:1; the count-preserving mutation (swapping the list dispatch for an already-matrix request) is caught, and an untraceable dispatch fails closed.',
  '- **Microservice extractability**: `microserviceExtractable = true` — zero foreign-module coupling, zero cross-module persistence/joins, Contracts-only cross-module boundaries (only `Catalog.Contracts` lookup seams plus the `Order.Contracts.Fulfillment` session seam), exact path↔namespace, no Host ownership remnants.',
  '- **Test delta**: baseline `NEW = 0` (pre-existing repository-global pin failures byte-identical at the starting head and after W3; no Wishlist-related failure introduced, none widened).',
  '- **Schema/migrations unchanged**; `guardsWeakened = NONE`; `baselinesWidened = NONE`; `unrelatedFilesTouched = NONE`.',
  '- **Host final closure preserved**: zero Host production file added, moved or widened; `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` untouched; `lastAcceptedTask` unchanged.',
  '- Stop gate `USER_REVIEW_WISHLIST_AMSC_001_W3`; `automaticNextImplementationTask = NONE`; evidence `docs/architecture/evidence/TB-TMAR-WISHLIST-AMSC-001-W3/certification.md`.',
  ''
].join('\r\n');

const content = original + section;
if (!content.startsWith(original)) {
  throw new Error('prefix of the original file was not preserved');
}
if (!content.includes(anchor) || !content.endsWith('\r\n')) {
  throw new Error('append produced an unexpected result');
}

fs.writeFileSync(file, content, 'utf8');
console.log('PATCHED docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (Wishlist W3 checkpoint)');
