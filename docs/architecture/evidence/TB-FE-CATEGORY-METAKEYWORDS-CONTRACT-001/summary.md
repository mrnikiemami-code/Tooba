# TB-FE-CATEGORY-METAKEYWORDS-CONTRACT-001 — Category meta-keywords helper contract evidence

**Mode:** `IMPLEMENT_BOUNDED_REPAIR` · **Branch:** `main`
**BASE / expected starting HEAD:** `a4703078913759304f4fc2289c8480783d975317` (verified `HEAD == origin/main == Expected-Starting-HEAD` at preflight)

## 1. Source proof — what actually broke

The UI and the test both referenced a named helper contract that **was never present at the owner**:

- `app/admin/category-admin-screen.tsx:40,42` imports `parseMetaKeywords` and `serializeMetaKeywords` from `./catalog-category-api.ts`.
- `app/admin/category-admin-form.test.ts:346,347` asserts the API module contains `parseMetaKeywords` / `serializeMetaKeywords`, and line 364 imports both from that module.
- Real consumers that need them: `MetaKeywordsViewChips` (view-only chips, line 339) and `MetaKeywordsTagInput` (removable tag chips, lines 372–382) — both live in the screen, next to the mandatory data-testids `translation-edit-meta-keyword-chips` / `translation-edit-meta-keyword-remove-`.
- The API/DTO module owns the field: `CategoryTranslationDto.metaKeywords: string | null` (`catalog-category-api.ts:32`), mapped from Host at `:279-282` (empty ⇒ `null`) and written back at `:473` (`input.metaKeywords ?? null`).

Proof the helpers were never committed anywhere (so this is a missing contract, not a regression from a rename):

- `git log --all -S "export function parseMetaKeywords"` → **no commits**.
- `git log -S "parseMetaKeywords" -- src/frontend/app/admin/catalog-category-api.ts` → **no commits**.
- Repository-wide search for any other definition → **none**.

So the correct fix is to create the contract at its true owner — the same module that owns `metaKeywords` as `string | null` — rather than to move the UI, change the DTO, or weaken the test.

**Expected semantics were already specified by the existing test** (`category-admin-form.test.ts:363-368`), so nothing was invented:

```ts
parseMetaKeywords("موبایل، گوشی, موبایل\nسامسونگ") === ["موبایل", "گوشی", "سامسونگ"]
serializeMetaKeywords(["موبایل", "گوشی"]) === "موبایل, گوشی"
parseMetaKeywords("") === []
```

i.e. split on Persian/Arabic comma + ASCII comma + newline, trim, drop blanks, drop duplicates keeping first occurrence (stable order), and serialize with `", "`.

## 2. Patch — one production file, nothing else

`src/frontend/app/admin/catalog-category-api.ts` (only changed file):

- `META_KEYWORDS_SEPARATORS = /[,\u060C\r\n]+/` — the accepted input separators (ASCII comma, U+060C Persian/Arabic comma, LF, CR).
- `META_KEYWORDS_JOINER = ", "` — the canonical serialized separator.
- `normalizeMetaKeywords(tags)` (module-private) — trim, drop blanks, dedupe with stable first-occurrence order. Single owner of the normalization rule.
- `export function parseMetaKeywords(value: string | null | undefined): string[]` (line 539) — accepts exactly the DTO shape plus the `""`/`undefined` cases the UI passes; `!value ⇒ []`.
- `export function serializeMetaKeywords(tags: readonly string[]): string` (line 548) — canonical output. Because serialization normalizes as well, `parseMetaKeywords(serializeMetaKeywords(tags))` is idempotent with the normalization of the input, so the persisted string can never accumulate leading/trailing separators, blank entries or duplicates across edit cycles.

Compatibility check against every existing caller (all unchanged, all satisfied):

| Caller | Effect |
| --- | --- |
| `MetaKeywordsViewChips` (`value: string \| null \| undefined`) | `parseMetaKeywords` accepts all three shapes; `[]` renders the existing `—` state. |
| `MetaKeywordsTagInput` initial tags | `parseMetaKeywords(value)` on the `string` draft. |
| `commitDraft` (`:375`) | `parseMetaKeywords([...tags, ...parseMetaKeywords(raw)].join(","))` — ASCII-comma join is one of the accepted separators, so the round trip is exact. |
| `removeTag` (`:382`) | filter-then-serialize, unchanged. |
| `:1684` save path | `serializeMetaKeywords(parseMetaKeywords(...)) \|\| null` still yields `null` for an empty set, so the **wire payload stays `string \| null`**. |

`src/frontend/app/admin/category-admin-screen.tsx` was **not modified** — its import now resolves and the three implicit-`any` parameters infer from the helper's `string[]` return type. The permitted "only if truly necessary" file was correctly not needed.

**No backend, schema, migration, DTO, locale, Home/PDP or locked-surface change.** The Host contract `metaKeywords` (string) and the DTO `string | null` are untouched; no new backend structure was invented.

## 3. Measured diagnostics

`cd src/frontend && npx tsc --noEmit` (TypeScript 5.9.3):

| | Errors |
| --- | --- |
| Recorded baseline | **49** |
| After this task | **42** |
| Removed | **7** |
| **Added (regressions)** | **0** |

Machine diff (file+line+column+code) → `removed=7 added=0`. The 7 removed are exactly the predictable set:

| Diagnostic | Kind |
| --- | --- |
| `category-admin-screen.tsx:40` `TS2305` missing export `parseMetaKeywords` | the two missing exports |
| `category-admin-screen.tsx:42` `TS2305` missing export `serializeMetaKeywords` | |
| `category-admin-form.test.ts:364:11` `TS2339` `parseMetaKeywords` | the two test import/member errors |
| `category-admin-form.test.ts:364:30` `TS2339` `serializeMetaKeywords` | |
| `category-admin-screen.tsx:345` `TS7006` `tag` implicitly `any` | the directly causal implicit-any diagnostics (view chips map, edit chips map, filter predicate) |
| `category-admin-screen.tsx:391` `TS7006` `tag` implicitly `any` | |
| `category-admin-screen.tsx:382` `TS7006` `t` implicitly `any` | |

No diagnostic remains in `catalog-category-api.ts` or `category-admin-form.test.ts`. The single remaining `category-admin-screen.tsx` diagnostic (`:1801` `TS2322` `EditSurface`) is a **different, non-causal cluster** (category edit-surface union) and was deliberately left untouched.

Overall `typecheck` is **NOT green and is not declared green** — 42 errors remain in the other audited clusters.

## 4. Tests actually run

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/category-admin-form.test.ts` | **24/24 pass, 0 fail** |
| `node --experimental-strip-types --test app/admin/catalog-category-api.test.ts app/admin/category-attributes-panel.test.ts app/admin/category-products-panel.test.ts app/admin/catalog-tag-api.test.ts` | **29/29 pass, 0 fail** |
| `npm run test:architecture` | **11/11 pass, 0 fail** |

The previously impossible contract test now passes for real: `✔ parseMetaKeywords splits commas and drops blanks/duplicates`, alongside `✔ translation meta keywords use removable tag chips not plain textbox` and `✔ API client exposes updateCore, upsertTranslation with full fields, move`.

**Pre-existing failures: none observed in the focused set** — all three commands were fully green, so nothing had to be recorded as a baseline failure and no retry loop was run.

**Tests deliberately NOT run / not claimed:** `npm run test:discovered` (full suite), `npm run test:admin`, `npm run test:category-tree`, `npm run test:product-workspace`, `npm run test:critical-storefront`, `npm run build`, backend build/tests, browser/live verification, any AMSC wave. No unrun test is reported as PASS.

**Critical-storefront scope note:** only one Admin API module was modified. No Home, PDP, listing, storefront or shared-composition surface and no design-system primitive was touched, so no locked visual contract is in scope.

## 5. Risks and rollback

- **Residual risk: very low.** One additive change in one Admin API module. Both helpers are pure functions with no I/O, no state and no dependency beyond a regex.
- **Deliberate contract decision (no guessing):** `serializeMetaKeywords` normalizes rather than blindly joining. Every existing caller already passes pre-normalized tags, so observable behaviour is unchanged for all of them; the normalization only makes the persisted `string | null` payload canonical and stable. Trim/dedupe semantics were taken from the existing test, not invented.
- **Rollback:** revert the single commit; it touches `catalog-category-api.ts` plus evidence/task docs, with no schema, migration or persisted-state change.

## 6. Stop state

`automaticNextImplementationTask = NONE`. 42 diagnostics remain in the other audited clusters (notably `shared-composition-renderer` 9, `admin-create-shipment-modal` 4, `menus/admin-menus-api` 3) and require their own bounded tasks and Architect decisions.
