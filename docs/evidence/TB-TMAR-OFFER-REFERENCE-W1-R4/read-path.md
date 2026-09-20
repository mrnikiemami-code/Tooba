# Read path

List/Get handlers return `SellerOfferListItem`/`SellerOfferDetailPage`. HTTP responses use the `ISender.Send` result directly. Create/Patch each dispatch one cohesive command and return its final detail result. `IOfferSellerPanel` was deleted.
