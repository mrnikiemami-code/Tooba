# Architect review — TB-229 — 2026-10-27

Reviewed main ad599cbcd12d3174a640496b7e6657c25e8d0457, compared against02250f55f06f572b5e476d47ddda87773203bea1.
Verdict: NOT ACCEPTED. New read-path source exists, but it is not live-proven and contains blocking defects.
Architect read service/planner/models/DI/frontend API/cards diff and current composer validation.
Root executed only an isolated JavaScript reproduction of the new NaN comparison, not repo suites/runtime.

Blocking source facts:
1. asNullableNumber uses NaN===NaN, always false. Exact expression executed: "abc" ->0,0 ->0,null ->null.
   New preview decoder also silently drops malformed groups/methods, guessesIRR and normalizes goods to0.
2. Planner passes translated label as candidate.MethodCode. Composer requires per-group unique trimmed
   MethodCode: equal labels across legitimate bindings, or leading/trailing spaces, reject the real plan.
   Binding matching/cache also fails to reuse218/219 case-insensitive service/option identity.
3. New captured component is an additional parallel card tree before legacy groups, not reuse as ONE
   source of the existing UI. isGroupedCart guard hides it entirely on the single-line UI branch.
4. Unevaluated group goods amount initialized0 then rendered as actual monetary facts. Unknown is not0.
5. Service silently passes legacy DeliveryHorizonDays to a parameter counting AVAILABLE DATES, contrary to
   explicit218/228 contract. Planner default switch treats unknown responsibility as Platform Store.
6. Catching composition ContractOperationException returns an empty group list, erasing independent groups
   and potentially hiding real dependency failure. No positive composition was executed to expose this.

Current218 composer DOES allow empty candidate lists, so no new validator relaxation is needed.
Current229 nine tests primarily prove refusal/zero-candidate paths. They do not prove the new planner's
positive call through218/217 or two-group money/calendar independence. Save/Commit source guard is first,
but its no-call behavior was not executed by the reported tests. Offer endpoint proof from229 still absent.
Assignment migration shared-applied per normal runner log reported by worker, not independently executed
by Architect. Do not reapply. No Host refresh/browser/screenshot acceptance or user trial granted.
Elapsed70vs45 remains noncompliant. Existing module/read primitives are not reopened.

229-R1 is a bounded correction + positive composition + real local read trial, not a new architecture.
Compact order evidence and selection/commit remain NOT implemented; preview must stay non-checkout-ready.
