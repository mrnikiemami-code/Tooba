# P1-A resumed operational validation

Status: FAIL / NOT_RUN items remain. No product, test, guard or contract edits.
Branch/HEAD: opencode/iso-agents-002 / db60582121aee320ba1fb6fadd8251ac03e6a2b0
Candidate product: b1b2113e27ecdb6244764923bae27cc5e91efaae
Candidate build: Host publish PASS; Host DLL SHA-256 9E1429C0855E0BFD53ECEF277523E00E2114AA9E7D39DB1073CB71EA40407FA9.
Candidate Host PID during tests: 25068; health ready; DB tooba_alpha.
Migration applied: inventory 20261126190000_R10ReservationPurpose PASS.
Restored Host: PID 33260; baseline DLL SHA-256 1A9604BD1889294542854B31A0DECB0D6F285A8788E6028A03B2EE63A32B23C8.

## Builds (all exit 0)
Host, Offer, Pricing, Inventory, Cart, Order, Fulfillment, Returns, Host.Tests.
Logs: evidence/architect-validation/resume-build-*.log.

## Tests
- Offer: 132 passed / 1 failed / 133.
  - FAIL `OfferInventoryProvisioningHandlerTests.Eligible_location_active_filter_and_code_order_are_source_verified` at line 115: expected source substring `x.LocationId == locationId && x.Status == InventoryLocationStatus.Active` no longer exists after candidate provisioning rewrite. Raw: resume-offer-source-failure.log.
- Pricing: 13/13 PASS.
- Inventory: 53/53 PASS.
- Cart: 29/29 PASS.
- Returns: 191/191 PASS.
- Architecture filters:
  - Offer 51/51 PASS; Pricing 7/7 PASS; Inventory 22/22 PASS; Cart 7/7 PASS; Returns 3/3 PASS.
  - Host.Tests architecture: 1382 passed / 85 failed / 1467. Example `HostAdminAmcW10GuardTests.Attribute_ProductValues_path_namespace_exact_and_validators_classified` line 115 expected False actual True. Full: resume-arch-Tooba.Host.Tests.log; example: host-arch-first.log. This is a real guard-suite failure.
- Order relational: 105 failed / 664 passed / 2 skipped; failures require task-owned `TOOBA_TB253R3_ORDER_POSTGRES` (127.0.0.1:15433/order_tb253r3). NOT_RUN against existing tooba_alpha by policy. Raw: test-Tooba.Order.Tests.log, order-first-failure.log.
- Fulfillment relational: 20 failed / 490 passed; requires task-owned `TB196R6_CONNECTION`. NOT_RUN. Raw: test-Tooba.Fulfillment.Tests.log, fulfillment-first-failure.log.
- Strict OpenSpec: exit 0 with existing long-requirement warnings. Raw: openspec-strict.log.

## Actual HTTP/PostgreSQL scenarios (candidate Host, tooba_alpha)
Raw: http-results.json, http-results.log; SQL snapshots and cleanup.sql.
PASS (14/14 applicable):
- Admin detail returns offer/price/position revisions, offeredQuantity null, admin capabilities physical+assign true; seller platform detail capabilities false.
- Seller platform physical write 403, no state change.
- Missing physical revision 409; unknown/inactive location 404; seller foreign location 403; failed provisioning created no extra position/fact.
- Concurrent physical absolute writes same revision: exactly one 200 value 110 and one 409 (`inventory.position.revision_conflict`); SQL facts only winner.
- Physical replay same operationId 200 with no additional fact; changed payload same operationId 409.
- Price existing update, concurrent writes same revision: one 200 value 1200 and one 409; one fact for winner. First-create race: one 200 value 2000 and one 409; one fact. Missing/stale 409; exact replay 200; changed payload 409.
- Legacy price without fact exposes real price-row revision.
- History: 7 events, stable 2-per-page paging, unique eventId, targetId/locationId, actor, old/new; admin vs seller capabilities differ correctly.
- Active duplicate seller+variant+channel 409 `offer.listing.duplicate_active`.
- Fixtures cleaned: 0 P1A locations/offers/prices/positions/inventory facts/price facts; baseline Host restored.

NOT_RUN / FAIL:
- A3 platform allocation lifecycle: NOT_RUN. DEV edition is SingleStore; candidate explicitly rejects offered-quantity outside Marketplace (`InventoryDirectory.Allocation.cs:35`), and endpoint returned 403 `inventory.location.not_eligible`; allocation count remained 0. Cannot test 100/20/5 hold/consume/release/cross-position race in this environment without changing edition (out of authorized scope; no new DB/Host).
- Seller-owned Marketplace A/B location positive: NOT_RUN for same SingleStore edition gate. Store-scoped admin positive and seller platform denial were executed.
- A4 failure injection before/after owner commit and transaction projection retry: NOT_RUN (no authorized fault-injection endpoint; no test modification).
- A4 offered-quantity expectedVersion success path: NOT_RUN; only SingleStore 403 denial observed. Wire field/route accepted, but success semantics cannot be verified here.
- Order/Fulfillment relational regression: NOT_RUN due missing task-owned disposable DBs.
- Host architecture 85 failures: FAIL.
- Offer source-guard 1 failure: FAIL.

No repair performed. STOP_FOR_ARCHITECT.
