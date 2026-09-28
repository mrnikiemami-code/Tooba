PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-MEDIA-EVACUATE-001
Parent-Task: TB-TMAR-HOST-REMAINDER-AUDIT-001
Parent-Commit: 045dbb44ec521ddb6a2987d4e846f27ed7e7f570
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_EVACUATION
Title: Evacuate Host Media HTTP boundary into Tooba.Media.Endpoints

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-REMAINDER-AUDIT-001 is ARCHITECT-ACCEPTED at:
045dbb44ec521ddb6a2987d4e846f27ed7e7f570

Verified:

Admin remains CANONICAL_PLATFORM_BOUNDARY_CERTIFIED
remaining Host inventory/classification produced
first recommended actionable area = Media
no production code was changed by the audit

GOAL

Evacuate the single Host-owned Media HTTP boundary:

src/backend/Host/Tooba.Host/Media/MediaEndpoints.cs

into the Media module's Endpoints boundary.

After this task:

Host must no longer own Media HTTP route implementations.
Host must have ZERO Tooba.Media.Application dependency caused by this surface.
route paths and behavior must remain unchanged.

MANDATORY AUDIT FIRST

Before editing:

inspect the current Media module project structure;
determine whether Tooba.Media.Endpoints already exists;
inspect existing Media Contracts/Application public seams;
inspect how module endpoints are mapped/registered elsewhere in the repo;
inspect current Program.cs Media mapping.

If Tooba.Media.Endpoints already exists:

extend it.

If it does not exist:

create the smallest canonical Tooba.Media.Endpoints project consistent with the repository's existing five-project module pattern and solution grouping.
do not create unrelated structure.

ROUTES TO PRESERVE EXACTLY

POST /v1/admin/media/upload
GET /v1/admin/media/
GET /v1/admin/media/{id:guid}
GET /v1/media/{id:guid}

Preserve:

DisableAntiforgery() on upload
query parameters and defaults
per-file upload result shape
public binary serving
range processing
placeholder SVG fallback behavior
stable error codes/statuses
content-type/kind normalization

AUTHORIZATION BOUNDARY

Current Host code calls the certified concrete AdminPanelAccess.

The module Endpoints MUST NOT reference Tooba.Host.*.

Prefer the already-neutral:
Tooba.BuildingBlocks.Security.IAdminPanelAccess

If that seam is accessible and behavior-compatible:

inject/use it directly in Media.Endpoints.

Do NOT create a Media-specific authorizer adapter unless required by an existing repo convention or compile boundary.

Admin certification must remain unchanged:

do not add files under Host/Admin/**
do not edit certified Admin production files

ERROR / PRESENTATION HYGIENE

Do not introduce:

ex.Message classification
message parsing
new hardcoded machine codes

Preserve existing machine codes:

media.upload.failed
media.missing
and all existing Media-originated PlatformHttpException codes.

If Media already has a canonical ApiResponseFactory/error catalog path, use it only if behavior parity is exact and bounded.
Do not broaden into a Media presentation redesign.

HOST TARGET STATE

Delete:
src/backend/Host/Tooba.Host/Media/MediaEndpoints.cs

Update Host composition/mapping only as required to call module-owned:
MapMediaEndpoints()

The Host/Media folder should become absent/empty after evacuation.

MODULE TARGET STATE

Media owns:

endpoint mapping
upload/query/get/serve handlers
ResolveContentTypePrefix
stored-media serving helper if still needed
placeholder SVG behavior

Use Media Application only from within the Media module boundary.

NO BUSINESS REDESIGN

Do not change:

IMediaDirectory
IMediaObjectStore semantics
upload business rules
storage behavior
persistence
schemas/migrations
frontend

ANTI-LOOP / EXECUTION BUDGET

ONE concern only.

Validation:

build Tooba.Media.Endpoints (or the existing Media endpoint-bearing project if audit proves a different canonical name)
build Tooba.Host
run ONLY focused Media route/behavior/ownership guard tests

On failure:

ONE repair attempt maximum
rerun ONLY the exact failing command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
No broad Host suite.
No solution-wide tests.
Do not loop.

DURABLE GUARD

Add focused ownership guard proving:

Host MediaEndpoints.cs no longer exists
Host has no namespace Tooba.Host.Media
Media route strings exist under Media module Endpoints
Media Endpoints has ZERO Tooba.Host reference
Host Media Application reference for this boundary = ZERO
all four route patterns are preserved
DisableAntiforgery() preserved
public serve fallback remains present
Admin certification guard remains present and untouched

FOCUSED BEHAVIOR TESTS

Preserve/verify existing relevant tests if they exist.
At minimum verify:

media missing => 404 media.missing
invalid/no upload files => 400 media.upload.failed
stored public media => file response path
missing legacy asset => SVG fallback

Do not create a large integration suite.

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-MEDIA-EVACUATE-001/

analyze.md
route-parity.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-MEDIA-EVACUATE-001.task.md

RECOVERY SOT

Append/update hostMediaEvacuate001:

parentCommit
hostMediaEndpointOwnership=ZERO
mediaEndpointOwner=MEDIA_ENDPOINTS
hostMediaApplicationReference=ZERO
routeParity=PRESERVED
adminCertification=PRESERVED
workflowStop=USER_REVIEW_HOST_MEDIA_EVACUATE_001

SUCCESS CRITERIA

PASS only if:

Host Media endpoint implementation is removed
Media module owns all four routes
Media Endpoints has no Host dependency
route/behavior parity is proven
Host build passes
focused validation passes
Admin certification remains untouched
task/evidence/SoT committed and pushed
HEAD == origin/main
tracked working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-MEDIA-EVACUATE-001
Parent-Task: TB-TMAR-HOST-REMAINDER-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Admin-Certification-Preservation-State:
Media-Endpoints-Project-State:
Host-Media-File-State:
Host-Media-Folder-State:
Media-Endpoint-Owner-State:
Media-Endpoints-To-Host-State:
Host-MediaApplication-Reference-State:
Admin-Authorization-Seam-State:
Upload-Route-State:
Query-Route-State:
Get-Route-State:
Public-Serve-Route-State:
Upload-Antiforgery-State:
Error-Code-State:
Placeholder-Svg-State:
Behavior-Parity-State:
Focused-Validation:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP COMPLETELY.

Do not start OperatorProfile.
Do not touch Seller/Storefront/root boundaries.
Wait for Architect review.

END_TOOBA_TASK