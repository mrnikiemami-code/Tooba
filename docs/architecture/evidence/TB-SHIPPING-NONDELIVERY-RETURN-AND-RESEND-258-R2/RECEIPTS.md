# TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2 — RECEIPTS

Point-in-time observations of one worker on the canonical machine. PIDs are observations, not live claims.

## R1. Worktree / base

```text
git merge-base --is-ancestor 29d3ce3688e58273e92e51fe1b1c782e485ebc1b HEAD   -> exit 0
git log --oneline 29d3ce36..HEAD
  0a9d42705 docs(shipping): hand off 258-R2 integrated gaps and retain five-issue ledger
  4ad0e20ce docs(shipping): hand off 258-R2 integrated gaps and retain five-issue ledger
  7b9aa1c44 docs(shipping): hand off 258-R2 integrated gaps and retain five-issue ledger
  3d1735d23 docs(shipping): hand off 258-R2 integrated gaps and retain five-issue ledger
  7904af959 docs(shipping): issue bounded 258-R2 integrated recovery repair
  8de1d5eb1 docs(shipping): identify remaining public recovery and receipt UI gaps
git rev-parse HEAD            -> 0a9d42705a3e4a8cd07d5ead1988daf2ff991d25  (== origin/main at start)
```

No force/reset/amend/stash/rebase was used. Other worktrees were listed only.

## R2. Focused backend proof — Fulfillment owning facts (real PostgreSQL `tooba_alpha`)

```text
$env:TB258_CONNECTION = 'Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
$env:TB258_EVIDENCE_DIR = docs\architecture\evidence\TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R2
dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj \
  --filter FullyQualifiedName~PostDispatchException
-> Passed!  - Failed: 0, Passed: 35, Skipped: 0, Total: 35, Duration: 3 s
```

Task-labelled `TB258-*` fixtures only; ids in `tb258-relational.txt`:

```text
A original=25e5b971-9864-45e1-a4bf-c570269af5ab resend=9f2fe1bc-ea13-4d8e-96f1-c958d421eefc lineA=ceb881af-8d1d-44dd-91b0-a02dafb2b650 lineB=513c4c82-a349-4555-a8b6-b641f5697117
B delivered=cc41ad17-0ee6-449b-b158-52c7e6da20af preDispatch=9f30917e-e107-410a-bc0b-03c13d11db43
C original=5c25fb3b-1483-4341-b23a-c5c175cccac4 cancelledResend=13d5af21-d807-4e96-b50b-d87a956ffb01
D winner=470e7cb5-62d3-4dbb-b7c7-64b13cac0623 loserKey=tb258-D-left resendRows=1 replay=same
E first=aee8d16d-d3e1-4377-b08d-8633501b8a09 secondInTransit=3aea1446-0050-46dd-a0d1-f1ff5605168c delivered=1
F package=fa8b5568-bb15-4044-bfc2-dfcf689ed11b members=66609150-1402-4fdf-af1e-2fbbf9e8eced,4eb672d2-5938-4b9b-8f23-e82e1a6cd172 locked=refused
G resend=fb6f9557-ee21-4220-83be-f1e83842b915 replay=same intentConflicts=2 rows=1
H first=b5adeb82-7ca2-40ec-81b7-8d83a5ffcf02->ef9ed448-4884-4c17-8a6c-03ed392d6641 second=bc063a12-0797-4190-b499-1d367ee9a61f->5a7ed3ef-d667-4185-a052-b5d9e3656011
I receiptLines=3 lineAusable=1 resend=f69a0f72-73eb-420f-afd7-f764e6c70668 changedReceiptRefused=fulfillment.return.receipt_conflict
J nondelivery=recorded/conflict return=recorded receipt=recorded(3 lines)/conflict resend=20fdbeda-6602-45bc-a0db-bc619ff48e52:recorded/conflict/blank:NotRecorded/cancelled:ResolvedCancelled rowsUnchanged=true
K mixed=usable2+damaged1:grants2 overTotal=4:refused(fulfillment.return.receipt_qty_exceeds) source=ea01f733-a492-4568-8ae0-93dd52d6c983 overSource=f05e1c0c-1b59-4f05-91ea-bf99d1f36fe5 clamped=false persisted=false
```

`J` is the new explicit read-only case: the reconcile answers only the **exact** durable fact and writes
nothing (`rowsUnchanged=true`), a blank occurrence key is `NotRecorded` (never a silent replay), and a
cancelled recorded resend is `ResolvedCancelled`.
`K` is the new mixed-condition case: `usable 2 + damaged 1` grants **2**; total 4 is refused with
`fulfillment.return.receipt_qty_exceeds`, **no clamp**, nothing persisted.

## R3. Focused backend proof — public Order command through the production adapter

```text
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj \
  --filter FullyQualifiedName~AdminOrderPostDispatchReplay
-> Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 1 s
```

Cases executed:

```text
Exact_recorded_non_delivery_answers_from_durable_facts_without_the_projection_gate
Exact_recorded_receipt_replays_the_same_mixed_condition_lines
A_changed_recorded_intent_is_a_typed_conflict_and_never_a_second_allocation
An_actor_without_fulfillment_authority_cannot_replay_a_recorded_intent
A_parcel_of_another_fulfillment_is_refused_even_when_a_record_exists
An_absent_record_still_goes_through_the_projection_gate_and_allocates_nothing
An_absent_record_for_a_still_actionable_parcel_runs_the_ordinary_command_once
Relational_public_command_replays_a_lost_resend_and_receipt_from_real_owning_storage   (real tooba_alpha)
```

```text
tb258r2-orchestrator-relational.txt
G orchestration fulfillment=185b0ab7-560e-4e5a-8720-dcbbd3324930 checkout=ce4f6824-574b-4ada-85c1-d2cb35dce18c source=4ea48680-67fd-4850-8385-c7003931a470 resend=973e2c2e-ef46-4a64-ad4e-8a9d42bbab7b receipt=5fb240a1-6056-4f17-b664-9b69c7a39997 lifecycle=public-command replay=exact(sameParcel) changed=conflict foreign=refused rows=shipments2/receipts1
```

The relational case crosses the formerly blocking gate: the first resend/receipt is committed, the fresh
detail/capabilities no longer offer the action, and the retried **public** command returns the exact
recorded parcel/receipt with zero new rows.

## R4. Focused frontend proof

```text
cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
-> tests 39 / pass 39 / fail 0   (3 new TB-258-R2 cases)
npx tsc --noEmit                 -> exit 0
npm run build                    -> exit 0
```

New mounted cases:

```text
TB-258-R2 mounted: origin receipt splits ONE line into usable and damaged quantities        (payload = two items, same line)
TB-258-R2 mounted: an unresolved recorded request keeps ONE recovery entry after the action vanishes
TB-258-R2 static:  no recorded request means no recovery entry, and no invented action
```

## R5. Backend build / publish / runtime

```text
dotnet build src/backend/Tooba.slnx -c Release --nologo -v q   -> 0 Error(s), 359 pre-existing Warning(s)
Stop-Process -Id <old host> -Force
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts\publish-primary  -> exit 0
DP keys before: key-d82e31a6-0269-4818-8766-c8267198bde9.xml 1015 bytes 2026-11-02
DP keys after : key-d82e31a6-0269-4818-8766-c8267198bde9.xml 1015 bytes 2026-11-02   (unchanged)

Start-Process artifacts\publish-primary\Tooba.Host.exe
  ASPNETCORE_ENVIRONMENT=Development
  ASPNETCORE_URLS=http://127.0.0.1:5088
  Tooba__DeploymentId=store-alpha-canonical
  Tooba__StorefrontCapturedPreview__{Enabled,SelectionEnabled,CheckoutEnabled}=true
  Tooba__StorefrontCapturedPreview__AvailableDateCount=7
  Storefront__CapturedPreview__{Enabled,SelectionEnabled,CheckoutEnabled}=true   (local aliases)
HOST_PID 34180 ; GET /health -> 200 ; logs Edition=SingleStore TenantId=store-alpha DeploymentId=store-alpha-canonical
```

```text
FE restart (src/frontend): node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1
FE_PID 3024
BUILD_ID mdzGQu5f7TP39KSWV_eKU
GET /_next/static/mdzGQu5f7TP39KSWV_eKU/_buildManifest.js   -> 200 (6612 bytes)
GET /_next/static/otEqpbYA0ejFbfq-8pTRE/_buildManifest.js   -> 404   (no stale process)
GET /fa                                                    -> 200 (references the new build)
```

Ports observed: `3000 -> PID 3024`, `5088 -> PID 34180`.

## R6. Database / environment boundaries honoured

```text
ONE database: Host=127.0.0.1;Port=5432;Database=tooba_alpha   (SingleStore / store-alpha / tenant-alpha)
No disposable/test/parallel/Docker database, no port 15433, no edition/connection/runtime switch.
No new migration: the code commit contains no Migrations file; migrations were not re-applied.
No manual DDL, no business SQL, no reset/reseed/backfill, no sweeping delete.
No provider call, no credentials, no fake waybill. Live Tipax remains deferred.
User order 01a1b92d-e97e-7000-999a-143350d0edaf was not mutated.
DataProtection key material untouched. CapturedPreview local ON / checked-in OFF preserved.
```

## R7. Git

```text
code commit      see result-contract.txt  (single code commit on top of Base 0a9d42705)
evidence commit  see result-contract.txt  (this folder + AGENTS / START-HERE / recovery / closure docs)
push             origin main (fast-forward, no force)
```
