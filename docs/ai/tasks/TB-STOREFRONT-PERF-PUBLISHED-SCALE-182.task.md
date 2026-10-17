PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-PUBLISHED-SCALE-182
Parent-Task: TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: dbbfa4c3b997b5cab000763d1efb70c49557907d
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_AUTHORIZED_SCRATCH_CATALOG_PUBLICATION_AND_SCALE_PROOF
Hard-Timebox: 45 minutes HARD; stop PARTIAL/BLOCKED rather than exceed.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the remaining PERF-CRIT-001 two-size Home performance evidence using ONLY real owner-authorized publishing of disposable demo products. TB-181 proved that demo seed produces Draft products (151 total but only 3 Published) and the storefront consumes Published. Authorize no generic permission bypass, no hand-authored SQL rows, and no production publication. Work on an independently guarded disposable scratch tenant/database. If no legitimate bounded publication route or valid prerequisites exist, stop with a precise blocker instead of repeating seed loops or falsely claiming a 4x storefront size.

MANDATORY FIRST CHECK
Verify HEAD == origin/main == Expected-Base-SHA before edits, or STOP. Read docs/planning/MUST-FIX-BEFORE-RELEASE.md, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md and TB-178 through TB-181 evidence, especially TB-181 product lifecycle/seed ownership and request-scoped measurement methodology. Inspect the actual Catalog publication commands/validators, Development demo seeder, StorefrontComposer Published filter, and the supported actor/tenant permissions. Respect source-folder and namespace conventions, architecture guards and exact Task-ID/path naming. Reuse proven scratch scripts; avoid bootstrap reruns where possible.

BOUNDED IMPLEMENTATION AND ACCEPTANCE

Preflight an isolated loopback PG + private Host with real Development bootstrap actor and only disposable data. Fail closed if any shared data, service, process or port could be touched. Confirm actual starting Published storefront item count (size A), product/offer/price/stock/seller relationships and no unintended published catalog.
Investigate a LEGITIMATE existing Catalog-owned publication action or supported demo fixture lifecycle. If the currently available 151 Demo Draft products can be published via real Catalog CQRS owner endpoints, only publish an appropriately bounded selection through those endpoints with genuine validators/authorization and all required referenced prices, offers, inventory and sellers. Do not bypass publication validation, write directly into product status, forge actor grants, clone rows, or seed the real store. A failed prerequisite is a stop-worthy source-backed blocker. The task does NOT authorize adding a public route, changing product lifecycle business rules, or altering existing shared data.
Only if legitimate, compare Home at size A vs size B with >=4x PUBLISHED storefront items, not >=4x total Draft DB rows. Capture exact Published counts and actual distinct product IDs in payload for both sizes, plus one warm-up and three warm measurements, HTTP 200, body bytes, response timing, and a request-scoped EF command count via a diagnostic EF=Information leg (never equate quiet logs to zero DB commands). Retain TB-178 Warning default. Do not compare measurements from differing data snapshots, log overrides or binary versions without clearly labelling the difference.
Verify payload integrity for each size: product IDs/names, sellers, prices/currency, stock/availability and links/media where present; ensure unrelated baseline products retain identical substantive fields, with no cross-tenant leakage. If size B cannot represent a realistic complete published catalog, report precisely why, and do not interpret an incomplete fixture as proof of normal scale.
If request-scoped commands grow disproportionately with published item count, identify exact owning module and query call sites and recommend ONE smallest follow-up implementation slice; do not combine a broad N+1 rewrite with this evidence task. No need to recreate logging fix. Keep PERF-CRIT-001 OPEN unless its full register acceptance, architect review and user visual responsiveness are actually established.

SAFETY AND TIME
One task only. Strict 45-minute hard stop, no polling/long seed loops. No shared :3000/:5088/:5432, tooba_alpha/store-alpha, Next caches, Redis/RabbitMQ, shared Host or dirty files; no service restarts, shared DDL, real customer data, payment, SMS, payout or carrier integration. Only task-owned scratch resources; exact-PID cleanup and port verification. No resets, stash/rebase/force push, unreviewed grant injection, fake publication or made-up data. Production changes only if a narrowly scoped, owner-correct defect is demonstrably necessary, separately tested and within time; prefer evidence-only. Contracts-only module communication, MediatR ISender, Result conventions; no Host business logic, Results.Json or architecture guard changes. Commit/push task/evidence with fast-forward parity.

PASS / PARTIAL / BLOCKED
PASS only with two true PUBLISHED storefront sizes >=4x, authorized fixture provenance, same-binary scoped measurements and item-level integrity, complete scratch cleanup. PARTIAL if legitimate progress but one required proof missing. BLOCKED if Catalog lifecycle/authorization makes it impossible within approved boundaries; provide precise missing authority/route and do not repeat failing attempts.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-PERF-PUBLISHED-SCALE-182
Parent-Task: TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Naming-And-Architecture-Guards: ...
Published-Fixture-Provenance: ...
Size-A-And-B: ...
Request-Scoped-Performance: ...
Payload-Integrity-And-Tenant-Parity: ...
Focused-Tests: ...
Remaining-Release-Gates: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
