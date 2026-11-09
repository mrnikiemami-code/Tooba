# Architect review — TB-256 — 2026-11-09
Reviewed code/evidence: c58e87e5eea9af1a40d5e409cc5bfc4b6b12c84d.
Decision: PARTIAL overall closure; authoring UI is available for USER TRIAL. No new task issued.

## Source-confirmed
Independent carrier/customer calendar, one-time copy, MAX use, half-open acceptance cutoff, accepted planned-dispatch value/read seam exist. Existing accepted orders remain without guessed plans.
Worker reports canonical deploy and migrations; Architect source review is not direct live browser verification.

## Open review findings — do not call complete
1. Missing carrier schedule is accepted as success/null in StorefrontPlannedDispatchResolver; BuildReceiving uses legacy MAX-only availability. New purchases can therefore still lack a cutoff/dispatch plan. Historical read compatibility must be distinguished from permitting new incomplete commitments. This differs from stage-1 fail-closed requirement.
2. BuildReceivingCalendar passes nowUtc as readiness; real preparation/readiness evidence is not integrated. Do not claim actual goods readiness merely because current time is known.
3. ResolveLatestHandoffForReceivingDate requires forward arrival EXACTLY equal to the chosen date. Need controlled review of a carrier-closed/customer-open date that is reachable by earlier arrival; do not claim this behavior correct without a product/feasibility check.
4. Planned dispatch is not visible in operational order UI, and no canonical accepted plan exists yet. End-to-end acceptance not proved.
5. Zero-day different-timezone comparison and MAX/search bounds need targeted review before unqualified scheduling acceptance; no repeated full-suite loop.
These are source review concerns, not authorization to start another worker task while user is testing.

## User priority overrides
User explicitly requires a visible test opportunity after each repaired defect. STOP after presenting deployed review link and short steps. Do not issue 257, 256-R1 or any next task before user's feedback/authorization to proceed.
User explicitly prohibited ALL extra databases, including disposable test DBs. Worker's reported 15433/order_tb231 relational run does not comply with that latest instruction. Do not repeat or require it. Cleanup status is not established here; do not claim cleaned. Canonical remains tooba_alpha; no destructive cleanup without identifying exact ownership.
Further proofs must respect same DB/ports and preserve user data, with bounded permitted fixtures only if authorized. No automatic creation of test orders.

## Trial available now
Existing draft: /admin/shipping-services/authoring/01a17d0d-e033-7000-af43-49a1daa28822?mode=edit
User carrier calendar is unconfigured; copy/independent edit can be reviewed locally, Cancel avoids changing the user's service.
Task-owned persisted draft: 01a1beed-d7df-7000-bfc8-0185ea61afe3.
Test tab/copy/independence first. Do not tell user cutoff is active for their published service until configured, saved and published through existing workflow.
