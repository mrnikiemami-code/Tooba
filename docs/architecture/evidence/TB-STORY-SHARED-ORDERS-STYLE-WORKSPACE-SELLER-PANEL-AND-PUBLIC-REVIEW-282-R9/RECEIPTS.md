# RECEIPTS — TB-282-R9

## Claim

- Bridge row `48f7ecdc-bb77-47dd-b4aa-9f3928bd24dd` via `/api/tasks/next`
- Expected-Base `dcd5e4e383f819924b182d421ffcb7dce32c1857` (ancestor)
- Claim HEAD `93df9aa520397953fb500bc8c1ad3f532b3f1352`
- Issuance TASK SHA256 `a3be49854bb93f27da42897d58b864f7f2ce2c4f9b5036e890a8815a99a9db3e` (receipt); file hash may differ by line endings

## Durable authority read (before recovery)

- `access_control.user_role_assignments`: admin `01a036c2-…` → `platform-admin` (store-alpha); seller `01a03628-3f68-…` → `seller-owner` party `01a030d1-40cb-…`
- Assignment count **3** before and after Host refresh (ZERO durable grant mutation)

## Focused tests

```
AccessControlInMemoryProjectionRecovery282R9Tests → 3/3
```

## Served

- Host `127.0.0.1:5088` PID `41552` `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`
- dll SHA256 `54D3DE3744F226403B96BCB5AE0AD281DEA5805C68DA80EF14CD70557E6EEDC3`
- Log: `AccessControl InMemory projection recovery eligible=True users=3 tenantMembers=1 partyMembers=2`
- `/health` 200; `/__platform-commerce` store-alpha
- `/v1/admin/dev-context` 200
- BFF login admin 200; `/v1/admin/stories` **200**
- Host `/v1/seller/stories` **200** for assigned seller Dev headers; unassigned second seller **403** (fail-closed)
- Next PID `34656` BUILD_ID `7nEINy82sBP3x0f0H6xdw` (R8 unchanged)
- DOM: vendor + admin `/new` form present; overflowX false @1440/@390; list-only review filter absent on create

## Schema / live

- Migration/DDL/seed/reset: NONE
- Live Story/Media business write: ZERO
- Persistent permission/account/membership: ZERO
