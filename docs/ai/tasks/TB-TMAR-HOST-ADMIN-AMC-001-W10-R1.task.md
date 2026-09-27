PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W10-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W10
Parent-Commit: bb510b65be7e27096e061036e4e5c5335e8e0b51
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W10-R1 — remove residual dead Attribute helper and Seller DTO leakage from partial Admin Attribute file

ARCHITECT REVIEW

W10 is NOT accepted yet.

The main Product Attribute migration is structurally sound, but two concrete residuals violate the W10 partial-host-file rule and PASS criteria.

BLOCKER 1 — DEAD HOST HELPER RETAINED

Current:
src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs

still contains:
MapAttributeInvalid(InvalidOperationException ex)

W10 evidence itself classifies it:
RETAIN_DEAD_HELPER

and says it is retained because W8/W9 guards still assert it.

This is invalid.

A dead helper must not remain in production code merely to satisfy stale architecture tests.

Required:

remove MapAttributeInvalid completely if it has zero runtime callers.
update W8/W9/W10 guards so they verify migrated Attribute surfaces no longer depend on message parsing, rather than asserting obsolete Host helper presence.
do not weaken guards broadly.
preserve retained variant/category-change behavior.

BLOCKER 2 — SELLER TRANSPORT DTO LEAKS FROM ADMIN ATTRIBUTE FILE

Current partial Admin file still declares:

SetProductAttributeRequest

Evidence says it remains only because Host Seller SellerPanelEndpoints consumes it.

But W10 PASS criteria required:
CatalogAttributeEndpoints.cs retained only for variant/category-change members.

A Seller-panel transport DTO is not a genuine retained member of this Admin Attribute slice.

Required:

remove SetProductAttributeRequest from Admin/CatalogAttributeEndpoints.cs.
relocate the transport record to the smallest lawful Seller-owned transport location used by the existing Seller endpoint.
preferred smallest repair: define the DTO next to/in the existing Seller endpoint surface that owns the HTTP body, preserving its JSON shape exactly.
do NOT create a new shared business contract merely to share one HTTP DTO.
do NOT move Seller business logic or start a Seller-folder migration.
this is caller-local transport cleanup only.

SCOPE

Repair ONLY these two closure defects plus necessary guards/evidence/SoT.

Do not:

alter the four W10 Catalog Product Attribute routes.
change ProductAttributeDirectory behavior.
change actor binding.
change Result/error codes.
change transaction/history behavior.
migrate variant/category-change routes.
start W11.
start another Host-folder recovery wave.
redesign SellerPanel.

MANDATORY AUDIT

Before edit prove:

MapAttributeInvalid runtime call count is zero.
identify every test/guard assertion mentioning MapAttributeInvalid.
identify exact Seller consumer(s) of SetProductAttributeRequest.
confirm DTO relocation can preserve body shape and binding semantics.
confirm no other code depends on the Admin namespace solely for that DTO.

TARGET STATE

Admin/CatalogAttributeEndpoints.cs contains only:

variant-axis route
variant editor/preview/apply/readiness
category-change-preview
primary-category replacement
helpers/transport records genuinely needed by those retained members

It must contain NEITHER:

MapAttributeInvalid
SetProductAttributeRequest

W10 Product Attribute Catalog-owned surface remains unchanged.

HOST INVENTORY

Admin production *.cs count should remain:
53 -> 53

unless the smallest lawful DTO relocation unexpectedly requires a new Admin file — avoid that.
Do not add an Admin file for Seller transport.

SELLER DTO RELOCATION

Use the existing Seller endpoint file/location.

Preserve exact request contract:

RawValue
EnumOptionId

No route change.
No status/JSON behavior change.
No namespace leakage back to Admin.

If Seller code currently imports Tooba.Host.Admin only for this DTO:
remove that dependency if no longer needed.

GUARDS

Repair guards to prove:

MapAttributeInvalid absent from Host production Attribute file.
no moved Attribute route relies on message parsing.
SetProductAttributeRequest absent from Admin/CatalogAttributeEndpoints.cs.
Seller endpoint still has an equivalent request body shape.
W10 four routes remain Catalog-owned exactly once.
retained variant/category-change routes remain Host-owned exactly once.
Host/Admin count remains 53.
W1–W9 plus W10 migration preserved.
StoreAppearance unchanged.
W11 not started.

Update W8/W9 guards only where they encode stale expectations.
Do not delete useful prior-wave assertions.

VALIDATION

Focused:

Host build
Host.Tests build
Catalog.Endpoints build only if touched by repair (expected no)
architecture guards W8/W9/W10
focused Seller endpoint compile/tests if available
focused Product Attribute W10 guards

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W10-R1/

Required:

analyze.md
dead-helper-cleanup.md
seller-dto-relocation.md
guard-repair.md
closure.md

Update W10 evidence only if needed to remove now-false residual statements; preserve history.
Prefer R1 evidence as authoritative repair record.

RECOVERY SOT

Add:
hostAdminAmcW10R1

Record:

parentTask/parentCommit
deadHelperState=REMOVED
sellerTransportLeakState=REMOVED_FROM_ADMIN_RELOCATED_TO_SELLER_OWNER
productAttributeMigrationState=PRESERVED
Host Admin count 53
retainedAttributeHostScope=VARIANT_CATEGORY_CHANGE_ONLY
W10 guards repaired
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W10_R1_CHECKPOINT

PASS CRITERIA

PASS only if:

MapAttributeInvalid has zero definition and zero runtime reference in Host production Attribute file.
stale guard expectations repaired.
SetProductAttributeRequest absent from Admin/CatalogAttributeEndpoints.cs.
Seller endpoint still binds the same body contract lawfully.
no new shared business contract introduced for this HTTP DTO.
W10 four Catalog Product Attribute routes unchanged and still canonical.
retained Host file contains only variant/category-change responsibility.
Host/Admin count stays 53.
builds/guards pass.
no schema/frontend change.
commit pushed.
HEAD==origin/main.
working tree clean.
W11 not started.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W10-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W10
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W10-Migration-Preservation-State:
Dead-MapAttributeInvalid-State:
Dead-Helper-Runtime-Reference-State:
W8-W9-Guard-Repair-State:
Seller-SetProductAttributeRequest-Admin-State:
Seller-DTO-New-Owner:
Seller-Body-Parity-State:
Admin-Namespace-Leak-State:
Attribute-Host-File-State:
Host-Admin-File-Count:
ProductAttribute-Route-Ownership-State:
Retained-Variant-CategoryChange-State:
Actor-Context-State:
Bulk-Transaction-State:
Product-History-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Next-Host-Folder-Started: false
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After returning:

STOP completely.
Do not start W11.
Do not migrate variant/category-change routes.
Wait for Architect review.

END_TOOBA_TASK