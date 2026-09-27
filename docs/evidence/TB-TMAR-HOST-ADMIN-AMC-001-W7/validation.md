# Validation matrix — W7 Categories

| Request | Classification | Validator |
|---|---|---|
| GetCategoryTreeQuery | VALIDATOR_REQUIRED | locale non-blank |
| GetCategoryWorkspaceQuery | NO_VALIDATOR_REQUIRED | — |
| CreateCategoryCommand | VALIDATOR_REQUIRED | Translations or LocalizedNames shape |
| UpdateCategoryCoreCommand | NO_VALIDATOR_REQUIRED | — |
| UpsertCategoryTranslationCommand | VALIDATOR_REQUIRED | Name/Slug non-blank |
| MoveCategoryCommand | NO_VALIDATOR_REQUIRED | — |
| ReorderCategoriesCommand | VALIDATOR_REQUIRED | OrderedCategoryIds not null |
| PublishCategoryCommand | NO_VALIDATOR_REQUIRED | — |
| ArchiveCategoryCommand | NO_VALIDATOR_REQUIRED | — |
| ResolveCategoryRouteQuery | VALIDATOR_REQUIRED | locale + slug non-blank |

FluentValidation = transport shape only. Tree/slug/lifecycle rules remain Domain/Application/Infrastructure Result logic.
