# ARCHITECT REVIEW — TB-282-R3 — 2026-11-19

Status: PARTIAL. Worker PASS is not accepted as complete S1/S2 closure.
Reviewed implementation: 70c5dd36a2a9508f7a95edce1ee575c0fda9dfae.
Reviewed implementation/evidence head: 9047660042696e205c04cf161c5c035abd912840.
Method: independent GitHub source/diff and evidence inspection; no local test execution, live browser verification or live write by Architect.

## Retained progress

finalizeResponse now refuses empty/non-JSON/unbound Story 2xx before clear; current-scope storage getter/enumeration failure is explicit. Catalogue platform options now use ICatalogPublishedProductSearchLookup and an EF Published query with search before count/paging, not the development first-40 sampler. Existing VendorShell, merchant editor, Tehran/Jalali controls, exact-byte journals and platform media implementation remain. No redesign or reopening 281/Gate0/Media.

## Remaining S1 implementation gap — the upload executor bypasses route validation

src/frontend/app/stories/management/story-executor.ts:85 calls bindStoryEnvelopeRoute only in runDurableStoryMutation. The upload_add production and recovery path calls runDurableUploadAdd (line 145 / replay dispatch line 426). That function calls validateStoryEnvelope, which only requires nonempty method/path and does not bind a body to an endpoint, and directly dispatches the journal at lines 188 and 253 without bindStoryEnvelopeRoute. Thus an otherwise well-shaped stored upload_add journal can fetch its stored upload path without the S1 allowed-route barrier. This is source-confirmed, not a claim of an observed live incident.

Finish the ISSUED S1 gate on both upload and add stages at the actual send boundary; validate the effective stage, exact allowed role/method/path, owning target, and frozen body/version/occurrence before fetch. The existing mediaUpload regex also uses an unanchored prefix: exact route matching must reject appended paths. Do not add a second journal or change successful normal wire. A wrong route/method/target/body upload_add must send zero requests, including recovery after the upload child completed. Preserve byte-identical unresolved evidence.

## S1/S2 required proof remains incomplete

story-management-282r3.mount.test.ts imports mount/flush but ends with void mount / void flush; none of its five tests mounts the workspace. Direct executor tests are useful and are retained, but they do not prove visible pending state/no success/navigation. The named negative control simply asserts that the NEW verifier rejects undefined and {}; it does not execute the pre-fix production function or a temporary precise production regression. RECEIPTS and COMPLETION-LEDGER descriptions must be corrected additively; preserve original reports.

The sole S2 new test stubs one option with totalCount=55 and verifies request query text. No owning-query fact exercises >40 products, a match beyond page one, disjoint next page/count, or two-shop eligibility. The source removal of the sampler is accepted; the required query scenario proof is not yet supplied. Complete focused controlled/rollback-only owner tests without live fixtures. Do not claim that a mock totalCount=55 is a 55-product catalogue proof.

## Narrow closure boundary

Only finish these existing S1/S2 obligations. No new feature phase, UI redesign, Gate0 sweep, general Media rewrite, full-suite recertification or unrelated landing change. No new task issued by this review. activeTask NONE; automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT.

Worker-reported tests: AMSC/W5 41/41; existing plus new Story tests 14/14; tsc/build green. These were NOT rerun by Architect and do not replace the missing scenario assertions. Unrelated critical-storefront landing bg-white failure remains 19/20, not Story-green.

Worker-reported serving: Host36320 DLL 4B6CE54076E736A2CD2C6379F957CFDCFB03AA20CF3470F54F437DD3B4A6FC53; Next35676 build fFmkskCMJiO4PrZyHPO6l; health/ready/routes 200. No Architect live check. ZERO migration/live business write reported.

## Owner review

The owner can inspect current screens at /fa/admin/stories and /fa/vendor-panel/stories and public /fa. Visual inspection is permitted; full connected completion, user acceptance and commercial readiness are not asserted. Existing purchased-template seller shell remains mandatory. Returns acceptances, payout backlog and separate contained key state are unchanged.
