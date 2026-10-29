PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3
Parent-Task: TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — preserve selected delivery date and hour while completing recipient fields
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REPRODUCE_AND_FIX_NON_ROUTING_ADDRESS_EDITS_CLEARING_DELIVERY_CHOICES
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

# User-reported defect

On the canonical shipping page, the user can choose a delivery day and hour before completing recipient fields such
as first name, last name and postal code. After those choices are visibly selected, completing or failing validation
of the other fields causes the selected day/hour to disappear or reset.

This is a product defect. Recipient/contact completion must not erase a valid delivery choice. Only a real change to
the authoritative shipping context may invalidate it.

# Required behavior

## Choices that must be preserved

For every captured group, preserve its selected method, date, slot key and exact start/end ticks while the user:

- types or edits first name;
- types or edits last name / recipient display name;
- types or edits contact mobile;
- types or edits postal address text;
- types or edits postal code;
- types optional order notes;
- clicks Continue while one of those fields is incomplete and receives the owning validation message;
- moves focus between fields or causes ordinary React rerenders.

These edits must not trigger a shipping projection, replace `capturedPreview`, change the captured context key, clear
`capturedChoices`, repaint an unselected date as selected, or silently choose another slot.

## Changes that may invalidate

A choice may be invalidated only when its authoritative context changes:

- country / administrative area / city / supported service-area identity;
- saved-address identity when it changes the trusted destination;
- coupon intent;
- cart version or exact group membership;
- selected publication/quote/calendar confirmation identity, amount, allowed date or allowed slot;
- method change within that group.

A genuine routing-context change must still hide stale price/date, refresh exactly once, and require fresh explicit
confirmation. Do not weaken stale-price, coupon, cart-version or server-echo guards.

# Reproduction first

Use the canonical runtime only:

- `D:\Users\User\source\repos\SarvNewVer`;
- FE 3000 → Host 5088;
- SingleStore/store-alpha/tooba_alpha;
- the actual authenticated user/cart from 244-R2 when still intact.

Without screenshots:

1. start with incomplete recipient/contact/postal fields but a current priced captured preview;
2. explicitly choose one date and one hourly slot for every group;
3. record selected radio/aria/data state and request count;
4. type each non-routing field separately and attempt Continue while another field is still incomplete;
5. identify the exact transition that clears or visually loses the choice, including whether state was cleared,
   preview identity changed, projection reran, component remounted, or only styling/scroll gave a false state.

Do not patch from assumption. Record the measured root cause in RESULT.

# Implementation constraints

Read and follow:

- `.cursor/skills/tooba-frontend-feature/SKILL.md`;
- `docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md`;
- `.cursor/rules/immutable-shopeiva-reference.mdc`.

Use the existing shipping page, destination-refresh coordinator, captured-selection intent and design system. Do not
add a second coordinator, cart client, quote path, endpoint or global state library.

Expected design direction, subject to measured cause:

- keep **routing/pricing identity** separate from **recipient/form completeness**;
- derive captured invalidation solely from the stable confirmed routing/coupon/cart/server-response identity;
- retain per-group choices by group+method+date+slot while that identity remains equal;
- let `resolveShippingContinueState` report incomplete address fields without mutating delivery choices;
- if a refreshed preview is semantically identical, do not clear choices merely because object identity changed;
- if a date/slot is no longer present in the server calendar, clear only the affected group's invalid choice.

Do not persist incomplete address data to the server just to retain UI state. Do not auto-select a date/hour.

# Focused proof

Add or extend pure/component tests for at least:

1. date+slot chosen first; first/last name edits preserve both;
2. mobile, postal address and postal-code edits preserve both;
3. Continue blocked by each missing field leaves both selected;
4. notes edit preserves both;
5. unrelated rerender and semantically identical preview preserve both;
6. province/city change invalidates and requests a fresh preview exactly once;
7. coupon/cart/group/quote/calendar change still invalidates;
8. one group's invalid slot clears only that group;
9. same-tick Continue still produces at most one selection PUT;
10. after all recipient fields are completed, Continue becomes enabled without reselecting date/hour.

Run frontend typecheck/build and the focused shipping tests once. Do not loop broad suites or rebaseline unrelated
guards.

# Real canonical acceptance

On the actual canonical account/cart:

- begin with incomplete name/postal/contact data;
- choose explicit delivery date and hour for each visible group;
- complete the missing fields;
- prove choices remain selected and no non-routing edit caused a projection request;
- Continue becomes enabled without selecting date/hour again;
- perform at most one lawful selection PUT if needed and load the existing payment-ready form;
- leave the cart unsubmitted with zero new checkout/Order/reservation/payment;
- preserve the 244-R2 approved merchant configuration.

No screenshots/images, no new account/cart, no alternate DB, no D:\temp runtime, no new port, no PSP or Visual
ACCEPT. Preserve user LOCAL changes and unrelated tabs. Checked-in gates stay OFF; TB185 stays paused/default-OFF.

# Evidence and recovery

Write:

- `docs/architecture/evidence/TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3/TASK.md` verbatim;
- `RESULT.md`, `RECEIPTS.md`;
- `docs/ai/tasks/TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3/TASK.md`;
- all three recovery TOPs.

Lead the result with the measured clearing trigger, final state/invalidation model, focused proof, canonical actual
cart acceptance, exact URL/stage and source/evidence SHAs. Worker PASS is not Architect acceptance.

Return exactly:

BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3
Parent-Task: TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — preserve selected delivery date and hour while completing recipient fields
Mode: REPRODUCE_AND_FIX_NON_ROUTING_ADDRESS_EDITS_CLEARING_DELIVERY_CHOICES
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
