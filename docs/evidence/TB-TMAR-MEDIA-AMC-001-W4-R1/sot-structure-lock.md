# TB-TMAR-MEDIA-AMC-001-W4-R1 — SoT structureLock

## Defect
`mediaAmc001` and manifest marked Media structure-certified, but `structureLock.certifiedModules` omitted Media.

## Repair
Appended `"Media"` once to `docs/architecture/tmar-current-state.json` → `structureLock.certifiedModules` after Identity.

Preserved prior entries: Order, Cart, StoreContext, Offer, Payment, Settlement, Fulfillment, AccessControl, AddressBook, Content, Identity.
