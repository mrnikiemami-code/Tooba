# TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241 — WORKER RESULT

Status: **PARTIAL (demoted by 241-R1)** — configurable receiving slot duration + primary consolidation remain
landed; Architect review + R1 proved the disclosed public `shipping/commit` 500 and TimeOnly slot wraparound /
unfit-duration preflight gaps. Successful 60/120 paths and Order tick persistence from this task stay valid.
Do **not** treat this RESULT as unqualified PASS. See TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1.
Stop-State: `STOP_FOR_ARCHITECT`. `automaticNextImplementationTask: NONE`.
No PSP/paid claim, no Visual ACCEPT, no screenshots.

Issue commit (Expected/Base): `5552a97f84ac28f948d690169be1e458420c0dd4`.
Code-SHA: `0bcfa52aa4ede4838b45dceb60ec2931219a6f7f`.
R7 appsource retained inside that line: `acb7f16e631927ea6bd4b99d34f84720f23fb9ba`.

## 0 — User outcome delivered

User asked for hourly choices («ساعت داشته باشد ۹تا۱۰ ۱۰تا۱۱ … با تنظیمات»), restoration of missing delivery-time
selection, ONE main surface (no two app versions), and updated recovery.

Measured on PRIMARY:

| Fact | Value |
| --- | --- |
| Primary URL | `http://127.0.0.1:3000/fa/shipping` (FE PID 18832 `next start -p 3000` from main SHA `0bcfa52aa`) |
| Host | `127.0.0.1:5088` PID 45168 `D:\temp\tb241-primary\Tooba.Host.exe`, DeploymentId `marketplace-tb241-primary` |
| Edition / DB | Marketplace, Tenant NULL, `tooba_marketplace_tb239` (original `store-alpha` NOT merged/relabelled) |
| Visible window | installed Chrome profile `D:\temp\tb241-chrome`, CDP **9350**, PID 47688, title «اطلاعات ارسال \| توبا» |
| Buyer | `GET /api/auth/me` 200, `userId 01a18247-54d3-7000-9f2f-219ac7de9e6d`, edition Marketplace |
| Review cart | `01a1846b-aede-7000-850f-de073a511739` v8 — TWO sellers «فروشگاه آرمان» + «دیجی‌استایل نمونه», **unsubmitted** |
| Slot UI | after date pick: Arman slots `09–11/11–13/13–15/15–17` (authored duration **120**); DigiStyle slots `09–10…16–17` incl. **14:00–15:00** (duration **60**) |
| Continue | disabled with owning reason «برای «فروشگاه آرمان» یکی از ساعت‌های تحویل همان روز را انتخاب کنید.» |

## 1 — Scheduling contract (admin → capture → calendar)

- Additive Shipping column `shipping.authoring_weekly_schedules.slot_duration_minutes` (NULL = legacy full window;
  CHECK 1..1440) via migration `20261028201416_ReceivingSlotDuration` — **applied** on task Marketplace DB.
- Capture format **2** when duration is set; format **1**/unset remains readable as ONE full open..close slot.
- Order evidence CHECK updated to `accepted_format_version = ANY (ARRAY[1, 2])` via
  `20261028200100_AcceptSlotDurationFormat` — **applied**.
- Server calendar emits unique dates with nested `slots[{slotKey, opensAt, closesAt, startTicks, endTicks}]`;
  frontend never invents hours.

## 2 — End-to-end same chosen subinterval

Prior proof cart selection (`tb241-sel.json`) stored per-group windows `14:00-15:00` / `10:00-11:00` / `16:00-17:00`.
StartPayment (`tb241-start.json`) created checkout `01a18468-1af1-7000-82ca-5e59db964f49`, payment Pending manual,
`reservationCount=3`. DB (`order.order_shipping_groups`) for that checkout:

| method | date | window | open ticks | close ticks | format |
| --- | --- | --- | --- | --- | --- |
| Arman store delivery 240 | 2026-11-01 | 14:00-15:00 | 504000000000 | 540000000000 | 2 |
| DigiStyle delivery 240 | 2026-11-01 | 16:00-17:00 | 576000000000 | 612000000000 | 2 |
| Platform delivery 240 | 2026-11-01 | 10:00-11:00 | 360000000000 | 396000000000 | 2 |

Arman draft later moved to duration **120**; the Order above still holds the original **60**-minute 14–15 ticks —
publication change did not rewrite history. R7 continue-state extended with a slot owning blocker (same authority).

Disclosed: one earlier `POST /v1/storefront/shipping/commit` returned 500 `ContractOperationException`
(`tb241-commit.json`); the authorized StartPayment path on the same runtime produced the Order receipt above.
No claim that every alternate commit edge was re-swept.

## 3 — Primary cutover + consolidation

1. Built Host binary to `D:\temp\tb241-primary` from main; started on 5088 with Marketplace connection to
   `tooba_marketplace_tb239` and local captured-preview gates ON for the named primary process only.
2. FE 3000 rebuilt/started from the same main SHA (`next start -p 3000`).
3. After primary health + slot UI + Order facts: stopped verified task listeners only —
   FE 3100/3101/3102/3103/3104 and Host 5188/5189/5190 (PIDs recorded in `D:\temp\tb241-stopped-ports.json`).
4. Remaining listeners for the user URL: **3000 + 5088** (+ task handoff CDP 9350). Profiles/DBs/worktrees retained.
5. Original SingleStore/`store-alpha` not rewritten; active edition reported honestly as Marketplace.

## 4 — Checks / preservation / not delivered

Focused unit coverage landed with the code commit (authoring slot duration, continue-state slot blocker,
receiving calendar slots, captured-selection). No full solution suite. Checked-in gates stay OFF; primary local env
enables the three captured flags only. TB185 paused/default-OFF/PendingAllocation. No screenshots. No secret in git.
Retained: external provider concurrency, 238 unknown 22001, 227 mobile empty-note, 225-R1 LOCAL loss.
NOT delivered: PSP/paid/exactly-once, Visual ACCEPT, SingleStore live re-trial on 3000 (supported path retained, not
promoted), no automatic 242.

## 5 — Elapsed

Claimed `2026-10-28T21:00:33Z`, result ≈`2026-10-28T21:48Z` ⇒ **≈48 min** inside the 60-minute operational box
(checkpoint slips recorded: migration Order first-apply failed once then succeeded; one commit-edge 500 disclosed).
Original 240-minute deadline remains MISSED; no reset.
