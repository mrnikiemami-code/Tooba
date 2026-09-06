# Internal notes

Order-owned `CheckoutOperationalNote` append-only (max 2000). `ICheckoutDirectory.ListNotesAsync` / `AddNoteAsync` via `order.checkout_operational_notes`. Host GET/POST `/v1/admin/orders/{id}/notes`; POST requires `order.handle`. Admin card «یادداشت داخلی» only; not on storefront.
