# Rebuild final — TB-P09-T022

Pre-dispatch:

1. `cancel_consolidated_package` → status Cancelled, memberships released (`ReleasedAt`)
2. Old package row remains in Admin `consolidatedPackages` list
3. Same ready shipments may join a new package (different method/tracking)
4. New package becomes active primary; Cancelled never primary for customer

After dispatch: cancel/rebuild unavailable; membership immutable (`fulfillment.package.cancel_after_dispatch`).
