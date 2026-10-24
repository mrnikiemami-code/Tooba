# TB205-R1 Architect review — 2026-10-24

Disposition: PARTIAL_INTERACTION_EVIDENCE_RECORDED. 205 remains open for successful mounted save/adoption and locale review.
Read committed RESULT/report, fixtures, harness response shaping and actual decoder/wire primitives at e827c5d60aabe52cc243369df6fa4a73e5f2be86. Current main c33103f2fa7be66194389dad2a95b086cd5617fc adds only the post-result.js artifact. No production changes and no Bridge script executed.

Useful reported evidence: mounted FA/EN CONTENT-language edits survive tab changes; one intercepted PUT with expectedRevision7, retained ids and raw0.50/1850000.00; no post-save GET; malformed2xx correctly produces outcomeUnknown, preserves edits and sends no second PUT. These are intercepted-contract observations, not live backend/database acceptance. Five FA screenshots were reported; Architect has not inspected their pixels. EN interface/mobile comparison is incomplete; B/C/E and pending doubleclick were not run. Do not declare205 complete.

The remaining failure is still a harness response-shape defect. PUT groups contain WRITE areas (countryId/admin/city/service tuple), title PATCHES and no server currency/order/new ids. GET/read groups need kind+id areas, full stored titles, currency/order, assigned ids and matching destinationFacts. Splicing PUT regionGroups into a GET document cannot produce a truthful read response. The report's destinationFacts explanation is incomplete: the read/write area shapes themselves also differ. No production decoder weakening is authorized.

## Concrete continuation prepared by Architect

Committed205-R2/scripts/save-scenario.mjs supplies ONE fixed combined scenario (not generic mock backend):
FA name 'ارسال استاندارد R2', EN name 'Standard shipping R2'; edit only Monday END to18:15, retain09:00:00.500 start; append a new Country2 group titled'گروه آزمایشی' with one0.50/0.00 row and delivery1..2; retained groups untouched. Prepared read response has revision8, assigned group/row ids, full currency/order/titles/read areas and matching facts. The expected WRITE request is independently specified and checked before success.
Architect executed preflight using the UNCHANGED production wire parser/writer and decodeEditorDocument: initial and saved responses both decode, revisions7/8; expected request retains raw tokens; negative replacing read areas with PUT write areas is rejected. This proves fixture validity only, not mounted save.
Use the prepared module. Do not rebuild response shaping or spend another box designing a mock engine.

Only205-R2: this exact mounted success/adoption + pending duplicate gate + four FA/EN viewport captures, bounded25min. Other unexecuted controls remain explicitly open unless actually exercised; do not infer full A-E completion. No production fix/new feature/broad proof replay. Production functional acceptance remains PENDING.
