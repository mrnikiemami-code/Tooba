# TB209-R2 Architect review —2026-10-25

Verdict: ACCEPT_BOUNDED_NESTED_RATE_ERROR_REPAIR; READY_FOR_USER_RETEST.
Worker sourcecebc455147e779245c7317a2106afce041e885e5, base2de29283a9d32739607d7d8be26666a19bb82a2a.
Main0011d86f378aadc2017111a8133e4fe4da3c13ff adds only post-result.js/result-contract.txt; preserved, neither executed.
Independently read full diff, mapper, RESULT and focused test file. Changes are four existing UI/error-mapping files, focused tests and docs. No backend/schema/catalog/runtime change or domain write.

## Accepted exact correction

The real rate path now preserves regionGroups[i] and rows[j], composite error keys and dedup include group+row+input.
Rendered group and row carry matching data indices. Summary focus uses explicit group->row->known input selector; document-global Nth shortcut removed.
Rate errors are looked up by composite position+row, so group1.row0 no longer paints group0.row0.
The reveal callback opens only ancestor details before focus, without changing field values/intent/revision.
Worker focused26/26 and typecheck clean are reported; tests combine pure mapper assertions and source-wiring assertions. Architect did not rerun them locally or exercise this latest focus change in user's browser. No claim of newly mounted group2 error proof.

## Limits retained, not new work

- Indices identify CURRENT ordered UI tree; they are not durable row ids after a user reorders/removes children following a rejected request. Do not claim stable identity through arbitrary subsequent topology changes.
- Group-level region error remains summary-only. Its selector uses a group-descendant groups-panel id that may not match (the panel is outside the group), so group-level focus is not proven. This is outside the accepted rate-input scope; do not claim all group-container focus correct from testR2-07, which only excludes rate-row selector.
- Missing/unknown rate-field container fallback focus is not guaranteed; exact known rate inputs are the repaired scope.
- Picker still displays four level selectors; General view uses disabled inputs. These were disclosed presentation limits, not blockers warranting another automatic proof/visual task.
- 12min vs10 hardbox and implementation~8min vs6 stop point are process deviations. Record them as such; no timing PASS, no test rerun to repair prose.
- No save/browser/data operation inR2. Accepted real save/readback remains209; pixel review/view/discard improvements remain209-R1.
- User Visual ACCEPT and production acceptance remain PENDING. No quote/activation/full-method-creation claim.

## Handoff

No active task; no awaiting worker result; Automatic-Next-Task NONE.
Call user now for retest of the same real page:
http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Latest observed datarev2: typeعادی, nameپیشتاز, Iran area, IRR freight120000/packaging5000,
weights1000–2000grams, delivery1–3, Asia/Tehran Monday09–17.
Use fresh actual state if user has edited since. Keep Host5088/FE3000 and retained output; no restart/migration/reset for routine handoff.
Await user feedback; only a real reported blocker merits a narrow follow-up. Do not emit another implementation or proof task automatically.
Explicit local-development execution authorization persists; no repeat approval for the same actions. Accepted190–209 capabilities remain bounded and closed; TB185 paused/default-OFF/PendingAllocation intact.
