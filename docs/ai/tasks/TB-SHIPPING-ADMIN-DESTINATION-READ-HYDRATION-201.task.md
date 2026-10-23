PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_CONTRACTS_ONLY_DESTINATION_IDENTITY_READ_HYDRATION
Expected-Base-SHA: b1c89e20f495e326a932e9c0b497105b0d89bec6
Hard-Timebox-Minutes: 30
Stop-New-Implementation-And-Tests-At-Minute: 23
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Mission / source-backed need

TB200 frontend foundation is accepted throughR2. Implement only the missing authoritative READ bridge required for the subsequent real UI. Editor GET returns area(kind,id); PUT requires known CountryId and appropriate parents. Current Geography HTTP has only hierarchical lists; IGeographyLookup.GetByIdsAsync takes mandatory CountryId and VALIDATES a supplied tuple, it cannot hydrate an unknown terminal's parents. Do not scan the world in the browser or guess ancestry.

Deliver minimal Geography-owned bulk-by-id Contracts seam over EXISTING snapshots, and Shipping-owned typed destinationFacts enrichment on editor GET and confirmed PUT response (all PUT dependency reads BEFORE its commit). No new Geography HTTP route, schema, frontend, actual destination write or activation.

Read AGENTS/latest recovery/backend feature skill, module-owned architecture/DI and TB200-R2 ARCHITECT-REVIEW. Scope these two related read surfaces only; no general AMSC audit/recertification. Verify expectedbase HEAD==origin/main afterfetch; safe fast-forward only, report source divergence. Preserve unrelated work/stashes, no reset/rebase/stash/force-push.

1. Canonical Geography identity reader (internal Contracts boundary)

Find an existing equivalent bulk-by-key seam FIRST; reuse if it actually returns parent facts. If none, add ONE Geography.Contracts.Places read interface, e.g. IGeographyPlaceSnapshotReader, using existing:
CountrySnapshot(CountryId,CountryCode,IsActive)
AdministrativeAreaSnapshot(AdministrativeAreaId,CountryId,ParentAdministrativeAreaId,Code,IsActive)
CitySnapshot(CityId,CountryId,AdministrativeAreaId?,Code,IsActive)
ServiceAreaSnapshot(ServiceAreaId,CityId,Code,IsActive).

Expose small typed bulk methods for each kind over caller ids, not untyped JSON, ids->label inference, a new catalog or an owner parameter. Implement through OWN GeographyDbContext under module Infrastructure, AsNoTracking/WHERE ids.Contains; emptyids returns[] with no DBquery; dedup requestedids, no paging/worldscan/foreignDbContext/SQLjoin/cache. Preserve inactive facts explicitly; absence legitimate, provider failure/cancellation propagate unchanged, no catch-to[].
Register once with correct scoped lifetime in Geography's own DI. No Domain/Application/Infrastructure reference crosses the module edge; Contracts-only. No new project/package or physical move. Existing GeographyLookup interface/HTTP/legacy resolver unchanged.

2. Shipping editor hydration projection

Add an own Application read port + Infrastructure adapter consuming ONLY the above Geography Contracts, existing IGeographyLabelLookup and canonical module tracer. Place cohesively in current Authoring capability.

Input is the server-owned draft's selected typed keys, or the exact submitted proposed keys after authorized owner/binding checks; not an arbitrary foreign-owner list. Terminal kind+id is the key (same GUID in distinct kinds must stay distinct).
Fetch terminal snapshots in batches, then only required city/admin/country parent ids. Reuse current authoritative ancestry rules/ShippingAuthoringDestinationResolver for deep admin chains/cycle detection, via a narrowly shared pure helper if needed; don't clone a competing validator or equate matching GUID with chain proof. Operation-local dedup/cache only. No global TTL cache/registry/catalog or backend scan of all countries/cities/serviceareas.

Source-backed nuance: CitySnapshot.AdministrativeAreaId is nullable; a city may legitimately have no admin parent. Its write projection must preserve that, never synthesize or require one. Recursive admin parents, contradictory country/parent, inactive/missing ancestor and cycles must fail resolution safely. Terminal identity MUST equal requested key.

Enrich ShippingAuthoringEditorDocumentItem ADDITIVELY with destinationFacts:
one deterministic entry per distinct selected(kind,id), truthful resolution Resolved/Unresolved, nullable canonical selected-area WRITE shape (kind,countryId,administrativeAreaId?,cityId?,serviceAreaId?) only when proven, and nullable localized label.
Use existing label lookup for the real defaultContentLanguageId from authoritative editor context; no locale->language guess, hardcodedfa/en, invented name or rawGUID as display label. If genuine label absent, keep null and let later UI show localized unavailable; don't change Geography's existing fallback policy or fake a name from UUID.
Keep original selected keys/region tree unchanged in every read. Missing/inactive target or ancestor is an explicit Unresolved fact with no fabricated write tuple; don't drop it or silently clear selection. Inconsistent duplicate/wrong-id Contract response fails closed (existing configuration_unavailable where appropriate); dependencyexception/caller cancellation propagate, never unresolved/empty success.

Authorization/business order unchanged:

GET: existing real full owner+draft query BEFORE foreign read; unknown/foreign404 leaks nothing.
PUT: owner/stale checks before foreign I/O; prepare/validate proposed tree by existing canonical workflow; acquire ALL hydration/label/type/context facts before first tracked mutation/save. Compose result from saved actual root + retained facts WITHOUT any foreign read/re-query after commit.
Clear/zero selections: destinationFacts[] and zero Geography reads. No new BeginTransaction/site, no change to root revision/noop/persistence rules.
Reuse pre-read facts for response even if downstream dependency disappears after write; later GET may truthfully fail. Don't add an enrichment call after directory.Save!
An additive read field is not authority: writer continues to revalidate actual Geography before accepting a PUT. Do not accept trusted hydrated facts from browser.

No new HTTP route or request expected; current four editor routes/query/command contracts preserved, aside from additive response property. Preserve positional constructor compatibility where practical via own init property or cohesive additive mapping. No new body/route/query owner/language override. No new validators exemption unless actual request universe changes (not intended).

3. Proportional focused proof

Reuse already accepted editor race/rollback/noop/storage proof: production persistence is untouched. No full645/Geography/Host/solution suite.

Geography reader uses own current DI/EF boundary: bulk selected ids return real parent facts incl inactive and city-null-admin; unrelated row absent; emptyids noquery; no SaveChanges or schema authority. Use established module test provider, no package/container/DB setup. Label test fixtures as fixtures, not live merchant DB.
Shipping projection: country, deep admin, city, servicearea, sameGUIDdifferentkind; exact requested identity/parents + labels using configured default language, dedup/bounded reads. Missing/inactive/cyclic/contradictory chain Unresolved with raw selection preserved, no guessed tuple. Nulladmincity valid. Inconsistent Contract facts fail closed; same exception/caller token propagation.
Real new editor handler query/command composition: foreign owner/stale before outgoing reads; complete actual selected keys hydrated. A scripted read/label source throws on any SECOND/post-write invocation: actual PUT succeeds from pre-read facts, response id/revision uses saved root; explicit later GET failure doesn't alter persisted draft. Empty selection has zero calls.
Real canonical ActivityListener for ONE actual new foreign read (parent/correlation/constant module+operation tags/status/error type only, exception identity, no ids/labels in telemetry). Reuse existing tracer/helper; don't introduce tracing framework. No need multiply span tests by each bulk method.

Build only affected Geography and Shipping test projects/references, focusedfilters oncegreen, command<=120s. One bounded harness correction/re-run allowed, then precisePARTIAL; no confidence loop or unrelated fixes. If you cannot preserve boundary without broader contract/schema change, STOP with exact blocker, don't improvise.

Change bounds / timing

Allowed: Geography.Contracts.Places minimal interface, Geography.Infrastructure own read implementation+DI, own focused tests; Shipping.Application Authoring port/read projection/models/query/command integration; Shipping.Infrastructure Authoring adapter+DI (reuse canonical ancestry extraction only as necessary); own Contracts response DTO if needed and focused tests; exact intentional structure guard facts only if required (no baseline widening).
No Geography domain/catalog/data/migration/HTTP changes; no ShippingDomain rule/persistence/DbContext/schema/migration/designer/snapshot/transaction changes; no Host/frontend/sharedlib/newproject/package/config/skill/AGENTS/flag or unrelated manifest promotion.

Docs: received artifact docs/ai/tasks/TB-SHIPPING-ADMIN-DESTINATION-READ-HYDRATION-201.task.md; own evidence RESULT.md; append precise implementation facts to Shipping rules; one recovery worker pointer. No UI/visual/certification/live DB claim.

No server/browser/HTTPprobe/container/DB/seed/migration/provider startup/image/package install, sharedports3000/3419/5088/5432/Redis/RabbitMQ/cache/bin/obj/.next/process operation or credentials. No Bridge testpayload.

30min HARD from claim; stop newcoding/tests at23; reserve7 for diff/evidence/scopedcommit/push/fetchparity/result. Result includes exact wire field, readerauthority/DI/Contracts edge, parent proofs/label provenance, prewrite-only proof/tracing, actuallyrun vsreused tests, files/limits/deviations/time/parity; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.

Only THIS task. After review the next dependency is the real existing-draft Admin form; don't issue that task automatically or implement it here.