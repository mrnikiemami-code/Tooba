# Runtime Smoke — TB-P09-T019

Host `:5088` `Host: alpha.localhost`. Script `docs/evidence/TB-P09-T019/_runtime.mjs`. Raw `runtime-raw.json` `ok: true`.

| Step | Result |
| --- | --- |
| A pre-delivery | `request_return` hidden; POST → `return.not_delivered` FA; label `7 روز پس از تحویل` |
| B decimal 0.25 kg | queue qty 0.25, Return=`Requested`, Refund=`none`, GUID-free refs |
| F approve | Return=`Completed` distinct from Refund=`completed` |
| J stale re-approve | 400 `return.stale` FA, no Bad Request |
| G retry while completed | 400 `refund.retry.invalid_state` |
| C split 0.50 then 0.75 | first 0.75 → `return.quantity_exceeded`; after second delivery 0.75 accepted |
| D non-returnable snapshot | `isReturnable=false`, no kebab action, POST → `return.non_returnable` |
| E expired | evaluator `return.expired` / «مهلت مرجوعی تمام شده است.» |
| I T016 cancelled paid | no `request_return`; no return rows |
| Filters | all 7 queue tabs 200 (EF-translatable) |
| H settlement | foundation compensation path reused |
