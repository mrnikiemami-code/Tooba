// TB-TMAR-PRICING-AMSC-001-W3-R2 — bounded validation audit (structure repair).
// Prints the endpoint-applicability and identity-preservation facts recorded in endpoint-applicability-audit.md
// and validation.md. Read-only: no file is created or modified.
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..', '..', '..');
const MODULE = path.join(ROOT, 'src', 'backend', 'Modules', 'Pricing');

function walk(dir, acc = []) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            if (['bin', 'obj', 'artifacts'].includes(entry.name)) continue;
            walk(full, acc);
        } else if (entry.name.endsWith('.cs')) {
            acc.push(full);
        }
    }
    return acc;
}

const read = (p) => fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, '');

function count(text, needle) {
    return text.split(needle).length - 1;
}

function countInProduction(needle) {
    const projects = ['Tooba.Pricing.Contracts', 'Tooba.Pricing.Domain', 'Tooba.Pricing.Application', 'Tooba.Pricing.Infrastructure'];
    let total = 0;
    for (const project of projects) {
        for (const file of walk(path.join(MODULE, project))) {
            total += count(read(file), needle);
        }
    }
    return total;
}

const hostProgram = read(path.join(ROOT, 'src', 'backend', 'Host', 'Tooba.Host', 'Program.cs'));
const hostCsproj = read(path.join(ROOT, 'src', 'backend', 'Host', 'Tooba.Host', 'Tooba.Host.csproj'));
const pricingModule = read(path.join(MODULE, 'Tooba.Pricing.Infrastructure', 'DependencyInjection', 'PricingModule.cs'));
const slnx = read(path.join(ROOT, 'src', 'backend', 'Tooba.slnx'));

const folderStart = slnx.indexOf('<Folder Name="/Modules/Pricing/">');
const group = slnx.slice(folderStart, slnx.indexOf('</Folder>', folderStart));

const codes = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Errors', 'PricingErrorCodes.cs'));
const contributor = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Errors', 'PricingErrorCatalogContributor.cs'));
const en = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Resources', 'PricingErrors.resx'));
const fa = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Resources', 'PricingErrors.fa.resx'));
const keys = (text) => [...text.matchAll(/<data name="(pricing\.[^"]+)"/g)].map((m) => m[1]).sort();

const endpointProjectDir = path.join(MODULE, 'Tooba.Pricing.Endpoints');
const endpointTestDir = path.join(MODULE, 'Tooba.Pricing.Tests', 'Endpoints');

const report = {
    module_cs_files: walk(MODULE).length,
    endpoint_project_dir_exists: fs.existsSync(endpointProjectDir),
    endpoint_test_dir_exists: fs.existsSync(endpointTestDir),
    production_Tooba_Pricing_Endpoints: countInProduction('Tooba.Pricing.Endpoints'),
    production_MapPricingModule: countInProduction('MapPricingModule'),
    production_AddPricingEndpointPresentation: countInProduction('AddPricingEndpointPresentation'),
    production_v1_pricing_literal: countInProduction('"/v1/pricing"'),
    production_MapGet: countInProduction('MapGet('),
    production_MapPost: countInProduction('MapPost('),
    production_MapPut: countInProduction('MapPut('),
    production_MapDelete: countInProduction('MapDelete('),
    production_MapPatch: countInProduction('MapPatch('),
    production_MapGroup: countInProduction('MapGroup('),
    production_ISender: countInProduction('ISender'),
    production_IEndpointRouteBuilder: countInProduction('IEndpointRouteBuilder'),
    host_Tooba_Pricing_Endpoints: count(hostProgram, 'Tooba.Pricing.Endpoints'),
    host_MapPricingModule: count(hostProgram, 'MapPricingModule'),
    host_AddPricingEndpointPresentation: count(hostProgram, 'AddPricingEndpointPresentation'),
    host_v1_pricing_literal: count(hostProgram, '"/v1/pricing"'),
    host_csproj_Pricing_Endpoints_ref: count(hostCsproj, 'Tooba.Pricing.Endpoints'),
    host_csproj_Pricing_Infrastructure_ref: count(hostCsproj, 'Tooba.Pricing.Infrastructure.csproj'),
    registration_catalog_contributor: count(pricingModule, 'AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>'),
    registration_resource_set: count(pricingModule, 'AddSingleton<IErrorResourceSet, PricingErrorResourceSet>'),
    contracts_catalog_contributor_classes: count(contributor, 'class PricingErrorCatalogContributor'),
    slnx_pricing_project_entries: (group.match(/<Project Path=/g) || []).length,
    slnx_pricing_has_endpoints: group.includes('Tooba.Pricing.Endpoints'),
    stable_codes: count(codes, 'public const string '),
    descriptor_factories: count(contributor, 'D(PricingErrorCodes.'),
    descriptor_ctor_sites: count(contributor, 'new('),
    en_resource_keys: keys(en).length,
    fa_resource_keys: keys(fa).length,
    en_fa_key_sets_identical: JSON.stringify(keys(en)) === JSON.stringify(keys(fa)),
    knowncodes_guard: codes.includes('private static readonly HashSet<string> KnownCodes'),
    isknown_guard: codes.includes('public static bool IsKnown(string? code)'),
    migration_files: fs.readdirSync(path.join(MODULE, 'Tooba.Pricing.Infrastructure', 'Persistence', 'Migrations'))
        .filter((n) => n.endsWith('.cs')).sort(),
};

console.log(JSON.stringify(report, null, 2));
