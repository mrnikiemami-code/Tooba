# R1 Runtime — Reverse Packing Before Allocation (Scenario B)

Same seller/line as Scenario A after partial pack (packed=2, allocated=0).

Projected actions before unpack: `mark_packed,unpack,create_shipment` (includes بازگشت از بسته‌بندی / `unpack`).

| Step | Result |
| --- | --- |
| `unpack` qty=2 | HTTP 200 |
| packed after | 0 |
| status | Processing |

Repack qty=2 then remaining qty=3 for subsequent scenarios.
