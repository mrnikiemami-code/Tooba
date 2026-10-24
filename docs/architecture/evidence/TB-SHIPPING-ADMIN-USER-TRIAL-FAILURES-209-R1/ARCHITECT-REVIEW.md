# TB209-R1 Architect review —2026-10-24

Verdict: ACCEPT_BOUNDED_PRESENTATION_AND_VIEW_REPAIR; REPAIR_NESTED_RATE_ERROR_TARGETING.
Worker commit893f35bfd1b6b5ec1f02f81637fb4526ad1a3e80, base4d4929a297b0f3f717a649e41f46939d93828e06. Mainb79669a124108841c37f402fae2c8b86acb9db0e adds only post-result.js and result-contract.txt; neither executed.
Read diff, RESULT, current screen/groups/rates/picker/error mapper. Independently retrieved and VIEWED all four committed PNGs (General edit, Regions edit with conditions open, desktop view, mobile view). Actual pixel review, not merely source/typecheck.

## Accepted bounded improvement

View Regions now renders text title/rate summary and no add/remove/row inputs; source branches RateRowEditor/RateRowSummary and screen callback guards prevent view/busy mutations.
General no longer duplicates stored type card; owner concise, widths constrained. Cost editing has four primary fields and optional conditions disclosure retaining active-condition summary. Desktop view is substantially simpler than prior ten-input row; mobile-view content is readable in its390px rendered area. General view still uses disabled inputs (disclosed), not a new failure.
Exact-code reason mapping replaces substring guesses; description now targets description; nested deliveryDays recognized as rates; timezone required/invalid distinct; diagnostics collapsed. Accepted concept, with targeting defect below.
Worker reports local fee edit→Cancel/discard restoration and unchanged persisted rev2/values, no domain write. Typecheck+10/10+6/6 worker reports; source tests/DOM interactions are bounded evidence, not mounted unit-test or physical input proof.
User Visual ACCEPT remains absent. This review accepts concrete improvements for eventual user retest, not “professional design finally approved by owner”.

## Remaining actual source defect — narrow repair

ServerFieldIssue carries rowIndex but NO groupIndex. parseProperty discards regionGroups[i] index.
Screen constructs Map<number,string> by rowIndex alone; every group receives that same map. revealServerIssue uses document-global querySelectorAll(testId)[rowIndex].
Consequences: regionGroups[1].rows[0].freightAmount highlights/focuses group0 row0; equal rowIndex/reason across two groups is de-duplicated. A rejected optional condition's input may be inside closed details and cannot be focused without opening its enclosing disclosure.
These contradict the task's exact rejected-row requirement and the report's “exact row” claim. Fix group+row ownership/focus/dedup, opening only the targeted field's containing disclosure; no new layout rewrite.

## Other limits / process

Picker STILL renders all four OptionSelect controls regardless of terminal choice (source and screenshot). Requested relevant-level-only rendering was not implemented; treat that as a remaining presentation limitation, not false completion or a reason for another broad UI loop.
The task did not redo valid save; bounded real persistence remains TB209 evidence. No current claim on server error rendering from a newly rejected mountedPUT.
37min>30 hardbox is process NONCOMPLIANCE, not PASS; no rerun to correct history.
PNG canvas sizes are not equivalent to viewport: screenshots contain unused right/bottom canvas outside emulated viewport. Review confined to rendered content. Mobile edit/EN comparison not executed here. Global sidebar scrollbar seen is not scoped evidence of feature overflow.
No backend/schema/seed/draft write reported. Current Host5088/FE3000 retained. User local-dev authorization persists; TB185/default-OFF unchanged.

## Continuation

Only209-R2,10min/stop6, exact nested-rate error targeting and focused regression; no database write, no save/visual replay, no broad suite.
After its review if corrected, hand off the same live URL to the user with honest limits. Do not create another visual/proof task automatically or reopen190–209 accepted behavior.
