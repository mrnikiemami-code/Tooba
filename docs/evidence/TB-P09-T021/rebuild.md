# Rebuild

Preferred correction: Cancel old package → Create new package.

- Pre-dispatch Cancel voids membership (`ReleasedAt`) and keeps historical Cancelled row.
- Same shipments may join a new combination.
- After Dispatch: Cancel rejected (`fulfillment.package.cancel_after_dispatch`); membership immutable.
