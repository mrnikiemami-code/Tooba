# RECEIPTS — TB-286

## Timing

- startedAt: `2026-11-23T03:48:00.1208060+03:30`
- endedAt: `2026-11-23T03:53:55.3494090+03:30`
- elapsed: **5.92 minutes (5m55s)**
- focused harness: `00:22:58.073Z` → `00:23:04.101Z`, 6.028 s
- setup and review not separately instrumented; no predetermined duration claimed.

## Environment

- Code SHA: `507d7933a54fe067c240beb6aeb62b63f4b110c0`
- Host: existing published `D:\temp\tb285-repair-host\Tooba.Host.dll`, SHA256 `143af9c5dc7459a8e5833b9a35f1abaecbeef2689b704492c9f88461cb3cb007`
- Host endpoint: `http://127.0.0.1:5093`
- DB: `tooba_offer286_proof`, PostgreSQL 17.10, container `postgres-db`, port 5432
- Seller A: `01a205c6-16b2-7000-8745-69972e21d16f`
- Seller B: `01a205c6-16b6-7000-8f41-5b89016d834f`
- Admin: `01a205c6-1a04-7000-a453-a019dfddf874`

## Commands

- `node docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286/evidence/focused-286.cjs`
  - exit 0; produced `evidence/focused-286.json` and `evidence/focused-286.log`.
  - V2 seller/admin: 201/201 same OfferId, 409 changed SKU; originalRows 1/secondRowCount 0; 0 price/stock/reservation writes.
  - V3: 201/201 same OfferId, one row, no errors, no owning writes.
  - V4: create 201; PATCH 200; original replay 409; changed replay 201 same OfferId; issue=true.
- `node docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286/VERIFY.mjs`
  - exit 0; `ok=true`; `decision=BLOCKED_ARCHITECT_DECISION`; mutation self-test rejected.
  - cleanup: DB absent, 5093 closed, canonical 5088/3000 200.

## Raw facts

- V2 seller: `172194c0-0f1c-5af7-a511-eeeb29ea7879`; before/after prices 7/7, stock positions 7/7, reservations 1/1.
- V2 admin: `11c9e867-0690-72f4-a564-848e90f0c017`; before/after prices 7/7, stock positions 7/7, reservations 1/1.
- V3: `34d9aeef-b5b2-6622-8337-f3e5ac263edc`; one Offer row; prices 7/7, stock positions 7/7, reservations 1/1.
- V4: `a839eb1c-8909-2a4c-5867-b2824eecf4ef`; initial `TB286-LATE-A/Draft`; edited `TB286-LATE-B/Draft`; original replay 409 `offer.create.intent_conflict`; changed replay 201 same id.

## Hashes

See `RESULT.md`. Producer script SHA matches evidence producer field. Raw `focused-286.log` and `verify-286.log` are committed.

## Safety

No product source, migration, Cursor, AGENTS, roadmap, OpenSpec spec or live data change. Disposable DB dropped; test Host stopped; canonical services healthy. 285 evidence untouched.
