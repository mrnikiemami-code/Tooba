# Validation matrix — W9 Category Attribute-Schema

| Request | Classification | Notes |
|---|---|---|
| GetEffectiveCategorySchemaQuery | NO_VALIDATOR_REQUIRED | Guid route param only |
| BindCategoryAttributeCommand | VALIDATOR_REQUIRED | Model non-null; DefinitionId non-empty |
| UpdateCategoryAttributeBindingCommand | NO_VALIDATOR_REQUIRED | Bool flags only |
| UnbindCategoryAttributeCommand | NO_VALIDATOR_REQUIRED | Route guids only |
| ReorderCategoryAttributeBindingsCommand | VALIDATOR_REQUIRED | OrderedDefinitionIds non-null |

Validators do **not** duplicate: category/definition existence, duplicate binding, exact reorder set, or variant-axis business rules (Application/Domain + directory Result).
