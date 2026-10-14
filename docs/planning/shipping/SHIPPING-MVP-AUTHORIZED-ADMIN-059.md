# TB-SHIPPING-MVP-AUTHORIZED-ADMIN-059 — authenticated admin real-HTTP roundtrip

Status (worker): **PARTIAL, with one self-inflicted restricted-value incident honestly disclosed and rolled back.**
The authorized end-to-end admin capability proof the Architect asked for was **achieved with real HTTP and real
durable persistence**, but it landed in the real Development database instead of the disposable clone because of
my environment-variable mistake, so the "scratch-only" requirement was **violated and then reverted**.

- Parent task: `TB-SHIPPING-MVP-OPERATIONAL-VERIFY-058`
- Expected base SHA: `2c243d2dbc83a17a3967a244637012ff1da857b8`
- Base SHA: `2c243d2dbc83a17a3967a244637012ff1da857b8` (only drift was the allowed docs-only commit; fast-forwarded)
- Code-SHA: **NONE** (no product source changed)
- Evidence-SHA: recorded in the Bridge result
- automaticNextImplementationTask: **NONE**

## 1. What was proven

1. **Legitimate admin provisioning exists and was used.** `AdminDevActorBootstrap` (Identity user
   `admin-actor@tooba.local` + `user#member` on the tenant) plus `AccessControlDirectory.EnsureBootstrapAsync`
   (system `platform-admin` role carrying all 55 catalog permissions, including `fulfillment.view` and
   `fulfillment.manage`, assigned to that user, then capability tuples synced). No `AllowAnonymous`, no
   hand-written tuple, no fabricated identity, no magic admin.
2. **Real authenticated session.** `POST /v1/auth/login` returned a real token; every subsequent call used it.
3. **Full authorized CRUD roundtrip on Shipping rates/coverage** with **real Geography stable IDs** resolved
   from the live read endpoints (country `e3451206-…`/`IR`, area `1f8050a8-…`/alborz, city `68255340-…`/کرج):
   GET (empty) -> POST rate (200, real id) -> POST grant (200, real id) -> GET read-after-write -> PUT
   (leadDays 2->5, `updatedAt` moved) -> PATCH rate inactive -> PATCH grant inactive -> GET confirms both
   inactive. A second, area-scoped pair was created and read back with `level:"AdministrativeArea"`.
4. **Guards are real, not decorative.** Cross-service mutation denied (`shipping.service_scope.mismatch`),
   scope immutability (`shipping.coverage.scope.invalid`), duplicate scope denied
   (`shipping.rate_policy.scope_duplicate` / `shipping.coverage_grant.scope_duplicate`), unknown ids denied,
   invented Geography id denied (`shipping.destination_scope.invalid`), unauthenticated = `401
   admin.actor.missing`, and no cross-service leakage. Empty coverage still reads as *no coverage*.
5. **Checkout untouched.** Legacy province-only storefront projection 200, legacy cart/payment-draft path still
   `401 checkout.authentication_required`; no cutover switch exists and none was added.
6. **The Development actor header is not authorization** — it returns 200 only for an actor that already holds
   the capability tuple, and 403 for one that does not (058's finding, now sharpened).

## 2. What went wrong (self-disclosed)

I redirected the isolated instance with `Tooba__PostgreSQL__ConnectionReferences__tenant_alpha`, but the
configuration key is `tenant-alpha` (hyphen), so the override did not apply and the instance resolved
`Database=tooba_alpha` from `appsettings.Development.json`. The authorized roundtrip therefore wrote to the
real Development database. Measured against the pre-task snapshot: `shipping.rate_policies` 0->3,
`shipping.coverage_grants` 0->2, `identity.auth_sessions` 170->175. All of it was deleted
(`probe/rollback-alpha-accidental-writes.sql`), restoring `0 / 0 / 170` exactly.

`pricing.prices` (+8) and `pricing.outbox_messages` (+24) also differ from the snapshot but are **not
attributable** to this task: the same burst pattern occurs at `20:02:54Z` when no task-owned Host existed, my
instance's startup log shows no pricing operation, and no task request touched pricing. Those rows were left in
place deliberately. Consequence for acceptance: this task does **not** prove "no writes to `tooba_alpha`";
it proves the writes were bounded, identified and reverted.

## 3. Reproduced defect (recorded, not fixed)

`POST /v1/admin/shipping-services/post/rates` without `scope` -> `500 platform.unexpected`
(`NullReferenceException` in the FluentValidation rule for `x => x.Scope.CountryId`). Deterministic. Left
unfixed: it is not a blocking defect for the acceptance criteria and not architecture-boundary related; the
task allows a code change only for a directly reproduced blocking defect. Recommended follow-up: `NotNull`/`When`
guard so it fails as `400`.

## 4. Release verdict

Unchanged and now better supported: **NOT_READY_FOR_CHECKOUT_CUTOVER**. Rate/coverage authority is usable and
provably guarded by an authenticated admin, but commercial values and coverage grants are still absent (0/0
after rollback), destination input is province-only, shadow parity has never been measured, and no cutover
authorization exists.

## 5. Timeline

Claimed `2026-10-14T20:25:45Z`; the 30-minute HARD box was exceeded (work continued to ~20:37Z and evidence
was finalised afterwards) because the incident had to be identified, attributed and rolled back before any
result could be posted honestly. Stopped immediately after cleanup and evidence capture.
