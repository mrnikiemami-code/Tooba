# TB-P09-T022-R2 — owned customer fixture

Script: `docs/evidence/TB-P09-T022/_r2_owned_fixture.mjs`  
Committed refs: `docs/evidence/TB-P09-T022/r2-fixture.json` (secrets redacted)  
Secrets (untracked): `.tmp-r2-fixture-secrets.json`

## Fixture summary

| Field | Value |
| --- | --- |
| Email | `r2.customer.1789015757792@example.test` |
| UserId | `01a089a6-1463-7000-b1aa-0024ab6d1016` |
| CheckoutId | `01a089a6-154f-7000-9586-bc1463ac6c8b` |
| Sellers | 2 (Pars + Arman offers) |
| Initial package | `MP-01A089A61CF4` / preferred `CENTRAL-T022-R2` |
| After rebuild | `MP-01A089AA19B2` / preferred `CENTRAL-T022-R2-REBUILT` |

## Construction

1. Register + Host login for a fresh customer account
2. Guest multi-seller cart → checkout → manual payment → Admin `confirm_deposit`
3. SQL rebind: `order.checkouts.placed_by_user_id = {userId}` (`placedByForced: true`)
4. Admin pack / create_shipment / assign_tracking per seller (`TRK-…`)
5. Admin `create_consolidated_package` with tracking `CENTRAL-T022-R2`
6. Owned Host fulfillments with Bearer session → **HTTP 200**, preferred `CENTRAL-T022-R2`

Do not commit passwords, access/refresh tokens, or raw guest secrets.
