# TB-P10-T004-R11 — Runtime
Host :5088 (`Host: alpha.localhost`). Raw: r11-runtime-raw.json ok=true.

| Scenario | Payable | Seller alloc | StoreShipping | Order Paid | Settlement-visible |
| --- | --- | --- | --- | --- | --- |
| A One seller + shipping | 201088 | 1088 | 200000 | 1/1 Succeeded inbox=1 | 1088 |
| B Two sellers + shipping | 2217588 | 2017588 | 200000 | 2/2; shippingOnSeller=0 | 2017588 |
| shipping-zero | 1088 | 1088 | 0 | 1/1 Succeeded | 1088 |
| C Manual confirm | 201088 | 1088 | 200000 | 1/1 evidence+confirm 200 | 1088 |
| D Sandbox success | 201088 | 1088 | 200000 | 1/1 | 1088 |
| E Expired retry then success | 201088 | 1088 | 200000 | 1/1 retry 200 | 1088 |
| F Late captured | 201088 | 1088 | 200000 | 1/1 inbox=1 | 1088 |
| G Duplicate Succeeded | 201088 | 1088 | 200000 | inbox 1→1 | 1088 |
