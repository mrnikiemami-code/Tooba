# Receipt, inspection and stock integrity proof — TB-274-R1 (A, B, C)

Every fact below separates **controlled unit proof** (in-memory EF + scripted ports), **relational proof** (real
`tooba_alpha`, caller-owned transaction rolled back, `committedRows = 0`), **served wire fact** (running Host, read-only)
and **untested**.

## 1. A — inspection is explicit at every reachable entry

### 1.1 Controlled unit facts (`ReturnsCustomerReceipts274R1Tests.A_*`, `ReturnsCustomerReceipts274Tests`)

| Fact | Assertion |
| --- | --- |
| `A_inspection_has_no_optional_default_on_any_reachable_shape` | reflection over `CustomerReceiptLine`, `CustomerReceiptLineCommand`, `AdminReturnReceiptLine`: the single `Condition` parameter is **not** `IsOptional` and has **no** default value — an omitted inspection cannot compile into a line |
| `The_wire_condition_token_is_explicit_and_a_missing_token_is_a_stable_refusal` | `Parse(null)`, `Parse("")`, `Parse("   ")` and `Parse("broken")` all fail with `return.receipt.line_invalid`; `Parse("Usable")` → `Usable`; `Parse(" DAMAGED ")` → `Damaged` (case/space tolerant, exactly two accepted values) |
| Direct owning construction | `new CustomerReceiptLine(item, 1m, ReturnItemCondition.Usable)` is the only construction shape; the argument no longer has a default, so the defined condition is supplied deliberately at every call site |

### 1.2 Served wire facts (read-only, `POST /v1/admin/returns/{id}/receipt`, non-existent case id)

| Request | Result |
| --- | --- |
| no admin actor header | `401 admin.actor.missing` |
| actor + line **without** `condition` | `400 return.receipt.line_invalid` — answered *before* the case is read (the id does not exist, yet the answer is the line fault, not `return.missing`) |
| actor + line with `"condition": ""` | `400 return.receipt.line_invalid` (same, before any read) |
| actor + line with `"condition": "damaged"` on the same missing id | `404 return.missing` (proves the two probes above refused before dispatch) |

No valid live case was posted, by design.

## 2. B — stored evidence and ownership fail closed

### 2.1 Controlled unit facts

| Fact | Assertion |
| --- | --- |
| `B_malformed_stored_rows_never_become_coverage_or_usable_quantity` | stored `0` and `-1` quantities and an undefined inspection `(ReturnItemCondition)7` make the summary **incoherent**, never complete, with `Usable = 0` and `Received = 0`; a `1.0000001` stored quantity is refused instead of rounded |
| `B_a_line_answering_for_another_cases_receipt_header_is_never_borrowed` | with `owningReceiptIds = [own]`, a line answering for `foreign` sets `IsCoherent = false` and only the own 1 unit counts |
| `B_a_new_occurrence_is_refused_when_the_stored_evidence_already_contradicts_itself` | an over-covered stored row (only a corrupting writer could produce it) makes the summary incoherent; a NEW occurrence is refused with `return.recovery.evidence_conflict` and the receipt/line counts stay `1`/`1` — no row is added |
| `B_the_directory_refuses_a_receipt_with_no_authenticated_actor` | `actorUserId = Guid.Empty` → `return.receipt.actor_required`, `0` receipts, `0` lines |
| `B_a_foreign_seller_scope_can_neither_approve_nor_reject_another_case` | `ApproveAsync`/`RejectAsync` with a foreign `SellerPartyId` refuse with `return.missing` inside the directory (no handler-only check), leaving the case, its decision and its money untouched |

### 2.2 Relational facts (real PostgreSQL, rollback-only)

| Fact | Assertion |
| --- | --- |
| `A_two_occurrences_of_the_same_line_and_condition_are_two_legal_rows_and_accumulate` | the unique identity is per occurrence `(receipt, item, condition)`, so two deliveries of the same line/condition are legal rows and accumulate cumulatively |
| `B_the_stored_checks_refuse_a_nonpositive_quantity_and_an_undefined_inspection` | a direct insert of `quantity = 0` / `-1` is refused by `ck_customer_return_receipt_lines_quantity_positive`; a direct insert of `condition = 'Broken'` is refused by `ck_customer_return_receipt_lines_condition_defined` |
| `B_a_line_naming_another_cases_receipt_is_refused_by_the_local_composite_foreign_key` | a line whose `return_request_id` is case B while its `return_receipt_id` belongs to case A is refused by `fk_customer_return_receipt_lines_receipts_owning_case` (and the item case by `fk_customer_return_receipt_lines_return_items_owning_case`) |
| `B_a_foreign_seller_scope_cannot_write_a_decision_on_the_real_store` | the owning-directory scope refusal holds on the real store: no decision row is written for a foreign seller scope |

Each fact opens its own caller-owned transaction and rolls it back; the suite prints `committedRows = 0` and the
canonical target is validated (loopback, `5432`, `tooba_alpha`, server-confirmed identity) before any write.

## 3. C — never claim stock completion without Inventory

| Fact | Assertion |
| --- | --- |
| `C_inspected_usable_without_an_owning_stock_binding_is_a_typed_pending_blocker` | usable 2 inspected, no reservation: the money **is** confirmed (1 gateway call, 1 attempt) but `0` Inventory calls and `Disposition = None`; the read reports `Blocked` + `return.recovery.stock_binding_required`, `FinancialComplete = true`, `PhysicalComplete = false`, **no** `RecoveryKind`; the `settle_physical_disposition` action refuses with the same code, `0` restock, `0` new attempt |
| `C_a_damaged_only_receipt_without_a_binding_completes_without_any_inventory_call` | a fully damaged line needs no binding: `0` restock calls, `DamagedNonSaleable` recorded, `Phase = Settled` — a damaged unit is never a saleable blocker |
| `C_a_mixed_line_restocks_only_the_inspected_usable_portion` | one requested line inspected as 1 usable + 1 damaged: exactly **one** restock of `1` (never `2`), `Disposition = SaleableRestock`, `SaleableRestockedQuantity = 1` while the requested quantity stays `2` |
| `C_a_rejected_case_that_received_goods_reports_the_remaining_physical_resolution` | a rejected case with a complete receipt keeps its receipt/usable facts visible, reports `Blocked` + `return.recovery.physical_resolution_required` with no kind, `0` restock, `0` gateway calls, `0` attempts, and `Disposition = None` |
| `C_a_still_requested_case_with_a_partial_receipt_is_not_reported_as_blocked` | a `Requested` case with 1 of 2 units received and no disposition stays `Phase = None`, no blocker, no kind — normal in-progress work is not reported as an owed resolution |

The restock quantity comes from `ReturnReceivingSummary` (`Usable` of that exact line); the damaged portion never
reaches saleable Inventory. The stable key `return-restock-{case:N}-{item:N}` is unchanged and a failed local save
reloads only the affected entry.

## 4. What is NOT proven here

- No real Inventory/PSP/Wallet provider was called; the ports are controlled and assert call counts and quantities.
- No live return case exists, so there is no served end-to-end receiving walkthrough (see §5 — the wire facts are
  real but read-only/refusal-only).
- The relational facts are true PostgreSQL constraints but rollback-only; no committed receipt row exists.
- The cumulative upper bound is enforced by the serialized owning code, not by a DB CHECK (a cross-row bound is not
  expressible as a single-row CHECK and was deliberately not faked).

## 5. Served state after the Host-only refresh

| Probe | Result |
| --- | --- |
| `GET /health` | `200` |
| `GET /health/ready` | `200` — `SingleStore`, `postgresql=configured`, `messaging=Healthy` |
| `GET /__platform-commerce` | `store-alpha` / `tenant-alpha` / `127.0.0.1` |
| `POST /v1/admin/returns/{missing}/receipt` without actor | `401 admin.actor.missing` |
| `POST /v1/admin/returns/{missing}/receipt` actor + omitted condition | `400 return.receipt.line_invalid` |
| `POST /v1/admin/returns/{missing}/receipt` actor + blank condition | `400 return.receipt.line_invalid` |
| `POST /v1/admin/returns/{missing}/receipt` actor + `damaged` | `404 return.missing` |
| `POST /v1/admin/returns/query` (actor) | `200 {"items":[],"page":1,"pageSize":20,"totalCount":0}` |
