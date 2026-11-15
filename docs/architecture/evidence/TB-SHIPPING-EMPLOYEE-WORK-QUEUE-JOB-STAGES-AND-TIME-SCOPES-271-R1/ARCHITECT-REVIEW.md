# Architect review — TB-271-R1 — 2026-11-15

Decision: ACCEPT WITHIN THE ISSUED REPAIR SOURCE/EVIDENCE SCOPE; the 271 queue as repaired is READY_FOR_USER_REVIEW.
Stop: STOP_FOR_USER_REVIEW. Active implementation task: NONE. automaticNextImplementationTask: NONE.

Reviewed repository main e533cad03b6626ca894738d9631f73eb4ef5f7fd, implementation 6632c42d424603f076a9a3a484133f21c056ff5e, submitted Result, current query/normalization/handler, counts builder, job classifier, physical-row obligation projection, frontend default-query wiring, focused query test source and query-selection-count-parity-proof.md.

Source conclusions:
- One InSelection predicate excludes history from active All and selects history explicitly, independent of time. SelectedTotal uses the same predicate as page selection. Stage/history/time badges have the declared dimensions.
- Stage-dependent Delivered/Cancelled SQL exclusion is removed; common filtered physical facts are classified before stage/time selection, so the archive badge no longer depends on the selected tab.
- Positive hasOpenObligation/preparation/allocation facts preserve blocked active work; absence of action permission no longer proves terminality. Actual returned remainder/history remains distinct.
- The live handler calls QueryWithCountsAsync once. This reads one server now and classifies once, deriving rows, counts and paging from one materialized set. This is NOT a claim of serializable or atomic cross-module snapshot.
- No-sort survives Normalize and the frontend QUEUE_DEFAULT_QUERY sorts=[]; explicit sort remains explicit. Unsupported advanced selection fields and unsupported top-level selection operators/cardinality are refused before query execution.
- New job-time boundary validates accepted zones and returns an explicit zone-unavailable/undated exception rather than silently applying UTC.

Evidence limits:
Worker reports 17/17 query repair, retained classifier 15/15, mounted queue 12/12, node 9/9, fulfillment 6/6 and successful compile/build/publish. Architect reviewed source/evidence, did not independently run these tests, a local browser or the user's runtime.
Live query/DOM receipts describe four active undated rows and one history row; history/active isolation and badge parity were observed by worker. Dated urgency/order, cutoff and independent zones rely on controlled tests because current data has no dated active example. No large-archive/performance certification.
No user VISUAL/FUNCTIONAL acceptance, all-shipping/carrier/provider/financial/customer-consent acceptance or real two-writer race is claimed.
270-R4 remains accepted within its four-defect repair scope; historical 271 PARTIAL and 270-R2/R3 PARTIAL reviews are not rewritten. The missing 270-R2 pre-apply backup remains disclosed; later backup is not retroactive protection.

Review URL: http://127.0.0.1:3000/fa/admin/fulfillments (same screen /admin/fulfillments).
User review: inspect stage/time controls, verbs/blockers, counts and exact detail links using existing records. No screenshot request, new fixture, mutation, reset or automatic next task. Existing user data stays intact.
