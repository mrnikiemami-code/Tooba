# Return display dedup

Backend `before_delivery` now returns empty remaining when it would repeat the policy label.

FE `canonicalReturnDisplay` never concatenates identical deadline/remaining. Eligible/partial prefers remaining when the two strings differ.

Runtime lines on the single-seller order: `returnDeadlineDisplay=7 روز پس از تحویل`, `returnRemainingDisplay=""`, `returnStatusCode=before_delivery` — shown once.
