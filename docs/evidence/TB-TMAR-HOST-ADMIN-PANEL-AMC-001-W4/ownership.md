# ownership — W4

| Surface | Owner |
| --- | --- |
| `GET /v1/admin/dev-context` | `Tooba.Host.Admin.Development.AdminDevContextEndpoints` |
| `GET /v1/admin/dashboard` | `Tooba.Host.Admin.Panel.AdminPanelEndpoints` (W3 preserved) |
| `GET /v1/admin/sellers` | `Party.Endpoints` (W1) |
| `POST /v1/admin/sellers/query` | `Party.Endpoints` (W2) |

Architect decision: `MOVE_TO_HOST_ADMIN_DEVELOPMENT`.
Panel retains only dashboard composition.
