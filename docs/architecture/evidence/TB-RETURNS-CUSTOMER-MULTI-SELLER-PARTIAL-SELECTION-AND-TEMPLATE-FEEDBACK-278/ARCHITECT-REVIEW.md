# Architect source review — TB-278 — 2026-11-17
Status: PARTIAL / NOT ACCEPTED as a complete customer workflow. No separate278-R1 issued; carry bounded blocking gaps into final consolidated279, per user's two-delivery instruction.
Source: d7eadc17c956d472786b5b8df23961a553b49faa + docs head57cccbbe7cffea23c30ee0f474291cb3dda7bc58, remote main verified.

Retain improvements: authenticated seller-order/customer eligibility read, per-item selection/owning quantum at creation, stable child bodies/keys and partial-success model, partially delivered entry, actual react-toastify on Returns admin/customer. No rollback/reimplementation or actual case mutation.

Blocking source gaps:
1 ReturnRequestModal.submit() always storeReturnSubmission(newAct); canSubmit ignores retained/unreadable pending evidence. A prior unknown child can be overwritten by selecting another line/quantity and submitting. useEffect maps unreadable to retained=null, and window.sessionStorage getter is uncaught.
2 No presentation epoch exists. dispatchChildren and its loop/callbacks/toast/setState/finally are unfenced. Opening/retarget resets inFlight=false while the older request is still pending. Closing/unmounting can continue remaining POSTs and an old response can update another surface/release its lock.
3 Submission storage is scoped only by checkoutId, not authenticated viewer. Parser accepts an arbitrary body string without binding it to wrapper/child identities; compare-clear/outcome compare submissionKey only, not exact immutable bodies.
4 UI stores one reason/quantity per orderLineId; same-line multi-reason portions are not selectable despite the report describing them as supported. New overlay is custom div role=dialog, not shared DS Dialog.
These are actual connected-flow gaps, not cosmetic follow-up. 279 must close them along with its planned remaining domain work, not spawn per-helper micro-tasks.

Boundaries/limits retained:
Unknown precision read blocks selection; write permits only integral unknown-precision quantities. This declared conservative mismatch is not evidence of unsafe fractional acceptance, nor identical read/action capability. No live customer session or active checkout was exercised. Controlled148/11 test counts, served chunk/401 and broad AMSC runs are worker evidence, not live end-to-end Marketplace acceptance.
Both TASK copies are declared reconstructions because Bridge content was empty; do not call them byte-identical originals. 279's full issued envelope will be committed BEFORE execution so reconstruction is unnecessary.
No actual PSP/Wallet/Inventory effect, schema or user-case write in278. Both real cases remain settled/read-only. Preserve prior accepted277 repairs and all historical audits.
