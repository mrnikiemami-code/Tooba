# Foreign-source contract map
- Payment: existing `Payment.Contracts.Admin.IPaymentAdminGateway`; receipt uses it.
- Party, Catalog, Fulfillment, Returns, Settlement, OperatorProfile, Identity: required contract-safe history/label projections were not completed.
- No foreign DbContext or foreign Application/Infrastructure dependency was introduced into Order.
