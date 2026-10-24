# TB204-R2 Architect review — 2026-10-24

Disposition: ACCEPT for the bounded existing-draft group/destination editor through204-R2. No mounted/browser/visual/live/production release acceptance.

Independently read the actual coordinator, choice eligibility, picker handler wiring, strict adapter and regression source at ec957367fadd94ceb14045e1b013d2ec308bade2; compared the worker base65d1842c5ab889a10815b7d03c29cf1713fb4aba. Current main eb93df3be7000fe1e5bff3a51c767a1ac335b753 adds only the post-result.js evidence artifact after that code SHA, no production change. No Bridge post script executed.

- Generation-bound parent evidence replaces the reusable map; starting fresh areas invalidates earlier evidence and settled state. Independent Architect Node24 execution of the unchanged coordinator: A->B->A yields only1 city read before freshAareas completes,2 after. Old parent cache no longer satisfies the new generation.
- destinationAddEligibility looks up CURRENT country/city/actual parent area/service rows and settled flags, checks activity/coherence and ignores unrelated deeper failures for upper-level terminals. Independent unchanged-helper execution rejects an active country-wide city under an inactive current parent, accepts the active-parent control.
- Picker derives canAdd from that helper and synchronously reads coordinator.current.getState in Add; legacy staged checks remain additional guards. Source wiring reviewed, not mounted.
- Adapter checks explicit admin filter before null-parent acceptance; countrywide requires known parent evidence. Empty known set remains distinct from missing evidence. Existing null-admin city/service controls retained.

Worker reports8 new,29 combined focused facts,tsc and7 guards. Architect did not rerun those suites. Nested/root/broad suites intentionally not re-run; four historical source-size failures untouched. The beginStage settled-state correction is a production correction, not merely a test harness adjustment; it was authorized within the repair and is recorded as such. Elapsed/compliance figures are worker-reported, not independently timed.

204 closes for group collection/titles/real Geography picker under the existing draft. Backend199/201,adapter200,General202,rates/week203 remain closed. Next only TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205: bounded mounted/browser checks against the real route/components using synthetic intercepted API responses, plus read-only runtime availability. No real merchant PUT, no backend/schema/shared-service changes. If runtime cannot be prepared within the setup bound, stop with exact readiness blocker; no new feature or proof loop.
