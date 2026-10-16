# SHIPPING-ONE-LEVEL-UI-105 — Catalog publish-readiness diagnostics

Task `TB-SHIPPING-ONE-LEVEL-TABS-105` (parent 104) · Channel `tooba-main`
Mode `BOUNDED_CATALOG_PUBLISH_READINESS_DIAGNOSTICS` · Base-SHA `222628615f7769ad276c1b3b6e76dc9c6e3dcd8b`
Stop-State `STOP_FOR_ARCHITECT` · **Status: PARTIAL** — the diagnostic is implemented, verified and green; the exact
`Missing[]` code that rejects the schema demo product on an empty database is **NOT YET OBSERVED** (deliberately: its
only reproduction starts the full Development legacy bootstrap, which this task forbids).

Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-105/`.

## 1. Where the codes were being lost

`ProductPublishReadiness` (Catalog `Application.Models`) carries six booleans plus
`IReadOnlyList<ProductPublishMissingRequirement>` — a `(Code, MessageFa, WorkspaceTab)` record whose **only**
possible codes are `category`, `identity`, `attributes`, `variants`, `media`, `seo`. There is deliberately no
commercial code (`ProductPublishReadinessReader` says so explicitly).

`ProductLifecycleDirectory.PublishAsync` collapses every refusal into a coarse `SemanticError`
(`WorkspaceProductPublishRejected`, lines 48–62), and `CatalogDirectory.UnwrapLifecycle` (line 849) then throws
`InvalidOperationException(ProductPublishRules.MessageNotReadyFa)`. That is exactly where the six real reasons become
unrecoverable — and it is a **validation** refusal, not an infrastructure failure: the reader had already computed the
reasons one frame earlier, and the existing `GetProductPublishReadinessAsync` contract returns them as a *success* with
`IsReady=false`.

So **no new contract, rule, persistence or configuration was needed** — the fix is a re-read of an existing contract.

## 2. The change (smallest Catalog-owned Development-only seam)

New `CatalogDevelopmentPublishDiagnostics` (`Catalog.Infrastructure/Development`) exposes
`PublishWithDiagnosticsAsync`: it performs the ordinary publish and, **only** when
`IHostEnvironment.IsDevelopment()` **and** the publish is refused, re-reads
`GetProductPublishReadinessAsync(productId, "fa-IR")` and emits one structured `LogError` with the seed stage, the
non-sensitive product id and the real missing-rule codes — then `throw;`s the **original** exception unchanged.

It cannot mask the refusal (a secondary readiness failure degrades to `readiness=unavailable:{Type}`), cannot
force-publish, touches no persistence, changes no HTTP contract, and emits nothing outside Development. The output is
technical keys plus rule **codes** only — no entity payloads, tokens, connection strings, customer data or new
user-facing prose in any language.

One call site changed: the single `PublishProductAsync` in `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` (old
line 99, the exact frame in 102's crash stack) now goes through the helper with stage
`catalog-dev.attribute-schema.publish-product`. The enricher's second publish site (line 67) was reported but left
untouched to keep the diff minimal.

## 3. Proof (3 focused tests, all green)

`CatalogDevelopmentPublishDiagnosticsTests` — `Passed! Failed: 0, Passed: 3, Skipped: 0`:

1. the pure projection contains only the codes it is given, and no `offer`/`price`/`stock` token;
2. on a **real** Postgres `CatalogDirectory`, a real rejected product still refuses with the same exception type and
   the same public message, stays Draft, and the emitted line contains exactly the codes the **real reader** returns
   for that product (verified true codes — not invented strings, not a Hardcoded copy);
3. the identical scenario with `IHostEnvironment = Production` throws identically and logs **nothing** — the
   diagnostic is disabled/redacted outside Development.

## 4. Pre-existing failure found and attributed (not repaired — out of scope)

`ProductPublishTests.Aggregate_readiness_and_lifecycle_gates_without_offer_dependency` fails at line 132: it expects
the aggregate's `MessageRestoreBeforePublishFa` for an archived→publish attempt, but
`ProductLifecycleDirectory.PublishAsync` (lines 48–51) short-circuits Archived with
`WorkspaceProductPublishRejected`, which maps to `MessageNotReadyFa`; the aggregate message is now unreachable via the
legacy seam. The test predates the lifecycle refactor (test 2026-08-29 `adaf7f01e`, lifecycle 2026-10-03 `5348b1988`).
**Empirically proven independent**: with this task's seed edit reverted to HEAD and the test project rebuilt, the same
assertion fails identically (`preexisting-failure-control-run.txt`). It is a lifecycle message-contract issue, not a
readiness-diagnostic issue, so it was reported rather than widened into.

## 5. Handoff — the one remaining observation

The exact code rejecting the schema demo product is still unobserved, and it is now cheap and safe to obtain: start an
isolated scratch target exactly as in 103 but with `Tooba:CatalogDemo:RunLegacyBootstraps=true`; the new diagnostic
prints `stage=catalog-dev.attribute-schema.publish-product; product=…; isReady=False; missing=[…]` **before** the host
aborts. One bounded run yields the fact; P2 of the 104 proposal (a bounded Development-only sellable fixture entry
point reusing only existing public application commands) then becomes a targeted fix instead of a design guess.

Nothing here claims storefront, Checkout or commercial shipping readiness.
