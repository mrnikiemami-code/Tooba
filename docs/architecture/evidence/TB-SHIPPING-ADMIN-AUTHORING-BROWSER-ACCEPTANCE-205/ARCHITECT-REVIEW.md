# TB205 Architect review — 2026-10-24

Disposition: PARTIAL_RUNTIME_EVIDENCE_RECORDED. 205 acceptance remains OPEN. No production/browser interaction/visual acceptance.
Reviewed RESULT, committed fixtures and CDP harness at926c3a6c397d0d0bd83d9bd494c6ef0ecb08a7d4 and compare againstb7f7ead6df5129a9b0a62ee201592c4a2888f1ae. Mainfb4f63e33957a9e29dbf3fc9cd2f15a8ed11ae03 differs only by the post-result.js artifact update; no production change. Bridge script not executed.

## What is reusable

Worker reports actual route mount on isolated frontend3517 + installed Chrome/CDP9555, browser-level fail-closed /v1 interception,2 editor GETs,4 countries GETs,0 PUTs, one FA desktop screenshot and task-runtime cleanup. This is mounted-route readiness as reported; Architect did not rerun local Windows runtime or inspect the screenshot pixels through a browser. No A-E interaction, FA/EN/mobile comparison or accepted-reference visual review was completed. Keep all these gaps explicit.

## Harness defects to fix before continuing

1. Architect executed the committed fixtures module: the null-admin city's parentId is STRING 'null', not JSON null. childOption always quotes its parent argument. This is rejected by the real strict decoder and would block the intended null-admin scenario.
2. First group simultaneously selects Country + its AdministrativeArea + City + ServiceArea. This is a parent/descendant overlap forbidden by canonical region preflight. Use lawful disjoint terminal selections; do not weaken production overlap validation to fit a mock.
3. Country-wide/filtered city mocks are currently identical regardless of requested filter; filtered reads include the null-admin row, contradicting the strict adapter.
4. Successful PUT fixture only replaces revision text in the ORIGINAL document. It does not reflect submitted changes or assign new group/row ids. Thus it cannot prove actual save/adoption, and would make the UI appear to discard edits on successful Apply. Use one explicit scenario-specific expected request + matching prepared response; no generic mock business engine.
5. authoring-types is a FEATURE read consumed by AuthoringGeneralTab, not merely shell traffic. Its failure renders messages.entryFailed ('بررسی پیش‌نویس ناموفق بود.'). The banner source is therefore feature type-choice failure by source inspection; the shell dev-context/capabilities may have independent effects. Correct205 wording; do not classify as a production defect.
6. Whitelist the required synthetic type-choice/dev-context/capabilities reads with envelopes grounded in actual readers. Continue failing all other /v1 calls locally. Current CDP send returns __error values: the interaction runner must stop/classify failures instead of silently treating a failed CDP command as success.

## Next

Only TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R1, continuation using the committed harness and known isolated startup. No production repair, new framework, broad tests or setup investigation loop. Bound fixture preparation/readiness, then real interactions and four locale/viewport screenshots. Record exact completed cases and first blocker; no whole-delivery completion from a partial mock run. Production functional acceptance remains PENDING. Backend199/adapter200/readbridge201/UI202-204 stay closed.
