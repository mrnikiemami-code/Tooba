# Architect review — TB268 — 2026-11-15

Decision: READY_FOR_USER_VISUAL_REVIEW for the exercised standalone group. STOP_FOR_USER_REVIEW; automaticNext NONE. User visual acceptance is not claimed.
Reviewed main f62bd7564d5394dc8e36df4193b065d17be38682, shared operational section source and committed RECEIPTS/tab-counts-and-navigation.md. Architect did not run local browser/tests or independently verify runtime/process identity.

Source aligns with intended scope: group parcel snapshot feeds counts/filtering; Resend is durable lineage excluding projected replacement links; actionability uses existing caps/recovery; initial choice waits capabilitiesReady; stable ordinals derive from whole standalone list; selected tab persists through rerender and zero count; dialogs remain owned at GroupParcels rather than inside filtered card. Cross-tab reveal selects existing card and focuses it. Shared local filter has no mutation endpoint. No backend/data rewrite in reported change.

Current group's documented live count: All2, Origin1, Resend1, Delivered1, NeedsAction0. DefaultResend shows only child2; source link moves to Origin and focuses source1. Count overlap is intentional (delivered resend belongs to both). Geometry390/1024/1440 and keyboard results are worker evidence, not independent visual approval. Other groups/package histories are not claimed browser-proven.
The reported no-strip for one nonzero category is broader than source: All plus one nonzero status can still display strip when totalParcels>1. This does not affect current two-parcel user trial; no extra task launched for it.
The file-size backlog and reported stash-based baseline probe remain disclosed; do not repeat baseline/stash/test loops or refactor architecture during the user's visual check.

Review URL:
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c&from=queue
Read-only: switch Resend/Origin/All, verify count/card correspondence and mobile strip scrolling; follow child-source link. No shipping/return/payment action needed. Preserve positive functional feedback and267 improved layout. No new task, no all-shipping/provider/financial acceptance.
