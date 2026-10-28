PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — seller access, safe replay and usable final handoff
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: FINISH_EXISTING_SELLER_AUTHORIZATION_PAYMENT_REPLAY_AND_UI
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — Cumulative deadline, no new estimate or scope waiver

This is continuation of the SAME240 delivery. Original end contract240 focused minutes; reported53+70+62=185 used,55 remaining before intervening actual focus. Do not restart the clock. Plan50 minutes including recovery, report exact blocker immediately at minute10 if seller write still denied, minute25 if mixed-owner order remains unreachable. These checkpoints do not extend the deadline or authorize calling partial work finished. The Architect recognizes completion is at serious risk. At cumulative240, report actual completed/unmet ledger and delay honestly; do not silently spend another hour or create automatic241.
Required outcome remains Marketplace Platform+SellerA+SellerB, partial/invalid/clear coupon, real Order/manual Pending, safe duplicate UI+HTTP, immutable original Order read and usable existing desktop/mobile payment; preserve already-delivered SingleStore. No PSP/real-money promise.

Read original240 TASK and240-R1 TASK for full acceptance/preservation, plus this current review. This task explicitly authorizes the two existing-module repairs the prior worker treated as outside scope: legitimate seller role commissioning and payment-attempt idempotency. Do not stop for another authorization on those repairs.

## 1 — Reuse actual current state, not another bootstrap cycle

Base source14c7353f9117711d78711a368ade54df00c1e510. Safe fast-forward to issue commit in isolated checkout.
Same task Host5190 Development/Marketplace/Deployment tb239-marketplace/TenantNULL/tooba_marketplace_tb239. Verify actualPID/binary before task-only restart. Reference languages/geography, active Fulfillment type+post:standard,3 offers,2 real sellers,prices/tax/stock and Platform capture ALREADY EXIST.
Country01a181c5-f010-7000-9d19-8290cdacb5d7, area01a181c5-f01b-7000-aeee-36edde29aa92, city01a181c5-f02f-7000-9554-fb47fa3aab6b.
SellerA01a181b0-5c87-7000-976e-ae5ce19864f6; SellerB01a181b0-5c9a-7000-bf9b-8c8e1c47da72.
ARM-LN-01 Platform01a181b0-5caf-7000-9c44-e82f2457a122, ARM-SC-01 SellerA01a181b0-5e93-7000-bcc8-85f8532bd345, DGS-LN-01 SellerB01a181b0-5cd7-7000-8ba1-a45b0df5425f.
Platform draft01a181ca-e730-7000-bfaf-a5d9e5778dee snapshot01a181ca-e8f8-7000-840c-1a09cbb105e5 source6 selection1.
Single-owner successful checkout01a181cf-afd3-7000-a33f-7ea2262fbe6e has manual Pending; preserve as evidence. Its cart was reduced to one line and converted; use a NEW normal buyer cart with all three existing offers for the mixed path. Never treat that reduced-cart order as mixed-owner success.
Do not reset/recreateDB, re-run product/stock seed, bulk catalog reset, migrate/rebaseline, or copy another DB. No research into already-fixed reference data.

## 2 — Seller permission blocker: use the EXISTING AccessControl authority, both real sellers

Root inspected actual source:
- AccessControl.Infrastructure/Directories/AccessControlDirectory.EnsureBootstrapAsync(admin, sellerPartyIds, tenantId, ct) ALREADY creates seller-owner roles, delegable catalog permissions and platform seller ceilings for EVERY supplied seller id, with nullable tenant supported.
- It DOES NOT assign those seller roles to their actual Identity users. Use its EXISTING ListRolesAsync/ListAssignmentsAsync/AssignRoleAsync/SyncUserCapabilityTuplesAsync authority for that missing step.
- Existing AccessControlDevelopmentSeedPrelude.EnsureSupportSeedPrerequisitesAsync requires nonblank tenant and handles only ActorA, then merely syncs: calling it with invented store-alpha/marketplace-platform as business tenant is WRONG for this TenantNULL Marketplace.
- SellerDevContextStore.EnsureAsync resolves the real user/Party membership, whereas later MarketplaceSellerDevBootstrap legacy Publish can overwrite its published pair with a synthetic one. Resolve/reconcile actual pair FIRST within the same scope, not a fixed default Actor GUID.

Authorized narrow implementation: extend the EXISTING AccessControl-owned Development prelude (and its existing Contracts interface only as needed) with an explicit Marketplace commissioning path for BOTH actual seller identities/business memberships. Inside AccessControl, call the existing directory bootstrap with real SellerA/B ids and tenant:null; assign each known actual user the existing seller-owner role for ONLY its own business scope if missing; sync through existing effective-access authority. Idempotent repeat, no duplicate role/assignment, no catalog rewrite or new grantlist.
Host orchestration only invokes that Contracts prelude behind existing explicit local opt-in + REAL Development + Marketplace/TenantNULL context. Validate user/business membership through existing Identity/Party Contracts/seller context before grants. No browser owner field, fake JWT/header authority, public unguarded endpoint, direct SQL role inserts, Platform grant to sellers, or new permission engine. Do not require a seller-scoped admin HTTP route where one does not exist: module-owned guarded commissioning is explicitly allowed.
Do not pass foreign Application/Infrastructure types across a module boundary or put AccessControl business persistence in Host.
Prove actual SellerA and SellerB own authoring write201/200 + foreign seller denied and admin route denied for seller. View-only own-scope write denied through existing legitimate Development control if available; never weaken guard to make publication green.
Then create/select the two SELLER captures via their normal authorized APIs on SAME existing binding/type as Platform, with own labels/rows/fees/dates/hours. Existing Platform capture is not copied into seller identity.
Minimum focused proof: both real memberships get correct owner role; repeat adds nothing; unknown/foreign pair cannot be commissioned; disabled/Production/SingleStore inert; no Platform permission grant. Do not broaden to an ACL redesign.

## 3 — Payment duplicate and JSON wire are explicitly IN SCOPE

Prior receipts: same request key returned same CheckoutId but DIFFERENT PaymentId+AttemptId and checkoutReplayed=false. This is not safe full replay. Do not claim the actual cause until measured.
Root source confirms StorefrontStartPaymentComposer deterministically derives start-payment:commit: and start-payment:payment: from raw key and forwards the latter to Payment.Contracts; Payment StorefrontPaymentOrchestrator passes it to IPaymentDirectory.InitiateAsync. Trace the existing owner adapter/directory/idempotency lookup, not a new coordinator.

Capture sanitized request key equality + derived key equality + persisted key equality, actual actor/cart/checkout and running binary parity. No token/password logging. Repair ONLY the actual owning dropped/transformed key, wrong lookup or replay-path defect in existing Order/Payment code; Payment idempotency remains Payment-owned. No per-request random fallback key, ad-hoc in-memory lock/cache, new replay table, blindly swallowed uniqueness exception, bypassed ownership, or newly fabricated response. Return the ORIGINAL authorized PaymentId AND AttemptId for same key; explicit NEW-key retry remains a distinct existing policy.
Verify sequential and simultaneous HTTP same-key attempts leave exactly one checkout/reservation/payment attempt for that intent (typed in-progress/conflict is acceptable for one concurrent response only if a later same-key replay returns the original, never two payment side effects). Client sync in-flight ref alone is insufficient. Preserve original primary exception/cancellation, no auto retry.

Wire report says camelCase body dropped while PascalCase bound. Root saw StorefrontStartPaymentBody is an ordinary record and endpoint receives it by standard binder, not a custom PascalCase reader. Verify exact served binary, actual JSON wire and current HttpJsonOptions before attributing source. Exercise the browser's REAL existing camelCase payload. Fix narrowly at appropriate existing serialization/endpoint DTO boundary if reproduced; no global blind serializer switch that changes every endpoint. ExpectedSelectionRevision remains TEXT lossless, JSON number refused; missing values may not silently turn into a lawful0. Add one actual endpoint/listener roundtrip fact or live receipt, not only constructing a record in-process.
Existing API token/coupon/address/cart ownership boundaries remain.

## 4 — Finish SAME practical flow, reserve the last15 minutes for visible handoff

Reuse task scripts/credentials privately, existing stock and normal APIs; no second automation framework. Create real3-line cart, read3 PRICED owner groups -> per-group chosen method+allowed date -> all-group save200 -> paymentpreview200 -> StartPayment -> real Order with3 own compact evidences/manual Pending. Each group retains own capture/owner/row/fees/receiving date/ticks on SAME catalog binding.
Actual Promotion coupon on ONE eligible seller/group only, other groups undiscounted. Record own goods/discount/tax facts and final total from server, no new arithmetic engine. Invalid coupon refuses; explicit clear/change invalidates previous confirmation. Existing projection/selection/final coupon context unchanged.
Authorized OrderGET after changing only one taskDEVpublication must retain original own evidence; foreign buyer denied. Existing Order storage/read DTO only, no mutableShipping repricing/new history engine/full JSON copy.

Boot/reuse isolated FE3102 -> taskHost5190. Fresh task-owned normal authenticated browser profile; existing shipping/cards/payment UI only. Capture native shipping AND PAYMENT1440x900,390x844,360x800, exact measured innerWidth==PNGwidth, no horizontal overflow or zoom/mask. Real UI choices/save/payment render must work, not payload/text injection or intercepted /v1. Pay double-click one request, failed destination refresh explicit retry no old money current. At least usable URL+authenticated review window must be handed off if a real page is reached; no API-only handoff claim.
No broad suite or long helper test loop can displace these final15 minutes. If blocked, name actual page/HTTP failure promptly, not generic "no time".

## 5 — Reference seed shortfalls and recovery must not stay invisible

240-R1 forgot ALL THREE TOP recovery updates: current main still says ISSUED despite executed result. Fix this in this task, preserve histories and original cumulative deadline.
Root also saw GeographyDevelopmentSeed returns LanguagesMissing/ContradictoryExistingShape but Host ignores that outcome. Honor non-Completed commissioning as an explicit startup commissioning refusal, not success; do not leave newly staged geography additions eligible for later SaveChanges after a contradictory early return. Keep existing caller Development/opt-in/Marketplace gating; no Production reference seed and no silent overwrites. Do not turn this into a separate geography audit.
Source notes / actual live receipts are separate: root did NOT execute Windows tests/runtime or independently certify the single-owner payment.

## 6 — Preservation, allowed ownership and result

Allowed repairs are AccessControl-owned guarded commissioning through existing Contracts/directory; existing Order/Payment idempotency/wire seams; existing frontend exact integration defects; module-owned reference outcome handling. Normal APIs for task-only seller publications, coupon, cart/address and manualPending. No schema/migration/history/baseline edit, new role catalogue, permission bypass, foreignDbContext, price/discount/calendar authority, parallel UI/client, direct businessSQL, authoredJSON per order or historical repricing.
Only affected narrow checks + necessary build/typecheck + actual HTTP/UI evidence. No fullHost/Order/Payment/Shipping/solution/guard sweeps.
Never touch USER watched3000/5088 or handoff3100/5188 + D:\temp\tb238-handoff-chrome, user tabs/unsaved work, original draft/rates/week/pointer/assignments, review230capture or handedoffcart. No enumerate/read/automate/reload/close/HMR. Task5190+isolatedFE3102 only, verify process before restart; preserve other handedofftargets. No kill-by-name/reset/reseed.
Checked-in gatesOFF/localtaskON only, TB185 paused/default-OFF/PendingAllocation.238unknown22001 remains unclassified unless actual provider metadata proves cause; no blind widening.227mobileempty-note and unrecovered225-R1LOCAL edits remain disclosed.

Save verbatim TASK under docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2/TASK.md and evidence TASK.md; RESULT/RECEIPTS/screens in evidence folder. Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md and SHIPPING-ARCHITECT-RECOVERY, plus deadline ledger:185+actual focus, NOT a fresh budget. Lead result with usableURL, actual stage and acceptance ledger for mixedowner/coupon/duplicate/history/mobile, exact ids/status/artifacts or blockers. No inferred Visual ACCEPT, paid/PSP claim or partial-as-final.
Exact-path normal commit/push HEAD:main/fetch parity, no amend/force/reset/stash/rebase/history rewrite/CRLF churn.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

## RESULT CONTRACT — one canonical result, no diagnostic probe posting

Use actual FULL SHAs, no editorial notes in SHA fields. Append the task-end marker as the final transmitted line after this template. No placeholder/probe result body or overwritten identity.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — seller access, safe replay and usable final handoff
Mode: FINISH_EXISTING_SELLER_AUTHORIZATION_PAYMENT_REPLAY_AND_UI
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actual full issue commit SHA
Base-SHA: actual full source start SHA
Code-SHA: actual full code SHA or NONE
Evidence-SHA: actual full evidence SHA or NONE
automaticNextImplementationTask: NONE

Usable URL and actual completed stage, per-item acceptance ledger with sanitized receipts and artifacts;
original240-minute cumulative consumed/remaining/overrun, honest blockers, parity, preserved user work.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
