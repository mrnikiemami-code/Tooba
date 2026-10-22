const base = '87ae74549d34e941a9c7d76ff61eb1f28ce51568';
const code = 'df743ddf160e45c64a62998389c0a04477ce60cb';

const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1
Parent-Task: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_PRESENTATION_ENDPOINT_AND_DYNAMIC_LANGUAGE_PROOF_COMPLETION
Bridge-Task-Row: d8a5ac2f-1d6c-4469-85fa-16eed099f1dd
Status: PASS
Expected-Base-SHA: ${base}
Base-SHA: ${base}
Code-SHA: ${code}
Evidence-SHA: ${code}
HEAD-Origin-Parity: HEAD == origin/main == ${code} after push and fetch; only unrelated historical untracked artifacts left in the tree, nothing staged.
Real-Presentation-Endpoint-Pipeline-And-Authorizer: PASS — 8 new facts execute the real admin and seller /authoring-drafts/{draftId}/presentation GET/PATCH delegates from MapShippingEndpoints, the real ShippingAuthoringOwnerAuthorizer (admin + seller-panel + effective-access seams), the real body reader, ValidationBehavior, the presentation query/command handlers, ShippingAuthoringContextProjection, ShippingAuthoringDraftDirectory over ShippingDbContext, ApiResponseFactory and the error catalog. Only platform auth seams and the Contracts language/commerce sources are controlled fixtures. Dispatch and execution are measured separately (SentCount behavior before ValidationBehavior, HandlerReached after it).
OneTwoThreePlus-Presentation-Matrix-And-Adapter-Composition: PASS — one active NON-fa default; two active mixed rtl/ltr with a second PATCH leaving the first language byte-identical; four known / three active with a NON-first default plus stored text for an inactive language. Each case PATCHes then GETs and asserts exact raw text, group titles per (group,language), contentLanguages identity/order, defaultContentLanguageId and hasDefaultLanguageName. Every case runs the production ShippingAuthoringContextSource with a controlled ILanguageLookup + ICurrentStoreCommerceContext (its call counter advances), so the lawful adapter path is exercised; fixtures are labelled fixtures, not a live merchant Localization database.
Deactivate-Reactivate-Default-Change-And-Raw-Text-Retention: PASS — against the SAME stored draft: deactivate a written language (raw text still returned, excluded from active registry), PATCH that inactive id -> 400 shipping.authoring.presentation_language_invalid with unchanged revision, reactivate -> EXACT previous text with no auto-translation, change default to a language with no stored name -> hasDefaultLanguageName=false while order follows owner SortOrder, then writing that default flips completeness truthfully.
Selected-Clear-NoOp-And-Interface-Culture-Invariance: PASS — explicit null description + null title clear only that (language|group,language) pair, the other entry and title stay byte-identical; []/[] and an identical active entry are true no-ops (revision AND UpdatedAt unchanged, read from the same context); stale expectedRevision -> 409 shipping.authoring.draft_revision_conflict with no change. GET/PATCH under fa-IR and en-US current culture (+Accept-Language) in a restored scope: payload byte-identical and the write still targets exactly the selected LanguageId; interface locale never supplies content language.
Owner-Permission-SingleStore-And-PreHandler-Denials: PASS — admin view-only PATCH -> 403 admin.authorization.denied with SentCount==0, HandlerReached==0 and zero registry reads; another store GET/PATCH -> 404 authoring.draft_not_found (same safe answer); invented group id -> 404 authoring.region_group_not_found with no partial edit; Marketplace seller GET/PATCH succeed through the real seller route; a scoped Category grant -> 403 authoring.owner_scope_denied before dispatch; SingleStore seller GET/PATCH -> 404 authoring.owner_scope_unavailable with SellerCalls==0 and SentCount==0.
Malformed-Input-And-Same-Context-Preflight: PASS — 13 shaped failures through the real body reader and FluentValidation (omitted/null methodTranslations or groupTitles, null elements, duplicate language, duplicate (group,language), Guid.Empty language, blank name, over-length name/description/title) return validation.failed with the exact canonical code while SentCount>0 and HandlerReached==0, registry reads==0 and revision unchanged. An inactive well-shaped id reaches the handler and is rejected with the stable language code. A valid FIRST entry followed by a bad LAST group id leaves the SAME context instance with an empty ChangeTracker, a subsequent SaveChanges publishing nothing, and the following valid PATCH succeeding.
Translation-Storage-Unique-FK-NotNull-Check-Negatives: PASS — independent PostgreSQL 17 matrix on a fresh task-owned container, each attempt in its own rolled-back transaction: duplicate (draft,language)/(group,language) -> 23505 with pk_authoring_draft_translations / pk_authoring_group_titles; invalid own parent -> 23503 with the exact fk_* name (no cross-schema Localization FK exists); NULL name/title -> 23502 with the column identity; blank '   ' -> 23514 with ck_*_length; over-length -> 22001 from the same owned 200/2000/200 varchar bounds (CHECK cannot fire first); valid Unicode method text and group title persist on a NEW-context readback; at-limit 200/2000/200 accepted.
Retained-Group-Reorder-And-RateEdit-Text-Preservation: PASS — after method text and group titles were saved: a retained-group REORDER keeps ids so both languages' method text and both titles survive; a later rate edit on the same group (3900 / IRR) preserves the text; the new order and freight are re-read from a new context, so the retention is not a stale read.
Reused-Versus-New-Executed-Proof: REUSED unchanged and still valid for their narrower scope — TB-192 Presentation_patch_touches_only... and the same-revision presentation/rate race (directory-level with supplied active sets), TB-188 context projection tests, and the historic 579/579. NEW executed proof — the 8 presentation API facts and the 2 PostgreSQL storage/retention facts above. Nothing unexecuted is claimed.
Production-Executable-Schema-Contract-Changes: NONE
Configured-Registry-Source-Versus-Executed-Proof: LIMITATION — language ids/metadata are test-owned Contracts fixtures fed through the real adapter; live merchant Localization DB and browser/UI configured-registry acceptance remain a later integration gate.
Focused-Commands-And-Test-Counts: dotnet build Tooba.Shipping.Tests 0 errors; presentation endpoint facts 8/8; raw storage facts 2/2; ONE final whole module suite with the DB live 589/589 passed, 0 failed, 0 skipped, 14 s.
Changed-Files: ShippingAuthoringPresentationEndpointTests.cs (new), ShippingAuthoringPresentationStorageTests.cs (new), ShippingAuthoringDraft.cs (XML remark only), TB-192 RESULT.md correction, TB-192-R1 evidence RESULT.md, recovery pointer, received task artifact.
Claim-UTC: 2026-10-22T08:26:00Z
Result-UTC: 2026-10-22T08:55:00Z
Elapsed-Minutes: 29
Hard-Deadline-Compliance: PASS — tests, cleanup, commit, push and report inside the 35-minute bound; no new implementation/test started after minute 30.
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: fresh task-owned postgres:17-alpine container (tooba-shipping-tb192r1-pg) on 127.0.0.1:55432 with database shipping_tb190r1 / role tb190r1, live-verified before writes, removed afterwards; port 55432 verified free; no shared :3000/:3419/:5088/:5432, Redis, RabbitMQ, Host start or shared migrator touched; no reset/rebase/stash/force-push.
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
    body: JSON.stringify({
      channelId: 'tooba-main',
      taskId: 'TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1',
      content,
    }),
  });
  console.log('RESULT', response.status, (await response.text()).slice(0, 200));
  if (response.status >= 200 && response.status < 300) {
    const done = await fetch(
      'http://127.0.0.1:17321/api/tasks/d8a5ac2f-1d6c-4469-85fa-16eed099f1dd/complete',
      { method: 'POST' });
    console.log('COMPLETE', done.status, (await done.text()).slice(0, 200));
    const pending = await fetch('http://127.0.0.1:17321/api/tasks?channelId=tooba-main');
    const rows = await pending.json();
    const list = Array.isArray(rows) ? rows : rows.items || [];
    console.log('REMAINING_PENDING', list.filter(x => x.status === 'Pending').length);
  }
})();
