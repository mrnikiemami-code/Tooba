# cqrs-validation — W2
Endpoint → IAdminPanelAccess → ISender → QueryAdminSellersGridQuery → Handler → ApiResponseFactory.
Result&lt;GridPageResponse&lt;AdminSellerListItem&gt;&gt;.
Validator: VALIDATOR_REQUIRED — QueryAdminSellersGridQueryValidator (Request NotNull only; machine code party.admin.sellers.validation.grid_request_required).
Field/operator/advanced whitelist remains PartyAdminSellersGridPolicies (not duplicated in FluentValidation).
