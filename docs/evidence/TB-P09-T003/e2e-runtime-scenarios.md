# TB-P09-T003 — E2E runtime scenarios

Host `http://127.0.0.1:5088`, actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`, Host `alpha.localhost`.

| Scenario | Checkout | Result |
|----------|----------|--------|
| A Awaiting payment | `01a04539-6c97-7000-9fd1-a131f4ad60d5` | ops=`cancel` only; `create_shipment`/`mark_processing` rejected |
| B/C Paid ReadyToFulfill → Delivered | `01a044f8-01ce-7000-a21f-3816257c0ad2` | mark_processing → packed → create_shipment → assign_tracking → dispatch → deliver |
| D Return eligible | same after deliver | `request_return` visible; request+approve → Completed + refund Succeeded |
| E Invalid return on pending | pending checkout | backend 400 reject |
| F Settled sample | `01a0451c-fabf-7000-99f0-30d410a58638` | ops empty (already returned); financialSummary present (payable/commission intact) |
| G Notes/history/invoice | paid/lifecycle checkouts | note actor human; history paged; invoice HTML 200 with فاکتور |

## Defects found & fixed during gate
1. `create_shipment` remained projected / domain allowed over-allocate while prior shipment still `Created` → fixed allocation check + Host projection.
2. Duplicate global `tracking_reference` unique conflict returned raw 500 → mapped to human FA 400 «این کد پیگیری قبلاً ثبت شده است.»
