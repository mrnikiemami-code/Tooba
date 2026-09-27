# Validation — W12

| Request | Classification | Rules |
|---|---|---|
| `PreviewCategoryChangeQuery` | VALIDATOR_REQUIRED | `NewCategoryId` NotEmpty → `catalog.validation.category_change_new_category_id_required` |
| `ReplacePrimaryCategoryCommand` | VALIDATOR_REQUIRED | `NewCategoryId` NotEmpty → same |
| Route `productId` Guid | NO_VALIDATOR_REQUIRED | route constraint |
| Locale | NO_VALIDATOR_REQUIRED | optional; defaulted fa-IR |

No DB existence / assignability / domain rules in FluentValidation.
