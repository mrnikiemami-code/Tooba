# Tasks — offer-production-readiness-296-r10

## 1. Plan (before production changes)
- [ ] 1.1 Read immutable R10 task, requirements and acceptance matrix; inspect actual source and architecture locks.
- [ ] 1.2 Record source-grounded architecture plan, delivery manifest, baseline guard violations, proof plan and scoped policy/authorization gaps.
- [ ] 1.3 Validate this OpenSpec change with strict validation; reconcile every R10 requirement and mandatory test without reducing scope.

## 2. Owning domain contracts
- [ ] 2.1 R10-01: Channel; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.2 R10-02: Create workspace; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.3 R10-03: Inventory provisioning; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.4 R10-04: Shared persistence; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.5 R10-05: Atomic concurrency; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.6 R10-06: History; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.7 R10-07: Unit and conditions; implement and verify the required owning path and positive/negative/boundary cases.
- [ ] 2.8 R10-08: Price and shipping; implement and verify the required owning path and positive/negative/boundary cases.

## 3. Frontend and architecture
- [ ] 3.1 R10-09: Central locale; preserve accepted layout and working behavior.
- [ ] 3.2 R10-10: Frontend architecture; preserve accepted layout and working behavior.
- [ ] 3.3 R10-11: Backend and Host; preserve accepted layout and working behavior.
- [ ] 3.4 R10-12: UX and feedback; preserve accepted layout and working behavior.

## 4. Verification and handoff
- [ ] 4.1 R10-13: comparable performance, responsive and actual regression proof.
- [ ] 4.2 R10-14: full connected matrix, architecture guards, focused tests, evidence checker negative controls and fixture restoration.
- [ ] 4.3 Record all 42 test outcomes, eight AQ gates, source/build/DB identity and exact remaining blockers; at most one consolidated repair and final rerun.
- [ ] 4.4 STOP_FOR_ARCHITECT. Do not archive this change as accepted or start TB-297 before architect decision.
