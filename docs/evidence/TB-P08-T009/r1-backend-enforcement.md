# r1-backend-enforcement

`ContentAdminAccess.RequireAsync` = AdminPanelAccess (tenant#view) + SpiceDB capability check (`permission/{id}#check`) via existing `IAuthorizationService`.
Wired on ContentEndpoints, ContentCategoryEndpoints, ContentAuthorEndpoints, ContentArticleMediaEndpoints.
403 uses established `admin.authorization.denied` — no raw permission ids in UI message.
platform-admin continues to receive all catalog permissions via EnsureBootstrapAsync reconcile.
