const fs = require('fs');
const path = require('path');

const repo = process.argv[2] || '.';
const manifestPath = path.join(repo, 'docs/architecture/tmar-module-structure-manifests.json');
const statePath = path.join(repo, 'docs/architecture/tmar-current-state.json');

const NL = '\n';

// ---------------------------------------------------------------- manifest ---
const manifestRaw = fs.readFileSync(manifestPath, 'utf8');
const manifest = JSON.parse(manifestRaw);

if (!Array.isArray(manifest.preCertModules)) {
  throw new Error('preCertModules missing');
}
if (manifest.modules.some((m) => m.module === 'Wallet')) {
  throw new Error('Wallet must not be certified in modules[] during W2');
}
if (manifest.preCertModules.some((m) => m.module === 'Wallet')) {
  throw new Error('Wallet already recorded in preCertModules');
}

const entry = {
  module: 'Wallet',
  structureCertified: false,
  lockVersion: 'ARCH-COMPLETE-002',
  structureState: 'READY_FOR_CERTIFY',
  structureAuthorityTask: 'TB-TMAR-WALLET-AMSC-001-W2',
  certificationNote:
    'AMSC re-standardization of src/backend/Modules/Wallet (W0 Analyze -> W1 Migrate -> W2 Structure). ' +
    'W2 is a behavior-preserving physical reorganization only: the Application tree is now capability-first ' +
    'shallow (Admin/{Commands,Queries,Models}, Customer/{Commands,Queries,Models}, Payments/Models, Refunds/Models) ' +
    'with the shared Composition/Ports/Models/Validation seams at root and zero single-file request leaf folders; ' +
    'the technical-axis-first Commands/<UseCase>/ + Queries/<UseCase>/ tree is retired. The 157-LOC mixed ' +
    'Application/Models/WalletDtos.cs bundle was split by capability (Admin.Models / Customer.Models / ' +
    'Payments.Models / Refunds.Models) with no type, member or wire-shape change. The 682-LOC multi-interface ' +
    'WalletDirectory god-file was MOVED from Infrastructure/Directories into Infrastructure/Persistence ' +
    '(namespace Tooba.Wallet.Infrastructure.Persistence) next to its DbContext AND split by capability into five ' +
    'cohesive partial files (WalletDirectory.cs account/ledger core + mapping helpers, WalletDirectory.Customer.cs, ' +
    'WalletDirectory.Admin.cs, WalletDirectory.Payments.cs, WalletDirectory.Refunds.cs) so no single file mixes ' +
    'customer/admin/order-payment/refund responsibilities (ARCH-MODULE-FILE-001); the public type, its three ' +
    'interface implementations, DI registration, ctor signature and every member signature are unchanged. The single-file ' +
    'Infrastructure/Development and Infrastructure/Migrations technical folders were retired by moving ' +
    'WalletDevelopmentSeedBootstrap.cs into Adapters and the InitialWallet migration into Persistence/Migrations. ' +
    'The Endpoints error catalog + resource set + bilingual resx moved into Contracts/Errors/Resources with ' +
    'path-exact namespaces (Tooba.Wallet.Endpoints.Contracts.Errors[.Resources]) and the two locked EmbeddedResource ' +
    'LogicalName values were repointed accordingly. WalletModule.cs stays in Infrastructure/DependencyInjection so no ' +
    'single-file composition-root ceremony folder is invented and the two callers (ToobaModuleComposition, ' +
    'ArchitectureBoundaryTests) keep their existing using. PATH_NAMESPACE_ALIGNMENT is now EXACT for every Wallet ' +
    'production .cs file across all five projects. Cross-module boundaries stay Contracts-only (the single legal ' +
    'foreign edge remains Tooba.Wallet.Infrastructure -> Tooba.Notification.Contracts); schema and migrations are ' +
    'byte-identical; no behavior changed. Wallet remains uncertified until the W3 Certify wave.',
  projects: [
    {
      projectName: 'Tooba.Wallet.Application',
      rootAllowlist: [],
      rootAllowlistJustification:
        'Wallet.Application has no root .cs: capability-first Admin/ and Customer/ CQRS trees plus Payments/Refunds/Models, and the shared Composition/Ports/Models/Validation seams; path<->namespace exact.',
      forbiddenRootFiles: [
        'WalletDtos.cs',
        'WalletEnumParsing.cs',
        'IWalletDirectory.cs',
        'IWalletDemoPreviewPort.cs',
        'RedeemCustomerGiftCardCommand.cs',
        'IssueAdminGiftCardCommand.cs',
        'RevokeAdminGiftCardCommand.cs',
        'AdjustAdminWalletCommand.cs',
      ],
      forbiddenTopLevelFolders: ['Commands', 'Queries', 'Validators'],
    },
    {
      projectName: 'Tooba.Wallet.Domain',
      rootAllowlist: [],
      rootAllowlistJustification:
        'Wallet.Domain has no root .cs; Aggregates hold the wallet/gift-card aggregates and ValueObjects the four boundary enums with exact path<->namespace.',
      forbiddenRootFiles: [
        'WalletAccount.cs',
        'WalletLedgerEntry.cs',
        'GiftCard.cs',
        'GiftCardRedemption.cs',
        'GiftCardStatus.cs',
        'LedgerDirection.cs',
        'LedgerEntryType.cs',
        'WalletAccountStatus.cs',
      ],
      forbiddenTopLevelFolders: [],
    },
    {
      projectName: 'Tooba.Wallet.Endpoints',
      rootAllowlist: ['WalletEndpointModule.cs'],
      rootAllowlistJustification:
        'Composition entry only; Admin/ and Customer/ hold the thin HTTP surfaces and Contracts/Errors[.Resources] the module error catalog and bilingual resources.',
      forbiddenRootFiles: [
        'WalletCustomerEndpoints.cs',
        'WalletAdminEndpoints.cs',
        'WalletErrorCatalogContributor.cs',
        'WalletErrorResources.cs',
      ],
      forbiddenTopLevelFolders: ['Errors', 'Resources'],
    },
    {
      projectName: 'Tooba.Wallet.Infrastructure',
      rootAllowlist: [],
      rootAllowlistJustification:
        'Wallet.Infrastructure has no root .cs: Persistence holds the DbContext + the multi-interface WalletDirectory + Migrations/, Adapters the demo/seed adapters and DependencyInjection the composition root.',
      forbiddenRootFiles: [
        'WalletDirectory.cs',
        'WalletDbContext.cs',
        'WalletOutboxRegistration.cs',
        'WalletDemoSnapshot.cs',
        'WalletDevelopmentSeed.cs',
        'WalletDevelopmentSeedBootstrap.cs',
      ],
      forbiddenTopLevelFolders: ['Migrations', 'Directories', 'Development'],
    },
    {
      projectName: 'Tooba.Wallet.Contracts',
      rootAllowlist: [],
      rootAllowlistJustification:
        'Wallet.Contracts has no root .cs: Errors/ carries the single canonical stable-code home, Dtos/ the shared currency helper and Payments/ + Refunds/ the two cross-module ports.',
      forbiddenRootFiles: ['WalletErrorCodes.cs', 'WalletCurrency.cs', 'WalletOrderPaymentPort.cs', 'WalletRefundCreditPort.cs'],
      forbiddenTopLevelFolders: [],
    },
  ],
  evidence: 'docs/architecture/evidence/TB-TMAR-WALLET-AMSC-001-W2/structure.md',
};

manifest.preCertModules.push(entry);

// Wallet stays honestly uncertified as an HTTP-owning module until W3.
if (!manifest.uncertifiedHttpOwningModules.includes('Wallet')) {
  throw new Error('Wallet unexpectedly absent from uncertifiedHttpOwningModules');
}

fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + NL);

// -------------------------------------------------------------------- SoT ---
const sot = JSON.parse(fs.readFileSync(statePath, 'utf8'));

sot.walletAmsc001W2 = {
  task: 'TB-TMAR-WALLET-AMSC-001-W2',
  mode: 'ARCHITECT_DIRECT_MANUAL_AMSC',
  skill: 'tooba-architecture-structure',
  state: 'WALLET_AMSC_001_W2_STRUCTURE_READY_FOR_CERTIFY',
  structureState: 'READY_FOR_CERTIFY',
  structureHandoffState: 'READY_FOR_CERTIFY',
  structureCertified: false,
  parentWave: 'TB-TMAR-WALLET-AMSC-001-W1',
  parentCommit: 'c802fe81',
  folderGranularityState: 'PROFESSIONAL_SHALLOW',
  solutionExplorerState: 'CANONICAL',
  pathNamespaceState: 'EXACT',
  physicalCopyState: 'CLEAN',
  rootAllowlistState: 'ENFORCED',
  fileCohesionState: 'COHESIVE',
  applicationPrimaryAxis: 'CAPABILITY_FIRST_ADMIN_CUSTOMER_PAYMENTS_REFUNDS',
  infrastructureDirectoryHome: 'Persistence',
  godFileState: 'SPLIT_INTO_COHESIVE_CAPABILITY_PARTIALS',
  endpointsLocalizationHome: 'Contracts/Errors/Resources',
  crossModuleCouplingState: 'NONE',
  crossModuleJoinState: 'ZERO',
  schemaState: 'UNCHANGED',
  behaviorChange: 'ZERO',
  guardsWeakened: 'NONE',
  baselinesWidened: 'NONE',
  microserviceExtractable: 'TRUE_CONTRACTS_ONLY_EXACT_PATH_NAMESPACE_PENDING_W3_CERTIFY',
  manifestState: 'preCertModules',
  certifiedModulesCount: 31,
  durableGuard: 'WalletModuleAmsc001W2StructureGuardTests',
  evidence: 'docs/architecture/evidence/TB-TMAR-WALLET-AMSC-001-W2/structure.md',
  commit: 'PENDING_W2_COMMIT',
  commitFull: 'PENDING_W2_COMMIT',
};

fs.writeFileSync(statePath, JSON.stringify(sot, null, 2) + NL);

console.log('manifest preCertModules:', manifest.preCertModules.length, 'modules:', manifest.modules.length);
console.log('sot walletAmsc001W2 recorded');
