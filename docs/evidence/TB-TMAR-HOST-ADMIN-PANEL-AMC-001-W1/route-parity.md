# route-parity — W1
Route GET /v1/admin/sellers preserved.
Success: ApiResponseFactory.From(Result<IReadOnlyList<AdminSellerListItem>>) => raw JSON array (same as prior Results.Json).
Auth: IAdminPanelAccess.RequireAuthorizedAsync (Host DI implementation).
Ordering/compose semantics match prior AdminPanelComposer.ListSellersAsync.
