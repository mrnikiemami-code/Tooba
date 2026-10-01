# route-parity — W2
| Route | Owner |
|---|---|
| POST /v1/admin/sellers/query | Party.Endpoints only |
| GET /v1/admin/sellers | Party.Endpoints (unchanged W1) |
| GET /v1/admin/dashboard | Host Panel (unchanged) |
| GET /v1/admin/dev-context | Host Panel (deferred) |
Host MapPost("/sellers/query") = ZERO. Duplicate route = ZERO.
Success body: GridPageResponse&lt;AdminSellerListItem&gt; via ApiResponseFactory.From.
