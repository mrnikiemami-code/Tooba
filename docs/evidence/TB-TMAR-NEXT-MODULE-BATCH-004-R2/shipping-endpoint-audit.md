# Shipping Endpoint Audit — TB-TMAR-NEXT-MODULE-BATCH-004-R2

## Routes preserved
- `GET /v1/admin/shipping-services/`
- `GET /v1/admin/shipping-services/{serviceId}`
- `POST /v1/admin/shipping-services/`
- `PUT /v1/admin/shipping-services/{serviceId}`
- `POST /v1/admin/shipping-services/{serviceId}/deactivate`
- `POST /v1/admin/shipping-services/ensure-seed`

## After
- Host: auth + `ISender` + `ApiResponseFactory` only (`ShippingServiceEndpoints.cs`)
- Application: list/get/create/update/deactivate/ensure-seed via Result + SemanticError
- Wire-only request DTOs remain in Host; response projection DTOs in Fulfillment.Application
- No Host `LoadDetailAsync`, no Host list projection, no `ILanguageDirectory`, no manual `{ title, errorCode }`

## Endpoint-State
`HOST_THIN_TRANSPORT`
