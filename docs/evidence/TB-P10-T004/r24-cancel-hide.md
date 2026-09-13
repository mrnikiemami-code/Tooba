# TB-P10-T004-R24 — Cancel + hide

Cancel (`POST /v1/storefront/checkout/{id}/cancel`): canonical lifecycle, releases supply, closes cycle, removes pending card, frees open-unpaid slot, churn row stays. Runtime E-cancel 200 checkout `01a09993-9e24-7000-9da7-c181759d080a`; open 2→1; replacement F-replacement 200.

Hide (`hide-pending-card`): presentation-only (LOCK-SF-104). Active hold returns 409; after expire hide 200. D-still-blocked 409; count unchanged. Customer still reaches Order via pending-payments / customer-panel/orders.

No overlap: hide does not call cancel; cancel does not hide-only.
