BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256
Channel: tooba-main
Worker-Id: tooba-worker-01
Status: PASS
Decision: INDEPENDENT_CARRIER_ACCEPTANCE_CALENDAR_MAX_BASED_RECEIVING_FEASIBILITY_AND_DURABLE_PLANNED_DISPATCH_EVIDENCE
Base-SHA: de492437322c887c20d640cddbbf07e957c214d4
Code-SHA: fe5eb2f9b7235dae0d0d98687954128d985f2c05
Evidence-SHA: see RECEIPTS (evidence commit appended after this file)
Issued-Expected-Base: e758169bde872c6706ebde010b64f363e00fb9b6 (descendant `de4924373` is the 256 issue/docs commit; nothing unrelated was reset)
NOT-Accept: Architect review, user visual acceptance and live Tipax remain NOT claimed by this worker result.
automaticNextImplementationTask: NONE
STOP_FOR_ARCHITECT
END_TOOBA_WORKER_RESULT_POSTAMBLE

## 1. What landed

### 1.1 Authoring — two independent timing calendars
- Shipping method authoring now has two tabs: «تحویل به مشتری» (existing customer week, unchanged controls) and «پذیرش حامل» (new carrier acceptance week: seven days active/closed + per-day open/close + trusted time zone). No customer slot duration is imposed on handoff.
- «کپی از ساعات تحویل مشتری» is a ONE-TIME copy into the independently editable carrier form. It only changes the local editor session until the existing Save; there is no implicit link, and reset restores the confirmed carrier calendar (never the customer week).
- Carrier edits are serialized as their own seven-day section (`carrierSchedule`) in the same owner/draft/revision document, through the existing PUT/query/validation/publication path. Copy/discard/reconcile semantics are preserved; carrier rejections land on the carrier section, not on the customer week.

### 1.2 Storage
- Shipping migration `20261109113000_AddCarrierAcceptanceSchedule`: additive `shipping.authoring_carrier_schedules` + `shipping.authoring_carrier_schedule_days` (owner/draft/revision scoped, seven day rows, trusted time zone, `1..3` format whitelist).
- Order migration `20261109120000_AddPlannedDispatchEvidence`: eight nullable `accepted_dispatch_*` columns on `order.cart_shipping_groups` and `order.order_shipping_groups`, capture format whitelist widened to `1..3`, and all-or-nothing `ck_*_accepted_dispatch_plan` constraints (0 or 8 columns; handoff ≤ accepted receiving date; open < close inside a day; policy ≥ 1; transport days 0..365; plan requires accepted core evidence and format 3).

### 1.3 One feasibility authority (no frontend arithmetic)
`Tooba.Shipping.Contracts.Scheduling.ShippingDispatchSchedulingAuthority` is the single pure authority (no IO, explicit `nowUTC`):
- `ResolveFirstHandoff` / `ResolveLatestHandoffForReceivingDate` / `BuildReceivingCalendar`, all sharing the same week mapping and cutoff rule.
- Storefront candidate receipt uses it through `StorefrontCapturedGroupCompositionCore.BuildReceiving`; the accepted evidence uses the same authority through `StorefrontPlannedDispatchResolver` (forward and backward agree by construction).
- `StorefrontShippingCalculator.BuildCustomerReceivingCalendar` is still used only for captured packages with NO carrier calendar (legacy mechanics), and even there the offset is now MAX, never MIN.

### 1.4 Exact counted-day / cutoff semantics recorded
- Customer receiving availability = MAX delivery days (`MaximumDeliveryDays`), never MIN; MAX < MIN or MAX out of range is `storefront.captured_group.maximum_delivery_days_invalid` with no calendar (never clamped to MIN).
- First feasible carrier handoff is evaluated in the configured carrier time zone against the day's half-open window `[open, close)`: before opening → opening of the same day; exactly at close or after → next working acceptance day; a closed day advances to the next working day; readiness is counted once (the order's own trusted `nowUTC`), with no extra day added for an after-close registration.
- Transport days: N = the effective MAX, counted as transport working days AFTER the handoff day, using the configured carrier acceptance working DAYS as the bounded policy for policy version 1 (explicitly recorded, because no separate transport calendar service exists). Zero is explicit and never offers an elapsed same-day slot.
- Customer dates and slots are then filtered by the SEPARATE customer receiving week (hours + slot duration). No invented holiday, no browser-locale guessing, no nonworking customer day accepted as a receiving date.
- No feasible schedule → stable reasons (`…no_working_days`, invalid MAX/MIN, invalid time zone, `storefront.dispatch_plan.no_feasible_handoff`, `storefront.dispatch_plan.unavailable`) and an unavailable calendar; selection refuses with the owning code (`…slot_unavailable` / `…unavailable`).
- Honesty: the chosen customer date/window is the receiving commitment. Daily transport duration is not presented as an exact transit-hour guarantee.

### 1.5 Durable planned dispatch on NEW accepted groups
- `ShippingGroupAcceptedEvidence.PlannedDispatch` (domain) is written with the acceptance and persisted through the existing Order capture seam: handoff date, open ticks, close ticks, carrier time zone, effective MAX transport working days, calculation policy version (`ShippingDispatchSchedulingAuthority.PolicyVersion` = 1), schedule snapshot id and source revision.
- Customer commitment facts are untouched: receiving date/ticks/time zone and money are still their own columns; the plan never overwrites them.
- Read seam: the existing `IOrderShippingGroupHistoryReader` projection now returns `OrderShippingGroupHistoryDispatchPlan?` (nullable) alongside the accepted evidence, so later due-queue work consumes Order-owned durable facts. No Shipping read, no recomputation.
- Actual `DispatchedAt`/`DeliveredAt` remain actual events; nothing in this slice writes them.

### 1.6 Historical compatibility
- Format versions 1 and 2 stay first-class; version 3 is accepted for new carrier-acceptance captures; version 4+ refused.
- Canonical measurement after migration: `order.cart_shipping_groups` 19 rows, `order.order_shipping_groups` 14 rows, **0** with `accepted_dispatch_handoff_date`, **0** at format 3. Historical groups read as explicit "unspecified"; no backfill, no recomputation from current settings, no changed receiving commitment or money.

## 2. Proof (executed)

| Proof | Command | Result |
|---|---|---|
| Scheduling authority A–F | `dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests --filter FullyQualifiedName~ShippingDispatchSchedulingAuthority256Tests` | **8/8 pass** (MAX never MIN; before-open/just-before-close/exactly-close/after-close; Wednesday 12:00 close vs customer 09:00–17:00 and Friday closed rollover; time-zone midnight skipping; explicit zero day; fail-closed calendars; backward latest handoff validated by the forward rule; readiness counted once) |
| Order plan + adjacent accepted-evidence/projection/writer | `dotnet test ...Tooba.Order.Tests --filter "PlannedDispatchEvidence256Tests|ShippingGroupAcceptedEvidence|StorefrontCapturedGroupProjectionComposer|StorefrontCapturedSelectionWriter"` | **56/57 pass**; the single failure is the PRE-EXISTING coupon mock defect (reproduced on a clean worktree at base `de4924373`, unchanged by this task) |
| Relational roundtrip on task-owned disposable PostgreSQL 16 (`127.0.0.1:15433/order_tb231`) | `TOOBA_TB231_ORDER_POSTGRES=… dotnet test ... --filter ShippingGroupAcceptedEvidenceStorageTests` | **11/11 pass** — plan roundtrips losslessly through the real OrderDbContext + production migrations; the DB itself refuses half-written/incoherent plans (time zone NULL, open = close, handoff after receiving date, revision 0, transport 366, policy 0, plan without core evidence); the read seam returns the plan for plan-bearing groups and `null` for legacy groups; customer receiving/money unchanged |
| Frontend model (decode/independence/copy/wire/rejection) | `node --experimental-strip-types --test features/admin-shipping-authoring/authoring-carrier-schedule-256.test.ts` | **10/10 pass** |
| Frontend mounted UI | `npx tsx --tsconfig tsconfig.mount-tests.json --test features/admin-shipping-authoring/authoring-carrier-calendar.mount.test.ts` | **6/6 pass** (unconfigured = configure + one-time copy, copy unavailable until customer week is configured, seven OWN days with bounds, edits via the component contract, carrier edit never changes customer hours, real time zone required) |
| Frontend typecheck | `npm run typecheck` | exit 0 |
| Frontend build | `npm run build` | exit 0, `BUILD_ID O_m0I-sF7M3RMbc_yjtQn` |
| Backend publish | `dotnet publish src/backend/Host/Tooba.Host -c Release` | exit 0 |

## 3. Deployed identity (canonical, unchanged target)

- FE `http://127.0.0.1:3000` — `next start` PID 27492, served build `O_m0I-sF7M3RMbc_yjtQn`; `/fa` 200.
- Host `http://127.0.0.1:5088` — published `artifacts/publish-primary` PID 12220, `ASPNETCORE_ENVIRONMENT=Development`, `Tooba__Edition=SingleStore`, `Tooba__DeploymentId=store-alpha-canonical-tb256`, tenant `store-alpha` / `tenant-alpha` → `tooba_alpha`; `/health` 200; startup log confirms `Edition=SingleStore DeploymentId=store-alpha-canonical-tb256`.
- Same edition, same database, same ports. No connection/edition/port/runtime switch, no second Host.
- Migrations applied by the repository MigrationRunner at Host startup after private backup `D:\temp\tb256-tooba_alpha.dump` (2,786,765 bytes): `shipping → 20261109113000_AddCarrierAcceptanceSchedule`, `order → 20261109120000_AddPlannedDispatchEvidence`; subsequent modules `pendingCount=0`.
- DataProtection key directory preserved (`App_Data/dataprotection-keys` still present across the republish).
- CapturedPreview: local ON with 7 dates; checked-in gates remain OFF.

## 4. Authorized read-only UI handoff

- Review URL (user's own draft, UNTOUCHED, revision 2, carrier calendar unconfigured so it shows configure + one-time copy):
  `http://127.0.0.1:3000/admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit`
- Task-owned draft used ONLY to prove save/readback (clearly not the user's service): `01a1beed-d7df-7000-bfc8-0185ea61afe3`, revision 3, seven carrier days configured (weekdays 09–17 except Wednesday 10–13, Friday closed, Saturday/Sunday accepting). Its carrier week is deliberately different from its customer week, and two API saves prove independence in both directions (carrier changed while customer week stayed 09:00–17:00 Monday–Friday; and the customer week unchanged on the second save).
- Headless render of both drafts with the task's own session/profile: RTL/fa, 0 horizontal overflow at 1440/1024/390, 0 sub-44px targets, 7 carrier day rows on the configured draft, 0 carrier days + copy offered on the untouched user draft, 119 requests all GET (no save/PUT during render).

## 5. Disclosed limits (no hidden claim)

1. `ShippingPersistenceModelTests.Model_and_the_initial_migration_are_in_sync` remains RED — pre-existing model-snapshot drift from main-branch carrier-shipment models; a diff probe showed the 7 pending operations belong to those earlier models, not to this task's tables. No baseline/snapshot repair was attempted beyond adding this task's own tables/migrations.
2. `StorefrontCapturedSelectionWriterTests.Nonblank_coupon…` remains RED — pre-existing, reproduced on a clean worktree at base `de4924373`.
3. The operational order UI does NOT yet show a «موعد ارسال» row. No canonical accepted group has a plan (0 rows) and a real checkout is not authorized here, so such a row could not be reviewed on real data; the durable facts are exposed through the existing Order group read projection for the later due-queue stage (260). No fabricated order was created.
4. No live Tipax/provider call, no real user checkout/payment, no user draft/published service mutated, no business SQL, no reset/reseed, no image/screenshot artifact.
5. Stages 257–260 remain NOT ISSUED; 255 stays parked.

## 6. Remaining list

- Architect review of this result (worker PASS is not acceptance).
- Later stages (tracking/quantity, non-delivery/return/resend, packing choice, due workqueue/operational view) remain unissued.
- Tipax live verification remains deferred by the user.
