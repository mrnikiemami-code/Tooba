# Fulfillment Shipping Ownership Audit

- Shipping service CRUD + ensure-seed owned by Endpoints/Shipping/ShippingServiceEndpoints.
- Wire DTOs live in Endpoints.Shipping.
- Enabled methods tree owned by ShippingMethodsEndpoints at `/v1/admin/shipping-methods`.
- ShippingServiceLanguageGate remains Fulfillment.Infrastructure + Localization.Contracts.
- State: MODULE_ENDPOINTS for HTTP + shipping-method route
