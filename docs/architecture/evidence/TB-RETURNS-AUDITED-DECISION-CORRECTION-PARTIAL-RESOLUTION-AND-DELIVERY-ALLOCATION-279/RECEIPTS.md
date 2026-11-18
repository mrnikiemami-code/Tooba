# TB-279 — Receipts (commands actually run, final state)

All commands were run once after the final code change unless marked. Working tree:
`D:\Users\User\source\repos\SarvNewVer`. Canonical store: ONE `tooba_alpha` @ `127.0.0.1:5432` / `store-alpha`.

## 1. Backend

| Command | Result |
| --- | --- |
| `dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests -c Release --no-build --filter "FullyQualifiedName~ReturnsDecisionCorrection279DirectoryTests"` | **11/11 passed**, 0 failed, 726 ms |
| `dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests -c Release --no-build --filter "FullyQualifiedName~ReturnsDecisionCorrection279Tests"` | **13/13 passed**, 0 failed, 30 ms |
| `dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests -c Release --no-build` | **172/172 passed**, 0 failed, 643 ms |
| `dotnet test src/backend/Host/Tooba.Host.Tests -c Release --no-build --filter "FullyQualifiedName~ReturnsModuleAmsc001"` | **34/34 passed**, 0 failed, 221 ms |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors**, 216 warnings (pre-existing) |
| `dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary` | **0 errors** |

## 2. Frontend (unchanged by the in-task backend corrections)

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/returns/return-submission-279.test.ts app/returns/return-submission.test.ts app/returns/return-quantity.test.ts app/returns/return-api.test.ts` | **25/25 passed** |
| `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-279.mount.test.ts app/admin/admin-return-workspace-277.mount.test.ts app/returns/return-request-modal-278.mount.test.ts` | **17/17 passed** |
| `npx tsc --noEmit` | 0 diagnostics |
| `npm run build` | 0 errors, `BUILD_ID 8fLqIGZTLx0CUcUquOuUG` |

## 3. Served identities

```
Host (5088)   pid 33892   started 2026-11-18T04:25Z (approx.)
              artifacts/publish-primary/Tooba.Host.dll
              SHA256 7EE36BFF34B5F87B851C47199D5DA631D16EA710659A49209A21CD0C880766B4
              GET /health             -> 200
              GET /health/ready       -> 200 {"status":"ready","checks":{"edition":"SingleStore",
                                          "postgresql":"configured","authorization":"inmemory",
                                          "messaging-transport":"postgresql-sql","messaging":"Healthy"}}
              GET /__platform-commerce -> {"edition":"SingleStore","deploymentId":"dev-singlestore",
                                          "tenantId":"store-alpha","connectionReference":"tenant-alpha",...}

Next (3000)   pid 5076    BUILD_ID 8fLqIGZTLx0CUcUquOuUG
              GET /fa/admin/returns                                       -> 200
              GET /_next/static/chunks/7944-0ba65eeffc7022bc.js           -> 200 (200072 bytes)
                 contains reopen_for_review / publish_resolution / record_custody / «بازگردانی»
              GET /_next/static/chunks/8308-0464ee1d919f7a5f.js           -> 200 (22414 bytes)
```

## 4. Live read-only wire (platform admin actor header, GET/typed refusals only)

```
GET  /v1/admin/returns/{user case}            no actor      -> 401 admin.actor.missing
GET  /v1/admin/returns/{user case}            platform admin-> 200 status=Completed, approvedAmount absent,
                                                              correction.canReopenForReview=false,
                                                              correction.blockerCode=return.correction.state_invalid
POST /v1/admin/returns/query                  platform admin-> 200 totalCount=0
                                                              counts{allOpen:0,...,history:3}
POST /v1/admin/returns/{case}/reopen          no occurrence -> 400 validation.failed
                                                              returns.validation.correction_occurrence_required
POST /v1/admin/returns/{case}/reopen          occurrence, expectedDecisionVersion=1
                                                            -> 409 return.correction.version_stale
POST /v1/admin/returns/{case}/reopen          occurrence, expectedDecisionVersion=0
                                                            -> 409 return.correction.state_invalid
POST /v1/admin/returns/{case}/resolution      no lines      -> 400 validation.failed
POST /v1/admin/returns/{case}/custody         foreign item  -> 400 return.receipt.line_invalid
POST /v1/admin/returns/{missing}/reopen       no occurrence -> 400 validation.failed (before any case read)
```

All refusals wrote nothing: the case row is unchanged (`Completed`, `refund_amount 2407839.0000`,
`approved_amount NULL`, `updated_at 2026-11-17 00:31:26.384627+00`) and the new tables stayed at 0 rows.

## 5. Data facts after every receipt

```
returns.return_requests: 3 rows, all Completed, all refund_amount 2407839.0000, all approved_amount NULL
  updated_at  01a1e6b0-f33a-7000-80ee-beffb6344490  2026-11-17 00:31:26.384627+00   (user case 1)
              01a1e8fc-c74e-7000-a155-fecfa1a3bf1a  2026-11-17 13:21:05.594614+00   (TB-276 review case)
              01a1e9c5-c729-7000-b614-79daf3d0e812  2026-11-17 14:03:06.669021+00   (user case 2)
receipts 3 | attempts 3 | decisions 3 | revisions 0 | resolutions 0 | custody 0 | allocations 0
columns present: return_requests.approved_amount numeric(18,4) NULL
tables present: return_request_decision_revisions, return_item_resolutions, return_item_custody,
                return_delivery_allocations
```

## 6. Migration runner receipts

```
Plan  (2026-11-17T20:16:58+03:30, trace df7203f4…)  returns current=20261116140705_BoundedReceiptIntegrityConstraints
                                                    pendingCount=1
Backup (2026-11-17T20:13:03+03:30) tooba_alpha-pre-tb279.dump 3011929 B
       SHA256 F33032033DA575DFD5B7C54EA2B26EAF579B134881BF9E69769C6C806361DE05
       pg_restore --list exit 0, 110531 B listing (restore-list.err empty) at 20:13:10
Apply (2026-11-17T20:22:46+03:30, trace 3d0e00a1…)  returns current=20261117164135_Tb279AuditedDecisionCorrection…
                                                    pendingCount=0 result=ok, every other module pendingCount=0

Plan2 (2026-11-18T04:19:09+03:30, trace 839c8ffd…) returns current=20261117164135… pendingCount=0
Plan2 (2026-11-18T04:49:07+03:30 [00:49:07Z], trace 1c1320ad…)
                                                    returns current=20261117164135… pendingCount=1
Backup (2026-11-18T04:19:35+03:30) tooba_alpha-pre-tb279fix-20261118041935.dump 3028309 B
       SHA256 B2C3CA9202580D22DA0FAF9ABBAEA7011B39BCB04688B2F2D5E376422EC31149
       pg_restore --list exit 0, 112831 B listing (restore-finish.err empty) at 04:19:35
Apply (2026-11-18T04:49:50+03:30 [00:49:50Z], trace 0eb4b83d…)
                                                    returns current=20261118004500_NormalizeApprovedAmountColumn
                                                    pendingCount=0 result=ok
```

Apply attempts #1–#3 of the FIRST migration failed and were declared, not hidden: each returned
`42703: column "ReturnRequestId" referenced in foreign key constraint does not exist` (a defect in the generated migration's
constraint ordering, fixed in source before attempt #4). The failed attempts left no partial schema — the next plan still
reported the same single pending migration and the same `current`, and after the successful apply the four new tables
existed with 0 rows. The successful apply is the single one reported above. No manual DDL, Down, restore, reset, reseed or
backfill was ever executed.
