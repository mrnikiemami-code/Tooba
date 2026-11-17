# TB-276 — The one lawful user review case

Scope D of the envelope authorised exactly ONE ordinary `CustomerDelivered` / `Requested` return case, created through
the EXISTING authorized owning command, so the populated queue/detail can be inspected on phone and desktop. Nothing else
was created, and nothing on the case was pre-filled.

## Identity

| Fact | Value |
| --- | --- |
| Return request | `01a1e6b0-f33a-7000-80ee-beffb6344490` |
| Human locator (queue `returnReference`) | `RET-TB-20261114175151-01-7d5f59-1` |
| Seller order | `01a1db2f-e375-7000-8197-9657a380a505` (status `Paid`, unchanged) |
| Owning checkout | `01a1db2f-e36e-7000-b7d3-afb2fe86e81a` — the candidate named in the envelope; eligibility was read from the owning reads, never inferred from the URL |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Requesting user | `01a1db2f-e248-7000-8e47-bcc8277adb7f` (the order's own customer, server-derived) |
| Origin / status | `CustomerDelivered` / `Requested` |
| Idempotency key | `tb276-review-case-01a1db2f` (stable task occurrence) |
| Reason | «پروندهٔ بررسی رابط کاربری مرجوعی — TB-276» |
| Refund amount / currency / destination | `2407839.0000` / `IRR` / `OriginalPayment` (server-derived from the order) |
| Created at | `2026-11-16T23:28:38.714497+00:00` |
| Line | `01a1e6b0-f33f-7000-9d63-58fe9922572c`, order line `01a1db2f-e379-7000-80f5-91d1401c225c`, quantity `1.000000`, one whole piece (no fractional trick), unit price `2407839.0000` |
| Reservation | `01a1db2f-e393-7000-b41e-4e131e320384` (the order line's existing reservation; no hold was created or changed) |

## Before / after

| Measure | Before (the state 275 left behind) | After |
| --- | --- | --- |
| `POST /v1/admin/returns/query` `totalCount` | `0` (recorded in 275's receipts) | `1` |
| `counts` | all zero | `allOpen=1 receiving=1 decision=0 refund=0 refundFailed=0 disposition=0 history=0` |
| `returns.return_requests` for this checkout | 0 | **1** |
| `returns.return_items` for this case | 0 | **1** |
| `returns.customer_return_receipts` | 0 | **0** |
| `returns.customer_return_receipt_lines` | 0 | **0** |
| `returns.return_request_decisions` | 0 | **0** |
| `returns.refund_attempts` | 0 | **0** |
| item `condition` / `disposition` | — | `Usable` / `None` (the stored default of a line nobody has inspected; **no receipt** exists, so the page must not read this as "arrived usable") |
| `order.seller_orders` status for the source order | `Paid` | `Paid` |

The item's `condition` column carries the pre-existing domain default. There is **no** receipt occurrence, so the owning
read publishes `receiving.received=0`, `usable=0`, `damaged=0`, `remaining=1.000000`, `isComplete=false` — the honest
"nothing has arrived yet" state. `SaleableRestockedQuantity` / disposition stay `None`, so no stock was touched.

## What the live owning read publishes

`GET /v1/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490` with the dev actor header → `200`:

```
status=Requested  origin=CustomerDelivered  currency=IRR  refundAmount=2407839.0000
refundDestination=OriginalPayment  paymentId(<none published>)  refundAttempts=[]
closurePhase=None  releasedHeldQuantity=0.000000
recovery={phase:None, financialComplete:false, physicalComplete:false}
receiving={received:0, usable:0, damaged:0, remaining:1.000000, isComplete:false, isCoherent:true,
           lines:[{requested:1.000000, received:0, usable:0, damaged:0}]}
receipts=[]
work={category:receiving, canRecordReceipt:true, canDecide:false, canResumeRefund:false,
      canRetryRefund:false, canSettlePhysical:false, isOpen:true, needsOperator:true,
      actionCodes:["record_receipt"]}
items[0]={productLabel:"لنوو اولترابوک مدل 2", unitLabel:"pcs", quantity:1.000000,
          unitPriceSnapshot:2407839.0000, condition:"Usable", disposition:"None"}
```

So the case genuinely sits in `receiving` with exactly ONE published action. No approval, no refund and no physical
settlement is offered, because none of them is lawful before goods are received — that is the policy telling the truth,
not a hidden fixture.

## Events and audit trail

The creation produced ONE normal domain event, `returns.outbox_messages` row
`{"returnRequestId":"01a1e6b0-…","sellerOrderId":"01a1db2f-e375-…","checkoutId":"01a1db2f-e36e-…"}`,
`event_type=return.requested.v1`, `processed_at` set, `attempt_count=1`.

Disclosed honestly: the `returns.outbox_messages` table holds **3** `return.requested.v1` rows in total. Two of them
(`2026-11-14 05:57:19` for request `01a1d8a1-b65e-7000-b5f9-c65e06529cfa` and `2026-11-14 05:57:45` for request
`01a1d8a2-1d88-7000-a146-787bdeabab62`) are **pre-existing leftovers** whose request rows no longer exist — they predate
this task and belong to the earlier task-labelled fixtures recorded in the 272 audit. This task wrote no history row and
deleted nothing; exactly one of the three rows corresponds to the case created here.

## Boundaries actually respected

- Created only through the existing authorized create command: server-derived actor, ownership, seller order, amount,
  currency, destination and quantity; no raw SQL, no `DbContext` bypass, no disabled guard, no seed.
- No receipt, inspection, decision, approval/rejection, refund, retry, recovery, physical settlement or disposition was
  executed — the counts above prove the absence, and the live `work.actionCodes` offers only `record_receipt`.
- No payment, order transition, shipment/parcel, stock, hold, reservation-quantity, provider, wallet, waybill or
  notification effect. The source order stayed `Paid` and the line kept its existing reservation id.
- No second case was created to improve appearance: `returns.return_requests` has exactly ONE row and exactly one case
  exists for this checkout (asserted by count, not by eye).
- No cleanup, delete, reject or reset: the case is left intact for the user's inspection.

## Honest limits

- The idempotent **replay** path of the creation was not exercised, because the creation response was received on the
  first attempt. What is proven instead is the resulting uniqueness: one row for the task's idempotency key and one case
  for the checkout.
- The case owns no money fact (`payment_id` is not set on this return), so no refund/retry path can be demonstrated on it.
  That is a property of the source order, not something this task may fabricate.
- The prices/quantity shown are the real snapshots of an existing paid trial order; the visible customer/seller display
  names come from the existing order reads and were deliberately left unsanitised (they are the same values the operator
  surface already shows).
