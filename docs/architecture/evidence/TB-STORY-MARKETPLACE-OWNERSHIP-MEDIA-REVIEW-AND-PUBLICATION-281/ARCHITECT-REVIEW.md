# Architect review — TB-STORY-281

Date: 2026-11-19 (Asia/Tehran)
Verdict: PARTIAL. First delivery is NOT accepted; connected UI delivery is NOT ISSUED.
Reviewed head: 860c6a2c72842c8971d8b9f9885e26cdb0f3f657; compare base c15a3a69c15675ae68acb7e001e01bf373f1d0b0.
Review method: independent GitHub source/diff and actual four test assertions. Architect did not execute local builds/tests/runtime or database writes.

## What landed

Shop-owner/creator fields, Media owner binding/occurrence tables, new seller media endpoints, a scheduled-aware pure policy, four versioned lifecycle commands and transport mappings are present. Two migrations are reported applied; treat them as immutable applied history. Worker correctly declares PARTIAL. Four domain/InMemory facts plus52 architecture-pin checks are not the issued Directory/Media/relational acceptance matrix. No partial result is promoted to Marketplace/user acceptance.

## Source-confirmed repair findings — consolidate into ONE continuation of delivery one

### B1 — fresh persisted public read filters every story before attaching its slides

StoryDirectory.cs:53–60 calls IsPubliclyVisible -> StoryPublicationPolicy.HasPlayableContent -> Items.Count before lines62–73 load/AttachItems. StoryDbContext ignores Items; a freshly materialized PostgreSQL aggregate has an empty private collection. Thus an otherwise valid persisted Active/Scheduled story is excluded and the method returns[] before loading its slides. The new schedule test calls the pure policy on an already-populated aggregate and cannot catch this regression.
Fix actual Directory reader and test with a fresh context/materialization, including start/end equality and Disabled negative.

### B2 — shop/media dependencies fail OPEN; media gates are incomplete

StoryDirectory constructor accepts null Party/Media ports; DI uses GetService. EnsureShopEligibleAsync returns when Party absent; BindSellerMediaAsync returns the client asset id/URL when Media absent. Both new Directory tests construct new StoryDirectory(db), bypassing exactly the owning checks that were meant to be proven.
Media authorization runs only on seller add/update item: cover create/update and admin editing a shop-owned story remain unbound, and item mediaType is not checked against Media's MIME/IsVideo. HasPlayableContent treats any non-empty Guid as “Ready-bound” without such proof. Publish/submit paths do not revalidate missing/Failed assets.
Required: non-optional authority or explicit typed refusal, all shop-owned cover/item entry paths and effectful publication validated through real owning ports, correct MIME, no guessed URLs or foreign assets. Keep lawful legacy admin media compatibility narrow. Admin-on-behalf upload is not actually exposed yet.

### B3 — upload replay does not bind bytes; Ready asset commits before owner/replay binding

MediaDirectory.Story.cs checks occurrence key+scope and immediately returns the previous asset without comparing file bytes/name/type/intent. MediaAssetOwnerOccurrence has no intent digest. Different file under same key answers the old asset. Missing key is allowed. UploadAsync commits the Ready asset/object first, then owner/occurrence rows are saved separately: a second-save failure leaves a Ready unscoped asset and retry may upload again. Concurrent duplicate has no scoped arbitration and may leave an extra object/asset. No controlled object-store/local-save-failure test exists.
Required: durable scope+full immutable upload intent, bounded coherent store/owner/replay outcome and exact conflict/replay; no transaction spanning external storage, no broad cleanup or fabricated Ready result.

### B4 — version/replay support covers only a minority of shipped paths

Old approve/reject/enable/schedule/update/add/update-item/remove/reorder and seller submit/create remain without the reviewed-version/occurrence contract. Lifecycle body accepts only ExpectedVersion and has no occurrence identity. Only admin-for-shop create calls TryReplay/RecordOccurrence; other create/add/lifecycle methods ignore or lack it. Exact replay after a committed lifecycle result is therefore a stale-version error, not the recorded outcome.
HashIntent joins unescaped fields with |; title/locale etc may contain that delimiter, so distinct immutable bodies can hash identically. Occurrence PK is global while lookup is tenant-scoped; same client key across tenants can hit an unrelated unique constraint.
Required: canonical unambiguous full intent with scoped durable identity, exact replay before new-write version gate after authorization, and reviewed-version enforcement on ALL relevant entry paths rather than a new-route-only fence.

### B5 — projected actions are a union of admin+seller, and stopped stories become stranded

MapAdmin concatenates admin and seller ProjectActionCodes and sends the union to seller reads. A submitted seller row can advertise approve/reject/admin actions even though endpoints refuse them. Conversely grid uses its old independent MapAdmin constructor, leaving new readiness/actions defaults; EnrichAsync only runs on some new methods. Old list/detail/public generally do not fill ShopDisplayName; public mapping supplies Party Guid only, optional name/public id remain null.
ProjectActionCodes offers return_to_draft only for Active/Scheduled; SellerStop makes Disabled while Approved, so the explicit return-to-draft command is lawful but disappears from the advertised seller path. Pure and persisted time/status projections must agree, including expired windows.
Required: role-scoped shared projection across list/query/detail/mutation/public, no action union, reachable stop->edit->resubmit chain, batch authoritative shop labels/public identity.

### B6 — first-delivery query/target scope still incomplete

No new seller query endpoint exists; admin grid shop-name search/filter is unchanged. Public shop scope/attribution not complete. CTA only checks token/scheme/target text, not the advertised seller's product authority. These are original issued A/E/G items, not optional enhancements.
Required: scoped server query/count, Party-based shop-name filters, canonical public attribution/scope and positive/negative owning-product target proofs.

### B7 — new Media endpoints bypass owning CQRS and duplicate tenant rules

MediaSellerEndpoints injects the Infrastructure-backed Contracts port directly; no ISender request/handler/validator, unlike the issued owning CQRS rule. ResolveTenant duplicates Story-specific hardcoded tenant Guids and the tooba:story hash algorithm in Media Endpoints. Its current formula matches Story, but it is a second unowned identity implementation. Avoid fixing architecture pins to redefine direct execution as acceptable.
Required: thin authenticated endpoints -> owning validated request/handler -> Media service, one canonical compatible tenant identity seam, exact relevant guards and registered-service tests.

## Evidence closure

Complete original G1–G7 with real Directory/Media ports, DI wiring, controlled object storage, failure injection and canonical rollback-only PostgreSQL facts (committedRows0). Include fresh persisted public read, malformed/foreign/unready cover/item, changed-byte replay, lost owner save, legacy-path stale review, role-specific grid/detail and shop-name query counterexamples above. Do not weaken product logic to keep old tests green.
Existing backup/apply narrative lacks detailed exit/timing receipts in the pack; preserve actual historical evidence if available, otherwise label the limit and verify archive NOW with current timestamp (not fabricated pre-apply proof). No Down/edit of applied migrations.
No screenshots/live mutation/fixtures/second DB. Preserve Returns acceptances and financial backlog. One consolidated repair is the next authorized continuation; no UI delivery or automatic task chain.
