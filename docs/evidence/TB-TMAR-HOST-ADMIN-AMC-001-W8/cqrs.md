# CQRS — W8 Attribute Definitions

| Operation | Request | Handler | Port method |
|---|---|---|---|
| List | `ListAttributeDefinitionsQuery` | ListAttributeDefinitionsHandler | ListAsync |
| Get | `GetAttributeDefinitionQuery` | GetAttributeDefinitionHandler | GetAsync |
| Create | `CreateAttributeDefinitionCommand` | CreateAttributeDefinitionHandler | CreateAsync |
| Update | `UpdateAttributeDefinitionCommand` | UpdateAttributeDefinitionHandler | UpdateMetadataAsync |
| Preview disable | `PreviewVariantAxisCapabilityDisableQuery` | PreviewVariantAxisCapabilityDisableHandler | PreviewVariantAxisCapabilityDisableAsync |
| Set capability | `SetVariantAxisCapabilityCommand` | SetVariantAxisCapabilityHandler | SetVariantAxisCapabilityAsync |
| Add option | `AddAttributeOptionCommand` | AddAttributeOptionHandler | AddOptionAsync |

All seven Admin routes dispatch via MediatR `ISender`. No Host Definition HTTP remains.
