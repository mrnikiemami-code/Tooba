# Independent architect review — TB-296-PRICING-FINAL-UAT-CLOSURE

**Disposition: REPAIR_COMMITTED / NOT_ACCEPTED / FINAL_VALIDATION_REQUIRED.** No discount work authorized.

## Identity and limits
- Branch: `opencode/iso-agents-002`; original base `1d777e2e0590581c5ab256901d9d7ce06a325f3a`; worker handoff `512291795dc2cabbbac4e30f424e1690277b1cee`; architect product candidate `4f19708be3d980a22535fb446d3cfaa1a2ddfc9c`.
- Reviewed actual remote source and worker RESULT.md, evidence/connected-verification.json, architecture log and relevant contracts, application, infrastructure, frontend and architecture policy. Worker runtime results apply to the worker candidate, not the repaired candidate. No fresh local build, execution or architecture guard result is claimed.
- Source review follows docs/opencode/ARCHITECT-QUALITY-GATE.md and ARCHITECT-DELIVERY-METHOD.md. Original TASK.md and ACCEPTANCE-MATRIX.json were not found at the expected task paths (404); original pre-issuance gate is unverified. This is an architect issuance/evidence deficiency, not evidence of worker misconduct. Do not fabricate or retrodate it.
- ARCHITECT-VALIDATION-MATRIX.json is an explicitly additive, architect-authored 18-case final validation matrix, frozen before final validation, not a substitute for historical proof. Preserve original MC01–MC06 requirements and worker evidence. Do not issue ACCEPT or acceptance receipt without resolving the original gate requirement and running required checks.

## Material findings and direct repairs
| Finding | Architect repair and owner |
|---|---|
| Registered currency reference with zero active entries could fall back to permissive behavior; missing trusted market could broaden reads | Pricing PriceDirectory: registered reference authoritative even when empty; typed currency parsing, market scope fail closed. No new market authority. |
| Raw fractional amount could differ from stored amount and durable fact/replay | Pricing PriceDirectory: canonical Money amount for write/fact/receipt and replay comparison. Historical records untouched. |
| Authoring read selected a different row/status from writer CAS | Pricing authoring projection selects the same latest record as existing writer and exposes actual status; effective quote resolution is unchanged. This is an explicit architect choice, not a new scheduling or lifecycle policy. |
| Non-default currency creation compared primary store price/revision | Both create routes and shared createAndApplyOffer use selected currency and its own revision/readback. |
| Draft recovery could overlook a concurrent non-primary update or discard unsaved input | Shared offer model, recovery and actions compare selected currency revision, reconcile without stale overwrite, warn before switching dirty currency. |
| Reference decoder and failed fetch could invent usable precision or silently degrade | Shared strict decoder, admin/seller adapters, loading/unavailable/retry state and price write blocking. |
| Precision and locale presentation were incomplete | Reference-scale validation, registry culture numeric parsing, localized fa/en/ru price messages, currency-specific input mode and actual authoring status. |
| Missing regression coverage | Added source-level tests for precision, reference failures, independent revisions, selected currency recovery and authoring/quote distinction. These are not claimed executed. |

The existing Pricing kernel supports configured 0/2-decimal currencies; arbitrary precision expansion is outside this goal. Existing advisory lock is offer-scoped, while currency revisions are independent. No database migration, new host, new database, or unrelated domain change was made by the architect.

## Eight architecture gates
All final AQ statuses are NOT_RUN until the repaired candidate is built, exercised and checked. Source assessments below are not acceptance statuses.

| Gate | Independent source assessment | Final |
|---|---|---|
| AQ-01 | Shared registry locale route/provider and culture formatting retained; localized pricing keys, fa/en/ru; actual registry third-locale and language-control proof required | NOT_RUN |
| AQ-02 | Pricing owns price, precision, market and persistence rules; Offer uses contracts; canonical money and authorization checks repaired | NOT_RUN |
| AQ-03 | Admin/seller compose shared OfferWorkspace, typed adapters and shared create/apply; decoder and recovery shared | NOT_RUN |
| AQ-04 | GetOfferCurrenciesQuery is MediatR IRequest<Result<...>>; thin authorized endpoints dispatch ISender; no-input query guard classification retained | NOT_RUN |
| AQ-05 | Existing workspace, MessageBox and design system reused; no duplicate grid, forms or notification infrastructure | NOT_RUN |
| AQ-06 | Existing module/feature folders and contracts-only references maintained; rerun dependency and frontend guards | NOT_RUN |
| AQ-07 | OfferWorkspace below 800-line source limit in reviewed candidate; actual architecture log previously had baseline failures beyond size, so compare exact identities, not only count | NOT_RUN |
| AQ-08 | Offer.Application references foreign Contracts only; Pricing.Infrastructure references foreign Offer/StoreContext Contracts; no new Host business logic; quote/default behavior preserved | NOT_RUN |

## Evidence gaps
Worker connected test supports admin edit EUR 20.5, IRR integer, USD 13.5, independent SQL rows and reload. It does not alone establish seller UI create/save, real language switching and third enabled locale, normalized durable fact amount, unknown/inactive currency rejection, stock adjustment, complete geometry/variant verification or new candidate correctness. Old architecture exit was nonzero; do not declare PASS from the reported 27 baseline count alone.

## Single bounded final verification
Use existing checkout and branch only. Fetch, verify candidate ancestry and clean tracked worktree; build exact repaired product candidate and run existing Host 5088, Next 3000 and tooba_alpha. Execute all 18 additive IDs and original affected MC01–MC06 coverage, affected regression tests, existing architecture guards and OpenSpec strict; capture concrete HTTP/browser/SQL/fact/permission/cleanup evidence and exact source/build/DB identity. No screenshots, no new host/DB, no migration, no reviewer subagents, no repeated review loops. Use finite command timeouts and report precise last command, phase, process/agent and error if blocked. Continue independent safe cases when possible. Do not loosen guard baselines or immutable criteria. Only narrow mechanical compile/harness corrections allowed under current delivery method; commit separately and report exact diff. Substantive product repairs remain architect-owned.

Always commit and push source and evidence changes, report remote branch HEAD and exact product/evidence SHA, verify remote ancestry and stop at STOP_FOR_ARCHITECT. Never declare PASS for unrun cases. Final independent architect receipt/gate and owner UAT are still required before the discount roadmap can proceed.
