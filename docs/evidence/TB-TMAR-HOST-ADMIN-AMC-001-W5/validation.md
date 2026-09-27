# Validation matrix — W5 MegaMenu

| Request | Classification | Evidence |
|---|---|---|
| `GetCategoryMegaMenuQuery` | NO_VALIDATOR_REQUIRED | Guid + optional locale only |
| `ListMegaMenuPlacementOptionsQuery` | NO_VALIDATOR_REQUIRED | Guid + optional locale only |
| `UpsertCategoryMegaMenuCommand` | VALIDATOR_REQUIRED | `UpsertCategoryMegaMenuCommandValidator` — Input not null; TitleOverride/BadgeText/ShortLabel max lengths |
| `RemoveCategoryMegaMenuCommand` | NO_VALIDATOR_REQUIRED | Guid only |
| `GetStorefrontMegaMenuQuery` | NO_VALIDATOR_REQUIRED | optional locale only |

Tree/business rules remain in Domain/MegaMenuDirectory, not FluentValidation.
Durable guard: `HostAdminAmcW5GuardTests.MegaMenu_path_namespace_exact_and_validators_classified`.
