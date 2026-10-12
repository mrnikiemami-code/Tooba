# TB-FE-ADMIN-MENUS-HELPER-SIGNATURES-001 — Admin Menus helper signature repair (evidence)

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Program:** `TOOBA-FRONTEND-TYPECHECK-RECOVERY` · **Branch:** `main`
**BASE / expected starting HEAD:** `eebb0a09ebbbea362a06f0f6d1d624724478364c` — preflight verified `HEAD == origin/main == Expected-Starting-HEAD`, no tracked modification, no divergence.

## 1. Source-grounded root cause

The three diagnostics are all in `src/frontend/app/admin/menus/admin-menus-api.ts` (84, 90, 91) and are **outdated calls against the canonical Admin helpers**, present since the file was created in `c3ced29ef`:

| Diagnostic | Called | Real signature (canonical owner) |
| --- | --- | --- |
| `TS2345` line 84 | `adminHeaders(Boolean(init?.method && init.method !== "GET"))` | `adminHeaders(extra?: Record<string, string>)` — `src/frontend/app/admin/admin-api.ts:999` |
| `TS2554` line 90 | `parseAdminProblemErrorCode(body)` | `parseAdminProblemErrorCode(payload: unknown, status: number)` — `src/frontend/app/admin/admin-error-map.ts:968` |
| `TS2345` line 91 | `mapAdminErrorMessage(code, "دسترسی مجاز نیست.")` | `mapAdminErrorMessage(raw, locale: AdminErrorLocale = "fa")` — `src/frontend/app/admin/admin-error-map.ts:925` |

The third call passed a **Persian sentence where a locale token belongs**, i.e. the file had invented a local error/locale contract instead of using the shared one.

## 2. Neighbouring canonical callers inspected (no invention)

`src/frontend/app/admin/landing-pages/admin-landing-pages-api.ts:126,140-145` and `src/frontend/app/admin/campaigns/admin-campaigns-api.ts:136,151-158` — the two closest Admin capability APIs on this program — both use exactly:

```ts
function fail(status: number, body: unknown): AdminResult<never> {
  if (status === 401 || status === 403) {
    return { ok: false, message: mapAdminErrorMessage("admin.authorization.denied", "fa"), denied: true };
  }
  const code = parseAdminProblemErrorCode(body, status);
  return { ok: false, message: mapAdminErrorMessage(code, "fa") };
}
```

`ADMIN_ERROR_MESSAGES["admin.authorization.denied"].fa` is `"دسترسی مجاز نیست."` — byte-identical to the string the menus file hardcoded — so the canonical path keeps the same customer-facing text while removing the local copy. The Admin surface is Persian (its own guard test asserts Persian labels), and `"fa"` is the canonical locale used by every sibling API, so **no new i18n behaviour or product decision was required and none was invented**.

The `./routing`-style owners are untouched: `adminHeaders`, `parseAdminProblemErrorCode`, `mapAdminErrorMessage` and the error-code inventory were **not** modified (no competing contract, no shared-helper refactor).

## 3. Exact change — one production file

`src/frontend/app/admin/menus/admin-menus-api.ts`:

```diff
 async function readJson(path: string, init?: RequestInit): Promise<{ status: number; body: unknown }> {
-  const response = await fetch(path, { ...init, headers: { ...adminHeaders(Boolean(init?.method && init.method !== "GET")), ...(init?.headers ?? {}) } });
+  const response = await fetch(path, { ...init, headers: { ...adminHeaders(), ...(init?.headers ?? {}) } });

 function fail(status: number, body: unknown): AdminResult<never> {
-  const code = parseAdminProblemErrorCode(body);
-  return { ok: false, message: mapAdminErrorMessage(code, status === 401 || status === 403 ? "دسترسی مجاز نیست." : "درخواست منو انجام نشد."), denied: status === 401 || status === 403 };
+  if (status === 401 || status === 403) {
+    return { ok: false, message: mapAdminErrorMessage("admin.authorization.denied", "fa"), denied: true };
+  }
+  const code = parseAdminProblemErrorCode(body, status);
+  return { ok: false, message: mapAdminErrorMessage(code, "fa") };
 }
```

**Why `adminHeaders()` and not `adminHeaders(init?.headers)`:** the sibling modules pass `init?.headers as Record<string, string>`, which is an unsafe cast (`HeadersInit` also admits `Headers` and `string[][]`) and is explicitly forbidden by this task. The request-header merge is already performed explicitly by the retained `...(init?.headers ?? {})` spread, so every call site keeps its exact previous wire headers (`Accept`, dev-actor header) and no request gains or loses a header. `adminHeaders(Boolean(...))` previously spread a primitive, contributing no keys — so this is **wire-neutral**.

**Behaviour delta (intended, and the point of the repair):** for non-401/403 failures the previous code produced `admin.http.undefined` and then passed it as the *locale*, so every server error degraded to the generic `UNKNOWN_FA` (`"خطایی رخ داد. لطفاً دوباره تلاش کنید."`). The canonical contract now extracts the real code with the HTTP status, so the already-shipped **menu-specific** inventory (`menu.title.required`, `menu.item.cycle`, `menu.url.unsafe`, `menu.header.ineligible`, … at `admin-error-map.ts:861-876`) finally resolves to its localized Persian message. `denied: true` is preserved for 401/403, and nothing technical (`Bad Request`, raw codes) can reach the UI.

## 4. TypeScript — measured, one check

`cd src/frontend && npx tsc --noEmit` against the committed baseline copied from `docs/architecture/evidence/TB-FE-MIDDLEWARE-LOCALE-TYPE-IMPORT-001/typecheck-after-tsc.txt`:

| | Errors |
| --- | --- |
| Before (baseline) | **41** |
| After | **38** |
| Delta | **−3** (expected `41 → 38`) |
| New diagnostics | **0** |
| `Compare-Object` of both outputs | no added line |
| Diagnostics left in `app/admin/menus/**` | **none** |

Exactly the three recorded diagnostics were removed; no unexpected or relocated diagnostics. Overall `typecheck` remains **not green (38 errors)** and no greenness is claimed — the remaining clusters belong to other bounded tasks.

## 5. Tests actually executed

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/menus/admin-menus-api.test.ts app/admin/menus/admin-menus.guard.test.ts` | **8/8 pass, 0 fail** |
| `node --experimental-strip-types --test app/admin/menus/admin-menus.guard.test.ts app/admin/admin-error-map.test.ts app/admin/admin-api.test.ts` | 14 pass / **1 pre-existing failure** (below) |

`admin-menus.guard.test.ts` is the existing, directly relevant architecture guard for this module and passes unmodified (routes `/v1/admin/menus` and `/v1/admin/menus/header` intact).

### 5.1 Focused regression test added — justification

`app/admin/menus/admin-menus-api.test.ts` (new, 5 tests) is the **only** test-file change. It was necessary because this repair is not a pure type-level move: it changes the **user-facing error path and the request-header construction**, which had **zero** existing coverage, so the alternative was to claim correctness from a green compiler alone — precisely what the Architect forbade. It uses the repository's existing `node:test` + `node --experimental-strip-types` harness (no new harness, no production seam, no export added) and stubs `globalThis.fetch`, then asserts the shared contract: admin headers present on the request, menu mutations still `POST /v1/admin/menus`, 403 → `denied: true` + `"دسترسی مجاز نیست."`, `menu.item.cycle` → `"چرخه در درخت منو مجاز نیست."`, and unmapped `Bad Request` → generic message with no technical leak. It adds **0** new TypeScript diagnostics.

### 5.2 Pre-existing failure — recorded, not chased

`app/admin/admin-error-map.test.ts:111` (`content article update and media errors are humanized`) fails on `content.category.language_mismatch`: actual `"زبان دسته‌بندی باید با زبان مقاله یکسان باشد."` vs expected `"دستهٔ انتخاب‌شده با زبان مقاله هم‌خوان نیست."`. **This is pre-existing and independent of this change:** the test imports only `admin-error-map.ts`, and `git status` shows the only modified production file is `admin-menus-api.ts`; the drift lives in the message map, outside this task's allowed scope. Per the task ("record it, do not chase it") it was left untouched.

**Tests NOT run:** the full `npm test`/`test:admin*`/`test:critical-storefront` suites, any build, backend tests, browser/live verification, and any AMSC wave. No unrun test is reported as pass.

## 6. Scope, risks, rollback

- **Scope:** production change is limited to `src/frontend/app/admin/menus/admin-menus-api.ts`; plus the new focused test and this evidence/receipt. No backend, schema, tsconfig, baseline, guard, Shopeiva surface, or unrelated Admin module was touched. No `any`, `@ts-ignore`, cast or suppression was used.
- **Residual risk / observation for the Architect (not fixed — outside the three diagnostics):** menu `POST`/`PUT` calls still send a JSON string body **without** `Content-Type: application/json`; the previous `adminHeaders(Boolean(...))` also contributed no such header, so this is unchanged behaviour, but it is a plausible latent Host-415 risk that a *separate* bounded task should decide on. Adding it here would have changed request semantics beyond the requested repair.
- **Rollback:** revert the single commit; the change is two source hunks plus one new test.

## 7. Stop state

`automaticNextImplementationTask = NONE`. 38 frontend diagnostics remain in the other audited clusters and require their own bounded tasks.
