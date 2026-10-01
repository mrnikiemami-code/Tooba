# route-matrix — CERT

| Route | Method | Owner | Mapping count |
| --- | --- | --- | --- |
| /v1/admin/dashboard | GET | Host Admin/Panel AdminPanelEndpoints | 1 |
| /v1/admin/dev-context | GET | Host Admin/Development AdminDevContextEndpoints | 1 |
| /v1/admin/sellers | GET | Party.Endpoints PartyAdminSellersEndpoints | 1 |
| /v1/admin/sellers/query | POST | Party.Endpoints PartyAdminSellersEndpoints | 1 |

Program wires MapAdminPanelEndpoints + MapAdminDevContextEndpoints + MapPartyEndpoints.
Panel seller/dev-context MapGet residue = ZERO.
