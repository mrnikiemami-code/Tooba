# Actor context — W11

All five Catalog variant Admin routes bind actor after authorization via W10 `CatalogActorRequestBinding`:

- Sets `ICatalogActorContext.ActorUserId` / `ActorDisplayName`
- Uses `IActorDisplayLookup` (OperatorProfile.Contracts) with default «اپراتور»
- Does not copy Host `CatalogActorHttpBinding`

Apply writes `ProductHistory` EventVariantsChanged with actor fields.

Host retained category-change group still uses Host `CatalogActorHttpBinding`.
