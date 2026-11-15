# TB-270 — user review

**There is no new action to try in this run.** The UI is unchanged; the durable own-plan feature is not implemented.

## What you can look at (read-only, unchanged behaviour)

* Queue: `http://localhost:3000/fa/admin/fulfillments`
* Order operations (the previously exercised resend order):
  `http://localhost:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations`

On those surfaces a **resend / replacement** parcel still shows **«موعد ارسال مشخص نشده»** and no handoff date. That is
the current, truthful state: the new attempt has no accepted plan of its own, and the app deliberately does not print the
original parcel's expired date on it. Nothing was regressed: first-dispatch parcels, the customer promise, the tabs, the
Persian dates and every action are exactly as they were after TB-269.

## What is still missing (why this is not the feature you asked for)

To let an employee see "when can this resend be prepared, leave, reach the carrier and reach the customer", the new
attempt needs its own durable plan. That needs two things that do not exist yet:

1. a small Shipping read that returns the **carrier / customer / processing working weeks** of the captured shipping
   configuration (today the only such read is the rate-quote endpoint, which needs a destination and measurements the
   order no longer carries), and
2. an Order-owned record of the attempt's plan plus a reviewed «ثبت برنامهٔ ارسال» step in the existing parcel card.

Both are described precisely in the evidence folder
(`docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270/ownership-and-plan.md`) and are the
recommended next bounded task. No order, stock, setting or history of yours was changed by this run.
