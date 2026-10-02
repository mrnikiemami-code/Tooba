# middleware-lifetime — TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT

TenantResolutionMiddleware: GLOBAL_HOST_MULTITENANCY_PLATFORM_CERTIFIED

- Conventional middleware; constructor = platform seams only
- InvokeAsync(HttpContext, IStoreCommerceContextAssigner)
- RequestServices ZERO; IServiceProvider ZERO
- Success order: Resolve → Items → Assign → Activity tags → BeginScope → _next
