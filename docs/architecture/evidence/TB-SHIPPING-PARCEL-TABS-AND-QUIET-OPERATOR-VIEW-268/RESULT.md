# TB-268 — RESULT (worker PASS within the issued presentation-only scope)

Task: `docs/ai/tasks/TB-SHIPPING-PARCEL-TABS-AND-QUIET-OPERATOR-VIEW-268/TASK.md` (persisted verbatim in this folder).
Parent: TB-267. Channel `tooba-main`. One worker, one task, no delegation, no historical agent resumed.

Status **PASS within the issued scope**, decision
`GROUP_SCOPED_PARCEL_TAB_STRIP_WITH_PERSIAN_COUNT_BADGES_QUIET_NONZERO_CATEGORIES_ACTIONABILITY_FIRST_DEFAULT_AND_CROSS_TAB_LINEAGE_REVELATION_ON_THE_EXISTING_OPERATOR_VIEW_WITH_ZERO_FUNCTIONAL_CHANGE`.
This is **NOT** Architect ACCEPT, **NOT** user VISUAL ACCEPT, and makes no shipping / carrier / financial claim.

## The user's complaint, read literally

«بهتر شد حالا اگر بخش مرسوله ها تبدیل به تب بشه مثلا ارسال مجدد(1) تحویل در مبدا(1) و...دقت کن کارمند با دیدین این فرم خیلی شلوغه براش»

267 was accepted as an improvement but the parcel area still shows every parcel at once. The operator should see **one category at a time**, with the counts written on the tabs, so the same form stops looking crowded.

## What landed

| Area | State | What this means |
| --- | --- | --- |
| 1 Tab strip | one real underline tablist, group scoped | Rendered inside the EXISTING shared operational parcel subsection, immediately above the cards. It filters only `detail.operationalDeliveryGroups[…]` parcels of **its own** group. «همه» + every non-zero category + the currently selected tab are the only tabs present, so eight tabs never become a wall. |
| 2 Counts | parcels, Persian digits, one snapshot | Tab counts and the visible cards are computed from the SAME per-group fact array, so a count can never disagree with the panel. «ارسال مجدد» is **lineage** and deliberately overlaps its status tab (the delivered resend is `ارسال مجدد` ۱ **and** `تحویل‌شده` ۱). |
| 3 Default | actionability first, pinned once | `needsAction → resend → inTransit → receivedAtOrigin → delivered → all`, chosen once per mounted group and only when the caller's action projection is authoritative (`capabilitiesReady={opsLoaded}`), so an unfinished projection can never hide a card behind a wrong default. |
| 4 Numbering & lineage | stable, never renumbered | «مرسولهٔ ۱/۲» keeps its ordinal in the WHOLE group; the human relationship chip is now a 44px-hit-area control that selects the counterpart's own tab and focuses the **existing** card by stable `shipmentId` — no cloned card, no duplicated action. |
| 5 Retained recovery | survives every tab switch | «نیازمند اقدام» is the server projection **plus** a retained unresolved reviewed attempt, so a recovery row stays discoverable even after its ordinary action disappeared. The review dialog, its envelope, occurrence key and handlers are untouched; switching tabs neither resets reviewed values nor retries anything. |
| 6 Empty state | honest, not surprising | A selected tab that reaches 0 on an authoritative refresh keeps its selection and says «مرسوله‌ای در این بخش نیست.» — no jump while the operator is reading. |
| 7 Keyboard / ARIA | real tabs, RTL | `role="tablist"/"tab"/"tabpanel"`, `aria-selected`, `aria-controls`, `aria-labelledby`, roving `tabIndex`; ArrowLeft advances in RTL, ArrowRight goes back, Home/End jump to the ends, focus follows the selection. A click is local state only: **zero** business requests. |
| 8 Quiet by default | no strip when it adds nothing | One parcel, or one non-zero category, renders no strip at all. |

## Evidence collected once, after the final edit

| Check | Result |
| --- | --- |
| `npx tsc --noEmit` | 0 diagnostics |
| mounted delivery suite (`npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts`) | **70 / 70** pass (63 retained + **7** TB-268) |
| `npm run build` | 0 errors, `BUILD_ID vJWB2FG1oJ2-vHasmjtve` |
| served `_buildManifest.js` + chunks `7825-41be5c01279d084e.js` / `967-9c7248c75c25d481.js` | 200 / 200 / 200; the served bytes carry «نیازمند اقدام» and «مرسوله‌ای در این بخش نیست» |
| live Order B, 1440 | document 1425/1425 (no overflow), strip 1022/1022 (fits), tabs 44px, card 1037 inside main 1169 |
| live Order B, 1024 | document 1009/1009, strip 606/606, tabs 44px |
| live Order B, 390 (fresh load) | document 390/390, strip 502 > 306 (scoped scroll, selected tab fully visible at 42..169 inside 42..348), tabs 44px, card 306 |
| live counts on Order B | همه (۲) · دریافت‌شده در مبدأ (۱) · ارسال مجدد (۱) · تحویل‌شده (۱); نیازمند اقدام / در مسیر / در حال برگشت / لغوشده are 0 and hidden |
| live default tab | «ارسال مجدد» — only the resend child card is rendered |
| live keyboard | ArrowLeft from «ارسال مجدد» → «تحویل‌شده», selection + focus move together, only the new tab holds `tabIndex 0` |
| live cross-tab lineage | chip «ارسال مجدد از مرسولهٔ ۱» → tab «دریافت‌شده در مبدأ», the source card `admin-order-operational-parcel-01a1dc15-…` is the focused element |
| architecture guards | identical to HEAD (stashed the three files and re-ran): 8/11 pass, the same 3 pre-existing baseline failures |

## What was deliberately NOT done

- No backend, endpoint, contract, capability, validator, handler, quantity, capacity or payload change.
- No schema, migration, DDL, business SQL or data write. No new order, draft, fixture or database.
- No create / dispatch / deliver / return / refund / cancel / save / publish anywhere; Order B was read only (GET).
- No screenshot artifact committed, no fullscreen dialog, no new page/search/toolbar, no layout redesign of 267.
- No new tab taxonomy invented: statuses that have no tab stay reachable through «همه» and «نیازمند اقدام».

## Runtime

ONE canonical `tooba_alpha` @ 127.0.0.1:5432 (SingleStore, `store-alpha`), FE 3000 → Host 5088.
FE rebuilt and restarted on `vJWB2FG1oJ2-vHasmjtve` (PID 32300, `/fa` 200). Host untouched (`/health` 200, `/health/ready` 200).
Keys / CapturedPreview unchanged. 255 parked, live Tipax deferred, parent-package business gaps unchanged.

## Known limits

- The strip lives inside the already-oversized `admin-order-operational-delivery-section.tsx` (1788 lines). That file was
  **already** `NEW_OVERSIZED_FILE` in the FE-SIZE baseline before this task; splitting it is real architecture work and is not
  part of a presentation-only task, so it is reported, not done.
- `FE-FOLDER-001`, `FE-FOLDER-002`, `FE-SIZE` fail the same way at HEAD and after this change (verified with a stash); they
  are long-standing task-wide baselines, not this change. `tailwind.config.ts` still does not scan `features/**`, so every
  class used here is under `app/**` and was confirmed in the served DOM.
- The tab strip is per operational group: an order with several groups repeats it per group by design.
- No user trial was run by the worker. STOP_FOR_ARCHITECT, `automaticNextImplementationTask: NONE`.
