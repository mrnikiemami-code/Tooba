# Architect review — TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264
Date:2026-11-12
Decision: PARTIAL — only264-R1 corrective task authorized;265 NOT ISSUED.
Reviewed revision bba723b1d82d3bd5e4916adc9b69ce2c0e040460; codeab77c12e0e4cee6a9a5b034c506a84ff70aaf1f4.
Source/evidence inspection only; Windows runtime/tests not independently rerun.

Retain owned minute-pair/revision plumbing, durable deadlines and existing queue/view. Concrete original-scope blockers:
A ResolvePreparationDeadlines requires preparationStart<transferDeparture while authoring and forward path explicitly accept preparation0. Same strict constraint in applied migration makes valid zero-policy acceptance fail. Need nonnegative ordering and full acceptance/storage proof.
B Resolver and backward authority accept receiving DATE only; selected receiving window/zone absent. Zero-transit tomorrow10–11 can receive carrier17:00 deadline. Need selected-window-aware same forward/backward authority through actual acceptance/read bridge, old evidence stable.
C Current now.AddMinutes(prep+transfer) treats every seller as24/7. Original task prohibited closed-processing-hour arithmetic unless explicitly allowed. No warehouse calendar exists; minimal explicit processing semantics/configuration needed, not silently customer/carrier calendar. Missing policy currently falls back to instant-ready v1 for NEW commitments, preserving core defect; legacy read compatibility must be distinct from new commitment readiness.
D Supplied receipts cover model tests/resolver+factory+DB, not issued actual authoring save/reload/publication and storefront selection chain. No concrete task-method trial URL supplied. Complete one connected task-owned handoff without real payment or user-data mutation.
Original code tests do not cover A backwardzero or B selectedwindow; additional narrow proof justified by these defects. No broad/module/base parity/green-loop requested.
Worker PASS is not functional acceptance. 263 stays accepted within scope;265 remains planned. Only bounded264-R1, one result then review.
