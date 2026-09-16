# Admin form / workflow gap matrix — TB-P11-T001

| Module | Create | Edit/View | Validation | Cancel/back | Unsaved | Confirm destructive | Localized errors | Raw GUID/enum | Selectors | First-item | Severity |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Products | Y | workspace | strong | dirty+beforeunload | Y | archive | mapAdminError | avoided | combobox | N | LOW |
| Categories | tree | tabs | strongish | partial | weaker | media slots | Y | guarded | tree | N | MEDIUM |
| Brands | **missing Admin CRUD** | n/a | — | — | — | — | — | — | product/landing only | — | HIGH product gap |
| Content | Y | editor | strong | dirty badge | partial | delete | Y | avoided | author/category | N | LOW |
| Store Pages | Y | composer | section | dirty | Y | publish paths | Y | hidden in selectors | ADG resources | first-section CTA | LOW |
| Menus | Y | editor | basic | — | **none** | delete | Y | hidden | destination | empty CTA | HIGH |
| Shipping | dialog | dialog | light | close | none | **no confirm** | often raw | codes OK | — | seed names | HIGH |
| Reviews | n/a | inline | — | — | — | **no confirm** | weak | — | — | — | HIGH |
| Promotions | none | deactivate | — | — | — | **no confirm** | weak | sellerPartyId | — | — | HIGH |
| Gift cards | issue | detail | amount | — | — | revoke | mixed EN | CardId | — | — | HIGH |
| Wallets | adjust | inspect | reason | — | — | — | weak | **ActorUserId GUID** | none | — | HIGH |
| Tickets | no admin create | detail | — | — | — | — | partial | ticketId | — | — | MEDIUM |
| ACC | roles | matrix | scope | role switch | dirty | archive | some | key search | user search | — | MEDIUM |
| Settings | tabs | inline | field | — | no global | sparse | mixed EN | — | — | — | MEDIUM |
| Page composition | n/a | reorder | — | — | none | restore? | raw | **sectionType enum** | — | — | HIGH |
| Sellers/customers | none | list | — | — | — | — | — | — | — | — | HIGH |
