# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R1

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R1`
- Bridge row: `40d51a3b-1dba-482a-ac8b-aebe1ad278de`
- Base-SHA: `c30817c6a88dc2ada5b13c0813491a355b72b030`
- ClaimedAtUtc: ~2026-11-02T16:04:37Z

## Tests

| Suite | Result |
|-------|--------|
| Shipping readiness/directory/classification/authoring harness | 44 passed |
| order-group-carrier-execution.test.ts | 4 passed |
| npm typecheck / build | 0 / 0 |
| Host publish | 0 |

## Backup / migrate

- Dump 2714315 bytes SHA256 `74D2D3101A6209C2077E840804387FB2D9E6DAC7F5E53780C112143AAAE6F392`
- Applied `20261102210000_CarrierShipmentCompletions`
- Readback tables: `shipping.carrier_shipment_sender_completions`, `shipping.carrier_shipment_package_completions`

## Runtime

- Host PID 5876 :5088 /health 200
- FE PID 40228 :3000 /fa 200
- GET capabilities checkout `01a185a2-2e5a-7000-a3b4-eee8c3710158` unauth → 401
- Classification 56 requests / 97 routes

## Controlled vs live

- Controlled readiness/directory unit proofs only; zero live Tipax; no mutation of user order.
