/**
 * TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164 — build the task-owned ISOLATED launch directory.
 * Same guarded recipe as TB-154, with the single difference this task explicitly authorizes:
 * the scratch-only overlay sets Tooba:ShippingOwnedRateAuthority:Enabled = <GATE>.
 * Committed appsettings stay false; the observe-only shadow gates stay unset.
 */
const fs = require('fs');
const path = require('path');

const repo = process.cwd();
const outDir = path.join(repo, '.tmp-164-run');
const evidenceDir = path.join(repo, 'docs', 'architecture', 'evidence', 'TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164');
const base = JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json'), 'utf8'));

const GATE = String(process.env.TOOBA_164_GATE || 'true').toLowerCase() === 'true';
const HOST_PORT = process.env.TOOBA_164_HOST_PORT || '5419';
const WEB_PORT = process.env.TOOBA_164_WEB_PORT || '3114';

const SCRATCH = {
  tenantId: 'store-alpha',
  reference: 'tenant-164-scratch',
  messagingReference: 'messaging-164-scratch',
  host: '127.0.0.1',
  port: '55484',
  database: 'tooba_164_scratch',
  messagingDatabase: 'tooba_164_messaging',
  user: 'scratch164',
  password: process.env.TOOBA_164_SCRATCH_PASSWORD || '',
};

if (!SCRATCH.password) { console.error('ASSERT_FAIL: scratch password must come from the environment'); process.exit(2); }
for (const ref of Object.values(SCRATCH).filter((v) => typeof v === 'string')) {
  if (ref.includes('tooba_alpha') || ref.includes('tenant-alpha')) { console.error('ASSERT_FAIL: shared alpha target'); process.exit(2); }
}
for (const forbidden of ['5432', '55473', '55475', '55476', '55477', '55478', '55479', '55480', '55481']) {
  if (SCRATCH.port === forbidden) { console.error('ASSERT_FAIL: scratch port collides: ' + forbidden); process.exit(2); }
}
if (!fs.existsSync(path.join(repo, '.tmp-164-host-out/Tooba.Host.exe'))) { console.error('ASSERT_FAIL: Host publish missing'); process.exit(2); }

fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(evidenceDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'appsettings.json'), JSON.stringify(base, null, 2));

const overlay = {
  Logging: { LogLevel: { Default: 'Information', 'Microsoft.AspNetCore': 'Warning', 'Microsoft.EntityFrameworkCore': 'Warning' } },
  Tooba: {
    Edition: 'SingleStore',
    DeploymentId: 'isolated-164',
    TrustedProxies: [],
    Marketplace: { ConnectionReference: '' },
    SingleStore: {
      Tenants: [{
        TenantId: SCRATCH.tenantId,
        DisplayName: 'Isolated 164 Store',
        Status: 'Active',
        ConnectionReference: SCRATCH.reference,
        ThemeReference: 'theme-alpha',
        DefaultMarketReference: 'IR',
        StoreCommerce: { DefaultCurrency: 'IRR', SalesChannel: 'Marketplace' },
        PrimaryDomain: 'alpha.localhost',
        Hosts: ['alpha.localhost', 'alpha.localtest.me', 'localhost', '127.0.0.1'],
      }],
    },
    PostgreSQL: {
      ConnectionReferences: {
        [SCRATCH.reference]:
          `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
          `Database=${SCRATCH.database};Application Name=tooba-164-isolated;Timeout=15;Command Timeout=120;Maximum Pool Size=20`,
        [SCRATCH.messagingReference]:
          `Host=${SCRATCH.host};Port=${SCRATCH.port};Username=${SCRATCH.user};Password=${SCRATCH.password};` +
          `Database=${SCRATCH.messagingDatabase};Application Name=tooba-164-isolated-messaging;Timeout=15;Command Timeout=120;Maximum Pool Size=10`,
      },
    },
    Messaging: {
      Enabled: true, Transport: 'PostgreSql', ConnectionReference: SCRATCH.messagingReference,
      Schema: 'transport', UseInProcessTestDouble: false,
    },
    Authorization: { Mode: 'InMemory', ApplySchemaOnStartup: false },
    AuthSecurity: {
      CorsAllowedOrigins: [`http://127.0.0.1:${WEB_PORT}`, `http://localhost:${WEB_PORT}`],
      AuthRateLimitPermitLimit: 120, AuthRateLimitWindowSeconds: 60,
    },
    Media: { LocalRoot: 'App_Data/media', MaxUploadBytes: 5000000 },
    CatalogDemo: { AllowResetAndSeed: false, RunLegacyBootstraps: true },
    ShippingOneLevelCatalog: { Enabled: true },
    // The ONLY authority flag this task may enable, and only here in the disposable overlay.
    ShippingOwnedRateAuthority: { Enabled: GATE },
    ShippingMethods: {
      EnabledCodes: ['post', 'tipax', 'snapp_courier', 'store_courier', 'in_person'],
      DefaultSellerPreparationDays: 2,
      DeliveryHorizonDays: 7,
    },
    Settlement: { MarketplaceChannelLedger: { Enabled: false } },
  },
  ConnectionStrings: {},
};
fs.writeFileSync(path.join(outDir, 'appsettings.Development.json'), JSON.stringify(overlay, null, 2));

const effective = JSON.parse(JSON.stringify(base));
effective.Tooba = Object.assign({}, base.Tooba || {}, overlay.Tooba);
const text = JSON.stringify(effective, null, 2);
fs.writeFileSync(path.join(outDir, 'effective-config.json'), text);

const tenants = effective.Tooba.SingleStore.Tenants;
const guard = {
  gate: GATE,
  gateFlagLocation: 'scratch overlay only (.tmp-164-run/appsettings.Development.json)',
  committedDefaults: {
    appsettings: JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.json'), 'utf8')).Tooba?.ShippingOwnedRateAuthority?.Enabled,
    development: JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.Development.json'), 'utf8')).Tooba?.ShippingOwnedRateAuthority?.Enabled,
    production: JSON.parse(fs.readFileSync(path.join(repo, 'src/backend/Host/Tooba.Host/appsettings.Production.json'), 'utf8')).Tooba?.ShippingOwnedRateAuthority?.Enabled,
  },
  tenantCount: tenants.length,
  activeTenantCount: tenants.filter((t) => t.Status === 'Active').length,
  tenants: tenants.map((t) => `${t.TenantId}:${t.Status}:${t.ConnectionReference}`),
  connectionReferences: Object.keys(effective.Tooba.PostgreSQL.ConnectionReferences),
  scratchPort: SCRATCH.port,
  hostPort: HOST_PORT,
  webPort: WEB_PORT,
  containsSharedAlphaDatabase: text.includes('tooba_alpha'),
  containsSharedAlphaReference: text.includes('tenant-alpha'),
  containsSharedPort5432: /Port=5432\b/.test(text),
  shippingRateAuthorityShadowSet: text.includes('ShippingRateAuthority"') || text.includes('ShippingRateAuthority:'),
  shippingOperationalShadowSet: text.includes('ShippingOperationalShadow'),
  edition: effective.Tooba.Edition,
  runLegacyBootstraps: true,
  allowResetAndSeed: false,
  legacyRatesPresent: Array.isArray(base.Tooba?.ShippingMethods?.Rates) && base.Tooba.ShippingMethods.Rates.length > 0,
  sharedFrontendPort3000Touched: false,
  sharedHostPort5088Touched: false,
};
guard.failClosed =
  guard.tenantCount === 1 && guard.activeTenantCount === 1 &&
  !guard.containsSharedAlphaDatabase && !guard.containsSharedAlphaReference &&
  !guard.containsSharedPort5432 &&
  !guard.shippingRateAuthorityShadowSet && !guard.shippingOperationalShadowSet &&
  guard.edition === 'SingleStore' &&
  guard.committedDefaults.appsettings === false &&
  guard.committedDefaults.development === false &&
  guard.committedDefaults.production === false &&
  guard.webPort !== '3000' && guard.hostPort !== '5088';
fs.writeFileSync(path.join(outDir, 'isolation-guard.json'), JSON.stringify(guard, null, 2));
fs.writeFileSync(path.join(evidenceDir, `isolation-guard-${GATE ? 'on' : 'off'}-164.json`), JSON.stringify(guard, null, 2) + '\n');
console.log('PREPARE=' + (guard.failClosed ? 'PASS' : 'FAIL') + ' GATE=' + GATE);
console.log('CREDENTIALS_WRITTEN_TO_EVIDENCE=false');
if (!guard.failClosed) process.exit(3);
