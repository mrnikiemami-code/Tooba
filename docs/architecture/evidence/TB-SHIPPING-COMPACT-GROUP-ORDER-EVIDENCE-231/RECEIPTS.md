TB-231 receipts (2026-10-27, worker tooba-worker-01)

Base: 5a3070567 (fast-forward only; Shipping line-ending drifts never staged)
Elapsed: ~16 min (HARD35). Status: EXECUTED — PARTIAL.

# 1. Disposable relational target (task-owned, exact identity)
container: tb231-order-pg / postgres:17-alpine / 127.0.0.1:15433
verified BEFORE DDL:  current_user=tb231  current_database=order_tb231
                       PostgreSQL 17.11 on x86_64-pc-linux-musl
migration chain: dotnet ef database update --context OrderDbContext
  ... 20261016090000_AddCartShippingGroups
  ... 20261016100000_AddOrderShippingGroups
  ... 20261027150000_AddShippingGroupAcceptedEvidence
  Done.
evidence column counts (information_schema):
  cart_shipping_groups   accepted_* = 19
  order_shipping_groups  accepted_* = 19
check constraints untouched (amount >= 0, currency length = 3, lead_days >= 0)

# 2. Builds
Tooba.Order.Infrastructure  0 errors
Tooba.Order.Tests           0 errors
Tooba.Host                  0 errors (pre-existing warnings only)

# 3. Tests
TOOBA_TB231_ORDER_POSTGRES="Host=127.0.0.1;Port=15433;Database=order_tb231;Username=tb231;Password=local"
filter: ShippingGroupAcceptedEvidence | OrderEvidenceRelationalTarget
  Passed! Failed: 0, Passed: 21, Skipped: 0, Total: 21
existing unchanged: OrderShippingGroupMaterializerTests
  Passed! Failed: 0, Passed: 9, Skipped: 0, Total: 9
not run: whole Order suite, 119/188/194, frontend, guards

# 4. Migration state
20261027150000_AddShippingGroupAcceptedEvidence  WRITTEN
applied ONLY to 127.0.0.1:15433/order_tb231
NOT applied to shared local tooba_alpha / store-alpha or any shared instance
OrderDbContextModelSnapshot.cs: restored byte-identical after the tool re-scaffolded the stale baseline
(dotnet ef migrations add produced a full-schema migration; it was deleted, not committed)

# 5. User review handoff
stopped: PID 33524 (chrome, --user-data-dir=D:\temp\tb229r2-chrome --headless=new --remote-debugging-port=9336)
         only that verified profile's own browser process; no user browser touched
opened:  PID 34788 chrome --user-data-dir=D:\temp\tb229r2-chrome --new-window http://127.0.0.1:3000/fa/shipping
runtime untouched: Host 5088 PID 24660, FE 3000 PID 28288 (pid recorded at task start; reverify from the
actual process if checked later)
no automation, reload or tab enumeration on that window after handoff

# 6. No live writes
no Host/FE restart, no shared migration apply, no checkout/order/payment/reservation,
no destination/address/cart/rate change for the user, no seed, no reset.
