# ARCHITECTURE-PLAN — TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284

Protocol: TOOBA-DELIVERY-001
Base-SHA: f67d6ecd6cb222a2a17d39d6e2d6af391218e4ad
Task-SHA256: f658d3da7745d0c3297c17c6c713f371dc2fdcc72c9bde156a7388192f3f2a5c
Prepared-before-production-edit: 2026-11-22T16:20Z; product source was not edited. Existing 283 code `611f97a85ec7c2e464e4f649820992202ca3754c` is the reviewed implementation.

## Outcome and fixed requirements

The task closes the two accepted gaps of TB-283 on one served seller edit page, one real owning Host, and one isolated PostgreSQL database. Original TB-283 REQ-01 through REQ-07 remain authoritative. No product rule, schema, provider, seed, permission or roadmap stage is reopened.

| REQ | Actor / entry | Trigger / expected effect | Owner and source | Invariants | Acceptance scenarios |
|---|---|---|---|---|---|
| REQ-01 | Seller owner, existing VendorShell edit page | Change only SKU of an existing offer with named positions 3 and 7; save; one owning Offer patch; independent GET and SQL show new SKU, same price, positions, reservations and inventory outbox count | Offer PATCH `/v1/seller/offers/{offerId}`; detail GET `/v1/seller/offers/{offerId}`; Pricing and Inventory stay untouched | Same offer/seller; no price or inventory command; independent persistence read; no optimistic-only claim | S-01, S-02 |
| REQ-02 | Seller owner, Draft offer | Draft SKU edit remains Draft; explicit both-limit clear persists after reread; unrelated PATCH preserves prior limits | Offer PATCH plus explicit `clearOrderQuantityLimits`; Offer detail | No status transition; absent fields preserve; explicit clear distinct from absent | S-03, S-04 |
| REQ-03 | Two independent seller browser sessions | Both load the same offer; B loads the old price; A saves a new price; B saves SKU only from its stale form; independent GET and SQL show both new price and new SKU; B sends no Pricing write | A: `/v1/seller/offers/{offerId}/price`; B: `/v1/seller/offers/{offerId}`; Pricing and Offer persistence | Stale form must not resend price; new price survives; stock and reservations unchanged | S-05 |
| REQ-04 | Seller owner, existing offer | Change only named second position 7 to 8; reread shows 3 and 8 and total 11; first position, price, SKU and reservations unchanged; ambiguous/foreign/below-reserve refusals leave zero change | Inventory write `/v1/seller/offers/{offerId}/inventory`; Inventory stock positions | Explicit `StockItemId`; same offer/seller; reserved floor; one position only | S-06, S-07 |
| REQ-05 | Seller browser | Wrong identity, stale value or disconnect after write must not produce false success; a GET reconciles; no blind second command | Seller API response validator and independent GET | Effect and identity required for success; unknown remains recoverable without retry write | S-08, S-09 |
| REQ-06 | Seller browser, 360x640 | Dirty form cancel sends zero writes; reopen shows old values; mobile position input, Save and Cancel remain reachable without horizontal document overflow | Existing VendorShell page and DS controls | No synthetic CSS test; actual Chromium geometry and normal scrolling | S-10, S-11 |
| REQ-07 | Seller browser | Save with no changes sends zero writes; a failed first section does not clear or commit a second draft; after deliberate second-section save both values persist | Seller page sectional save, owning commands, independent GET | No cross-section overwrite; independent persistence | S-12, S-13 |

## Architecture and real route

Real browser route: served Next page `http://127.0.0.1:3100/fa/vendor-panel/products/{offerId}` in an isolated frontend copy built with `TOOBA_HOST_ORIGIN=http://127.0.0.1:5090`. Next rewrites `/v1/*` to the isolated Host. The VendorShell obtains the legitimate Development seller actor and party from `/v1/seller/dev-contexts`, stores the exact pair, and sends `X-Tooba-Seller-Party-Id` and `X-Tooba-Dev-Actor-User-Id`. Host authorizes the actor for that party before the seller route runs.

Offer detail is composed by `OfferReadModelComposer` from Offer ownership, Pricing and Inventory. Commercial PATCH goes to `UpdateOfferCommand`; price goes to the existing Pricing seller command path; each stock position goes to the existing Inventory seller write with explicit `StockItemId`. The page sends only changed sections: commercial patch, price, and per-position inventory. A successful response is checked for the same offer, seller and requested effect. An uncertain response performs an independent GET and requires the requested value before success.

Independent verification is an authenticated GET with the same actor/party plus direct SQL against the isolated database. No React state, HTTP 2xx alone, toast, regex or fixture-authored response is accepted as persistence proof.

## Contract and lifecycle

The existing 283 contracts are used unchanged: `SellerOfferDetailPage` with `inventoryLocations`; `SellerInventoryLocation`; `SellerOfferInventoryWriteRequest` with optional `StockItemId`; `clearOrderQuantityLimits`; price and inventory responses with offer/seller identity and effect. Absent limit fields preserve stored limits; explicit clear sets both to null. A multi-position caller without `StockItemId` is refused. Foreign target and below-reserved quantity are refused. The server remains the authority; the browser cannot choose another seller or another offer.

## Scenario matrix

| ID | Requirement | Entry/role/provider | Preconditions and action | Expected result + side effects | Proof level | Before-fix observation |
|---|---|---|---|---|---|---|
| S-01 | REQ-01 | Real seller browser, isolated Host and PostgreSQL | Offer has positions 3 and 7, reservations 1 and 2, price 2,500,000, SKU ARM-LN-01. Change SKU only and Save | Exactly one Offer PATCH; zero price/inventory writes; new SKU visible | Connected browser and HTTP capture | 283 controlled browser only |
| S-02 | REQ-01 | Same connected chain plus direct SQL | Record SQL before, save SKU, GET and SQL after | New SKU; price, positions, reservations and inventory outbox count unchanged | Connected persistence | 283 split rollback tests |
| S-03 | REQ-02 | Real seller browser, Draft offer | Draft offer; save SKU only; reload | Status remains Draft; SKU persists | Connected browser and GET | 283 rollback only |
| S-04 | REQ-02 | Real seller browser and SQL | Set both limits, explicit clear both, reload | Both null after reload; unrelated patch preserves limits | Connected browser and SQL | 283 rollback only |
| S-05 | REQ-03 | Two real seller browser contexts | B opens old price; A saves new price; B saves SKU from stale form | New price and new SKU both persist; B sends zero Pricing writes; stock unchanged | Connected two-session and SQL | 283 controlled browser only |
| S-06 | REQ-04 | Real seller browser and SQL | Change only second position 7 to 8 | Reread 3 and 8, total 11; first position, price, SKU and reservations unchanged | Connected browser and GET | 283 split rollback |
| S-07 | REQ-04 | Owning Host and PostgreSQL | Ambiguous multi-position, foreign target, below-reserve attempt | Typed refusal; zero change in all positions and inventory outbox | Connected negative control | 283 rollback only |
| S-08 | REQ-05 | Real browser with controlled response substitution | Return other offer/seller or stale same-offer response | No success claim; independent GET remains authoritative | Browser error path | 283 controlled browser |
| S-09 | REQ-05 | Real browser with transport loss after write | Drop response after the owning write | Honest unknown; GET reconciles; no automatic second write | Browser error path and SQL | 283 controlled browser |
| S-10 | REQ-06 | Real browser | Dirty cancel and reopen | Zero writes; old values restored | Browser behavior | 283 controlled browser |
| S-11 | REQ-06 | Chromium 360x640 | Open dirty form; measure positions, Save, Cancel and document overflow | All reachable; document overflow zero | Browser geometry | 283 controlled browser |
| S-12 | REQ-07 | Real browser | No-change Save | Zero writes | Browser behavior | 283 controlled browser |
| S-13 | REQ-07 | Real browser with one refused section | Edit two sections; first fails; save second; reload | Second draft preserved until its own save; both correct after save | Browser and GET | 283 controlled browser |

## Internal waves, same task

| Wave | Depends on | REQs/scenarios | Small connected change | Focused checks | Exit result |
|---|---|---|---|---|---|
| W1 | none | environment | Isolated database, Host 5090, Next 3100, fixture offer with two positions and reservations, preflight browser write and reset | Host health, seller dev-contexts, real page, one disposable PATCH, canonical DB unchanged | Environment safe; preflight PASS |
| W2 | W1 | REQ-01 / S-01 S-02 | Execute SKU-only save through the real form | One PATCH, zero price/inventory writes, GET and SQL before/after | CONNECTED-01 PASS |
| W3 | W2 | REQ-03 / S-05 | Two browser sessions, stale B, new price A, SKU B | Price and SKU survive; B zero price writes; stock unchanged | CONNECTED-03 PASS |
| W4 | W3 | REQ-02, REQ-04, REQ-05, REQ-06, REQ-07 | Recheck affected connected behaviors and negatives | Real browser and PostgreSQL per scenario | All affected checks PASS or truthful gap |
| W5 | W4 | all | Run OpenSpec strict, tsc, affected tests, production build, review gate; push; candidate only after PASS | Structural and evidence check | READY or BLOCKED with exact cause |

## Controlled data, regression and served handoff

Isolation target: disposable PostgreSQL database `tooba_offer283_connected` in the existing `postgres-db` container on 127.0.0.1:5432. The canonical database `tooba_alpha`, its Host 5088, its Next 3000, its DataProtection and its user rows are not written. The isolated database is migrated and seeded by the existing Development bootstrap; fixture changes are task-labelled. The fixture offer is `01a2041e-bb6b-7000-9163-d736d71634d5` for seller `01a2041e-bb65-7000-9b34-1d049b3a788a` with positions `01a2041e-bbe0-7000-9897-006e14fe11c0` and `01a2041e-bbd3-7000-9fe5-0e7d2157003e`. Price is 2,500,000 IRR, SKU ARM-LN-01, positions 3 and 7 with reservations 1 and 2.

Cleanup is dropping the isolated database and stopping only the isolated Host and Next processes. No seed, grant, migration or fixture is applied to canonical data. Browser failure rows (wrong response, transport loss) use controlled response substitution only for the error branch; the real success path uses the owning Host and PostgreSQL.

Affected code checks: seller API and page focused tests, `npx tsc --noEmit`, production Next build, backend build if code changes. OpenSpec strict validation of `seller-offer-safe-edit-283`. Review gate with the exact issued task hash. If no production code change is required, the final code SHA remains `611f97a85ec7c2e464e4f649820992202ca3754c`.

## Checkpoint log

W1 preflight: disposable database created; Host 5090 ready with PostgreSQL configured and messaging disabled; Next 3100 built against Host 5090; real Chromium loaded the two-position seller page; one disposable SKU PATCH persisted; fixture reset to ARM-LN-01; canonical Host health 200 and canonical data untouched. Internal worker evidence is not independent Architect review.
