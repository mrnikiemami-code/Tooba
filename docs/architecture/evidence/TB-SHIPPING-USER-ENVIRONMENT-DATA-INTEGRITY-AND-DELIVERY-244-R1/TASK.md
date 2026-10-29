PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1
Parent-Task: TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — restore merchant-data integrity and finish the actual cart lawfully
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REVERT_GUESSED_MERCHANT_DATA_THEN_DELIVER_WITH_AUTHORITATIVE_VALUES_ONLY
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Architect ruling on 244

244 is **REJECTED** as delivery despite its worker PASS. It reached the canonical runtime and actual cart, but it made business-data guesses forbidden by the task:

- four previously unassigned offers were all rewritten as Platform + one shipping type without an authoritative per-offer decision;
- `demo_attr_mobile_weight` was changed from g to kg using sibling values as inference;
- three different products were all assigned `1.5 kg` rather than an authoritative product/package weight;
- the original `[1000,2000)` rate was expanded with `[0,1000)` and `[2000,1e9)` at the same charge, inventing a nearly unconditional rate;
- the accepted draft/payment preview therefore depends on guessed responsibility, measurement and price coverage.

Canonical runtime/account/cart identification and the no-Order handoff are retained as useful evidence. The guessed merchant configuration and its accepted selection are not accepted.

# Required outcome

On the same canonical checkout, primary 3000/5088, SingleStore/store-alpha/tooba_alpha and the same real user/cart:

1. safely restore every task-244 guessed merchant-data mutation to its exact pre-244 state unless a later independent user edit exists;
2. invalidate/replace the draft selection derived from those guesses through normal APIs;
3. find and use only authoritative existing product/package weights, per-offer fulfillment responsibility/type and merchant-approved rate coverage;
4. if those business values do not exist, stop at one compact decision table for the user. Do not manufacture another green path;
5. if authoritative values do exist, complete the same actual-cart projection → explicit date/hour selection → one selection PUT → payment-ready form, leaving the cart unsubmitted.

No new implementation task follows this one.

# A. Safe compensation ledger

Before mutation, read current values and compare them with the exact 244 receipts. Build a compensation ledger for:

- the four offer fulfillment assignments;
- attribute definition `01a05387-d504…` unit;
- the two category bindings added by 244;
- the three product attribute values set to `1.5`;
- the two adjacent rate rows added around the original `[1000,2000)` row;
- the selected publication/snapshot and the actual cart's accepted draft groups.

Use normal owning Admin/Offer/Catalog/Shipping APIs. Do not use business SQL, reset/reseed, edit migration history or delete unrelated data.

Compensate only values still exactly equal to the 244 write. If any value was independently edited after 244, preserve it and report an exact conflict instead of overwriting it.

Restore:

- offer fulfillment to its exact pre-244 null/unassigned state when still equal to the 244 assignment;
- the attribute unit to its exact pre-244 `g` when still equal to the 244 change;
- only the category bindings created by 244;
- only the three product weight values created by 244;
- only the `[0,1000)` and `[2000,1e9)` rows created by 244; keep the original `[1000,2000)` row unchanged;
- publish the restored draft through the normal publication path.

Remove or supersede the actual cart's selection derived from snapshot `01a185c1-458a…` through the normal selection/draft capability. No stale guessed evidence may remain presented as payment-ready.

# B. Authoritative-data investigation

For each of the four actual cart lines, trace only stored authoritative sources:

`line → offer → seller → fulfillment assignment → variant/product → active Catalog shipping-weight definition/value → unit → quantity-total → full owner/type → active binding → selected capture → exact matched rate`.

Weight rules:

- an explicit active Catalog Number attribute intentionally designated by the merchant for shipping/package weight is acceptable;
- product specifications, descriptions, sibling products, modal values, names, internet/manufacturer lookup or “typical phone weight” are not authority;
- do not change a definition unit to reinterpret existing values unless an existing migration/configuration record proves that unit was wrong for the entire definition;
- do not assign the same value to different products merely to make a rate match;
- total group weight must use the authoritative per-unit value and cart quantity with checked conversion/overflow.

Fulfillment rules:

- use an existing explicit per-offer assignment or a merchant configuration that deterministically maps that offer;
- do not classify all offers as Platform or one type because the edition is SingleStore;
- Seller/Marketplace/Store ownership and the real seller remain distinct.

Rate rules:

- use only merchant-authored ranges;
- do not widen, erase or clone price rows to cover missing measurements;
- a gap is an honest unavailable result requiring a merchant decision.

# C. One minimal business-decision table if data is missing

If authoritative data is absent after the investigation, do not mark PASS. Return one short table containing only the missing decisions:

| Offer/product | Missing responsibility/type | Missing per-unit shipping/package weight + unit | Resulting group total | Existing rate coverage | Exact merchant decision required |
| --- | --- | --- | --- | --- | --- |

Use the real sanitized offer/product labels and IDs. Ask no broad technical questions. The decision must be something the merchant can answer directly, for example “weight of packaged unit in grams” or “Platform vs Seller responsibility and shipping type.”

The canonical runtime must remain healthy and the cart intact. It is acceptable for the page to show an honest actionable unavailable reason until the merchant supplies the missing values. It is not acceptable to call that delivery complete.

# D. Lawful completion when authority exists

If all required values are authoritative:

- configure only through normal owning APIs;
- preserve the original weekly schedule and 60-minute slot duration;
- project the same actual cart without deleting/replacing lines;
- show every group with its real owner, price, dates and configured hourly slots;
- require explicit date and hour per group;
- issue exactly one successful selection PUT with revision TEXT and exact echoes;
- load the existing payment draft/form with matching amounts/windows;
- verify accepted draft evidence from tooba_alpha;
- leave cart Active and unsubmitted with zero checkout/order/reservation/payment created for this proof.

# E. Canonical runtime and UI bounds

- Keep source/runtime in `D:\Users\User\source\repos\SarvNewVer`, FE 3000, Host 5088, SingleStore/store-alpha/tooba_alpha.
- Preserve user LOCAL changes, current account/cart and unrelated tabs.
- No replacement buyer/cart, D:\temp runtime, Marketplace DB, new ports, screenshots, PSP, payment or Visual ACCEPT.
- No source change is expected unless a real source defect is independently reproduced after the guessed data is removed.
- Checked-in gates stay OFF; local primary settings may remain ON.
- TB185 stays paused/default-OFF/PendingAllocation.

# F. Focused verification and evidence

Run only focused checks required by an actual source change. Do not use tests to substitute for the real-cart result.

Write:

- `docs/architecture/evidence/TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1/TASK.md` verbatim;
- `RESULT.md` and `RECEIPTS.md`;
- `docs/ai/tasks/TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1/TASK.md`;
- all three recovery TOPs.

Lead the result with:

- compensation status for every 244 mutation;
- authoritative per-line ledger;
- whether the task completed lawfully or stopped on the compact merchant-decision table;
- exact canonical URL/stage and whether the actual cart remains unsubmitted;
- SHAs and local==origin/main parity.

Return exactly one envelope:

BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1
Parent-Task: TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — restore merchant-data integrity and finish the actual cart lawfully
Mode: REVERT_GUESSED_MERCHANT_DATA_THEN_DELIVER_WITH_AUTHORITATIVE_VALUES_ONLY
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <resolved issue commit>
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT

Append the task-end marker separately. Never send placeholder/probe content.
END_TOOBA_TASK
