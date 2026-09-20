# Characterization tests — TB-TMAR-HOST-W6

ShippingServiceAdminTests (6):
- Create persists service + translation + option
- Duplicate code → PlatformHttpException 409 shipping_service.code_duplicate
- Unknown language → shipping_service.language_invalid
- Update + Deactivate
- EnsureSeed idempotent (5 services)
- Host endpoints use CQRS Commands; no SaveChangesAsync / .Add( / RemoveRange
