PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244
Parent-Task: TB-SHIPPING-ORIGINAL-STORE-DELIVERY-CLOSURE-243-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — final delivery on the user's canonical project, database, account and cart
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: DELIVER_FROM_CANONICAL_USER_CHECKOUT_ON_REAL_ACCOUNT_CART_AND_DATABASE
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# Outcome

Deliver the shipping flow on the user's own canonical installation. Success requires all of these at the same time:

- source and running frontend come from `D:\Users\User\source\repos\SarvNewVer\src\frontend`;
- source and running Host come from `D:\Users\User\source\repos\SarvNewVer\src\backend\Host\Tooba.Host`;
- the usable surface is the primary `http://127.0.0.1:3000` backed by `127.0.0.1:5088`;
- CommerceContext is SingleStore, tenant `store-alpha`, database `tooba_alpha`;
- the acceptance target is the user's existing signed-in account and the actual cart currently shown on the primary shipping page, not a task-created buyer, substitute cart, copied profile or prepared fixture;
- every real cart line has an honest shipping outcome, and all serviceable groups can select their own configured method, date and delivery hour and continue to the existing payment form;
- leave the user's actual cart unsubmitted at the payment-ready stage. Do not create an Order, reservation or payment for that cart merely to prove the task.

A task runtime from `D:\temp`, another database, another account/cart, a relabelled Marketplace environment, or a controlled test harness is not delivery.

# 1. Canonical checkout and safe cutover

1. Inspect the canonical checkout's branch, HEAD, status, unstaged/staged/untracked files and active watcher command lines before changing disk state.
2. Preserve every user LOCAL/unsaved change. Do not reset, stash, force, mass-stage, overwrite dirty files or discard browser state. Stop only the verified primary watcher PIDs before a safe fast-forward/update so HMR cannot overwrite the user's work.
3. Bring the canonical checkout to this issued source through a normal safe update. If a real content conflict with user work cannot be resolved without guessing, report one exact `RECOVERY_CONFLICT` with paths and preserve both sides. Do not serve a temporary worktree as the final workaround.
4. Build and run FE/Host from the canonical checkout. Normal project-local `.next` and `bin/Release` outputs are lawful; `D:\temp\tb*` source/build directories are not the final runtime.
5. Verify from process cwd/command line, proxy traffic and readiness that 3000/5088 use the canonical source and `tooba_alpha/store-alpha`. Do not start new user ports, kill by process name, restart Docker globally, reset/reseed/drop/copy a database or point the primary at Marketplace.
6. Persist normal private local launch configuration so the user can restart the same project/database using documented commands. Do not commit credentials or globally enable checked-in gates.

# 2. Narrow authorization to inspect the actual user trial

The user explicitly authorizes read-only diagnosis of the specifically identified current/foreground `3000/fa/shipping` session and its own auth/cart/network state so the actual account and cart can be identified. This supersedes older task freezes only for that narrow shipping session.

- Use existing session/application sources first. Do not enumerate or automate unrelated tabs.
- Do not reload/close the user's tab, clear storage, extract/inject cookies or JWTs, bypass identity, or lose typed/unsaved input.
- Do not create a replacement buyer/cart for handoff.
- The screenshot showed an approximately 18-item actual cart, several “fulfillment responsibility not assigned” groups, no usable date/hour choice and disabled Continue. Establish the real IDs/count/version from the live app; do not assume historical IDs.
- If safe access truly needs one missing locator, request only that exact locator early and continue all independent canonical-runtime work.

# 3. Actual-cart diagnostic ledger, then repair

Within 10 focused minutes after the canonical runtime is available, record a sanitized per-line ledger for the actual cart:

`cartLineId → offerId → seller → trusted responsibility → shippingTypeId → active catalog binding → full owner → selected capture/pointer → destination → authoritative weight/conditions → matched rate → dates/slots → exact unavailable reason`.

No password, token, address, mobile or personal data belongs in evidence.

Classify every failure as one of:

- product/source defect;
- missing but determinable merchant configuration;
- missing business decision that cannot lawfully be guessed;
- genuinely unsupported product/destination.

Repair source defects in the existing vertical flow. Finish missing merchant configuration only through its normal owning Admin/Offer/Fulfillment/Shipping APIs when existing stored semantics make the choice determinate. The already-approved original store service remains `post/null`, original shipping type `01a16f43-6b4a-7000-8670-4c16e14c6345`, weekly authored hours and slot duration 60 unless the user's stored configuration says otherwise.

Never:

- delete/substitute cart lines to make the test green;
- bulk-assign every offer to Platform or one guessed type;
- invent weight, owner, responsibility, funder, price, rate, date or slot;
- erase weight/rate bounds or make every row unconditional;
- infer authoritative weight from product labels;
- downgrade an unavailable group into legacy shipping or silently omit it.

If a real line requires a merchant decision absent from all trusted data, expose the smallest exact decision immediately and continue the remaining fixes. The whole cart is not delivered while required groups remain honestly unavailable.

# 4. Existing architecture only

Use the existing Order/Shipping/Offer/Fulfillment/Catalog/Geography/AddressBook capabilities. Do not add a second cart, quote engine, calendar, selection coordinator, endpoint or client.

Preserve these contracts:

- full owner identity: Store(exact tenant), Marketplace, or Seller(real Offer seller);
- assignment payload must match each line's OfferId and SellerPartyId and carry defined responsibility plus non-empty active type;
- group identity includes full owner/binding and exact line membership, not label or method code alone;
- one authoritative assignment batch, active catalog/type checks, selected 219 publication for the full owner/binding, 216 quote per candidate and the shared 213 calendar;
- date/hour choices originate only from the selected published service schedule. A configured 60-minute 09:00–17:00 window yields 09–10 … 16–17; NULL duration remains the authored full window. Do not invent hourly slots in the frontend;
- selection carries exact server echoes, goods amount, revision TEXT, selected date and selected slot ticks; all groups are required;
- final acceptance remains inside the existing Order transaction/CAS boundary and compact evidence remains per group;
- coupon/owner/history behavior already delivered must not regress;
- legacy behavior and both SingleStore/Marketplace code paths remain supported even though this acceptance runs on SingleStore.

Read and follow `.cursor/skills/tooba-frontend-feature/SKILL.md`, `docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md`, `.cursor/rules/immutable-shopeiva-reference.mdc` and the existing delivery components. Use the current design system. Do not redesign the whole page.

# 5. UI acceptance without screenshots

The user's no-screenshot instruction is binding. Produce no screenshots, image captures or viewport campaign.

Prove the actual page through DOM, accessibility, computed geometry and real HTTP:

- each actual serviceable group visibly names its seller/owner and method;
- choosing a date does not auto-select a hidden hour;
- configured hourly slots appear as readable radio choices, and date/hour choices remain independent per group;
- unavailable groups show the owning, actionable reason instead of a silent disabled button;
- Continue has one exact owning blocker and becomes enabled only when recipient/address, current priced response, every method/date/slot and server echoes are complete;
- FA/RTL and EN labels remain coherent, controls are keyboard accessible, and no global clipping/header workaround is added from a full-page-capture artifact;
- same-tick Continue emits at most one selection PUT.

# 6. Final acceptance — the user's exact account and cart

Acceptance must be one connected run on the actual user account/cart from the canonical 3000/5088/tooba_alpha runtime:

1. `/api/auth/me` identifies the existing user session; current cart ID, version, line IDs and count match the diagnosed actual cart. No line is deleted or replaced.
2. The diagnostic ledger has a truthful final state for every line.
3. Projection returns every required group. Each serviceable group has its own published binding, money, dates and configured slots; any unsupported line remains visibly and specifically blocked and therefore prevents a delivery claim.
4. The user explicitly selects each group's date and delivery hour. No render/effect silently chooses them.
5. Exactly one selection PUT succeeds with the complete cart, exact confirmation echoes, slot ticks and revision TEXT.
6. The existing payment draft/form loads from the same accepted root and displays the same groups, shipping amounts, dates and hours.
7. Database readback from `tooba_alpha` confirms the draft revision and each group's accepted evidence/slot. No checkout/order/reservation/payment row is created for the user's cart.
8. Leave the visible canonical page/session ready for the user's own final action, preserving typed address and selections. State the exact URL and stage; provide no password, OTP, secret or hidden task account.

Do not call the task PASS if this exact actual-cart chain is incomplete. Unit/integration tests, a one-line laptop cart, a task buyer or a different DB cannot substitute for it.

# 7. Verification and bounds

- Run each focused meaningful test set once, canonical frontend typecheck/build, affected backend builds and only the directly affected tests.
- Do not loop broad suites or rebaseline unrelated guards. Record pre-existing unrelated reds honestly.
- No screenshots, PSP/paid claim, production activation, real-funds action or Visual ACCEPT.
- Preserve TB185 paused/default-OFF/PendingAllocation and checked-in gates OFF.
- Retain known unrelated history: 227 mobile empty-note, 238 unclassified old 22001 receipt and unrecovered 225-R1 LOCAL edit loss.
- The original 240-minute deadline is already missed. Do not reset it or invent a new ETA. At 10-minute checkpoints report the actual stage and blocker; keep this one cohesive delivery rather than issuing another implementation task.

# 8. Evidence, recovery and result contract

Update:

- `docs/architecture/evidence/TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244/TASK.md` (this task verbatim);
- `docs/architecture/evidence/TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244/RESULT.md`;
- `docs/architecture/evidence/TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244/RECEIPTS.md`;
- `docs/ai/tasks/TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244/TASK.md`;
- `AGENTS.md`;
- `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`;
- `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`.

The result must lead with the canonical runtime lineage, actual account/cart identity (sanitized), per-line/group outcome, exact final URL/stage and whether the actual cart reached payment-ready without submission. Include source/base/evidence SHAs and local==origin/main parity. Worker PASS is not Architect acceptance.

Return exactly one result envelope containing:

BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244
Parent-Task: TB-SHIPPING-ORIGINAL-STORE-DELIVERY-CLOSURE-243-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — final delivery on the user's canonical project, database, account and cart
Mode: DELIVER_FROM_CANONICAL_USER_CHECKOUT_ON_REAL_ACCOUNT_CART_AND_DATABASE
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <resolved issue commit>
Base-SHA: <sha>
Code-SHA: <sha-or-NONE>
Evidence-SHA: <sha-or-NONE>
automaticNextImplementationTask: NONE
<full honest result>
END_TOOBA_WORKER_RESULT

Append the task-end marker separately after the result. Never send placeholder/probe content.

END_TOOBA_TASK
