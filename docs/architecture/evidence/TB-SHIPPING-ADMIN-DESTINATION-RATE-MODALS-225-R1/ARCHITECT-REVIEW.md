# Architect review —225-R1 requires one bounded continuation
Main1d06252188a3278127401e804f53b263725c47e1, source2107a63db. Independently inspected actual money
model/field, rate modal and rateConfirmIssue. Current-value stored-fraction comparison and editable-handler
guard correctly implemented; grouped-offset mapping exists. Root did NOT execute local browser/tests.
Remaining source-confirmed defect: trusted = readMoneyText(previousRaw).valid re-infers provenance from
shape. Invalid paste12,34 -> append5 returns raw12,345 valid:false; append6 sees previous12,345 as well-formed
grouping, strips current comma and accepts123456. validity/provenance not persisted by MoneyField.
R1 tests cover one subsequent keystroke only, not repeated transition. Fix persistent field provenance and
execute fresh installed Chrome keystrokes; current Electron attempt did not deliver actual keys.
Serious process incident: worker reloaded existing user edit tab and discarded unsaved LOCAL edits.
DB unchanged does NOT mean user work preserved or recoverable. No claim recovery; stop using user webview.
Use ONLY newly created isolated task Chrome target, never matching first tab by URL.
One observed empty MessageBox reason not reproduced on deliberate Confirm; retain as evidence limit and
assert non-empty reason in required isolated trial, don't invent repaired claim.
No final user trial acceptance. Issue225-R2. No UI redesign/188suite loop/Storefront/seller work.
