// TB-TMAR-IDENTITY-AMSC-001-W3 — Master Recovery checkpoint (docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md).
// Text-anchored: inserts the module-local Identity AMSC checkpoint after the CustomerProfile W3-R1 block,
// leaving the authoritative current region and every historical block byte-identical.
const fs = require('fs');
const path = require('path');

const file = path.join(__dirname, '..', '..', 'TOOBA-TMAR-MASTER-RECOVERY.md');
let text = fs.readFileSync(file, 'utf8');
const crlf = (s) => s.replace(/\n/g, '\r\n');

const anchor = `- Evidence root: \`docs/architecture/evidence/TB-TMAR-CUSTOMERPROFILE-AMSC-001-W3-R1/\`.
- Stop gate: \`USER_REVIEW_CUSTOMERPROFILE_AMSC_001_W3_R1\`.
`;

const block = `
Identity AMSC module recovery checkpoint (authoritative, module-local)

Recorded by \`TB-TMAR-IDENTITY-AMSC-001-W3\` (Certify). This is the AMSC-001 ARCH-COMPLETE-002 certification for Identity; the earlier AMC-001 lineage (\`TB-TMAR-IDENTITY-AMC-001\` W1→W6, implementation \`aafd14e0\` / docs stamp \`c7e473cd\`) stays in the repository as historical evidence only and is explicitly marked \`HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY\`.
- Accepted lineage: \`TB-TMAR-IDENTITY-AMSC-001-W0\` Analyze \`91eec1fd\` → \`TB-TMAR-IDENTITY-AMSC-001-W1\` Migrate \`93a6b192\` → \`TB-TMAR-IDENTITY-AMSC-001-W2\` Structure \`7c79f8c6\` → \`TB-TMAR-IDENTITY-AMSC-001-W3\` Certify \`this wave\`.
- Final verdict: \`COMPLETE_REFERENCE_PATTERN\` / \`ARCH-COMPLETE-002\` \`STRUCTURE_CERTIFIED\`; final \`structureState = CERTIFIED\` (W2 \`structureState = READY_FOR_CERTIFY\` preserved as historical W2 truth).
- 13 endpoint-reachable requests / 13 real MediatR 12.5.0 \`IRequest\`/\`IRequestHandler\` pairs; 9 \`VALIDATOR_REQUIRED\` (all present) + 4 \`NO_VALIDATOR_REQUIRED\`; module-owned routes only (13), Host HTTP ownership ZERO.
- Canonical mechanisms: \`Result\`/\`Result<T>\` + \`IdentityOperation\` typed-fault seam + \`ApiResponseFactory\`; single stable-code owner \`Contracts/Errors/IdentityErrorCodes.cs\` (12 declared = 12 registered descriptors, including the three OTP-delivery codes previously emitted as raw string literals); zero hard-coded client-facing fault text; zero raw \`Results.BadRequest/Problem\`; the only raw \`Results.Json\` is the intentional locked \`201\` register DTO.
- Localization: \`IdentityErrors.resx\` + \`IdentityErrors.fa.resx\`; every declared \`identity.*\` code is registered with a stable English descriptor title and carries a Persian title.
- Structure: capability-first shallow Application (\`Auth/{Commands,Queries,Models,Validators}\` + \`Composition\` + shared \`Models/Options/Ports\`); \`Contracts/Errors\` replaces the pre-ARCH-COMPLETE-002 \`Contracts/Problems\` vocabulary; zero single-file request leaf folders; zero technical-axis-first request tree; path↔namespace \`EXACT\`; root allowlists \`ENFORCED\`; \`/Modules/Identity/\` solution grouping (5 projects); \`Domain\` keeps only its legitimate self-module \`Contracts\` reference for stable codes.
- Host Identity ownership ZERO (no \`Host/Tooba.Host/Identity\` folder, no \`Tooba.Host.Identity\` namespace); the generic Host auth platform seam (session middleware, current-session adapter, throttle seam) is retained and is NOT Identity ownership. Cross-module boundary Contracts-only (\`Tooba.CustomerProfile.Contracts\` \`ICustomerProfileDirectory\`); foreign App/Infra/Domain coupling ZERO in every Identity project; cross-module join ZERO; persistence ownership correct (own \`identity\` schema).
- Schema / migrations unchanged (0 migration files touched); blocking residual debt ZERO; \`microserviceExtractable = true\`; \`automaticNextImplementationTask = NONE\`.
- Durable guards: \`IdentityModuleAmsc001W3CertGuardTests\`, \`IdentityModuleAmsc001W2StructureGuardTests\`, \`IdentityModuleAmcW5CertGuardTests\`, \`IdentityModuleAmcW2StructureGuardTests\`, \`IdentityModuleAmcW1SolutionGuardTests\`, \`IdentityValidatorCoverageGuardTests\`, \`IdentityFoundationTests\`, \`IdentityLifecycleTests\`, \`AuthenticationHttpTests\`, \`AuthSecurityHttpTests\`, \`AuthenticationV2CanonicalizationGuardTests\`, \`OtpDeliveryProviderTests\`, \`StorefrontAccountIdentityTests\`, \`CheckoutIdentityContractTests\`, \`HostAdminAmcCheckoutIdentityGuardTests\`.
- Focused validation: Identity/Auth/Otp filter 62 passed / 6 skipped / 0 failed at the W3 starting HEAD \`7c79f8c6\`; the full Host suite has 82 pre-existing failures unrelated to Identity (Host/Admin StoreAppearance count guards, Grid/Catalog/Party/Reviews module guards, Fulfillment/Tax/Pricing/Promotion domain tests, Master-Recovery history pins, source-size baselines) and no guard was weakened.
- Evidence root: \`docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W0..W3/\`.
- Stop gate: \`USER_REVIEW_IDENTITY_AMSC_001_W3\`.
`;

const marker = 'Identity AMSC module recovery checkpoint (authoritative, module-local)';
if (text.includes(marker)) {
    console.log('Master Recovery already carries the Identity AMSC checkpoint; nothing to do.');
    process.exit(0);
}

// Invariant: the authoritative current region (everything before the explicit historical boundary)
// must remain byte-identical.
const boundaryOf = (s) => s.slice(0, s.indexOf('HISTORICAL / SUPERSEDED'));
const beforeCurrent = boundaryOf(text);

const f = crlf(anchor);
const count = text.split(f).length - 1;
if (count !== 1) {
    throw new Error(`anchor not unique (${count}) for Master Recovery Identity checkpoint`);
}

text = text.replace(f, f + crlf(block));
fs.writeFileSync(file, text, 'utf8');

const check = fs.readFileSync(file, 'utf8');
if (!check.includes(marker) || !check.includes('USER_REVIEW_IDENTITY_AMSC_001_W3')) {
    throw new Error('Identity checkpoint missing after patch');
}

if (boundaryOf(check) !== beforeCurrent) {
    throw new Error('the authoritative current region changed; Master Recovery patch must be additive only');
}

console.log('Master Recovery patched: Identity AMSC module recovery checkpoint inserted.');
