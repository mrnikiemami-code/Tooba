# RECEIPTS — TB-274-R1

Canonical target for every command: `D:\Users\User\source\repos\SarvNewVer`, branch `main`, database
`tooba_alpha` @ `127.0.0.1:5432` (edition `SingleStore`, tenant `store-alpha`). No second database, no Docker image, no
port/connection/setting/key change.

## 1. Repository state

| Command | Result |
| --- | --- |
| `git rev-parse HEAD` | `145bb1a0747e6a97c607c06d22c374e34a001bc8` — equals `Expected-Base-SHA` |
| `git log --oneline -3` | `145bb1a07 docs(returns): review 274 as partial and bound receipt/stock/decision repair` / `7e7c2ee3d feat(returns): customer receipt/inspection facts gating ordinary refunds (TB-274)` / `ca2dbed34 docs(returns): accept 273-R2 …` |
| `git status --porcelain` | only the A–E code/test/migration files (plus pre-existing untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` scratch files, which are **not** part of this change) |
| Final commit / push / origin parity | performed once after the evidence was written; the resulting SHA is recorded in the Bridge result (no self-referential hash pin in these files) |

## 2. Schema / backup (see `migration-and-backup-proof.md` for the ordered narrative)

| Command | Result |
| --- | --- |
| pre-apply plan (`Tooba.MigrationRunner plan`) | Returns `current=20261116122318_AddCustomerReceiptsAndDecisionReason`, `pendingCount=1`, `result=ok` (17:39:24) |
| `pg_dump … > tooba_alpha-pre-tb274r1.dump` | exit 0, 2,979,578 B, SHA256 `4D3060A6…6D87BC` (17:40:14) |
| `pg_restore --list` on dump #1 | exit 0, 1445 TOC lines, 0 stderr bytes (17:40:29) |
| `Tooba.MigrationRunner apply` (attempt #1) | **failed** `42703 column "ReturnRequestId" named in key does not exist`, fully rolled back (17:41:02) |
| `pg_dump … > tooba_alpha-pre-tb274r1-constraints.dump` | exit 0, 2,979,578 B, SHA256 `31D47A05360045904846661C09F064EA11381D52C8F713FC2BA261B589527A52` (17:44:10) |
| `pg_restore --list` on dump #2 | exit 0, 1445 TOC lines, 0 stderr bytes (17:44:11) |
| `Tooba.MigrationRunner apply` (attempt #2) | exit 0; Returns `succeeded=true`, `currentMigration=20261116140705_BoundedReceiptIntegrityConstraints`, `pendingMigrations=[]`, `durationMs=128` (17:44:21) |
| post-apply plan | Returns `current=20261116140705…`, `pendingCount=0`, `result=ok` (17:44:53) |
| `psql -U admin -d tooba_alpha -c "\d returns.customer_return_receipt_lines"` | both CHECKs, both composite FKs, both owning indexes present |
| `psql … select … from returns.__ef_migrations_history` | head `20261116140705_BoundedReceiptIntegrityConstraints` |
| `psql … count(*)` on `customer_return_receipts` / `customer_return_receipt_lines` / `return_request_decisions` | `0` / `0` / `0` |
| `pg_restore --list` on the **original 274** dump (verification now, labelled 18:33:02) | exit 0, 1431 TOC lines, 0 stderr bytes; size 2,966,890 B and SHA256 `ADCE728B…DC64CD16` unchanged |
| `dotnet ef migrations has-pending-model-changes --project …Tooba.Returns.Infrastructure… -c Release --no-build` | "No changes have been made to the model since the last migration." |

## 3. Tests (all executed once after the final code change unless stated)

| Command | Result |
| --- | --- |
| `dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --filter "…ReturnsCustomerReceipts274R1Tests|…ReturnsCustomerReceipts274Tests"` | **39 passed / 0 failed** (R1 14 + parent 25) |
| `dotnet test src/backend/Host/Tooba.Host.Tests/… --filter "…ReturnsCustomerReceipts274R1RelationalTests"` | **4 passed / 0 failed** (real `tooba_alpha`; each fact caller-owned + rolled back) |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (pre-existing warning set only) |
| `dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary` | **0 errors**; `Tooba.Host.dll` SHA256 `5CE96FB414ED4B51C02EBDA17400150CC700D0BEC97697E6C3610D8422506971`, 222,720 B |

Declared out-of-selection runs (disclosed, not claimed as recertification): whole `Tooba.Returns.Tests` **105/105**,
`ReturnsModuleAmsc001*` **34/34**, and `ReturnsCustomerReceipts274RelationalTests|ReturnsRecovery273RelationalTests`
**9/9**.

## 4. Runtime (Host-only refresh, backend only)

| Step | Result |
| --- | --- |
| graceful stop of the previous Host (`PID 6000`) | port 5088 free |
| publish + start (`dotnet Tooba.Host.dll`, `artifacts/publish-primary`, `ASPNETCORE_ENVIRONMENT=Development`) | `PID 22064` listening on 5088 |
| `GET /health` | `200` |
| `GET /health/ready` | `200` — `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}` |
| `GET /__platform-commerce` | `store-alpha` / `tenant-alpha` / `127.0.0.1` / `IR` |
| `Tooba.Host.dll` hash of the running module | `5CE96FB414ED4B51C02EBDA17400150CC700D0BEC97697E6C3610D8422506971` |
| freshly built Returns assembly actually loaded by the running process | `Tooba.Returns.Application.dll` (18:36:25) SHA256 `9BC7969E36304DF23D7DE984BA944DBBF4206B5F36F0B83D6E48E566529C1FA2`, loaded from `artifacts\publish-primary` (process start 18:41:59) |
| restarts | two Host-only stops/starts (the second after the final code correction) — no FE restart, no schema/seed effect on start |
| FE 3000 | **untouched** — not rebuilt, restarted or inspected |

## 5. Live read-only wire probes (`POST`, non-existent return id, no business write)

| Probe | Result |
| --- | --- |
| receipt, no admin actor | `401 admin.actor.missing` |
| receipt, actor, line without `condition` | `400 return.receipt.line_invalid` |
| receipt, actor, `"condition": ""` | `400 return.receipt.line_invalid` |
| receipt, actor, `"condition": "damaged"` | `404 return.missing` |
| `POST /v1/admin/returns/query` with actor | `200 {"items":[],"page":1,"pageSize":20,"totalCount":0}` |

The two `line_invalid` answers on a **non-existent** case id are the served proof that the inspection token is refused
before any case read or write; no live valid case was posted.

## 6. Boundaries held

One canonical `tooba_alpha`; no other module schema; no manual DDL outside the additive migration; no seed/backfill/
cleanup/delete/reset/restore; no committed business or test row; no live approval/reject/refund/receipt mutation; no
provider/PSP/Wallet/Inventory/waybill/notification/money operation; no connection/port/setting/key/edition/CapturedPreview
change; FE untouched; no demo case/order/hold created; original 274 evidence left unchanged.
