# TB202-R1 Architect review — 2026-10-23

Disposition: ACCEPT for the bounded existing-draft General UI repairs. TB202 is closed through R1 for this scope; no visual, live merchant, production release or module certification acceptance.

Source independently read at86c0a10932f48760978681dec4d606ddbf24b33e: form, session reducer, controller, actual hook, screen/exit wiring, keyed page and new tests; compared the exact base a66573615d902df3ceeefc43adf571a2db59179f file set. Main independently confirmed at the worker SHA. No backend/schema/legacy-dialog change.

## Findings

- Unchanged configured settings now omit their wrapper; one changed axis retains the other and a revert becomes clean. Description-only unnamed language reports nameRequired. Raw invalid changes feed localDirty inside the existing session and block empty Apply.
- Architect executed the unchanged new form module with Node24 type stripping: configured untouched/change/revert, blank stored name, incomplete settings and unnamed-language description assertions all pass. These are synthetic pure-form checks, not React/browser execution.
- formSeed now changes on accepted load/save rather than every clean render; reconcile intentionally preserves local form and pending edits. Screen requires actual intent plus no local issues before Apply.
- Cancel Edit/Back share exit planning, confirmation destination and busy gating; rejecting discard keeps local state. The explicit unknown-commit discard path performs the existing fresh-read recovery; it does not assume the write failed.
- Route keys the screen by draft identity. Hook replaces/retire controllers and StrictMode effect re-setup creates a live controller with a new subscription render. Same draft/locale does not replace the slot. Disposed Apply returns ignored and does not set savedRevision/formSeed. This is source inspection, not a mounted proof.
- Explicit same-id select/null clear remains intentional wire input, even where raw values equal baseline; that is preserved202 behavior, not a phantom settings write.

## Evidence limits and process

Worker reports48 feature facts, typecheck and5 guards green; Architect did not rerun that suite. The10 new facts cover pure form/controller/exit planning, not actual mounted hook lifecycle or all Dialog click paths. The hook comment/source supports the intended lifecycle; mounted React/visual/live behavior remains unverified. Accept this bounded repair with the explicit limitation; do not issue another evidence-only loop for it.
Worker reports~12 minutes and two corrections; timestamps are reported, not independently measured. No process deviation is silently reclassified.

## Continuation

Only TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203: edit rate rows under EXISTING groups and the one method-level week through the same editor document/ONE Apply. Keep geography and group topology read-only in this slice. New group/destination selection comes after203 review, using existing Geography authority; then bounded visual/live review. No automatic create/activation, cutoff/carrier/free funding or quote claim.
