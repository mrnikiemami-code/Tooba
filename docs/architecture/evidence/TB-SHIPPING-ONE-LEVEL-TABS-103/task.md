PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-103
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-102
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_SCRATCH_BOOTSTRAP_PREREQUISITE_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: cbbdc7cbe196914d0df2af7925e9492d22ebfe5e
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. At deadline CLEAN UP and STOP; do not extend to finish proof.

ARCHITECT RULING
Task 102 is accepted BLOCKED_ISOLATED_CART_PROOF: real scratch customer registration, login and empty cart were reached, but no genuinely sellable offer exists. CatalogDevelopmentSeed's attribute/schema post-migration bootstrap attempts PublishProductAsync on a new database and fails with «محصول برای انتشار آماده نیست». The allowed fix is the MINIMUM Catalog-owned Development-only seed prerequisite/ordering correction using official Catalog domain/application commands or a pre-existing authorized demo seed API, exclusively on a disposable scratch database. Do not manufacture offer/inventory/price/attributes through raw SQL; do not relax PublishProductAsync domain rules. The purpose is one authentic sellable offer, not redesign of Catalog.

PREFLIGHT — SAFETY AND DRIFT

Claim exactly one Pending task on tooba-main, save receipt UNSTAGED, verify Expected-Base-SHA is ancestor of origin/main. Accept only nonoverlapping result-bookkeeping drift; STOP RECOVERY_CONFLICT on overlapping source. Never reset/stash/rebase/force-push or touch pre-existing dirty/untracked work.
Read AGENTS.md, pipeline/controller, 102 evidence including legacy-bootstrap-crash-excerpt, CatalogDevelopmentSeed.PostMigration, CatalogAttributeSchemaDevelopmentSeed, CatalogDirectory.PublishProductAsync and the official demo seed/reset endpoint if any. Verify the specific missing publish prerequisite from source and error/result, not by guessing.
Read-only snapshot of shared :5088/:3000 provenance and tooba_alpha counts. No shared writes, restarts, gate changes, session/carts/orders or storefront transactions.

MISSION A — SINGLE PRECONDITION REPAIR

Decide the smallest supported scratch-only path: (PREFERRED) invoke an existing authorized Development demo seed API or bootstrap in its supported order; (ALTERNATIVE) make the narrowest Catalog-owned Development seed-order/prerequisite correction if source proves a genuine defect. Keep domain publish validation, merchant authority, and production behavior unchanged. If neither is provably safe within time, STOP BLOCKED_CATALOG_SEED_AUTHORITY with concrete missing prerequisite and a repair recommendation; do NOT invent SQL fixtures.
If source fix is needed, change ONLY the Catalog Development seed owner, its direct focused tests and narrowly necessary wiring. No changes to Order, Shipping, Fulfillment, Checkout, production Catalog business rules, financial modules, architecture guards without evidence, or unrelated frontend. A new generic seeding framework is forbidden.
Create disposable loopback Postgres and isolated Host from current code, with exact offline effective-config guard (all references scratch-only), DB-side isolation barrier, isolated /__platform-commerce provenance, generated ephemeral secrets ignored and deleted. Run only the authorized seed/bootstrap. Prove with API/readback that at least ONE genuine product/offer is publishable and sellable under the existing domain/application rules (including any price/inventory prerequisite). Distinguish "published" from "sellable"; if publish succeeds but offer is unavailable, report precise gap.

MISSION B — BOUNDED PROOF, IF TIME REMAINS

Only after a sellable offer exists: use supported register/login/cart/create-line APIs to add one line to an owned scratch cart. Assert successful real cart-line addition, not merely a seeded offer. If <=8 minutes remain, SKIP storefront label comparison and stop with PARTIAL; provide the ready prerequisite for a dedicated next task.
Optional if time remains safely: real authenticated POST /v1/storefront/shipping/projection gate OFF and gate ON on the SAME scratch cart, same language/scope. Assert only translated human Label differs; exact eligible method set, methodCode, price, lead days and other monetary fields identical. One-level catalog needs a matching eligible method with a deliberately distinct scratch label set through authorized existing bootstrap/module seam; never write raw shipping tables to fabricate it. If this cannot be done without expanding scope, do NOT do it.
Do not create an order, initiate Checkout/payment, activate inactive methods or enable ShippingRateAuthority/ShippingOperationalShadow. No fake identity headers, guessed credentials or unauthorized control paths.

MISSION C — PROOF, CLEANUP, HANDOFF

At time limit, stop isolated processes, remove scratch containers, output/run dirs and secrets; verify isolated port closed and shared Development processes and DB counts unchanged. Cleanup takes precedence over finishing a test.
Focused Catalog tests only if source changed; do not execute broad known-failing Host suite. Record actual failing/green tests and precise elapsed time. If no code modification, no unrelated test run needed.
Commit/push only allowed code/tests (if strictly necessary) plus docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-103.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-103/**. Preserve receipt UNSTAGED, include base/code/evidence SHA, read-only shared parity and isolation/teardown logs. Do not append master recovery outside allowed scope.

HARD ACCEPTANCE
PASS: official Catalog bootstrap produces a truly sellable offer on disposable scratch with a successful owned cart-line add; optionally complete storefront parity if time allows. PARTIAL: supported seed fix tested but genuine offer/cart line unproven. BLOCKED: exact missing Catalog precondition remains or correct fix needs larger architecture/authority. NEVER label source-only proof as live, and NEVER exceed 30 minutes.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-103
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-102
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_SCRATCH_BOOTSTRAP_PREREQUISITE_REPAIR
Bridge-Task-Row: <assigned at claim>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: cbbdc7cbe196914d0df2af7925e9492d22ebfe5e
Base-SHA: <actual>
Code-SHA: <commit or NONE>
Evidence-SHA: <commit>
automaticNextImplementationTask: NONE
Provide exact paths, catalog root cause, official seed route/ordering proof, real scratch product/offer and cart-line HTTP outcomes if reached, test counts, unchanged shared parity, isolated cleanup and actual minutes.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK