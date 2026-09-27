# Validation — W4 Tags

| Request | Classification | Validator |
|---|---|---|
| `ListTagsQuery` | NO_VALIDATOR_REQUIRED | — |
| `CreateTagCommand` | VALIDATOR_REQUIRED | `CreateTagCommandValidator` (code/slug max length; LocalizedNames present; locale keys non-blank) |
| `GetTagQuery` | NO_VALIDATOR_REQUIRED | Guid binding |
| `ListProductTagsQuery` | NO_VALIDATOR_REQUIRED | — |
| `AssignProductTagCommand` | NO_VALIDATOR_REQUIRED | route Guids |
| `RemoveProductTagCommand` | NO_VALIDATOR_REQUIRED | — |
| `ListCategoryTagsQuery` | NO_VALIDATOR_REQUIRED | — |
| `AssignCategoryTagCommand` | NO_VALIDATOR_REQUIRED | — |
| `RemoveCategoryTagCommand` | NO_VALIDATOR_REQUIRED | — |

Business uniqueness, existence, mutation guard stay in `TagDirectory` Result logic.
Name-required remains `catalog.tag.invalid` in directory (parity with prior wire code).
