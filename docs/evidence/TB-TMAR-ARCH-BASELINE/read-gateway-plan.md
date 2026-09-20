# Read-Side Microservice Readiness

## Target

Composer/QueryHandler → ICatalogReadGateway / IPricingReadGateway / IInventoryReadGateway / IOfferReadGateway / …

Today: in-process adapters. Tomorrow: HTTP/gRPC/read model behind same contracts.

## Host/composer violation sites (direct multi-DbContext)

Primary: StorefrontComposer, ProductWorkspaceComposer, AdminPanelComposer, SellerPanelComposer, CustomerPanelComposer, Admin*GridQueryEngine*, StoreLandingPageComposer, StorefrontShipping/PendingPayment composers.

Rules: no cross-schema JOIN; Pricing resolves price; Inventory availability; Offer buy-box; Promotion campaign eligibility; Host only composes projections.
