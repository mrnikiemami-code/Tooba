# Fulfillment Auth Boundary

- Endpoints: IFulfillmentSellerAuthorizer, IFulfillmentAdminAuthorizer, IFulfillmentCustomerAuthorizer.
- Host adapters only: HostFulfillmentSellerAuthorizer, HostFulfillmentAdminAuthorizer, HostFulfillmentCustomerAuthorizer.
- No Endpoints→Host refs; adapters have no DbContext / no Fulfillment business orchestration.
