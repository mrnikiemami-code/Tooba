# accessor-di — TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT

HttpCommerceContextAccessor: KEEP / THIN_HOST_CONTEXT_ADAPTER_CERTIFIED

- internal sealed; Scoped
- interfaces: ICurrentCommerceContext, ICurrentEdition, ICurrentTenant, ICommerceContextAssigner
- ItemKey = Tooba.CommerceContext
- Current = _assigned ?? Items
- Assign null-guard
- Focused DI identity test: same scope ReferenceEquals across four interfaces; distinct across scopes
