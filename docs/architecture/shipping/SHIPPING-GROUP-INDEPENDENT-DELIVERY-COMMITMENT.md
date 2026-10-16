# LOCKED — Independent seller shipping commitments, per-group delivery promises and scheduling

Status: **USER APPROVED / NORMATIVE / MUST IMPLEMENT** — 2026-10-16
Scope: Shipping storefront, Order draft and accepted order, Fulfillment, Pricing/Tax/Payment/Settlement, service-specific scheduling and Admin integration.
Authority: supersedes conflicting earlier designs that use one delivery date/time for a whole cart or merge seller shipments by method name alone.
Current implementation is **NOT certified compliant**; this is an architectural requirement, not a statement of completed work.

## 1. Non-negotiable business rule

**Every actual shipment is an independent delivery commitment of its seller.** Its selected shipping service, applicable origin, eligible goods and quantities, promised readiness/transport/delivery window, selected delivery date, selected time slot, rate and lifecycle belong to that shipment/group. The commitments of unrelated sellers are NEVER implicitly shared.

**Equal carrier, provider or shipping method code MUST NOT merge distinct sellers' shipment groups.** Even if two sellers select `tipax:standard`, each seller has a separate fulfillment/obligation identity, shipping price, availability, scheduling, and status.

### Binding example
- Seller A, Tipax Standard, earliest valid delivery **2 days from the reference/quote date** -> **ShippingGroup A**, independently selectable date/time.
- Seller B, Tipax Standard, earliest valid delivery **3 days from the same reference/quote date** -> **ShippingGroup B**, independently selectable date/time.
- Storefront must render **two distinguishable delivery cards** (both may display `تیپاکس معمولی`, but show Seller A vs Seller B, their own items, independent 2-day/3-day earliest promise, independently selected date/window, fees and status).
- Do NOT group those into one Tipax card merely because method code or name matches. Do NOT force one common date by intersecting horizons across all selected methods/groups.
- If both groups happen to have the same earliest date or selected time, they STILL remain separate seller obligations. Shared values do not imply merged identity or physical shipment.

Another case: a SINGLE seller with two different shipping methods must also form two groups. A single seller with the same method but incompatible fulfillment origins, preparation commitments, carrier service constraints or other operational promises must form separate groups. An optional visual common heading by carrier is permissible ONLY if each real group remains a clearly separate editable card with its own promise and actions; never hide independent groups inside one undifferentiated fee/date choice.

## 2. Group identity and partition invariants

- Canonical stable `ShippingGroupId` identifies ONE independently executable obligation, carried unchanged from draft through accepted Order, Payment allocations, Fulfillment and Settlement. SellerPartyId is mandatory and trusted. Tenant isolation and SellerOrder ownership must be validated.
- Grouping/compatibility key is NOT just `MethodCode`, and NOT merely `SellerPartyId + MethodCode`. It includes seller + canonical service/method/option + trusted fulfillment origin when available + fulfillment/availability constraints + **compatible delivery promise and schedule** + destination context; a domain owner decides if lines can share one obligation. Never trust a browser-supplied origin/promise, and never invent a missing origin.
- Whole cart lines are assigned once to their group in v1; preserve exact line identities, quantities and no duplicate/omitted/wrong-seller membership. Any later partial line split must maintain quantity reconciliation and explicit policy.
- A service/provider such as Tipax is not a shipment group identity. Each independently committed seller shipment is charged, fulfilled, rescheduled, cancelled and refunded by its real `ShippingGroupId`.

## 3. Shipping page UI — independent schedule INSIDE EACH group

The shipping page must render one **shipping commitment card per group**, not one per seller with one global date picker, and not one per service code with one global picker. Each card shows:
1. Selected canonical method and localized service label.
2. Seller name, all member products, quantities and fulfillment context where known.
3. Its own authoritative fee and current readiness/earliest-delivery promise.
4. Its own **date picker** containing ONLY backend-authorized dates for this specific group.
5. Its own **time-slot picker** containing ONLY backend-authorized windows for this specific group.
6. State and revalidation errors scoped to that group.
The Continue button is enabled only when destination and **EVERY active group** has an eligible chosen method, date and slot; an unavailable group cannot be silently overridden with another group's date.
Changing a method, line membership, seller promise or destination revalidates **only the affected group and any actually dependent groups**. Do not overwrite unrelated groups' selections. Return/back/reload must restore each group's own date/window and show accurate stale-state messages only where necessary.
UI may use an optional service-family heading for readability, but under it there must still be two independent cards when sellers or promises differ. All Admin/Storefront existing styling, tokens, layout, RTL and responsiveness must be preserved; change composition rather than invent a new design system.

## 4. Authority: scheduling, earliest delivery and semantics

- Server-owned shipping/fulfillment eligibility evaluates **each group** using seller-specific readiness, service-specific transit lead time, trusted origin, destination, working days/cut-offs/capacity and any current eligibility/promise constraints.
- Separate **seller preparation** from **carrier transit** and from **customer-facing earliest delivery**. A statement of `2 days` or `3 days` must be typed: preparation days, transit days, or total earliest-delivery offset. Do not relabel one as another or calculate dates in the browser.
- Return per-group eligible delivery dates/windows, earliest allowed date, quote/policy version and timezone/context from the authoritative owner; customer selects one allowed date/window **per ShippingGroupId**.
- Preserve schedule snapshots when the order is accepted. Quote expiry/changed rules may fail closed and demand re-selection for the affected group; never silently replace accepted commitments. Never derive a universal date by intersecting dates across all groups: this is the old behavior that MUST BE REPLACED.
- Historical single-group checkouts may map to one group for compatibility, but a multi-group checkout cannot be represented by a single authoritative global date/time.

## 5. Required read/write contracts and persistence

Order-owned draft and accepted-order contracts must support `Groups[]` where EACH entry is keyed by stable ShippingGroupId and carries member line IDs, seller/method identity, quote, earliest promise, **SelectedDeliveryDate** and **SelectedDeliveryTimeWindow** independently. A single global draft date/window may remain as an explicit legacy compatibility projection for one-group checkouts only, never as source-of-truth for multi-group scheduling.

Backend selection must validate group membership, seller ownership, method eligibility and **per-group schedule** atomically; reject missing, expired, ineligible or cross-group-mismatched slots. One globally selected date/window with a Groups[] array is NOT an acceptable multi-group write contract.

On order commit, freeze each group's seller commitment, selected date/slot and rate under that stable ShippingGroupId. Fulfillment must execute shipment(s) scoped to that group, and shipment status changes, cancellations and refunds must not mutate another seller's obligations. Payment/Settlement group allocations remain per-group and must never double-count freight; commercial shares remain PendingAllocation until approved business policy exists.

## 6. Legacy implementation expressly superseded

Earlier implementation and tests around TB-133/TB-134/TB-136 intentionally computed a shared intersection of all selected methods' date horizons (a single `deliveryDate`/`deliveryTime` at cart level) and rendered one date/time picker at the bottom of the page. That implementation was adequate for the earlier test but **is NONCOMPLIANT with this new confirmed business requirement**. It must be replaced through an owner-correct contract/data/UI evolution; do not report a PASS by merely rearranging existing CSS or presenting separate labels while retaining a global date picker.

The TB-126/127 initial shipping-group identity/relationships and TB-129 Fulfillment shipping groups are useful baseline, but do not prove per-group scheduling or appropriate compatibility grouping. In particular any existing hard invariant of one group per `(seller, method)` must be revisited to allow distinct promises/origins under the same seller+method where operationally required; changes must be explicit and migration-compatible.

## 7. Mandatory acceptance examples (real, not only mocked UI)

**A**: Seller A Tipax Standard 2-day and Seller B Tipax Standard 3-day -> two distinct group ids, separate items and rate, two independent schedule widgets with different server-derived earliest dates. Selecting A's date/time leaves B unchanged.
**B**: Same method and same delivery date from two sellers -> still two groups, never one operational commitment.
**C**: Same seller, two methods -> two groups with independent dates and slots.
**D**: Same seller, same method but different trusted origin/promise -> distinct groups where not operationally compatible.
**E**: Change one method/availability -> only dependent group invalidates; unaffected group retains its date/time.
**F**: Continue, Payment, refresh/back -> every selected group schedule persisted and restored exactly; no premature order/payment on page navigation.
**G**: After final submit, Order/Payment/Fulfillment/Settlement identities, items, dates, time windows and fees reconcile; one group failing or cancelling does not overwrite others.
**H**: Legacy one-group customer flow remains compatible.
Tests must use authoritative server-specific horizons and validate DB persistence, not invent 2/3-day fake quotes in frontend. A production implementation may need test-only sanctioned Development fixtures for differing seller promise times.

## 8. Delivery discipline and design approval

First perform a targeted owner/contract gap review and prepare a narrow visual wireframe of the **per-shipment** cards; get user approval before disruptive UI rearrangement. Then implement backend group scheduling and frontend, in bounded result-oriented tasks (45 min hard limit), only necessary tests, no redundant exploration. Phase status remains OPEN until the actual user-visible flow and persisted group schedule are proven.

No unapproved changes to the Admin single-method architecture, carrier commercial policy, global stylesheet, theme, RTL, or unrelated pages. The separate locked Admin authority is `docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md`.
