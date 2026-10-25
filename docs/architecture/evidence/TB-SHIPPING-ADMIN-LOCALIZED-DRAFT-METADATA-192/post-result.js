const fs = require('fs');

const base = 'cb3b52908d7cc88aba26d8d37052289b6abfcd81';
const code = 'd300dfc57a2c2531a01e9827f94b96a62da657ad';

const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Parent-Task: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DB_LANGUAGE_KEYED_OWNER_DRAFT_PRESENTATION_METADATA
Bridge-Task-Row: b7029832-59b7-4a55-86e0-8e0bb5d8b177
Status: PASS
Expected-Base-SHA: ${base}
Base-SHA: ${base}
Code-SHA: ${code}
Evidence-SHA: ${code}
HEAD-Origin-Parity: HEAD == origin/main == ${code}; clean tracked tree after commit
Owner-Local-Presentation-And-Catalog-Preservation: PASS - own tables authoring_draft_translations / authoring_group_titles keyed by real LanguageId; no Fulfillment catalog row mutated, cloned or seeded; no cross-schema FK/navigation/join; LanguageId is validated through Contracts facts, never a DB FK.
Authoritative-Language-Source-And-OneTwoThreePlus: PASS - unchanged TB-188 IShippingAuthoringContextSource/ILanguageLookup; its readiness projection extracted once into ShippingAuthoringContextProjection and reused (context query behavior/codes unchanged, its tests pass). No fa/en array, no hardcoded default/order/direction. Fixtures exercised one active non-fa, two mixed RTL/LTR and three-plus with a non-first default plus an inactive stored language.
Selected-Language-Only-Patch-And-TitleClear: PASS - each entry replaces only its own LanguageId; missing entry = NO CHANGE; [] = valid no-op; duplicate/inactive/unknown ids, null elements, blank/bounded text rejected before mutation; explicit null/empty description or title clears only that selected pair; no whole-dictionary replacement.
Inactive-Text-Retention-Default-And-Truthful-Completeness: PASS - stored text for an inactive language is retained and returned truthfully; GET exposes the ACTUAL active registry + default id + HasDefaultLanguageName computed from stored content, never a fallback name. No background delete or auto-translation.
InterfaceLocale-Versus-ContentLanguage: PASS - content is selected only by explicit persisted LanguageId; no locale/Accept-Language/query-string language selection; fa/en interface switch leaves raw text, registry ids and rules identical.
Authorized-API-And-Foreign-Identity-Denials: PASS - GET/PATCH .../presentation under /v1/admin and /v1/seller; ONE Application query + ONE command; fulfillment.view on read and fulfillment.manage on write independently and before dispatch; body has no owner/mode/tenant/currency; foreign/unknown draft or group maps to the same safe not-found with no data change; single-store seller unavailability path unchanged.
Same-Context-Preflight-And-Root-Revision-Race: PASS - full preflight before the first tracked mutation; one implicit SaveChanges with the SAME root revision token; no new BeginTransaction; no-op documents do not bump revision; deterministic two-reader race (presentation PATCH vs rate replacement on revision 1) leaves exactly one winner, canonical 409 loser and winner-dependent state with no hybrid and an empty loser tracker.
RegionRate-Preservation-And-GroupTitle-Cascade: PASS - region replacement/reorder and rate writes preserve method text and retained-group titles; removing a group cascades only its own titles; exact stored regions/rates/currency verified after metadata writes.
Additive-Migration-And-Postgres-Roundtrip: PASS - one additive migration 20261022081351_AuthoringPresentationTranslations (+designer+snapshot); historical migrations byte-identical; composite PK uniqueness, own cascade FKs and non-null CHECKs (blank/over-length) on a real PostgreSQL 17 container; migration-forward.sql persisted.
Contracts-DI-Tracing-Validation-Error-And-Guard-Parity: PASS - Contracts-only boundaries, canonical Endpoints/ISender/CQRS, no new package/project or foreign reference; two new stable codes with exactly one descriptor + neutral/fa resources; request classification manifest updated with exact routes/requests/validators (35 routes / 26 requests) and certification guards pass unchanged.
Configured-Registry-Source-Versus-Executed-Proof: LIMITATION - language ids in tests are controlled Contracts fixtures; they prove handler/domain/persistence behavior and lawful wiring, NOT a live merchant Localization database or browser session. Configured-registry runtime/UI acceptance remains a later integration gate.
Focused-Commands-And-Test-Counts: dotnet build Shipping.Tests 0 errors; focused new relational tests 2/2 passed; whole Tooba.Shipping.Tests 579/579 passed, 0 failed, 0 skipped, 12 s on isolated 127.0.0.1:55432 PostgreSQL 17.
Changed-Files: Contracts (draft contracts, error codes); Domain (ShippingAuthoringDraft.cs, new ShippingAuthoringPresentation.cs); Application (presentation query/command, shared context projection, port, view models, validators, context query); Infrastructure (directory, DbContext, one migration+designer+snapshot); Endpoints (admin/seller routes, error catalog, neutral/fa resources); Tests (relational x2, model/classification/catalog/route-count expectations); docs (rules decision, recovery pointer, task artifact, evidence RESULT + migration SQL).
Claim-UTC: 2026-10-22T08:07:08Z
Result-UTC: 2026-10-22T08:50:00Z
Elapsed-Minutes: 43
Hard-Deadline-Compliance: PASS - implementation, tests, cleanup, commit, push and report inside the 45-minute hard bound.
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: fresh task-owned postgres:17-alpine container on 127.0.0.1:55432 removed; port released; no shared :3000/:3419/:5088/:5432, Redis, RabbitMQ or shared bin/obj/.next/cache touched.
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT`;

(async () => {
  const response = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ channelId: 'tooba-main', taskId: 'TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192', content }),
  });
  console.log('RESULT', response.status, (await response.text()).slice(0, 200));
  if (response.status >= 200 && response.status < 300) {
    const done = await fetch('http://127.0.0.1:17321/api/tasks/b7029832-59b7-4a55-86e0-8e0bb5d8b177/complete', { method: 'POST' });
    console.log('COMPLETE', done.status, (await done.text()).slice(0, 200));
  }
})();

