/**
 * TB-SHIPPING-GROUP-ID-CONTINUITY-153 — build the task-owned ISOLATED launch directory.
 * Same guarded recipe as TB-151 (TB-143/141/140 lineage), unchanged in every safety dimension: one
 * Active tenant, no tooba_alpha / tenant-alpha / Port=5432 / prior scratch port, no ShippingRateAuthority,
 * no ShippingOperationalShadow, no Edition swap. Task-153 owns new names/ports plus the same
 * seller-specific preparation days (A = 2, B = 3) so the two same-method promises stay independent.
 * The password comes from the environment and is never written into committed evidence.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-153-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-SHIPPING-GROUP-ID-CONTINUITY-153');
const basePath = path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json');
const base = JSON.parse(fs.readFileSync(basePath, 'utf8'));

const SCRATCH = {
  tenantId: 'store-alpha',
  reference: 'tenant-153-scratch',
  messagingReference: 'messaging-153-scratch',
  host: '127.0.0.1',
  port: '55472',
  database: 'tooba_153_scratch',
  messagingDatabase: 'tooba_153_messaging',
  user: 'scratch153',
  password: process.env.TOOBA_153_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) {
  console.error('ASSERT_FAIL: scratch password must come from the environment, never hard-coded');
  process.exit(2);
}
for (const ref of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
  if (ref.includes('tooba_alpha') || ref.includes('tenant-alpha')) {
    console.error('ASSERT_FAIL: a scratch reference must never point at the shared alpha target');
    process.exit(2);
  }
}
const priorPorts = ['5432', '55447', '55448', '55449', '55450', '55451', '55452', '55453', '55454', '55455', '55458', '55460', '55461', '55462', '55463', '55470'];
if (priorPorts.includes(SCRATCH.port)) {
  console.error('ASSERT_FAIL: scratch port must not be the shared 5432 listener nor a prior scratch port');
  process.exit(2);
}
if (!fs.existsSync(path.join(repo, '.tmp-153-host-out/Tooba.Host.exe'))) {
  console.error('ASSERT_FAIL: isolated Host publish output is missing');
  process.exit(2);
}

/** Seller-specific preparation days: task-owned, from the disposable scratch config, never invented. */
const sellerPrep = JSON.parse(fs.readFileSync(path.join(outDir, 'seller-prep.json'), 'utf8'));
const prepEntries = Object.entries(sellerPrep);
if (prepEntries.length < 2) {
  console.error('ASSERT_FAIL: two seller-specific preparation days are required for this proof');
  process.exit(2);
}
const prepValues = prepEntries.map(([, v]) => Number(v));
if (prepValues[0] === prepValues[1]) {
  console.error('ASSERT_FAIL: the two preparation days must differ (otherwise no independent earliest date)');
  process.exit(2);
}

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const HOST_PORT = process.env.TOOBA_153_HOST_PORT || '5413';
const WEB_PORT = process.env.TOOBA_153_WEB_PORT || '3108';

const overlay = {
  Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning' } },
  Tooba: {
    Edition: 'SingleStore',
    DeploymentId: 'isolated-153',
    TrustedProxies: [],
    Marketplace: { ConnectionReference: '' },
    SingleStore: {
      Tenants: [
        {
          TenantId: SCRATCH.tenantId,
          DisplayName: 'Isolated 153 Store',
          Status: 'Active',
          ConnectionReference: SCRATCH.reference,
          ThemeReference: 'theme-alpha',
          DefaultMarketReference: 'IR',
          StoreCommerce: { DefaultCurrency: 'IRR', SalesChannel: 'Marketplace' },
          PrimaryDomain: 'alpha.localhost',
          Hosts: ['alpha.localhost', 'alpha.localtest.me', 'localhost', '127.0.0.1'],
        },
      ],
    },
    PostgreSQL: {
      ConnectionReferences: {
        [SCRATCH.reference]:
          `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
          `Database=${SCRATCH.database};Application Name=tooba-153-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
        [SCRATCH.messagingReference]:
          `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
          `Database=${SCRATCH.messagingDatabase};Application Name=tooba-153-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
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
      CorsAllowedOrigins: [`http://127.0.0.1:${WEB_PORT}`, `http://localhost:${WEB_PORT}`],
      AuthRateLimitPermitLimit: 90,
      AuthRateLimitWindowSeconds: 60,
    },
    Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
    CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: true },
    ShippingOneLevelCatalog: { Enabled: true },
    ShippingMethods: {
      EnabledCodes: ['post', 'tipax', 'snapp_courier', 'store_courier', 'in_person'],
      DefaultSellerPreparationDays: 1,
      DeliveryHorizonDays: 7,
      SellerPreparationDaysByPartyId: sellerPrep,
    },
    Settlement: { MarketplaceChannelLedger: { Enabled: false } },
  },
  ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
const effectiveText = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), effectiveText);

const tenants = effective.Tooba.SingleStore.Tenants;
const guard = {
  tenantCount: tenants.length,
  activeTenantCount: tenants.filter((t) => t.Status === 'Active').length,
  tenants: tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
  canonicalGatewayTenantPresent: tenants.some((t) => t.TenantId === 'store-alpha' && t.Status === 'Active'),
  connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
  messagingReference: effective.Tooba.Messaging.ConnectionReference,
  containsSharedAlphaDatabase: effectiveText.includes('tooba_alpha'),
  containsSharedAlphaReference: effectiveText.includes('tenant-alpha'),
  containsSharedAlphaPort: /Port=5432\b/.test(effectiveText),
  containsPriorScratchPort: /Port=(55447|55448|55449|55450|55451|55452|55453|55454|55455|55458|55460|55461|55462|55463|55470|55471)\b/.test(effectiveText),
  scratchPort: SCRATCH.port,
  hostPort: HOST_PORT,
  webPort: WEB_PORT,
  runLegacyBootstraps: true,
  allowResetAndSeed: false,
  oneLevelCatalogEnabledInOverlay: effective.Tooba.ShippingOneLevelCatalog.Enabled === true,
  edition: effective.Tooba.Edition,
  editionSwapPerformed: false,
  saleChannelOfScratchTenant: tenants[0]?.StoreCommerce?.SalesChannel,
  sellerPreparationDaysByPartyId: Object.keys(sellerPrep).length,
  sellerPreparationDaysDiffer: prepValues[0] !== prepValues[1],
  shippingRateAuthoritySet: effectiveText.includes('ShippingRateAuthority'),
  shippingOperationalShadowSet: effectiveText.includes('ShippingOperationalShadow'),
};
guard.failClosed =
  guard.tenantCount === 1 &&
  guard.activeTenantCount === 1 &&
  guard.canonicalGatewayTenantPresent &&
  !guard.containsSharedAlphaDatabase &&
  !guard.containsSharedAlphaReference &&
  !guard.containsSharedAlphaPort &&
  !guard.containsPriorScratchPort &&
  !guard.shippingRateAuthoritySet &&
  !guard.shippingOperationalShadowSet &&
  guard.edition === 'SingleStore' &&
  guard.saleChannelOfScratchTenant === 'Marketplace' &&
  guard.sellerPreparationDaysByPartyId >= 2 &&
  guard.sellerPreparationDaysDiffer &&
  guard.webPort !== '3000' && guard.hostPort !== '5088';
fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));
fs.writeFileSync(
  path.join(evidenceDir, 'isolation-guard.json'),
  JSON.stringify(guard, null, 2) + '\n');

console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL'));
console.log(`GUARD=${JSON.stringify(guard)}`);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) { process.exit(3); }
