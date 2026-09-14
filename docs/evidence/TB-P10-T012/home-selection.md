# Home selection

PUT /v1/admin/pages/home with published page id.
Public GET /v1/storefront/home-selection returns selectedPage or usesCanonicalHome.
Invalid/unpublished Home falls back to canonical Home; homepage is never blank.
Unset restores accepted Home composition.
