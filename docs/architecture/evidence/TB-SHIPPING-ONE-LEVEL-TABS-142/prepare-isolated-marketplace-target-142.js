/**
 * TB-SHIPPING-ONE-LEVEL-TABS-142 — build the task-owned ISOLATED Marketplace-edition launch directory.
 *
 * Mission A of TB-142 authorizes a disposable Marketplace-edition overlay ONLY through the canonical
 * existing Marketplace connection-reference authority (Tooba:Marketplace:ConnectionReference) with
 * fail-closed tenant binding, and forbids fabricated production settings.
 *
 * This recipe therefore:
 *  - keeps Edition=Marketplace and the ONE canonical marketplace connection reference;
 *  - keeps SingleStore.Tenants EMPTY (a Marketplace registry has no per-tenant binding by construction,
 *    so tenant binding is fail-closed: any request resolves to the marketplace reference or fails);
 *  - points the marketplace + messaging references at two disposable scratch databases on a fresh
 *    scratch port, never at tooba_alpha / tenant-alpha / shared 5432;
 *  - uses the fake no-charge sandbox Payment gateway only;
 *  - never enables automatic Settlement host-startup migration registration (no source change at all).
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-142-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-SHIPPING-ONE-LEVEL-TABS-142');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
    edition: 'Marketplace',
    marketplaceReference: 'marketplace-142-scratch',
    messagingReference: 'messaging-142-scratch',
    host: '127.0.0.1',
    port: '55463',
    database: 'tooba_142_marketplace',
    messagingDatabase: 'tooba_142_messaging',
    user: 'scratch142',
    password: process.env.TOOBA_142_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) {
    console.error('ASSERT_FAIL: scratch password must come from the environment, never hard-coded');
    process.exit(2);
}
for (const value of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
    if (value.includes('tooba_alpha') || value.includes('tenant-alpha')) {
        console.error('ASSERT_FAIL: a scratch reference must never point at the shared alpha target');
        process.exit(2);
    }
}
const priorPorts = ['5432', '55447', '55448', '55449', '55450', '55451', '55452', '55453', '55454', '55455', '55458', '55460', '55461', '55462'];
if (priorPorts.includes(SCRATCH.port)) {
    console.error('ASSERT_FAIL: scratch port must not be the shared 5432 listener nor a prior scratch port');
    process.exit(2);
}
if (!fs.existsSync(path.join(repo, '.tmp-140-host-out/Tooba.Host.exe'))) {
    console.error('ASSERT_FAIL: isolated Host publish output is missing');
    process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
    Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
    Tooba: {
        Edition: SCRATCH.edition,
        DeploymentId: 'isolated-142',
        TrustedProxies: [],
        Marketplace: { ConnectionReference: SCRATCH.marketplaceReference },
        SingleStore: { Tenants: [] },
        PostgreSQL: {
            ConnectionReferences: {
                [SCRATCH.marketplaceReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.database};Application Name=tooba-142-isolated-marketplace;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
                [SCRATCH.messagingReference]:
                    `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
                    `Database=${SCRATCH.messagingDatabase};Application Name=tooba-142-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
            },
        },
        Messaging: {
            Enabled: true,
            Transport: 'PostgreSql',
            ConnectionReference: SCRATCH.messagingReference,
            Schema: 'transport',
            UseInProcessTestDouble: false,
        },
        Authorization: { Mode: 'InMemory', ApplySchemaOnStartup: false },
        AuthSecurity: {
            CorsAllowedOrigins: ['http://127.0.0.1:3107', 'http://localhost:3107'],
            AuthRateLimitPermitLimit: 30,
            AuthRateLimitWindowSeconds: 60,
        },
        StoreCommerce: { DefaultCurrency: 'IRR', SalesChannel: 'Marketplace' },
        Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
        // Marketplace Development startup runs MarketplaceDevelopmentBootstrap, NOT the SingleStore
        // legacy Catalog bootstrap chain. Both flags are pinned false so no SingleStore seed path is
        // reachable from this overlay.
        CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: false },
        ShippingOneLevelCatalog: { Enabled: true },
        ShippingMethods: {
            EnabledCodes: ['post', 'tipax', 'snapp_courier', 'store_courier', 'in_person'],
            DefaultSellerPreparationDays: 1,
            DeliveryHorizonDays: 7,
        },
    },
    Payment: {
        Gateway: {
            Mode: 'Sandbox',
            DefaultProvider: 'fake',
            WebhookSigningSecret: '',
            InitiateBaseUrl: '',
            StatusQueryBaseUrl: '',
            StatusQueryApiKey: '',
            AllowedStatusQueryHosts: [],
        },
    },
    ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
effective.Payment = Object.assign({}, base.Payment || {}, overlay.Payment, {
    Gateway: Object.assign({}, (base.Payment || {}).Gateway || {}, overlay.Payment.Gateway),
});
const effectiveText = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), effectiveText);

const tenants = effective.Tooba.SingleStore.Tenants;
const guard = {
    edition: effective.Tooba.Edition,
    marketplaceConnectionReference: effective.Tooba.Marketplace.ConnectionReference,
    marketplaceReferenceConfigured: typeof effective.Tooba.Marketplace.ConnectionReference === 'string'
        && effective.Tooba.Marketplace.ConnectionReference.trim().length > 0,
    tenantCount: tenants.length,
    activeTenantCount: tenants.filter((t) => t.Status === 'Active').length,
    tenants: tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
    connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
    messagingReference: effective.Tooba.Messaging.ConnectionReference,
    containsSharedAlphaDatabase: effectiveText.includes('tooba_alpha'),
    containsSharedAlphaReference: effectiveText.includes('tenant-alpha'),
    containsSharedAlphaPort: /Port=5432\b/.test(effectiveText),
    containsPriorScratchPort: /Port=(55447|55448|55449|55450|55451|55452|55453|55454|55455|55458|55460|55461|55462)\b/.test(effectiveText),
    scratchPort: SCRATCH.port,
    runLegacyBootstraps: effective.Tooba.CatalogDemo.RunLegacyBootstraps,
    allowResetAndSeed: effective.Tooba.CatalogDemo.AllowResetAndSeed,
    authorizationMode: effective.Tooba.Authorization.Mode,
    paymentGatewayMode: effective.Payment.Gateway.Mode,
    paymentDefaultProvider: effective.Payment.Gateway.DefaultProvider,
    pspEndpointsEmpty: effective.Payment.Gateway.InitiateBaseUrl === ''
        && effective.Payment.Gateway.StatusQueryBaseUrl === ''
        && effective.Payment.Gateway.WebhookSigningSecret === '',
    oneLevelCatalogEnabledInOverlay: effective.Tooba.ShippingOneLevelCatalog.Enabled === true,
    shippingRateAuthoritySet: effectiveText.includes('ShippingRateAuthority'),
    shippingOperationalShadowSet: effectiveText.includes('ShippingOperationalShadow'),
    // Fail-closed binding: Marketplace carries no tenant list by construction; the only commerce
    // binding is the canonical marketplace connection reference, and it must resolve.
    failClosedTenantBinding: tenants.length === 0
        && effective.Tooba.Marketplace.ConnectionReference.trim().length > 0
        && Object.prototype.hasOwnProperty.call(
            effective.Tooba.PostgreSQL.ConnectionReferences,
            effective.Tooba.Marketplace.ConnectionReference.trim()),
};
guard.failClosed =
    guard.edition === 'Marketplace' &&
    guard.marketplaceReferenceConfigured &&
    guard.failClosedTenantBinding &&
    guard.tenantCount === 0 &&
    !guard.containsSharedAlphaDatabase &&
    !guard.containsSharedAlphaReference &&
    !guard.containsSharedAlphaPort &&
    !guard.containsPriorScratchPort &&
    guard.runLegacyBootstraps === false &&
    guard.allowResetAndSeed === false &&
    guard.authorizationMode === 'InMemory' &&
    guard.paymentGatewayMode === 'Sandbox' &&
    guard.pspEndpointsEmpty &&
    !guard.shippingRateAuthoritySet &&
    !guard.shippingOperationalShadowSet;

fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));
fs.writeFileSync(path.join(evidenceDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2) + '\n');

console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL'));
console.log(`GUARD=${JSON.stringify(guard)}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) { process.exit(3); }
