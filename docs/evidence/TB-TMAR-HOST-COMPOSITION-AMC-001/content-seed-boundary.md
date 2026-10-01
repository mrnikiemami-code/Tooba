# Content seed boundary

Host ContentDevelopmentSeedHost: no ContentDbContext / LocalizationDbContext / MediaDbContext / Database.MigrateAsync.
Module ContentDevelopmentSeedBootstrap owns Content migrate+seed.
Localization/Media migrate via IModuleSchemaMigrator during DevelopmentSchemaMigrator.MigrateSchemaOnlyAsync.
