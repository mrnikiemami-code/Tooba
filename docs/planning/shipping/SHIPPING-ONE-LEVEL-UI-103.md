# SHIPPING-ONE-LEVEL-UI-103 — scratch Catalog bootstrap prerequisite (PARTIAL)

Task `TB-SHIPPING-ONE-LEVEL-TABS-103` (parent `TB-SHIPPING-ONE-LEVEL-TABS-102`) · Channel `tooba-main`
Mode `BOUNDED_CATALOG_SCRATCH_BOOTSTRAP_PREREQUISITE_REPAIR` · Stop-State `STOP_FOR_ARCHITECT`
**Status: PARTIAL** — no production source changed (as required when no provable defect was proven), the official
authorized seed path was exercised on a genuinely isolated scratch stack and reached 545 publish-ready Draft
products, but it did not produce a sellable offer inside the timebox.

## 1. Preflight

| Check | Result |
| --- | --- |
| Claim | exactly one Pending task (`07aeb343-…`) via `GET /api/tasks/next?channelId=tooba-main`; receipt kept **UNSTAGED** |
| Base | `Expected-Base-SHA` `cbbdc7cbe…` == `HEAD` == `origin/main`; `git merge-base --is-ancestor` true; zero intervening commits; `src/` shows only the two pre-existing dirty frontend screens — untouched |
| Shared stack | read-only: `:5088` `/health` 200, gate unchanged, `:3000` untouched, `tooba_alpha` 7 methods / 14 translations / 50 carts / 78 lines / 14 drafts / 26 seller orders |

## 2. Root cause of the 102 crash (from source, not guessed)

The 102 crash is the seed's own `PublishProductAsync` call
(`CatalogAttributeSchemaDevelopmentSeed.ApplyAsync`, line 99) being rejected by the aggregate publish-readiness
gate: `CatalogDirectory.UnwrapLifecycle` maps the `WorkspaceProductPublishRejected` code to
`ProductPublishRules.MessageNotReadyFa`. The gate is
`ProductPublishReadinessReader.GetAsync`, which evaluates six independent prerequisites — category level-3 primary
membership, localized identity, required attributes, variant validity, media (≥1 primary), and SEO (valid slug +
title/fallback + description + identity). Reading the seed against those six checks shows the *specific* failing
prerequisite cannot be proven from static reading alone: the seed assigns a published level-3 leaf, sets the
required non-axis `screen_size` value, creates two value-complete variants, attaches a first (primary) media
reference, and calls `ProductPublishPrep.EnsureMinimalSeoForPublishAsync` while the product carries a valid
`SlugSeam`. Because the seed **returns early when the demo product already exists**
(`db.Products.AnyAsync(p => p.SlugSeam == DemoProductSlug)`), this create-then-publish branch has never been
exercised on the shared Development database — the defect is latent there and only manifests on a truly empty
database.

Consequently no source edit was justified: the task authorises a source change only where source *proves* a genuine
defect, and a blind change would have been a guess. The supported alternative in the task — the official authorized
Development demo seed path — was exercised instead.

## 3. Mission A — official authorized seed path on isolated scratch

Isolation was rebuilt exactly as in 102/099 but on a new disposable container: `tooba-103-scratch-pg`
(`postgres:17`, loopback `127.0.0.1:55443`, no volume) with `tooba_103_scratch` / `tooba_103_messaging`; an
isolated Host from current code on `http://127.0.0.1:5292` with provenance
`isolated-103 / store-alpha / tenant-103-scratch`. `CatalogDemo.AllowResetAndSeed=true` with
`RunLegacyBootstraps=false` so Host startup itself does not hit the 102 defect.

Real admin credential login on the isolated Host then drove the **authorized Development seed API**:

| Call | Result |
| --- | --- |
| `POST /v1/auth/login` (`admin-actor@tooba.local`) | `200`, real session — no header bypass |
| `GET /v1/admin/catalog/demo/status` | `200` — all counts `0`, `allowResetAndSeed=true`, `environment=Development` |
| `POST /v1/admin/catalog/demo/reset-and-seed` | **client-side abort at the 300 s HTTP header timeout** (Node undici default) |
| server-side scratch state when it stopped | **545 Draft products, 116 categories, 41 attribute definitions**, `offers=0`, `prices=0`, `stock=0` |

So the official path is real and does run on an empty scratch database — it got past everything the
attribute-schema seed failed on and produced 545 Draft publish-ready products — but the longest phase (the
sellable enricher that creates offers, prices and inventory) had not run when the request was aborted. Aborting the
HTTP request cancels the server-side token, so the seed stopped rather than completing in the background. That is
the precise remaining gap: **the reset+seed operation must be allowed to run to completion without a client-side
abort** (long-timeout/background invocation, or the narrower `POST /v1/admin/catalog/demo/seed-only`), and only
then can an offer be published and put in a cart.

Because cleanup takes precedence over finishing a proof and the 30-minute HARD timebox had expired, the task
stopped here instead of starting a second multi-minute seed run.

## 4. Mission B — not reached

No sellable offer existed (0 offers / 0 prices / 0 stock), so no cart line could be added and the optional
gate-OFF vs gate-ON storefront label comparison was legitimately skipped.

## 5. Cleanup and shared-state parity

- Isolated Host stopped, `5292` listeners **0**; scratch container removed (only pre-existing `rabbitmq` and
  `postgres-db` remain); task-owned run/output dirs and untracked credential files deleted; all scratch
  identifiers/credentials redacted from committed artifacts (secret scan 0).
- Shared stack untouched: `:5088` `/health` 200, provenance `dev-singlestore / store-alpha / tenant-alpha`, gate
  unchanged, `:3000` never restarted, `tooba_alpha` parity identical to preflight (7/14/50/78/14/26). **No shared
  DML.** No production source, schema, migration, config or frontend change.

## 6. Recommendation to the Architect

1. Re-run this exact isolated recipe but invoke the seed so it is not aborted: a Node script with
   `AbortSignal.timeout(0)`/no timeout and a large `undici` `headersTimeout`, or drive
   `POST /v1/admin/catalog/demo/seed-only` (no reset phase) — the observed partial state shows the seed needs
   longer than 5 minutes to reach the enricher.
2. Only if a later task still needs the *attribute-schema* demo branch should the publish prerequisite be
   instrumented: the cheapest next step is to log the readiness `missing[]` entries for the demo product on a
   scratch database (a diagnostic, not a blind fix), which will name the one failing prerequisite exactly.
3. Minor observation carried from 102: a duplicate identity registration returns `500 platform.unexpected`
   (`IdentityDuplicateIdentifierFault`) instead of a typed conflict.
4. Elapsed: claimed ≈03:24 local, stopped ≈04:01 local ≈ **37 minutes** — a disclosed overrun of the 30-minute HARD
   timebox; the environment was fully destroyed and every artifact was captured before reporting.
