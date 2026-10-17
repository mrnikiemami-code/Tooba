TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165
Plain text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165
Parent-Task: TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 85083b40e1823add0e60597d185b0463cf5051a3
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_REAL_STOREFRONT_UNAVAILABLE_METHOD_UX_AND_PRIVATE_BROWSER_ACCEPTANCE
Hard-Timebox-Minutes: 45
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_TASK
Goal

Finish the customer-visible portion of TB-163/164 strict Shipping-owned rates WITHOUT enabling production defaults. Implement a truthful, accessible explanation for methods reported in StorefrontShippingProjection.UnavailableMethods, then prove with a REAL authenticated customer browser against a disposable private Host + DB + frontend that unavailable methods cannot be selected and that payable amounts use per-method owned quotes (two sellers, same tipax:standard, 205000 IRR each). No mock browser screenshots, no invented price or shipping commitment.

Mandatory preflight
Confirm HEAD == origin/main == Expected-Base-SHA, otherwise STOP with SHA difference; preserve unrelated dirty files/stashes.
Read canonical documents docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md, docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md, and TB-163/164 result/evidence; inspect exact current frontend projection normalization and shipping method-selection components before editing.
Reuse TB-164 private scratch harness (copy/adapt task-owned scripts rather than changing its evidence). Validate fail-closed isolation before starting any process or applying migration. Shared ports :3000/:5088/:5432, databases tooba_alpha / store-alpha, RabbitMQ, Redis, shared Host, unrelated local dirty work are READ-ONLY/UNTOUCHED. No broad process kills; clean up only PID+path-matched private processes and explicitly named disposable container.
Implementation scope (smallest owner-correct change)
On the frontend, consume the additive authoritative UnavailableMethods response through the existing storefront API boundary; preserve OFF compatibility when the property is absent/null.
For an option rejected by owner-quote pricing, show a short localized Persian reason on the correct method in the correct seller selection context (e.g. dedicated rate missing, restricted coverage, measurement required, destination unknown, currency mismatch, owner unavailable). Translate stable server reason codes only; use a generic truthful fallback for unknown codes. Never display 0 or رایگان for unavailable pricing, never offer a selectable clickable option for it; never attribute another method's rejection to a sibling.
Method becomes selectable only from the existing server's eligible Methods, not inferred from an unavailable entry. An unavailable entry is explanatory only, cannot override eligible server data, and never mutates cart group membership or fee calculations.
Continue remains fail-closed if a group's previous selection becomes invalid, without erasing other groups' schedules; preserve real server-owned group keys, one-group legacy behavior, RTL and storefront styles. The default-off configuration remains false across all committed appsettings; preserve the observe-only gates as-is.
If UnavailableMethods lacks sufficient identity/provenance to map safely to the correct seller/method, do NOT fabricate. Either implement the smallest additive backend attribution from already trusted inputs, or STOP/PARTIAL with the exact gap; do not make a page-wide error masquerade as a per-method failure.
No Admin redesign, no create-method API, no coverage policy changes, no parcel measurement fabrication, no schema/migration, no Financial/Seller/Marketplace/Carrier payout invented, no checkout final click, no PSP/SMS, no Host business logic, no Results.Json.
Tests and required actual proof
Focused frontend deterministic tests for OFF null/absent property, independent seller/method attribution, unavailable fail-closed selection, reason fallback, group B unchanged when group A loses eligibility, RTL/read-only semantics, and one-group compatibility. Run TypeScript typecheck and targeted shipping tests (no broad repeating suites). No files >800 LOC newly introduced; extract small modules if needed.
Private authenticated browser using real app frontend and private Host with owned-rate gate ON only in scratch overlay; seed the minimal real owner option-specific rate, coverage declaration and necessary tax data. Demonstrate visible two cards with correct values, independent dates/windows, customer-owned quote 205000 per group and payment preview sum 410000, persisted on full reload/back. Capture genuine screenshots and DOM assertions. No final payment.
Negative browser/HTTP case: deactivate or restrict tipax:express in the scratch owner; show method unavailable with precise reason, not selectable; tipax:standard remains selectable/205000, and rejected PUT cannot corrupt persisted draft. Capture HTTP responses and browser DOM.
OFF parity: private Host gate OFF returns current legacy methods/fees, no new visual errors when UnavailableMethods absent. Do not claim an option-based quote is enforced when OFF.
Verify zero early checkout/order/payment mutations, isolation, cleanup, and no shared service modification.
Acceptance reporting
PASS only if the real authenticated private browser AND HTTP/DB proofs succeed, reasons are attributed correctly, and OFF behavior is preserved. PARTIAL on any missing browser leg / unresolved identity / missing real quote proof; STOP rather than falsify a PASS.
Deliver docs/architecture/evidence/TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165/ including RESULT.md, focused-test/typecheck logs, sanitized ON/OFF projections/payment draft, DOM assertions, true screenshots, isolation guard and cleanup log. No secrets/cookies/tokens in evidence.
Commit/push only task-scoped code/evidence to main with fast-forward; exact code/evidence/HEAD SHA and HEAD == origin/main in Bridge result. Preserve unrelated dirty files and no force operations.
At hard 45 minutes, stop, report PASS/PARTIAL/BLOCKED with proven facts and exact next blocker. STOP_FOR_ARCHITECT; NEVER auto-start a second task.
