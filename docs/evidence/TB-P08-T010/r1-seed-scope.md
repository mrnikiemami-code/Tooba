Root cause: Program called ContentDevelopmentSeed.ApplyAsync(app.Services) which resolved scoped ContentDbContext from the root provider.

Fix: ContentDevelopmentSeedHost creates IServiceScope, assigns store-alpha CommerceContext, migrates Localization/Content/Media, then seeds via scoped provider. Idempotent seed path unchanged.
