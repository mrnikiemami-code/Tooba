# TB-SHIPPING-MVP-DEV-ADMIN-UI-065 — real Development host + admin UI end to end

**Program:** TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
**Mode:** BOUNDED_EXISTING_DEV_HOST_ADMIN_UI_END_TO_END
**Parent:** TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064
**Expected-Base-SHA:** `6f87d7dd54b2aa2e2904361b52af24330a8b538e`
**Timebox:** 45 minutes
**Verdict:** PASS

---

## 1. The target: the user's real Development host, positively identified

| Property | Observed |
| --- | --- |
| Process | `Tooba.Host.exe` (PID 37028) + its `dotnet run` wrapper (PID 36540) |
| Launch command | `dotnet run --project src\backend\Host\Tooba.Host --no-launch-profile --no-build` |
| Binary path | `src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe` — inside this repository |
| Port | `http://localhost:5088` (matches the `Tooba.Host` launch profile's `applicationUrl`) |
| Effective target | `/__platform-commerce` → `edition=SingleStore`, `deploymentId=dev-singlestore`, `tenantId=store-alpha`, `connectionReference=tenant-alpha`, `resolvedHost=127.0.0.1` |
| Database | `tenant-alpha` → `Host=127.0.0.1;Port=5432;Database=tooba_alpha` (docker container `postgres-db`) |
| Development proof | `GET /v1/admin/dev-context` → `200` with `actorUserId` + `tenantId=store-alpha`. That route is mapped only inside `if (app.Environment.IsDevelopment())`, so the running process is Development. |
| Not staging/production | `appsettings.Production.json` is not loaded; no production connection reference is in use; the only reachable database is the Development `tooba_alpha` on loopback. |

No PID/port was assumed from earlier evidence — it was re-established from the live process table.

## 2. Restart: why it was needed, and how to roll back

The running binary predated the Shipping module's Host wiring and 060/063/064, so
`GET /v1/admin/shipping-services/post/rates` answered **404** — the Shipping endpoints were not mapped
at all. Mission A explicitly requires confirming current committed behaviour on the live host, so a
controlled restart was unavoidable and is authorised by the task.

Recorded launch command (also saved as `launch-dev-host.cmd` next to this document):

```text
cd <repo root>
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5088
dotnet run --project src\backend\Host\Tooba.Host --no-launch-profile --no-build
```

That is the same command/env pair the user was running, expressed without a launch profile
(the profile itself sets exactly these two values). **Rollback = stop the `Tooba.Host` process and
re-run that file.** The host came back healthy in 4 s and served the same provenance and the same
database as before, so the restart is a no-op apart from loading current code.

The frontend dev server on `:3000` (Next.js, `next dev` for `src/frontend`) was **not** touched; it
proxies `/v1/*` to `http://127.0.0.1:5088` via `next.config.ts`.

## 3. Mission A — live health, routes, auth, and the 064 regression

Full matrix in `log-http-065.txt`.

| Probe | Result |
| --- | --- |
| `GET /health` | 200 `{"status":"ok"}` |
| authorized `GET .../shipping-services/post/rates` | 200 `{"serviceCode":"post","ratePolicies":[],"coverageGrants":[]}` |
| authorized `GET /v1/admin/geography/countries` | 200 (`IR`) |
| `GET .../rates` unauthenticated | **401** `admin.actor.missing` |
| `GET .../rates` unknown actor | **403** `admin.authorization.denied` |
| malformed POST/PUT/PATCH on all five write routes (D1–D6) | **400** `validation.failed`, `errors.request=[shipping.validation.body_invalid]` |
| malformed POST with no actor (D7) | **401** (authorization still runs first) |
| `POST` without `scope` (S1) | **400** `Request.Scope=[shipping.validation.scope_required]` |
| `POST .../coverage-grants` `{}` (S2) | **400** `shipping.validation.scope_required` |

So the 064 malformed-body contract is present and correct on the live Development host, and the
063 missing-scope contract is unchanged.

## 4. Mission B — real fixtures on real IDs and an existing service code

Services used: **`post`** (an existing ACTIVE row of `fulfillment.shipping_services`). No imaginary
`svc-065` service was invented, and no service row was created.

Geography IDs came from the live authorized Geography admin read, not from fabricated values:
`country IR = e3451206-e503-51a8-91ad-afa9ab70e330`, `area alborz = 1f8050a8-…`,
`city IR-alborz-01 = 68255340-c69f-5d20-bf33-e8e0b24edde9`, and later
`area east_azerbaijan = 995347e5-…`, `city IR-east_azerbaijan-02 (marand) = ebf0cb15-…`.

The fixture marker is the **scope**: every fixture is pinned to one real city (or, for the pre-fix UI
run, the country) and carries a deliberately synthetic amount (65 000 / 71 000 / 111 111 / 222 222 /
123 456). Nothing here reproduces a real tariff. All amounts live only in the Development database.

## 5. Mission C — two directly observed frontend defects, repaired at the Shipping screen

### D1 (blocking): the client refused a valid city scope, so the primary workflow was impossible

`shipping-rates-coverage-model.ts` rejected any selection that had both an administrative area and a
city:

```text
if (selection.cityId && selection.administrativeAreaId) {
    issues.push("شهر و استان همزمان قابل انتخاب نیستند؛ سطح دامنه را یکی کنید.");
}
```

That contradicts the domain contract, which says a scoped object must always carry **all** of its
parents — `ShippingDestinationScope.ForCity(countryId, administrativeAreaId, cityId)` — and the
server proves it: the HTTP fixture created with country+area+city returned `200` with `level=City`,
and the UI itself renders such rows as `شهر · … / استان … / شهر …`. The rule confused "the chosen
level" with "the parent chain", and its error message pointed the user at a nonexistent conflict.

Repair: the rule is removed (country-required and service-area-requires-city are kept). The picker's
`clearDependents` already kept the area when the city changed, so no other code needed to move.

### D2: rate-form errors were displayed inside the destination error list

`submitCreateRate` merged scope problems and rate-form problems into one state that renders under
`data-testid="shipping-destination-issues"`, so "کد ارز باید سه حرف لاتین باشد." appeared under the
Geography picker. `shipping-rate-issues` existed only for edit rows and never appeared for create.

Repair: a dedicated `createFormIssues` state renders under a new
`data-testid="shipping-create-rate-issues"` list next to the rate fields; destination problems stay
in `shipping-destination-issues`.

Both repairs are inside the allowed scope (`src/frontend/**` owning the existing admin
shipping-services screen and its directly owned model/test). No backend file was changed — the
backend already behaved correctly, which is exactly why D1 was a UI defect and not a server defect.

## 6. UI vs HTTP matrix — what was exercised through which channel

| Capability | Through the UI | Through HTTP | Persisted |
| --- | --- | --- | --- |
| Open service list | yes | n/a | — |
| Open rates/coverage dialog for `post` | yes | n/a | — |
| Cascading Geography select (country → area → city) | yes | geos read 200 | — |
| Create rate | yes (city scope, after D1 fix) | yes (city scope) | yes, both |
| Create coverage grant | yes (city scope, after D1 fix) | yes (city scope) | yes, both |
| Read-after-write | yes (close/reopen + full reload) | yes | yes |
| Edit rate money/lead-time | yes (charge 123 456 → 222 222, no revert) | yes (65 000 → 71 000) | yes |
| Toggle rate active | yes | yes | yes |
| Toggle coverage active | yes | yes | yes |
| Visible validation error | yes (see D2) | n/a (server 400 for missing scope) | — |
| Non-admin denial | yes (403 on GET/DELETE attempts via the screen) | yes (401 no credential, 403 unknown actor) | — |
| Fail-closed coverage on untouched service | yes (empty coverage list) | yes (`tipax`, `store_courier`, `in_person`: 0 rates / 0 grants) | — |

The browser run drove the real Next.js app in a real browser against the real backend; every mutating
step produced a real `POST`/`PUT`/`PATCH` with `200`. Nothing was mocked.

## 7. Fixtures and controlled cleanup

Six fixtures were created (three rates, three grants) — see `log-fixtures-and-cleanup.txt` for the
full list with IDs and scopes. Cleanup: the Shipping admin surface exposes exactly six routes and
**no delete route**, so a fixture cannot be removed over the API. Every fixture was therefore
**deactivated** through the canonical `PATCH …/active` route — the strongest available state, where an
inactive policy/grant is not coverage and does not quote. Final state for `post`: 3 rates, **0 active**;
3 grants, **0 active**; database-side confirmation is identical. No row was deleted by hand: nothing in
this task authorises silent deletion, even though the user described the Development data as disposable.

## 8. Tests

* `shipping-rates-coverage-model.test.ts` — updated: the "inconsistent selection" case now asserts the
  corrected contract (a city with its area parent is **valid**; a service area without its owning city
  is still refused). This reflects the domain rule rather than relaxing it.
* `shipping-services-roundtrip.test.ts`, `shipping-services-form-model.test.ts`,
  `shipping-rates-coverage-model.test.ts` — **27 passed, 0 failed**.
* `npx tsc --noEmit` on `src/frontend` — clean (9 s).

## 9. Changed paths

| Path | Change |
| --- | --- |
| `src/frontend/app/admin/shipping-services/shipping-rates-coverage-model.ts` | D1: removed the false area+city rejection |
| `src/frontend/app/admin/shipping-services/shipping-rates-coverage-dialog.tsx` | D2: dedicated `shipping-create-rate-issues` list |
| `src/frontend/app/admin/shipping-services/shipping-rates-coverage-model.test.ts` | contract-aligned assertion |
| `docs/planning/shipping/SHIPPING-MVP-DEV-ADMIN-UI-065.md` | this document |
| `docs/architecture/evidence/TB-SHIPPING-MVP-DEV-ADMIN-UI-065/**` | evidence |

## 10. Evidence index

| File | Content |
| --- | --- |
| `log-dev-host-identity.txt` | pre-restart process/port/provenance identification and the recorded stop |
| `launch-dev-host.cmd` | the recorded, reproducible launch command (and rollback) |
| `log-host-startup.txt` | post-restart startup log excerpt (migrations, bootstrap, module seeds, listening) — filtered from 28 795 live lines |
| `host-stdout.txt`, `host-stderr.txt` | local-only live stdout/stderr of the restarted host (untracked: the host keeps appending runtime tracing) |
| `log-http-065.txt` | full HTTP matrix (A1–A5, D1–D7, S1–S2, F1–F2, T1–T9, G1–G2) |
| `log-fixtures-and-cleanup.txt` | fixture IDs/scopes, deactivation results, final DB-side state |
| `probe-http.ps1`, `cleanup-fixtures.ps1` | re-runnable scripts |
| `RESULT.bridge.txt` | the Bridge result envelope |

## 11. Remaining limitations

* Two residual observations from the browser run were **not** repaired, as neither blocks the
  workflow and the timebox did not justify speculative change:
  - Immediately after picking an area, the city `<select>` can transiently list every city of the
    country before settling to the area-scoped set (the fetches race). Values stay correct once
    settled.
  - The rates dialog is a native `<dialog>` whose two `بستن` buttons carry no `data-testid`, so
    automation must locate them by text. Cosmetic.
* `Tooba.Host.Tests` was not run: the Development host is up and holds the Host DLLs, which the task
  explicitly forbids fighting. The frontend changes were verified in the real browser instead.
