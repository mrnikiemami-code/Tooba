# Returns Endpoint Boundary

Endpoint-State: HOST_THIN_TRANSPORT

## Ownership
HTTP Host mapping → ReturnPanelComposer / IReturnDirectory / IAdminReturnGridQuery → Returns Infrastructure → ReturnsDbContext

## Host may
- Customer/seller/admin auth seams
- Parse refund destination via Returns.Application `ReturnSemanticMapper.ParseDestination` → Result/SemanticError
- Map InvalidOperationException known codes via `ReturnSemanticMapper.MapException`
- ApiResponseFactory presentation

## Host must NOT (verified)
- ReturnsDbContext in endpoints/panel composer
- ReturnErrorMapper (deleted)
- Persian prose throw in ParseDestination
- Manual Results.Json expected-error envelopes `{title=mapped.Fa...}`

## Grid
Admin `/v1/admin/returns/query` → module `AdminReturnGridQueryEngine` behind `IAdminReturnGridQuery`
