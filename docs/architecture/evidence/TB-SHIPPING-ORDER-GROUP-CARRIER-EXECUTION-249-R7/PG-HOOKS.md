# TB-249-R7 disposable PostgreSQL hooks (parent PG agent)

Filter: `Category=TB249R7` on `Tooba.Shipping.Tests`.

## Cases

| Case | Method | Expectation |
|------|--------|-------------|
| `prebound-concurrent-first-request` | `CaseR7_prebound_concurrent_first_request_one_create_one_allocation` | Two concurrent creates with the **same client UuidV7** → one provider create, one fulfillment allocation, both success results share that occurrence. |
| `legacy-no-locator-not-response-loss-safe` | `CaseR7_legacy_no_locator_after_succeeded_mints_new_occurrence_not_idempotent` | After Succeeded, a second **no-locator** create with identical payload mints a **new** occurrence (2 provider calls). Documented as **not** response-loss recovery — clients must prebind. |

Also keep R6 `barrier-same-occurrence` as adjacent proof.

## Safe legacy behavior (explicit)

- Nullable/legacy omit of `PackageDispatchId` remains allowed for open Pending/Unknown payload match and for minting a **new** parcel after Succeeded.
- It is **not** idempotent recovery after a lost first-create response once Succeeded exists.
- FE R7 always prebinds UuidV7 before first create POST.

## Run (parent)

```text
dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj --filter Category=TB249R7
```

Use only task-owned disposable Postgres; do not write to `tooba_alpha`.
