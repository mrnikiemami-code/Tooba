# Order Attribution

Settlement entries are loaded via `ListEntriesBySellerOrderIdsAsync` and projected with `NetAmount` for that SellerOrder only.

PayoutRequest batch totals are intentionally not stamped onto Order Detail.
