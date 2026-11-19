# Architect review — TB-STORY-281-R1

Date: 2026-11-19 (Asia/Tehran)
Verdict: PARTIAL; first consolidated delivery NOT ACCEPTED; connected UI delivery NOT ISSUED.
Reviewed head ae947c863867437c13008029971a77eeb90edd41; implementation eb69ac7da58ab8ffcbeed16f01280f94800551c2. Independent GitHub source/diff/assertion review; no local tests/runtime/database writes by Architect.

## Closed increment (retain, do not rebuild)

B1 attach-before-readiness public reader is corrected; a fresh-context InMemory Directory fact now catches the prior ignored-Items regression. Role-specific MapAdmin and grid mapping remove the admin/seller union. Missing Party/Media now produces a typed refusal on the paths that call those seams. Classic admin version parameters and canonical length-prefix create hash were added. Preserve these changes; they are useful progress, not full first-delivery acceptance. Seven focused facts/52 pins/6 FE mapping tests are worker receipts, not the required complete owning/relational matrix.

## Still open — same issued work, no expanded feature

1. Media: UploadForSellerAsync still calls UploadAsync that separately saves Ready asset/object before owner/occurrence rows. The catch deletes the object then calls SaveChanges on the SAME failed tracker including pending binding/occurrence entries, swallowing compensation errors. A transient save failure can leave inconsistent metadata/result and a repeated occurrence can upload again; a unique collision is not specifically arbitrated. The intent digest addition alone is not atomic upload/replay proof. There is no controlled recording-store/save-failure suite.
2. Ownership/readiness: admin-for-shop cover create, all owned cover update/admin item paths and submit/publish revalidation are unfinished; admin shop upload absent. Optional services now refuse at selected seams but readiness still assumes a Guid means Ready. Complete the original authority boundaries.
3. Occurrence/version: original seller create/add/submit and item mutation/ordering paths still lack full durable identity/version fences; new lifecycle methods still do version-before-recorded-replay and do not record their own occurrences. No complete all-route matrix.
4. B5 is PARTIAL, not fully closed: role union is fixed, but policy STILL only advertises return_to_draft for Active/Scheduled. After SellerStop sets Disabled/Approved, lawful return-to-draft becomes invisible; the new action test never executes stop->read->return. Enrichment across old list/detail/grid/public remains incomplete.
5. B6/B7 remain OPEN exactly as worker reports: seller server query/count, admin human shop-name filter, product CTA ownership/public identity and Media thin CQRS + canonical tenant seam. These were already required; no external blocker or authorization problem was stated.
6. Required PostgreSQL rollback facts, Media object-store failures, registered DI/HTTP seam proof and precise contract handoff remain unrun/unfinished. Do not replace these with architecture pin counts.

## Provenance and runtime receipts needing correction

Issued R1 receipt and original downloadable file SHA256 are 05ca4b11bccc68281174ce44fbc12a6b1af9809e73a0ec2e1afbc89190673e48,16772 bytes. Worker RECEIPTS says 2BCA9E9A... instead. Repo TASK.md was not changed by the R1 compare. Preserve history and append a truthful transport/hash/actual claim clarification; do not silently replace the issued envelope or retroactively assert a different base.

The reported Development pricing.overlap restart issue is not acceptance evidence for a fresh served process. Program.cs:365–440 runs module bootstraps in Development; do NOT repeatedly restart into canonical business seeding or repair Pricing data. Prefer a documented existing skip-bootstrap path; if absent the completion envelope may permit ONE composition-only process opt-out preserving Development auth and edition. No business seed, pricing mutation or environment/DB switch is permitted. Verify actual listener PID and loaded artifact after a safe refresh; disk hash or a previous serving process alone is insufficient.

## Next step

ONE continuation of the unfinished first-delivery scope, with a concrete completion ledger and original G1–G7 proof. No UI implementation or new product capability until reviewed. Do not treat “only focused subset completed” as a scope-completion reason; continue in-scope work unless there is an actual reproducible external blocker. Parent/earlier PARTIAL evidence and applied migrations stay immutable. No user/provider/Marketplace acceptance, live business write or key operation claimed.
