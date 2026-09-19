# TB-P10-T022-R16 — Active Selection + Future/Teasing

## Active (`ResolveActiveByTypeAsync`)

Filter: StoreId + type Code (active) + Published + StartAt ≤ now + (EndAt null | EndAt > now).

Order: **Priority DESC → StartAt DESC → Id ASC**. First row wins.

Also updated foundation `ResolveActiveCampaignAsync` to the same order.

## Future (`ResolveFutureByTypeAsync`)

Filter: StoreId + type + Published + StartAt > now.

Order: **StartAt ASC → Priority DESC → Id ASC**.

`IsTeasing = Published && StartAt > now`.

Never returned by active path (proven in `MerchandisingCampaignRuntimeTests`).

## Exclusions from active

Expired (EndAt ≤ now), Archived, Draft, Future — all proven inactive at fixed clock.
