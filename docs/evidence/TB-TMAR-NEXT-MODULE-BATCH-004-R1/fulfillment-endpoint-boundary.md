# Fulfillment Endpoint Boundary

Endpoint-State: HOST_THIN_TRANSPORT

## Ownership
HTTP Host mapping → MediatR Command/Query (Fulfillment.Application) → ports/directory → FulfillmentDbContext (Infrastructure)

## Host may
- SellerPanelAccess / AdminPanelAccess auth
- Build AccessControl permission snapshot input for seller mutate
- Call ISender / IAdminFulfillmentWorkQueueQuery / ApiResponseFactory
- Normalize generic grid input via AdminListGridPolicies

## Host must NOT (verified)
- RequestServices.GetRequiredService
- OrderDbContext / FulfillmentDbContext in endpoints or panel composer
- Construct AdminFulfillmentWorkQueueQueryEngine
- PlatformHttpException for module business outcomes (auth panel catch → SemanticError only)
- Manual Results.Json `{title,errorCode,detail}` / ex.Message detail

## Seller authorization seam
`ISellerFulfillmentAuthorizer` + Order.Contracts `ISellerOrderAuthReader` preserve:
- ownership, order.handle GlobalWithinOwner, category-scoped all-lines, missing→not found, denied→forbidden
