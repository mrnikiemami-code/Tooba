=== TB-SHIPPING-FULL-OPERATIONS-071 :: focused verification summary ===

TASK      : TB-SHIPPING-FULL-OPERATIONS-071 (BOUNDED_SCHEDULING_VERTICAL_SLICE)
BASE      : 1c64b45731c59b16e36f6f4ed418219cef120800
VERDICT   : PASS (worker)

--------------------------------------------------------------------
1. FOCUSED TEST SUITE — ACTUALLY RUN
--------------------------------------------------------------------
command : dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj
result  : Passed!  Failed: 0  Passed: 154  Skipped: 0  Total: 154

34 new scheduling tests:
  Scheduling/ShippingOperatingScheduleDomainTests        13 tests
  Scheduling/ShippingOperatingScheduleAdminTests         10 tests
  Admin/ShippingOperatingScheduleEndpointOwnershipTests   3 tests
  Compatibility/OperatingScheduleCompatibilityTests       3 tests
  (plus updated Persistence/ShippingPersistenceModelTests, 3 new/updated assertions)

Coverage of the required boundary cases:
  * Sunday 13:00-14:00 plus a second distinct window            accepted, persisted, read back
  * adjacent windows 13:00-14:00 / 14:00-15:00                  NOT an overlap (half-open)
  * overlapping windows on one weekday                          refused, shipping.schedule.window.overlap
  * reversed / zero-length / overnight window                   refused, shipping.schedule.window.range_invalid
  * weekday outside ISO 1..7                                    refused, shipping.schedule.weekday.invalid
  * non-positive / oversized capacity                           refused, shipping.schedule.window.capacity.invalid
  * missing or unresolvable timezone                            refused, ...timezone.required / ...timezone.invalid
  * unknown option for the service                              refused, shipping.service_option.unknown
  * cross-service toggle of a schedule row                      refused, shipping.service_scope.mismatch
  * repeat of the same body                                     idempotent, same schedule id, same window set
  * empty schedule set                                          returned as empty, never a fabricated default
  * no non-Shipping source file references the schedule surface  asserted over the whole backend tree

--------------------------------------------------------------------
2. LIVE DEVELOPMENT ROUNDTRIP
--------------------------------------------------------------------
host     : existing Development process, 127.0.0.1:5088 (one controlled restart to load the new endpoints)
target   : GET /__platform-commerce => edition SingleStore, deploymentId dev-singlestore,
           tenantId store-alpha, connectionReference tenant-alpha, resolvedHost 127.0.0.1
database : tooba_alpha on localhost:5432 (container postgres-db, postgres:17)
evidence : log-live-roundtrip.txt, log-authorization-boundary.txt

Real Sunday-only window 13:00-14:00 plus a second distinct window 16:30-18:00 (capacity 25) was
created over HTTP and verified by a DIRECT database read, not only by the API echo.

Refusals observed live (all 400, stable codes, no partial write):
  shipping.validation.schedule_window_overlap
  shipping.validation.schedule_window_range_invalid   (reversed AND overnight)
  shipping.validation.schedule_weekday_out_of_range
  shipping.service_option.unknown
  shipping.schedule.timezone.invalid
  shipping.schedule.not_found
  shipping.service_scope.mismatch

Authorization boundary observed live:
  no actor header                       => 401 admin.actor.missing
  authenticated, unprivileged actor id  => 403 admin.authorization.denied
  canonical Development admin actor     => 200

--------------------------------------------------------------------
3. MIGRATION
--------------------------------------------------------------------
name    : 20261015045130_OperatingSchedule  (Shipping module only)
tables  : shipping.operating_schedules, shipping.operating_windows
applied : only to the verified Development tooba_alpha
rollback: drop the two new tables + delete the one history row, or
          `dotnet ef database update OptionLevelRatePolicy`
preserved: shipping.rate_policies 9 rows, shipping.coverage_grants 7 rows (unchanged)

--------------------------------------------------------------------
4. CAPACITY ENFORCEMENT STATUS — DISCLOSED
--------------------------------------------------------------------
CONFIGURATION ONLY. capacity and configuredCapacityTotal are stored, validated and returned, but no
atomic reservation, counting or locking path exists in this slice, so the configured value caps
nothing at runtime. It is labelled as configuration-only in the contract XML docs, the admin DTO
and the tests.

--------------------------------------------------------------------
5. COMPATIBILITY
--------------------------------------------------------------------
* No file outside src/backend/Modules/Shipping/** references the schedule types, tables or routes.
* No consumer reads the schedule, so an absent or inactive schedule cannot shut down legacy Checkout.
* The existing read-only IShippingRateQuoteLookup seam is unchanged; no writable Contracts seam was
  added for scheduling (it stays admin-only).
* Program.cs and ToobaModuleComposition were NOT modified; the new directory is module-owned DI and
  the routes ride the existing MapShippingEndpoints() composition.
* No schema migrator was registered, so Host startup emits no shipping DDL.
