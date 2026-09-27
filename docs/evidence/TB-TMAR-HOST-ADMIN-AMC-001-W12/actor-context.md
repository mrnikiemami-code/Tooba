# Actor context — W12

Reused module-owned `CatalogActorRequestBinding` (W10/W11 pattern).

- Bound per route after `ICatalogAdminAuthorizer.RequireAuthorizedAsync`
- Sets `ICatalogActorContext.ActorUserId` / `ActorDisplayName`
- History events `EventCategoryChanged` and `EventUnpublished` carry actor from directory `_actor`
- No Host `CatalogActorHttpBinding` copy; Host Attribute group filter deleted with file
