// TB-TMAR-IDENTITY-AMSC-001-W3 — manifest reconciliation (docs/architecture/tmar-module-structure-manifests.json).
// Text-anchored: adds the AMSC-001 certification note and repairs the stale Contracts allowlist
// justification ("Problems" -> "Errors"). No unrelated record is reformatted.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'tmar-module-structure-manifests.json');
let text = fs.readFileSync(file, 'utf8');
const crlf = (s) => s.replace(/\n/g, '\r\n');

function replaceOnce(from, to, label) {
    const f = crlf(from);
    const count = text.split(f).length - 1;
    if (count !== 1) {
        throw new Error(`anchor not unique (${count}) for ${label}`);
    }
    text = text.replace(f, crlf(to));
}

const note =
    'TB-TMAR-IDENTITY-AMSC-001-W3 (tooba-architecture-certify) certified the Identity module under ' +
    'ARCH-COMPLETE-002: verdict COMPLETE_REFERENCE_PATTERN. W0 91eec1fd / W1 93a6b192 / W2 7c79f8c6 / W3 this wave. ' +
    'W1: three OTP-delivery machine codes (identity.otp.delivery.rate_limited 429, invalid_destination 400, ' +
    'unconfigured 400) moved from raw string literals to IdentityErrorCodes and registered in ' +
    'IdentityErrorCatalogContributor (12 declared = 12 registered); OtpDeliveryProviderSender throws ' +
    'ContractOperationException instead of raw InvalidOperationException; IdentityOperation is the single ' +
    'ContractOperationException -> SemanticError(code) seam; LoginIdentifierNormalizer replaced six hard-coded ' +
    'Persian fault messages with the stable identity.validation.failed code; IdentityErrors.fa.resx added; 12 ' +
    'duplicate using directives removed. W2: Contracts/Problems became Contracts/Errors with namespace ' +
    'Tooba.Identity.Contracts.Errors, Application/Validators/IdentityValidationCodes.cs moved to ' +
    'Auth/Validators, four unreferenced transport response records deleted, three shape-only validators added ' +
    '(LoginWithPasswordCommandValidator deliberately omits IdentifierKind so an unknown kind still collapses to ' +
    'identity.authentication.failed 401 instead of identity.validation.failed 400), and two pre-existing RED ' +
    'guards that asserted a flat /Modules/ solution folder were replaced by strictly stronger whole-solution ' +
    'uniqueness assertions. W3 verified: path<->namespace EXACT, root allowlists ENFORCED, /Modules/Identity/ ' +
    'Solution Explorer grouping with 5 projects, 13 module-owned routes with zero Host HTTP ownership and zero ' +
    'Host business/persistence authority, 13 endpoint-reachable requests each a real MediatR 12.5.0 ' +
    'IRequest/IRequestHandler pair dispatched through ISender, validator coverage 9 REQUIRED (all present) + 4 ' +
    'NO_VALIDATOR_REQUIRED, canonical ApiResponseFactory with zero Results.BadRequest/Problem and the single ' +
    'intentional 201 register raw DTO, both-culture resx coverage, unchanged migrations, and cross-module ' +
    'coupling limited to Tooba.CustomerProfile.Contracts (ICustomerProfileDirectory) with zero foreign ' +
    'Application/Domain/Infrastructure project edge in any Identity project. Identity remains a pure ' +
    'HTTP_OWNING module: microserviceExtractable = true. Behavior, routes, DTO shapes, status codes, error-code ' +
    'values, resources and schema were preserved by W3 itself (W3 introduced zero runtime behavior change). ' +
    'Durable guards: IdentityModuleAmsc001W3CertGuardTests, IdentityModuleAmsc001W2StructureGuardTests, ' +
    'IdentityModuleAmcW5CertGuardTests, IdentityModuleAmcW2StructureGuardTests, ' +
    'IdentityModuleAmcW1SolutionGuardTests, IdentityValidatorCoverageGuardTests.';

replaceOnce(
    `      "module": "Identity",
      "structureCertified": true,
      "lockVersion": "ARCH-COMPLETE-002",
      "projects": [`,
    `      "module": "Identity",
      "structureCertified": true,
      "lockVersion": "ARCH-COMPLETE-002",
      "certificationNote": "${note}",
      "projects": [`,
    'Identity certificationNote');

replaceOnce(
    '"rootAllowlistJustification": "Identity.Contracts has no root .cs: Auth/Actors/Contacts/Problems (+ Resources) carry contracts; Auth namespace is Tooba.Identity.Contracts.Auth."',
    '"rootAllowlistJustification": "Identity.Contracts has no root .cs: Auth/Actors/Contacts/Errors (+ Resources) carry contracts; Auth namespace is Tooba.Identity.Contracts.Auth."',
    'Identity.Contracts allowlist justification');

fs.writeFileSync(file, text, 'utf8');

const parsed = JSON.parse(text.replace(/^\uFEFF/, ''));
const identity = parsed.modules.find((m) => m.module === 'Identity');
if (!identity.certificationNote.includes('TB-TMAR-IDENTITY-AMSC-001-W3')) {
    throw new Error('certification note missing after patch');
}

console.log('Manifest patched: Identity certificationNote + Contracts allowlist justification.');
