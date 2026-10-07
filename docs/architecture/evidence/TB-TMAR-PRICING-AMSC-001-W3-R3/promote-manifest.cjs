// TB-TMAR-PRICING-AMSC-001-W3-R3 — manifest promotion: move Pricing from preCertModules into the
// certified modules array with a fresh W3-R3 certification note. Structure is byte-preserved
// (2-space indent, no BOM, LF); only the Pricing entry is relocated and its metadata refreshed.
const fs = require('fs');

const path = 'docs/architecture/tmar-module-structure-manifests.json';
const raw = fs.readFileSync(path, 'utf8');
const bom = raw.startsWith('\uFEFF') ? '\uFEFF' : '';
const body = bom ? raw.slice(1) : raw;
const doc = JSON.parse(body);

const index = doc.preCertModules.findIndex((m) => m.module === 'Pricing');
if (index < 0) {
    throw new Error('Pricing not found in preCertModules');
}

const entry = doc.preCertModules.splice(index, 1)[0];
if (doc.modules.some((m) => m.module === 'Pricing')) {
    throw new Error('Pricing already present in modules');
}

entry.structureCertified = true;
entry.lockVersion = 'ARCH-COMPLETE-002';
delete entry.structureState;
delete entry.structureRepairTask;
entry.structureAuthorityTask = 'TB-TMAR-PRICING-AMSC-001-W3-R2';
entry.structureAuthorityCommit = '7159c8f773c1faa9b4b6d425b19067f50ca27572';
entry.certificationNote =
    'RECERTIFIED by TB-TMAR-PRICING-AMSC-001-W3-R3 (tooba-architecture-certify) over the W3-R2 structure authority (commit 7159c8f773c1faa9b4b6d425b19067f50ca27572). The superseded TB-TMAR-PRICING-AMSC-001-W3 certification (commit 3c2cc61e) claimed NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER while the module still carried a ceremonial Tooba.Pricing.Endpoints project, an empty /v1/pricing route group and Host mapping/registration ceremony; W3-R2 removed that ceremony and this fresh wave independently re-verified the repaired surface. Pricing is INTERNAL_ONLY: five projects (Contracts, Domain, Application, Infrastructure, Tests), zero module/Host HTTP routes, zero endpoint-reachable requests, so the CQRS/validator matrix is NOT_APPLICABLE_INTERNAL_ONLY by construction. Single Contracts/Errors stable-code home (11 declared codes + KnownCodes/IsKnown), one PricingErrorCatalogContributor (11 descriptors) and one PricingErrorResourceSet claiming the pricing. keyspace with the bilingual PricingErrors.resx/.fa.resx pair (11 EN + 11 FA keys); the catalog contributor and resource set are registered exactly once by PricingModule.AddServices in the Infrastructure composition root (certified Inventory precedent). Application/Composition/PricingOperation.cs is the single dual typed-fault-to-Result seam (ContractOperationException by declared code + SemanticException) with zero ex.Message classification. Capability-first shallow folders, path<->namespace EXACT (0 mismatches), empty root allowlists (Domain keeps only the GlobalUsings namespace bridge), CLEAN physical copies and the canonical /Modules/Pricing/ five-project solution grouping. Microservice-extractable: zero foreign Application/Infrastructure/Domain edge; the only foreign reference is the legal Tooba.Offer.Contracts lookup seam; Promotion consumes Tooba.Pricing.Contracts.Ports only; no cross-module join; own pricing schema, own PricingDbContext, own outbox, own IPricingSchemaMigrator and the single unchanged migration 20260823085546_InitialPricing. Host authority ZERO (no Host/Pricing folder, no Host Pricing business/persistence/HTTP file) and the global HOST_ROOT_FINAL_CERTIFIED checkpoint is preserved. Schema/migrations unchanged; production behavior preserved; guardsWeakened NONE; baselinesWidened NONE. Post-cert recovery debt disclosed as POST_CERT_RECOVERY_RECONCILIATION_REQUIRED and deliberately NOT repaired in this wave. Stop gate USER_REVIEW_PRICING_AMSC_001_W3_R3; automaticNextImplementationTask NONE.';

doc.modules.push(entry);

const out = JSON.stringify(doc, null, 2) + (body.endsWith('\n') ? '\n' : '');
fs.writeFileSync(path, bom + out, 'utf8');

console.log('modules', doc.modules.length);
console.log('preCertModules', doc.preCertModules.map((m) => m.module).join(','));
console.log('pricingProjects', doc.modules.find((m) => m.module === 'Pricing').projects.map((p) => p.projectName).join(','));
