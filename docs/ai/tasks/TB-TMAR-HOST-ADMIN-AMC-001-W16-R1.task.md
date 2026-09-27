PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W16-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W16
Parent-Commit: bf9dda6ae2b336a4159d37dbc790026aa578b3f2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W16-R1 — remove expected InvalidOperationException control flow from ProductPublishReadinessReader category readiness

ARCHITECT REVIEW

W16 is NOT accepted yet.

The migration itself is structurally sound, but one PASS blocker remains in production code.

BLOCKER

Current:
src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductPublishReadinessReader.cs

contains:

C#
try
{
    CatalogCategoryTreeRules.EnsureAssignableProductCategory(primaryCategoryId, parentById);
    return true;
}
catch (InvalidOperationException)
{
    return false;
}

This is expected-control-flow via InvalidOperationException.

It contradicts the W16 claimed state:

PublishReadiness-InvalidOperationExpectedFlow-State: ABSENT_ON_PUBLISH_READINESS_HTTP_AND_READER_LEGACY_UNWRAP_OUTSIDE

The exception is not message-parsed, but it is still being used as the ordinary "not assignable" boolean path.

REPAIR OBJECTIVE

Use a non-throwing canonical Domain rule for category assignability.

Preferred:

CatalogCategoryTreeRules.IsAssignableProductCategory(...)
if it already exists and is semantically equivalent for this exact check.

Do NOT:

duplicate hierarchy logic in Infrastructure.
add try/catch around another throwing helper.
change assignability semantics.
change readiness ordering/messages.
change route ownership.
start W17.

MANDATORY AUDIT

Before edit:

inspect CatalogCategoryTreeRules completely.
confirm existing non-throwing API and exact semantics.
compare it with EnsureAssignableProductCategory.
prove W16 reader only needs boolean assignability and no error-code distinction.
enumerate any other expected IOE control flow inside ProductPublishReadinessReader.

REPAIR REQUIREMENTS

Replace the try/catch category readiness logic with canonical non-throwing Domain authority.
ProductPublishReadinessReader must contain no expected InvalidOperationException flow.
no message parsing.
no duplicated category-tree algorithm.
no weakening of W16 behavior.
preserve:
category readiness boolean
level-3 assignability
missing order
ProductPublishRules messages
reuse of ProductSeo/ProductMedia/ProductVariants/ProductAttributes
workspace.product.missing
view-scope GET behavior
route/JSON parity
Host/Admin count 52

GUARDS

Add/update guards proving:

ProductPublishReadinessReader contains no catch (InvalidOperationException.
no throw new InvalidOperationException in moved readiness reader.
category readiness uses canonical Domain non-throwing API.
no category hierarchy duplication in reader.
W16 route ownership unchanged.
focused readiness dependency reuse unchanged.
missing requirement order unchanged.
Host/Admin count remains 52.
W17 not started.

TESTS

Focused non-DB tests:

no primary category -> false.
non-assignable category -> false.
assignable level-3 category -> true.
no exception thrown for expected non-assignable state.
readiness result unchanged for same fixtures if existing tests cover it.

Focused validation:

Catalog.Domain tests/build.
Catalog.Infrastructure build.
Catalog.Endpoints build.
Host build.
Host.Tests build.
W16 + W16-R1 guards.
ProductPublishReadinessCapability tests.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W16-R1/

Required:

analyze.md
category-readiness-repair.md
guard-repair.md
closure.md

RECOVERY SOT

Add hostAdminAmcW16R1.

Record:

parentTask/parentCommit
W16 migration preserved
categoryReadinessAuthority=CatalogCategoryTreeRules_NON_THROWING
productPublishReadinessInvalidOperationExpectedFlow=ZERO
messageClassification=ZERO
readinessDependencyReuse=PRESERVED
Host Admin count=52
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W16_R1_CHECKPOINT

PASS CRITERIA

PASS only if:

ProductPublishReadinessReader has zero expected InvalidOperationException control flow.
canonical Domain non-throwing assignability API is used.
no duplicated hierarchy logic.
W16 routes/behavior/errors/readiness ordering/reuse preserved.
focused builds/tests/guards pass.
task/evidence/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W17 not started.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W16-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W16
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W16-Migration-Preservation-State:
Category-Readiness-Authority-State:
Category-Readiness-Boolean-Parity-State:
ProductPublishReadiness-InvalidOperationExpectedFlow-State:
ProductPublishReadiness-Message-Classification-State:
Readiness-Dependency-Reuse-State:
Missing-Requirement-Order-State:
PublishReadiness-Route-Ownership-State:
PublishReadiness-Error-Code-State:
Workspace-View-Scope-State:
Host-Admin-File-Count:
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

After result:

STOP completely.
Do not start W17.
Wait for Architect review.

END_TOOBA_TASK