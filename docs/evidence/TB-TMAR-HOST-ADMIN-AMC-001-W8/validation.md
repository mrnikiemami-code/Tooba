# Validation matrix — W8

| Request | Classification | Validator |
|---|---|---|
| ListAttributeDefinitionsQuery | NO_VALIDATOR_REQUIRED | none |
| GetAttributeDefinitionQuery | NO_VALIDATOR_REQUIRED | none |
| CreateAttributeDefinitionCommand | VALIDATOR_REQUIRED | Code non-blank |
| UpdateAttributeDefinitionCommand | NO_VALIDATOR_REQUIRED | none (bools/nullable metadata) |
| PreviewVariantAxisCapabilityDisableQuery | NO_VALIDATOR_REQUIRED | none |
| SetVariantAxisCapabilityCommand | NO_VALIDATOR_REQUIRED | none (bool body) |
| AddAttributeOptionCommand | VALIDATOR_REQUIRED | Code non-blank |

Not duplicated in FluentValidation: DB uniqueness, domain capability rules, option existence, Offer usage.
