# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R2

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R2`
- Bridge row: `85819a34-f2e1-46f4-af1c-b54642c46d75`
- Base-SHA: `84cae82b2cdc5a5f142b850b9fca9a0c352a08a4`
- ClaimedAtUtc: `2026-11-02T16:42:03.8972555Z`
- Channel: `tooba-main` / Worker: `tooba-worker-01`

## Timing (approx UTC)

- Claim / wake: ~16:42Z
- Software + focused tests complete: ~16:55Z
- Backup + migration apply: ~17:00Z
- Host/FE redeploy + evidence: ~17:10Z
- Cumulative focused work after claim: ~30–40 min (prior session + this wake)

## Tests

| Suite | Result |
|-------|--------|
| Shipping Carriers + classification + authoring context/owner harness | 61 passed |
| order-group-carrier-execution.test.ts (`npx tsx --test`) | 4 passed |
| frontend `tsc --noEmit` | 0 |
| frontend production build | 0 |
| Host Release publish | 0 |
| Disposable PostgreSQL Order+Shipping §4 matrix | **NOT RUN** — env target absent |

## Backup / migrate

- Dump: `D:\temp\tb249r2-backup\tooba_alpha-pre-tb249r2.dump`
- Bytes: 2720660
- SHA256: `BC5316A6A2A77440D267E6265326E5906EF8CF148416F85F396D686EB0F723DB`
- Applied: `20261102220000_CarrierShipmentExecutionSnapshot` (Shipping, store-alpha / tooba_alpha)
- Readback: `shipping.__ef_migrations_history` includes `20261102220000_…`; column `execution_input_snapshot_json` on `carrier_shipment_operations`

## Runtime

- Host PID **29424** :5088 `/health` 200 — `artifacts/publish-primary\Tooba.Host.exe`
- FE PID **18548** :3000 `/fa` 200 — `next start -p 3000 -H 127.0.0.1`
- Edition: SingleStore / store-alpha / `tooba_alpha`
- Unauth capabilities GET checkout `01a185a2-2e5a-7000-a3b4-eee8c3710158` → **401**
- Classification: **57** requests / **99** routes (sender-completion admin+seller)

## Controlled vs live

- InMemory directory/readiness prove completion→create unlock and inactive Track refusal
- Zero live Tipax; zero mutation of user order
- Authorized session for order UI not available this wake → disclosed limit
- Relational disposable proof blocked: `TOOBA_TB249R2_POSTGRES` unset
