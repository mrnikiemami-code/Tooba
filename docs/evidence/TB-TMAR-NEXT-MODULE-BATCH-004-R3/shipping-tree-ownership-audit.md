# Shipping Tree Ownership Audit — R3

## Before (Host)

`ShippingServiceEndpoints.ListEnabledMethodsTreeAsync` owned:

- enabled-code filtering via `ShippingMethodRegistry`
- language resolve/fallback
- catalog read + active filter + sort
- empty-catalog registry fallback
- `DefaultColor` / `DefaultOptions` projection

Called from `AdminOrderOperationsEndpoints` `/v1/admin/shipping-methods`.

## After (Fulfillment.Application)

- Query: `ListEnabledShippingMethodsTreeQuery`
- Handler: `ListEnabledShippingMethodsTreeHandler`
- DTOs: `EnabledShippingMethodTreeItemDto` / `EnabledShippingMethodOptionDto`
- Host caller: `ISender.Send(ListEnabledShippingMethodsTreeQuery)` → `Results.Json(result.Value)` only
- Host helpers deleted: `ListEnabledMethodsTreeAsync`, `DefaultColor`, `DefaultOptions`
- No `IShippingCatalogReader` usage remains in `ShippingServiceEndpoints.cs` for tree projection

Authority: **APPLICATION_OWNED**
