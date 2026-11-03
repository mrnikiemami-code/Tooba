PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R8
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Storefront — correct rial amounts, cart totals and seller grouping
Mode: REPAIR_ACTUAL_CART_MONEY_AND_GROUPING_ON_CANONICAL_RUNTIME
Expected-Base-SHA: 4ce605905af623edcbce96ca5c46decfdc2d4817
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER REPORT / ACCEPTANCE TARGET
The user reports /fa/cart shows fractional IRR prices, zero goods/payable totals despite three priced lines, and no meaningful seller grouping. Screenshot shows Samsung and Huawei from Arman, Xiaomi from Nilva. They require recovery documentation updated.
No new screenshots. Fix actual behavior on canonical user source/FE/backend/database, not a new preview app.

CONFIRMED SOURCE FINDINGS

src/frontend/app/storefront/storefront-api.ts formatOfferAmount uses Intl.NumberFormat("fa-IR") without currency precision; IRR fractions are rendered.
storefront-cart.tsx renders cart.lines.map directly; seller label is repeated inside each isolated line, no seller section.
storefront-cart-api.ts mapStorefrontCart maps missing/invalid subtotal through asNumber default0; cart UI also defaults missing subtotal to0. This is a plausible zero-total masking path, NOT an independently measured backend root cause. Capture actual response and trace authority before choosing the fix.

A — MONEY AUTHORITY / ZERO TOTAL
Read actual canonical cart response, sanitized; trace per-line and root subtotal values/types through owning Cart/quote service, DTO and mapper into DOM. Distinguish omitted/mismatched/invalid root amount from lawful zero.
Repair the owning defect. Nonzero priced lines must not coexist with a fake zero subtotal/payable. Invalid/unpriced/loading totals show a clear unavailable/pending state and appropriate action guard, never zero fallback.
Keep authoritative currency/discount/tax semantics. Do not blindly sum formatted labels or infer totals from product prices. Where contract defines subtotal as sum of quoted lines, derive/verify using exact decimal arithmetic at its owning seam; avoid JS floating-point money loss.
Cart is pre-shipping/pre-final tax: labels must honestly identify known goods totals and pending shipping/tax. Do not call an exclusive-tax goods subtotal the final payable if it is not final.
Retain legitimate free goods/zero totals. No data mutation to force green.

B — IRR PRECISION
Customer-facing IRR must render integer rials, with locale separators and no fractional suffix, across this cart's unit/line/group/summary and directly shared mini-cart/checkout amount displays.
Use the repository currency/rounding authority and document its actual policy. Distinguish fractional tax-exclusive intermediate values from chargeable totals. Preserve internal precision where needed; do not round unit then multiply or independently round rows into inconsistent grand totals.
If chargeable IRR lacks a canonical rounding boundary, implement it in the owning money/quote calculation seam with an explicit deterministic policy consistent with existing architecture; do not make display formatting redefine stored orders/payment amounts.
Non-IRR currencies retain registered precision (do not globally force zero decimals). No currency conversion or IRR→toman relabeling. No historical repricing/backfill.
Prove displayed subtotal/discount/tax/fees/grand total reconciliation under the chosen existing policy; retain exact server evidence for final commit.

C — SELLER GROUPING / POLISHED EXISTING UI
Group cart lines by stable sellerPartyId, not sellerDisplayName, product ID or method label.
Two Arman lines appear in ONE seller section; Nilva in another. One clear section header/name and optional authoritative/valid seller subtotal; compact product rows underneath. No repeated giant card/header, duplicate delete controls or technical explanatory paragraphs.
Same-name different seller IDs stay separate. Preserve stable line keys, quantities, availability, variant identity, links, delete/update and coupon context. Unknown seller identity must not silently merge unrelated lines.
Cart seller sections are NOT delivery commitment groups: one seller may later have distinct Platform/Seller/method obligations. Do NOT merge/renumber accepted shipping groups or change owner/responsibility/calendars/shipping fees. No claim cart grouping alone proves shipping grouping.
Use existing design system/theme/RTL/locale. No new framework or broad redesign. Keep fast navigation/pending feedback from 245 and all 249 carrier/physical-spec fixes intact.

D — BOUNDED STATE / VERIFICATION
One owning cart data flow; do not add render/effect fetch loops. Cancel/ignore superseded responses. Test quantity mutation/cart event doesn't install an older subtotal or duplicate revalidation.
Focused executable tests:

three lines/two seller IDs -> correct sections, nonzero consistent totals; same-name distinct IDs; one seller; unknown identity.
actual root DTO amount type/path; missing/malformed subtotal never silently0; lawful zero preserved.
IRR fractional intermediate, discount/tax and multi-qty rounding boundary; appropriate non-IRR fraction regression; no precision loss.
mounted actual cart: seller sections + unit/line/summary amounts, update/remove/coupon state, bounded network calls; source-token tests alone insufficient.
Run affected typecheck/build and relevant focused money/Cart tests; no unrelated broad sweep.
Canonical authorized read-only proof on the user's actual cart: endpoint/DOM identity and nonzero totals, grouping by real seller IDs, no IRR decimal glyph. Do not submit checkout/order/payment, remove/add lines or modify merchant prices/weights/rates to manufacture proof. Use isolated fixtures for mutations. No screenshots or image work.

CANONICAL DELIVERY / RECOVERY
D:\Users\User\source\repos\SarvNewVer FE http://127.0.0.1:3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. Same source and DB; no D:\temp app as final runtime, new final port or replacement buyer/cart.
Read actual base/main; preserve intervening commits and unsaved user work. No reset/stash/rebase/force, business SQL writes, reseed/reset. No migration expected; justify any additive one and use backup+normal runner+readback. Preserve DataProtection.
Keep R8 accepted narrowly for reviewed ambiguous-create recovery; do not declare live Tipax verified. User defers credentials and actual carrier calls.
Update ALL THREE recovery authorities:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record new cart issue, source root cause, exact resolution/proof, final canonical source/runtime and open external-only Tipax verification without erasing prior history. Do not claim Visual/User ACCEPT.
Preserve full TASK under docs/ai/tasks/TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250/TASK.md and evidence TASK/RESULT/RECEIPTS.
Report exact code/evidence/base SHA, real root total values before/after, currency rounding authority, seller section counts, tests and deployment. No automatic next task. STOP_FOR_ARCHITECT.
END_TOOBA_TASK
