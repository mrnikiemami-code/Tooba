# TB-P10-T022-R19 — Cart Merge

MergeAnonymousAfterLoginAsync preserves MerchandisingCampaignId when merging lines by OfferId.
Preferred: merchandisingCampaignId ?? existing; then ValidateOfferAndQuoteAsync re-resolves authoritatively.
Same OfferId still deduplicates to one line; campaign context does not create duplicate discounted lines.
Authenticated cart survives logout; guest merge remains canonical (LOCK-SF-117…120 preserved).
