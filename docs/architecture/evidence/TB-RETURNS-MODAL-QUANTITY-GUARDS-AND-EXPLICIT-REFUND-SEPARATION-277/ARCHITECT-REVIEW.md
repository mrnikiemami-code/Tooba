# Architect source review — TB-277 — 2026-11-17

Status: PARTIAL — three bounded repairs remain; retain the delivered modal/explicit-refund work.
Source reviewed: fac017f94d3d6af09939eb495cc34f6231eb5aac, delivery head418a2b264372512c349be8b4be95cffdc32631c8. Remote main verified at delivery head. Architect did not execute worker runtime/test suites; worker receipts remain worker evidence.

Positive source findings:
ApproveAsync now persists the decision/Approved and no longer creates/refires refund attempts or calls money/stock. First refund has a distinct CQRS route and deterministic case-derived identity; Approved/no-attempt has an appended AwaitingFirstRefund description. Existing DS Dialog is used for action reviews. Preserve these improvements and all historical reports; no reimplementation or new lifecycle requested.

Source-confirmed remaining defects:
A. Unit precision is only UI/read enrichment. ReturnDirectory.Receipts.cs is unchanged in277; NormalizeReceiptLines checks positive + numeric(18,6) representability, and the serialized owning write checks cumulative ceiling, but never reads Order quantity precision. A direct receipt POST of0.5 for an owning pcs/0-decimal line can pass those checks. The277 server claim is therefore incomplete. Workspace also defaults missing unit precision to6, which guesses permission rather than proving the unit rule.
B. Workspace collectReceiptItems adds JS Numbers for usable/damaged, and the draft ceiling is Math.max(0, requested-received). This is not exact decimal quantity arithmetic: independent JavaScript evaluation confirms0.1+0.2=0.30000000000000004, so legitimate portions0.1+0.2 against ceiling0.3 are rejected. Clamp/epsilon/rounding is not an acceptable fix.
C. return-review-intent.ts still lists admin.authorization.denied, admin.actor.missing, order.operation.denied and generic conflict/state refusals as PROVEN_NO_EFFECT. A later denial/conflict does not prove an earlier possibly-sent command did not write; clearing the retained intent can still abandon financial uncertainty.277 added first_not_lawful without resolving this issued requirement.

Evidence limits: current canonical case is already Settled and offers no modal. jsdom proof does not establish live modal geometry. Worker disclosed a POST start-refund replay on this live case despite the GET-only requirement; reported zero state/effect change does not erase that boundary deviation. No additional live mutation/probe is authorized. Preserve this report and the successful historical attempt; do not reset/clone/create another case.

Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1 next: owning unit precision, exact scaled-decimal client bounds and durable unknown-intent retention. No broad audit, whole-module rerun, schema change, new case or real money/stock effect. No new functional stage. User/financial/full-Returns acceptance pending.
