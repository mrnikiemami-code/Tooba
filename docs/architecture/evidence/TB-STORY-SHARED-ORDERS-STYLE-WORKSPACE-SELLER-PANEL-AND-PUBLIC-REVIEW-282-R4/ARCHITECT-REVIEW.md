# ARCHITECT REVIEW — TB-282-R4 — 2026-11-19

Status: PARTIAL; useful merchant-UX progress retained, full A–E closure NOT accepted.
Implementation reviewed: 35b9ffc6406a770147ddfd8fc1ef514f0db20a1e.
Fresh main at review: 08f73aaa7dce43c73ff25d4a67c8ffec6a394314.
The posted outer result names f13ea9c68971e88c38e6ef2a1c03df0f8e7393da; fresh GitHub shows subsequent docs-only final parity commit 08f73aaa. No incoming production delta beyond reported implementation.
Method: independent GitHub source/diff/evidence inspection. Architect did NOT rerun tests/builds or inspect Windows live browser. No live write.

## Retained scoped progress
- Shared add/edit DS Dialog now exists for both role configurations; existing slide edit constructs PUT /stories/{story}/items/{item}, preserving stored media and carrying version/occurrence.
- Save title/language is compact, native file input is hidden behind localized button; placement identity and admin creation choices retained. No duplicate seller shell.
- Story column defaults have title first and title sticky removed. Saved-view order is normalized without dropping filters/widths.
- Existing legacy-grid-bridge automatically pins actions at logical end (RTL left / LTR right) with lockPinned/lockPosition. This is existing shared behavior, NOT a new R4 pin implementation. Architect's earlier assumption that the missing sticky property meant unpinned actions was incomplete; only actual grid runtime/saved-state proof can confirm the reported symptom is resolved.
- Upload/add executor now invokes route binding at entry and actual dispatch; media route regex is end-anchored; update_item recognized. The specific R3 upload-route bypass is corrected at source level.
- Source removal of Development sampler remains. Seller adapter scenario uses actual Story adapter with controlled Offer/title ports.

## F1 — product label can announce the wrong product; reload lacks owning label hydration
StoryManagementScreen.tsx:215:
return productLabelById[id] ?? (selectedProductLabel || copy.productSelectedFallback)

selectedProductLabel is ONE current editor selection, not the product owned by this slide. Concrete counterexample: two stored slides point to different products A/B; the map starts empty; choose A in one dialog; the other row, whose B key is absent, now prints A's name. Cancelling the dialog does not restore a per-row identity binding. The backend target is not changed by this rendering bug, but merchant text falsely identifies it.
The map is filled only on picker click/success (lines 1156/1281/1342); no stored-product id hydration exists. After reload an existing target outside the current options page remains a generic selected-product label rather than its own name.
Finish issued C: resolve stored selected ids through lawful owner reads/DTO, scoped by Story/shop, render each label from its own target only; unavailable is explicit and never borrowed from another editor. Keep targets intact, including if not on current options page. Test two different stored products, reload, search pagination, and cancel after changing selection.

## F2 — required connected proof is incomplete; keep the useful tests but correct claims
R4 mounted test opens the existing-slide dialog but never edits fields/saves it; no captured PUT payload/no-upload/no-delete assertion, no wrong-target/unknown response card or no-navigation assertion. Thus it does not prove the requested update behavior beyond opening UI. Test actual change-and-save once for admin and seller role cases with correct media/version/key/body; cancel sends nothing.
The named R4 negative-control fact only calls current bindStoryEnvelopeRoute/current verifier. No pre-fix production execution or temporary precise regression is recorded; do not label this a completed negative-control experiment.
No R4 served DOM geometry/scroll receipt at 390/1440 or real grid old-saved-view assertion is supplied; the ledger's A unit+mount claim is broader than the tests. Verify actual shared grid pin/order and compact controls with supported mounted/live read-only methods; controlled vs live must be explicit.
StoryProductOptions282R4Tests first fact tests real StoryProductOptionsAdapter against FakePublishedSearch, but that fake itself implements all search/filter/paging/count logic. Therefore 55-product assertions prove contract delegation, NOT the new CatalogPublishedProductSearchLookup query. FakePublishedSearch.SamplerUsed is never assigned, so Assert.Null is not evidence that the production sampler is unused. Add the issued focused owning Catalog query test (>40/search beyond page one/disjoint pages/count, controlled or rollback-only), preserving the already useful seller adapter isolation test.
Do not report these proof gaps as observed production money/ownership failures.

## Scope / recovery
No new feature phase or redesign. Finish F1 and the existing focused acceptance obligations inside this closure scope. Exact issued TASK.md remains immutable; append corrections to historical reports instead of rewriting prior PASS claims.
No new task issued by this review; activeTask NONE; automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT. Owner can visually inspect current screens, but full connected/user/commercial acceptance is not claimed.

Worker reported: focused FE 13/13 + retained 14/14; Host S2 2/2; tsc/build green; Next BUILD_ID VKOLCT7WFUKHA67tlaFV-, PID13564; Host18724 unchanged DLL 4B6CE54076E736A2CD2C6379F957CFDCFB03AA20CF3470F54F437DD3B4A6FC53; routes/health 200. Architect did not independently execute these.
Known unrelated critical-storefront bg-white failure remains disclosed. Some detail toolbar labels remain Persian-primary; not expanded into another language redesign task.
ZERO migration/live business writes reported. Preserve existing VendorShell /story.view, Returns acceptances, payout backlog, separate contained key state and all user data.
