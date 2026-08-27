# 17 — Attachments DEFER

Task: TB-P06-T025

## Decision

Attachments are **out of scope**. No upload API, no blob storage, no message attachment entities.

## FE

Shopeiva form/thread attach controls are **hidden** in Tooba Support UI (`support-ui.tsx` comment: attachments DEFERRED). No Paperclip/Upload affordance on live pages.

## Future

Separate task may add attachments without changing ticket core contracts.
