# C02 / C04 / C05 PASS — real connected run on existing dev env (TB-289-R6, 2026-11-24)
Candidate 287+288+Promotion+Cart+architect-snapshot Host dll SHA E4152AE996EF3DB33A7B1A9B11D9AD12C82A713D5520B7E5C7EEF175C42961BF
deployed on existing Host 5088 against dev DB tooba_alpha (verified backup SHA256 4839D664...07FB9C).

287 harness adapted to be environment-driven (TB287_HOST/TB287_DB/TB287_SA/SB/ADMIN env; TEARDOWN off; scope
allow-list broadened to the R6 change set: Offer/Promotion/Cart/Host.Tests/.opencode/openspec/task dirs).

- C02 (2nd normal boot idempotence): /health=200; OOS rows=1 distinct=1 status=Archived; role assignments unchanged
  (platform-admin=1, seller-owner=1, mobile-order-op=1).
- C04 (287 T01-T16 on candidate): T01..T14 PASS, T15 DELEGATED, T16 PASS -> all 16 PASS.
- C05 (VERIFY realOk): realOk=true, t15Pass=true, decision READY_FOR_ARCHITECT_REVIEW; mutations
  delete-T05/flip-T02/tamper-hash all REJECTED.
- Restore: pg_restore --clean exit 0 -> offers=3223, variants=1361, products=567; original 5088 restarted /health=200; 3000 /fa=200.

Remaining: C07 admin-panel UI subset has no dedicated spec (seller+mobile 7/7 already PASS). Admin C07 NOT_RUN.
