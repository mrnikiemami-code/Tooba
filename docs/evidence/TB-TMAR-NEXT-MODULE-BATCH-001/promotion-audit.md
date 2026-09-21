# promotion-audit

## Structure
Domain/Application/Contracts/Infrastructure. Host owns merchandising admin transport via IMerchandisingCampaignDirectory/Query (no PromotionDbContext). Tests project added.

## Repairs
- Host production no PromotionDbContext; schema via IPromotionSchemaMigrator
- PromotionDirectory/MerchandisingCampaignDirectory: IClock/IIdGenerator; Domain Create takes explicit ids
- MigrationRunner uses PromotionModuleMigration adapter
- Domain Persian invariant prose → stable English codes
- Infrastructure refs Offer/Pricing/Inventory Contracts only (no foreign Application/Domain)

## Result adoption
No new Promotion-owned HTTP Result surface in this batch. Host campaign endpoints remain Host transport composition over Directory ports. Checkout promotion port preserved in Contracts.

## Residuals
Host MerchandisingCampaignAdminEndpoints still composes Directory (justified Host transport). Full MediatR CQRS for campaign admin not introduced (would be broad rewrite; AntiPattern gate).