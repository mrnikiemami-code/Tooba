# Host leak scan — TB-TMAR-NEXT-MODULE-BATCH-002

## WalletDbContext (production Host)
| File | Classification |
|------|----------------|
| Wallet/WalletDevelopmentSeedHost.cs | migrate/seed bootstrap — allowed |
| Development/Marketplace* / ProductWorkspace* | none for Wallet beyond seed host |

## PaymentDbContext (production Host) — authority leaks
| File | Authority |
|------|-----------|
| CommerceHoldPolicy.cs | reads MethodHoldOverrides |
| Admin/HoldPolicySettingsEndpoints.cs | R/W MethodHoldOverrides |
| Grid/AdminPaymentsGridQueryEngine.cs | Payments query authority |
| Storefront/StorefrontPendingPaymentComposer.cs | Payments/Attempts batch |
| Admin/AdminPanelComposer.cs | injects PaymentDbContext |
| Program.cs | wires PaymentDbContext into composers |
| *Bootstrap migrate | allowed |

## Target absorption
- IPaymentHoldSettingsDirectory (Application Ports) — method hold override load/save
- IPaymentAdminQueryPort / pending-payment batch query port — payment snapshots by checkout/filter
- Host keeps CatalogDbContext for store hold settings (Catalog-owned); Payment method overrides leave Host
- Migrate/seed bootstrap may keep GetRequiredService<*DbContext>() 

## Host.Tests
- May construct DbContexts in fixtures — not production authority
