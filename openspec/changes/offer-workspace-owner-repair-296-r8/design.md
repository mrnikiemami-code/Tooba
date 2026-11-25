# Design — offer-workspace-owner-repair-296-r8

Use the existing R7/295 workspace and project primitives. Picker data is read through a Catalog-owned product picker plus the existing variant editor labels; Offer duplicate lookup is an auth-scoped read. Localization consumes the active locale definition and a message catalog; formatters use Intl culture/calendar. Persisted drafts are session recovery only, never domain writes. Save operations use the owning routes and independent detail rereads. No parallel form, theme, grid or mutable business contract is introduced.
