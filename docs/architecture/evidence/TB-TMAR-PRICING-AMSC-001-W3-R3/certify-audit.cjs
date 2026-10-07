// TB-TMAR-PRICING-AMSC-001-W3-R3 — independent certification verification (tooba-architecture-certify).
// Read-only: re-derives every certification axis directly from disk. No file is created or modified.
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..', '..', '..');
const MODULE = path.join(ROOT, 'src', 'backend', 'Modules', 'Pricing');
const HOST = path.join(ROOT, 'src', 'backend', 'Host', 'Tooba.Host');

const PRODUCTION = [
    'Tooba.Pricing.Contracts',
    'Tooba.Pricing.Domain',
    'Tooba.Pricing.Application',
    'Tooba.Pricing.Infrastructure',
];

const read = (p) => fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, '');
const count = (t, n) => t.split(n).length - 1;

function walk(dir, ext = '.cs', acc = []) {
    for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, e.name);
        if (e.isDirectory()) {
            if (['bin', 'obj', 'artifacts'].includes(e.name)) continue;
            walk(full, ext, acc);
        } else if (e.name.endsWith(ext)) {
            acc.push(full);
        }
    }
    return acc;
}

function productionSources() {
    return PRODUCTION.flatMap((p) => walk(path.join(MODULE, p)));
}

function allSources() {
    return walk(MODULE);
}

function refs(project) {
    const csproj = path.join(MODULE, project, `${project}.csproj`);
    return [...read(csproj).matchAll(/Include="([^"]*\.csproj)"/g)].map((m) => m[1]);
}

const foreignModule =
    /Tooba\.(?!Pricing\.)[A-Za-z]+\.(Application|Infrastructure|Domain|Endpoints)/;

const pricingModule = read(path.join(MODULE, 'Tooba.Pricing.Infrastructure', 'DependencyInjection', 'PricingModule.cs'));
const dbContext = read(path.join(MODULE, 'Tooba.Pricing.Infrastructure', 'Persistence', 'PricingDbContext.cs'));
const operation = read(path.join(MODULE, 'Tooba.Pricing.Application', 'Composition', 'PricingOperation.cs'));
const codes = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Errors', 'PricingErrorCodes.cs'));
const contributor = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Errors', 'PricingErrorCatalogContributor.cs'));
const resourceSet = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Errors', 'PricingErrorResourceSet.cs'));
const en = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Resources', 'PricingErrors.resx'));
const fa = read(path.join(MODULE, 'Tooba.Pricing.Contracts', 'Resources', 'PricingErrors.fa.resx'));
const keys = (t) => [...t.matchAll(/<data name="(pricing\.[^"]+)"/g)].map((m) => m[1]).sort();

const hostProgram = read(path.join(HOST, 'Program.cs'));
const hostCsproj = read(path.join(HOST, 'Tooba.Host.csproj'));
const hostSources = walk(HOST);

const slnx = read(path.join(ROOT, 'src', 'backend', 'Tooba.slnx'));
const folderStart = slnx.indexOf('<Folder Name="/Modules/Pricing/">');
const slnxGroup = slnx.slice(folderStart, slnx.indexOf('</Folder>', folderStart));

const manifest = JSON.parse(read(path.join(ROOT, 'docs/architecture/tmar-module-structure-manifests.json')));
const sot = JSON.parse(read(path.join(ROOT, 'docs/architecture/tmar-current-state.json')));

const pricingProjects = PRODUCTION.map((p) => {
    const dir = path.join(MODULE, p);
    return {
        project: p,
        rootCs: fs.readdirSync(dir).filter((n) => n.endsWith('.cs')).sort(),
        folders: fs.readdirSync(dir, { withFileTypes: true })
            .filter((e) => e.isDirectory() && !['bin', 'obj', 'artifacts'].includes(e.name))
            .map((e) => e.name).sort(),
    };
});

// path<->namespace exactness for every production .cs
const nsMismatches = [];
for (const p of PRODUCTION) {
    const dir = path.join(MODULE, p);
    for (const file of walk(dir)) {
        const rel = path.relative(dir, file);
        if (path.basename(rel).toLowerCase().startsWith('globalusings')) continue;
        const sub = path.dirname(rel);
        const expected = sub === '.' ? p : `${p}.${sub.split(path.sep).join('.')}`;
        const m = read(file).match(/^namespace\s+([A-Za-z0-9_.]+)/m);
        if (!m) nsMismatches.push(`${rel}: NO NAMESPACE`);
        else if (m[1] !== expected) nsMismatches.push(`${p}/${rel}: ${m[1]} != ${expected}`);
    }
}

const foreignHits = [];
for (const file of productionSources()) {
    if (foreignModule.test(read(file))) {
        foreignHits.push(path.relative(ROOT, file));
    }
}

const hostAuthorityHits = hostSources
    .filter((f) => /PricingDbContext|AuthoredPrice|IPriceDirectory|MapPricingModule|Pricing\.Endpoints/.test(read(f)))
    .map((f) => path.relative(ROOT, f));

const report = {
    head: {
        branch: (() => {
            const head = fs.readFileSync(path.join(ROOT, '.git', 'HEAD'), 'utf8').trim();
            return head.startsWith('ref: ') ? head.slice(5).replace('refs/heads/', '') : 'DETACHED';
        })(),
        startingHead: '7159c8f773c1faa9b4b6d425b19067f50ca27572',
    },
    structure: {
        projectDirs: fs.readdirSync(MODULE, { withFileTypes: true })
            .filter((e) => e.isDirectory()).map((e) => e.name).sort(),
        endpointProjectDir: fs.existsSync(path.join(MODULE, 'Tooba.Pricing.Endpoints')),
        endpointTestDir: fs.existsSync(path.join(MODULE, 'Tooba.Pricing.Tests', 'Endpoints')),
        slnxPricingProjectCount: (slnxGroup.match(/<Project Path=/g) || []).length,
        slnxHasEndpoints: slnxGroup.includes('Tooba.Pricing.Endpoints'),
        perProject: pricingProjects,
        namespaceMismatches: nsMismatches,
    },
    httpApplicability: {
        production_MapGet: productionSources().reduce((a, f) => a + count(read(f), 'MapGet('), 0),
        production_MapPost: productionSources().reduce((a, f) => a + count(read(f), 'MapPost('), 0),
        production_MapPut: productionSources().reduce((a, f) => a + count(read(f), 'MapPut('), 0),
        production_MapDelete: productionSources().reduce((a, f) => a + count(read(f), 'MapDelete('), 0),
        production_MapPatch: productionSources().reduce((a, f) => a + count(read(f), 'MapPatch('), 0),
        production_MapGroup: productionSources().reduce((a, f) => a + count(read(f), 'MapGroup('), 0),
        production_IEndpointRouteBuilder: productionSources().reduce((a, f) => a + count(read(f), 'IEndpointRouteBuilder'), 0),
        production_ISender: productionSources().reduce((a, f) => a + count(read(f), 'ISender'), 0),
        production_MediatR: productionSources().reduce((a, f) => a + count(read(f), 'MediatR'), 0),
        production_EndpointsRef: productionSources().reduce((a, f) => a + count(read(f), 'Tooba.Pricing.Endpoints'), 0),
        host_MapPricingModule: count(hostProgram, 'MapPricingModule'),
        host_AddPricingEndpointPresentation: count(hostProgram, 'AddPricingEndpointPresentation'),
        host_EndpointsRef: count(hostProgram, 'Tooba.Pricing.Endpoints'),
        host_csproj_EndpointsRef: count(hostCsproj, 'Tooba.Pricing.Endpoints'),
        host_csproj_PricingInfraRef: count(hostCsproj, 'Tooba.Pricing.Infrastructure.csproj'),
    },
    errorsAndLocalization: {
        declaredCodes: count(codes, 'public const string '),
        knownCodesGuard: codes.includes('private static readonly HashSet<string> KnownCodes'),
        isKnownGuard: codes.includes('public static bool IsKnown(string? code)'),
        catalogContributorClasses: count(contributor, 'class PricingErrorCatalogContributor'),
        descriptorFactories: count(contributor, 'D(PricingErrorCodes.'),
        resourceSetClasses: count(resourceSet, 'class PricingErrorResourceSet'),
        enKeys: keys(en).length,
        faKeys: keys(fa).length,
        enFaIdentical: JSON.stringify(keys(en)) === JSON.stringify(keys(fa)),
        registrationContributor: count(pricingModule, 'AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>'),
        registrationResourceSet: count(pricingModule, 'AddSingleton<IErrorResourceSet, PricingErrorResourceSet>'),
        contractsOwnsContributorAssembly: resourceSet.includes('Tooba.Pricing.Contracts'),
    },
    typedFault: {
        contractOperationIsKnownFilter: count(operation, 'catch (ContractOperationException ex) when (PricingErrorCodes.IsKnown(ex.Code))'),
        semanticExceptionCatch: count(operation, 'catch (SemanticException ex)'),
        messageClassification: count(operation, 'ex.Message'),
        production_ExMessage: productionSources().reduce((a, f) => a + count(read(f), '.Message'), 0),
    },
    boundaries: {
        projectRefs: Object.fromEntries(PRODUCTION.map((p) => [p, refs(p)])),
        foreignAppInfraDomainSources: foreignHits,
        promotionReferencesPricingApplication: walk(path.join(ROOT, 'src/backend/Modules/Promotion'), '.csproj')
            .filter((f) => read(f).includes('Tooba.Pricing.Application')).map((f) => path.relative(ROOT, f)),
        promotionReferencesPricingContracts: walk(path.join(ROOT, 'src/backend/Modules/Promotion'), '.csproj')
            .filter((f) => read(f).includes('Tooba.Pricing.Contracts')).map((f) => path.relative(ROOT, f)),
    },
    persistence: {
        schemaLiteral: count(dbContext, '"pricing"'),
        dbSetAuthoredPrice: count(dbContext, 'DbSet<AuthoredPrice>'),
        dbContextClasses: count(dbContext, 'class PricingDbContext'),
        schemaMigratorRegistration: count(pricingModule, 'AddModuleSchemaMigrator("Pricing", ModuleSchemaMigrationOrder.Pricing,'),
        outboxRegistration: count(pricingModule, 'PricingOutboxRegistration'),
        migrations: fs.readdirSync(path.join(MODULE, 'Tooba.Pricing.Infrastructure', 'Persistence', 'Migrations')).sort(),
    },
    hostAuthority: {
        hits: hostAuthorityHits,
        hostPricingFolder: fs.existsSync(path.join(HOST, 'Pricing')),
    },
    cohesion: {
        largestProductionFileLoc: productionSources()
            .map((f) => ({ f: path.relative(ROOT, f), loc: read(f).split('\n').length }))
            .sort((a, b) => b.loc - a.loc).slice(0, 3),
        over800: productionSources().filter((f) => read(f).split('\n').length > 800).map((f) => path.relative(ROOT, f)),
    },
    manifest: {
        certifiedCount: manifest.modules.length,
        pricingInModules: manifest.modules.some((m) => m.module === 'Pricing'),
        pricingInPreCert: (manifest.preCertModules || []).some((m) => m.module === 'Pricing'),
        pricingEntry: manifest.modules.find((m) => m.module === 'Pricing') || null,
    },
    sot: {
        certifiedModuleCount: sot.structureLock.certifiedModules.length,
        pricingInCertified: sot.structureLock.certifiedModules.includes('Pricing'),
        pricingInCertifiedExactlyOnce:
            sot.structureLock.certifiedModules.filter((m) => m === 'Pricing').length,
        hasW3R3Block: Object.prototype.hasOwnProperty.call(sot, 'pricingAmsc001W3R3'),
        hostCheckpoint: sot.currentHostCheckpoint,
        lastAcceptedTask: sot.lastAcceptedTask,
        automaticNextImplementationTask: sot.automaticNextImplementationTask,
    },
};

console.log(JSON.stringify(report, null, 2));
