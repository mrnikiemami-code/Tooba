# Returns Auth Boundary

Endpoints define:
- IReturnCustomerAuthorizer
- IReturnSellerAuthorizer
- IReturnAdminAuthorizer

Host adapters only (no business/DbContext):
- HostReturnCustomerAuthorizer
- HostReturnSellerAuthorizer
- HostReturnAdminAuthorizer

Actor/seller IDs passed explicitly into Commands/Queries.
