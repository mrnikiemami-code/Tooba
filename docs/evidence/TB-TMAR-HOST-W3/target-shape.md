# Target shape — TB-TMAR-HOST-W3

HTTP Endpoint → StoreAppearanceSettingsComposer → ISender → SaveStoreAppearanceSettingsCommand → SaveStoreAppearanceSettingsHandler → IStoreAppearanceSettingsDirectory → StoreAppearanceSettingsDirectory (CatalogDbContext.SaveChangesAsync) → Host projector Invalidate + GetEffective view.
