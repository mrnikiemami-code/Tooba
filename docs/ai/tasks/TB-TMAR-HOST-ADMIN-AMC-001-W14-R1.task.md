PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W14-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W14
Parent-Commit: 1a97aa24cefd5848bca38c0b64214d10c58cbac8
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W14-R1 — eliminate expected InvalidOperationException control flow from ProductSeo slug normalization

ARCHITECT REVIEW

W14 is NOT accepted yet.

The SEO migration is otherwise structurally sound, but one concrete PASS blocker remains in production code.

BLOCKER

Current:
src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductSeoDirectory.cs

contains expected-control-flow handling:

C#
try
{
    ...
    slug = CatalogCategorySlugNormalizer.SlugifyFromName(name);
    ...
    slug = CatalogCategorySlugNormalizer.NormalizeSlug(input.Slug);
}
catch (InvalidOperationException)
{
    return Result.Failure<ProductSeoDetail>(
        new SemanticError(CatalogErrorCodes.WorkspaceProductSlugInvalid));
}

This contradicts the W14 claimed state:

Seo-InvalidOperationExpectedFlow-State: ABSENT_ON_PRODUCT_SEO_HTTP_AND_PRODUCTSEODIRECTORY

and the W14 PASS requirement:

no expected InvalidOperationException flow on migrated SEO surface.

The catch does not parse the message, but it still uses InvalidOperationException as expected invalid-input control flow.

ROOT AUTHORITY

CatalogCategorySlugNormalizer.NormalizeSlug is the canonical Domain slug authority and currently throws when normalization produces an empty slug.

Do NOT duplicate the slug algorithm in Infrastructure/Application.

REPAIR OBJECTIVE

Provide a non-throwing, typed/boolean Domain normalization path and make ProductSeoDirectory use it.

Preferred smallest design:

In CatalogCategorySlugNormalizer add a lawful non-throwing API such as:

TryNormalizeSlug(string? input, out string normalized)
and/or
TrySlugifyFromName(string? input, out string normalized)

Requirements:

same canonical normalization algorithm.
no duplicate divergent implementation.
existing NormalizeSlug / SlugifyFromName behavior remains compatible for existing callers unless a safe internal refactor lets both delegate to one private normalization core.
no message parsing.
ProductSeoDirectory returns workspace.product.slug.invalid directly when Try* fails.
ProductSeoDirectory contains no catch for expected InvalidOperationException.

Do not replace this with regex/pre-validation in Infrastructure that reimplements Domain behavior.

SCOPE

Repair ONLY:

Domain slug normalizer safe path.
ProductSeoDirectory usage.
focused tests/guards/evidence/SoT.

Do NOT:

change SEO routes.
change DTOs.
change slug output.
change uniqueness behavior.
change optimistic concurrency.
change localization.
change history.
start W15.
touch unrelated ProductWorkspace slices.

PRESERVE W14 SUCCESSFUL STATE

Must preserve:

SEO Host routes 3 -> 0.
three Catalog-owned routes exactly once.
ProductSeo capability structure.
IProductSeoDirectory/ProductSeoDirectory.
ICatalogAdminAuthorizer.
CatalogWorkspaceScope.
GET allowed / PUT denied for view scope.
CatalogActorRequestBinding on PUT.
Result + ApiResponseFactory.
existing workspace.* SEO codes/statuses.
ProductSeoRules authority.
CatalogCategorySlugNormalizer authority.
ExpectedUpdatedAt semantics.
slug uniqueness.
fa-IR SeoTitleSeam behavior.
single SaveChanges.
EventSeoChanged actor.
Host/Admin count 52.
StoreAppearance deferred.
frontend/schema unchanged.

MANDATORY AUDIT

Before edit:

enumerate all callers of NormalizeSlug and SlugifyFromName.
verify current behavior for:
null/blank
whitespace
punctuation-only
mixed Persian/Latin
slash/backslash/underscore/hyphen collapse
Unicode letters/digits
prove safe API returns exactly the same normalized value for valid inputs.
prove existing throwing APIs retain compatibility for unrelated callers.

GUARDS

Add/update guards proving:

ProductSeoDirectory contains no catch (InvalidOperationException.
ProductSeoDirectory contains no expected exception-based slug control flow.
ProductSeoDirectory uses canonical Domain safe normalization path.
no slug normalization algorithm duplication outside Domain.
W14 route ownership/pipeline unchanged.
existing workspace SEO codes unchanged.
Host/Admin count remains 52.
W1–W14 migration state preserved.
W15 not started.

TESTS

Focused non-DB tests for Domain normalizer:

valid ASCII slug parity.
Persian slug parity.
spaces/underscore/slash/backslash/hyphen normalization parity.
punctuation-only -> Try false.
blank/null -> Try false.
valid Try result equals existing NormalizeSlug result.
SlugifyFromName safe path parity.

Focused validation:

Catalog.Domain build/tests.
Catalog.Infrastructure build.
Catalog.Endpoints build.
Host build.
Host.Tests build.
W14 + W14-R1 guards.
existing ProductSeoRules tests.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W14-R1/

Required:

analyze.md
slug-normalization-repair.md
guard-repair.md
closure.md

RECOVERY SOT

Add hostAdminAmcW14R1.

Record:

parent W14/commit.
W14 migration preserved.
slug normalization authority preserved.
nonThrowingSlugNormalizationState=CANONICAL_DOMAIN_TRY_PATH.
productSeoInvalidOperationExpectedFlow=ZERO.
messageClassification=ZERO.
Host Admin count=52.
StoreAppearance deferred.
frontend/schema unchanged.
nextHostFolderStarted=false.
workflowStop=USER_REVIEW_HOST_ADMIN_W14_R1_CHECKPOINT.

PASS CRITERIA

PASS only if:

ProductSeoDirectory has zero expected InvalidOperationException control flow.
safe normalization is Domain-owned and behavior-equivalent.
no duplicated normalization algorithm.
existing throwing APIs stay compatible for unrelated callers.
W14 routes/behavior/errors/concurrency/history/atomicity preserved.
focused builds/tests/guards pass.
task/evidence/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W15 not started.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W14-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W14
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W14-Migration-Preservation-State:
Slug-Normalizer-Authority-State:
Slug-Safe-API-State:
Valid-Slug-Parity-State:
Invalid-Slug-State:
Existing-Throwing-API-Compatibility-State:
ProductSeo-InvalidOperationExpectedFlow-State:
ProductSeo-Message-Classification-State:
Seo-Route-Ownership-State:
Seo-Error-Code-State:
Optimistic-Concurrency-State:
Seo-Atomicity-State:
Product-History-State:
Workspace-Scope-Policy-State:
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
Do not start W15.
Wait for Architect review.

END_TOOBA_TASK