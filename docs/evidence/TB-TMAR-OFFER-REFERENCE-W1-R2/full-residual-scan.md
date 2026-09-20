# Full residual scan
| Rule | File | Line/Type | Severity | Action | Result |
|---|---|---|---|---|---|
| Localized Domain exception | SellerOffer.Activate | Persian IOE | High | SemanticException code | Fixed |
| String IOE business | SellerOffer quantity | string codes | High | SemanticException | Fixed |
| UuidV7 Domain | SellerOffer.Create | UuidV7.New | High | offerId arg + IIdGenerator | Fixed |
| UtcNow Infra | OfferDirectory | DateTimeOffset.UtcNow | High | IClock | Fixed |
| Localized Contracts IOE | ReturnPolicyResolver | Persian IOE | High | SemanticException | Fixed |
| UtcNow Host Offer BFF | SellerPanelComposer | DateTimeOffset.UtcNow | Med | IClock | Fixed |
| Infra→Catalog/Party.App | OfferDirectory csproj | ProjectReference | Low | Grandfather documented | Accepted residual |
| Outbox technical IOE | OfferOutboxRegistration | English technical | Info | Keep (non-business) | OK |
| Checkout LabelFa | ResolvedReturnPolicy | FA label DTO | Info | Boundary snapshot label | OK |
