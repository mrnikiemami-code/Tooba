PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2
Parent-Task: TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — apply user-approved demo merchant settings and finish the real cart
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: APPLY_EXPLICIT_USER_MERCHANT_DECISION_AND_COMPLETE_ACTUAL_CART
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# User authorization

The user explicitly authorized the assistant to choose the necessary settings so the current demo data works. The
Architect selected and the user approved this exact merchant configuration for the current products:

- all four current cart offers: fulfillment responsibility **Platform**; in SingleStore this must resolve to
  Order owner **Store(store-alpha)**, never Marketplace or Seller;
- shipping type: existing original type `01a16f43-6b4a-7000-8670-4c16e14c6345`;
- method/publication: original `post/null` “پیشتاز” draft `01a16eeb-70b3-7000-87c2-564c82835496`;
- packaged per-unit shipping weight for Huawei, Samsung and Xiaomi demo products: **500 grams** each;
- freight per resulting shipping group for every supported group weight: **120000 IRR**;
- packaging per resulting shipping group: **5000 IRR**;
- delivery metadata: preserve the original 1–3 days, geography, timezone, authored weekly windows and
  `SlotDurationMinutes=60`.

These values are now explicit merchant decisions, not worker inference. Do not substitute other values.

# Required outcome

On the canonical user checkout/runtime and the same actual user/cart, apply the approved settings through owning
APIs, publish them, atomically replace the rejected stale selection, and leave the actual cart on the existing
payment-ready form without submitting an Order/payment.

Success is only:

- canonical source `D:\Users\User\source\repos\SarvNewVer`;
- FE 3000 → Host 5088;
- SingleStore / store-alpha / tooba_alpha;
- the actual cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` if it is still the current intact quantity-18 cart;
- no replacement buyer/cart, alternate DB, D:\temp runtime, screenshots or final payment submission.

# A. Resolve documentary identity before writes

244 RESULT and 244-R1 RECEIPTS recorded different user IDs. Read the real authenticated primary session and the
cart's actual owner through normal application boundaries. Record the one correct sanitized userId and correct the
earlier evidence append-only. Do not merge identities, inject cookies/tokens, reset the account or create a task
buyer. If the current cart is no longer owned by the authenticated user, stop with an exact identity conflict.

# B. Apply the approved fulfillment and weight settings

Use normal owning Admin/Offer/Catalog APIs:

1. Assign each of the four actual cart offers to Platform responsibility plus the approved original shipping type.
   Verify each payload OfferId/SellerPartyId and read back the exact assignment.
2. Keep `demo_attr_mobile_weight` as an active Number definition in **g**. Bind it only to the three actual demo
   product categories needed by this cart, using the normal Catalog API.
3. Set each actual Huawei, Samsung and Xiaomi demo product's packaged shipping weight to exact numeric **500 g**.
   Do not reinterpret sibling values or modify unrelated products/definitions.
4. Verify quantity multiplication: each group total must be the sum of 500g × its own line quantities, with the
   exact line membership recorded.

# C. Apply the approved fixed rate without arbitrary gaps

Configure the original manual draft through the normal Shipping authoring API so every group weight supported by
the product's numeric domain is covered at freight 120000 + packaging 5000 and 1–3 delivery days.

- Prefer the model's lawful unbounded upper bound when supported.
- If the API requires explicit bounded half-open ranges, use the documented maximum accepted by the domain and
  state it exactly; ranges must be non-overlapping, contiguous from zero and cover the measured actual group totals.
- Do not use an unexplained magic maximum. Do not alter geography, currency, payer/funder, timezone, weekday
  windows or 60-minute slot duration.
- Publish once and read back selected snapshot/source revision/selection version/rate rows.

This broad coverage is authorized specifically for the current demo merchant configuration.

# D. Replace stale rejected selection and complete the actual flow

1. Fresh projection must use the approved assignments, 500g weights, selected publication and actual destination.
2. Every actual cart line must belong to exactly one server group; no line may be deleted, replaced, omitted or
   silently downgraded.
3. Each group must show the real owner/store, method, freight 120000, packaging 5000, dates and server-authored
   60-minute slots.
4. Explicitly choose a lawful date and hour for each group. No hidden auto-choice.
5. Perform exactly one successful selection PUT with all server echoes, goods amount, revision TEXT and exact slot
   ticks. A duplicate/stale control may fail typed but must not create another accepted graph.
6. The selection must atomically replace the stale groups derived from rejected snapshot `01a185c1-458a…`.
   Database readback must show only the current lawful graph as the active draft selection.
7. Load the existing payment draft/form from that same accepted root. Amounts, dates and hours must match.
8. Leave cart Active and **unsubmitted**. Required counts for this actual cart: zero checkout, zero Order,
   zero reservation and zero payment created by this task.

# E. UI handoff

Use the existing primary signed-in session where safely available. Preserve typed address and unrelated tabs.

- Final usable URL must be the canonical `http://127.0.0.1:3000/fa/payment?cartId=...` or the canonical shipping
  page ready to continue with all selections intact.
- State precisely which stage is left for the user.
- No screenshots, images or viewport campaign.
- Do not expose credentials, OTP, cookies or bearer tokens.
- Do not call Visual ACCEPT, PSP, paid or production activation.

# F. Verification and preservation

- Read back offer assignments, product weight values/units/category bindings, publication/rates and Order draft
  evidence from tooba_alpha.
- Run only focused checks if source changes are actually necessary; no source change is expected.
- Preserve canonical user LOCAL changes and repository parity. Checked-in gates remain OFF; task-local primary gate
  may remain ON.
- TB185 remains paused/default-OFF/PendingAllocation.
- Do not touch Marketplace DB, reset/reseed/drop data, use business SQL, start new ports or serve temp source.
- Retain unrelated known history (227, 238, unrecovered 225-R1) without widening this task.
- No automatic 245 or further implementation envelope.

# G. Evidence and result contract

Write:

- `docs/architecture/evidence/TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2/TASK.md` verbatim;
- `RESULT.md`, `RECEIPTS.md`;
- `docs/ai/tasks/TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2/TASK.md`;
- all three recovery TOPs;
- append-only corrections to the 244/244-R1 identity and acceptance statements.

Lead the result with canonical runtime lineage, corrected actual user/cart identity, exact approved settings,
per-group weight calculation/rate, active selection replacement, final URL/stage and zero submission counts.

Return exactly:

BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2
Parent-Task: TB-SHIPPING-USER-ENVIRONMENT-DATA-INTEGRITY-AND-DELIVERY-244-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — apply user-approved demo merchant settings and finish the real cart
Mode: APPLY_EXPLICIT_USER_MERCHANT_DECISION_AND_COMPLETE_ACTUAL_CART
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
