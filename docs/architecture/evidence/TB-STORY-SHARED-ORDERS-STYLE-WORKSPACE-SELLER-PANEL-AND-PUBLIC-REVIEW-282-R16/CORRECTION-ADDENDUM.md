# CORRECTION ADDENDUM — TB-282-R16

Owner rejected R15 readiness handoff: Reject dialog typing reloaded the Story grid and cleared selection.

Cause: Story-owned inline callbacks changed `queryAdapter` identity → AppDataGrid force-load. Fix stabilizes Story integration callbacks/refs only; does not redesign AppDataGrid, auth, or moderation. Ineligible main-storefront rows stay skipped.
