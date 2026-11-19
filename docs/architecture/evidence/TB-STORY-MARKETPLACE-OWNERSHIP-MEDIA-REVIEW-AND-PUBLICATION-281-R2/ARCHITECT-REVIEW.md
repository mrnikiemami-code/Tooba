# TB-281-R2 — ARCHITECT REVIEW — 2026-11-19

Reviewed commit: 4c086d32346ce2b6f695c254d7fa58db98c4f322, independently fetched from origin/main.
Verdict: PARTIAL. Retain implemented progress; no whole-delivery ACCEPT, UI issuance or user acceptance.

## Retained progress
Persisted public Items loading and role projections remain; seller server query, human shop-name filter, product ownership port, stop/return-to-draft cycle, Media CQRS upload and single-save asset/binding/occurrence algorithm landed. Process-only skip-development-bootstrap avoids repeating development seed on refresh. These are progress, not completed failure/relational proof.

## Source-confirmed closure findings
1. StoryDirectory.cs AdminUpdateAsync passes raw cover/CTA directly into Update; AdminAddItemAsync and AdminUpdateItemAsync pass raw media/CTA without the shop binding and product checks used by seller paths. AdminAddItemAsync has no reviewed-version check. Admin-created-for-shop content can therefore bypass the owning checks applied to the same shop through seller methods. Submit checks CTA but does not reauthorize every stored media reference. Composite approve/publish/schedule in Lifecycle.cs performs transitions without the full stored-content revalidation.
2. Replay is incomplete in source, not only in a missing matrix. SellerAddItemAsync hashes HashItemIntent(command) without storyId; TryReplayMappedAsync selects existing StoryId from the occurrence and never compares the requested target. Reusing an add key/body on another story of the same shop can return the first story. Seller item version checks are conditional on a non-null int; SellerSubmitAsync carries neither reviewed version nor occurrence. Admin composites have no occurrence parameter. Other mutations must be enumerated once and closed through the same owning protocol, not a second route-specific authority.
3. StoryDbContext maps command_occurrences PK to OccurrenceKey alone while replay lookup scopes by tenant. Tenant-local reuse can collide globally rather than answering under its declared identity. Exact constraint identity, winner re-read and clean/scoped failure handling require real relational proof.
4. MediaDirectory.Story.cs now saves asset+owner+occurrence together. Recording-store failure proof remains absent. IsUniqueViolation uses message substring matching (unique/duplicate/23505), rather than exact supported PostgreSQL constraint identity. Do not call best-effort object deletion a distributed transaction. Prove stored winner, failures and caller state rather than source comments.
5. Ownership.cs EnrichPublicAsync deliberately assigns ShopPublicId=null. The actual existing seller route is /seller-profile/[publicId], and storefront-api reads /v1/storefront/sellers/{publicId}. Use its owning identity authority, not Party Guid, a new public-id algorithm, or a fabricated link.
6. Required recording-store and PostgreSQL rollback-only cases are explicitly OPEN in the worker ledger. Four new InMemory facts plus architecture pins are not substitutes. No external blocker preventing those proofs is identified by the report.

## Evidence and scope limits
Architect performed source review, not local Windows execution. Worker test/runtime claims remain attributed to worker receipts. No live business write authorized or performed by this review. Existing applied migrations are immutable.
Task hash receipts compare exact repository blob bytes; Windows worktree newline normalization can be explained separately. Do not label different byte hashes equivalent or a hash-tool variance without an actual byte comparison.

## One continuation
Issue 281-R3 only: close these owning paths and focused failures/relational proof in the existing delivery; preserve finished work. Shared Orders-style UI remains planned, not issued until review of this completion. Mandatory seller integration: existing /vendor-panel/stories under VendorShell and story.view, no parallel seller panel. See SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
