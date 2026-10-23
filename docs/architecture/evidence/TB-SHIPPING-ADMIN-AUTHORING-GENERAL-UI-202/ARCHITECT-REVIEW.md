# TB202 Architect review — 2026-10-23

Disposition: REPAIR_REQUIRED. Reviewed source at 41dc0a7fcf94e56209c8a87b84aab18a66f907eb against base 439c43017ab38bdd6fb6999e26976796365e8140, the committed RESULT, form/controller/hook/screen and route. Main independently confirmed at that SHA. No backend changes in the compared file set.

Preserve the delivered existing-draft route, VIEW-only legacy entry, real General fields, strict destinationFacts, nullable-admin compatibility, ONE PUT transport and read-only regions/week. Worker reports 38 feature tests, typecheck and five guards green; Architect did not rerun that suite. Visual NOTRUN remains valid and is not Visual ACCEPT.

## Concrete defects

1. Form/session dirty state is derived from serializable edits, losing invalid local work. Architect executed the unchanged editor-form.ts with Node24 type stripping and synthetic minimal documents:
- configured untouched settings -> edits.methodSettings emitted, issues=[]; the screen hasIntent therefore enables Apply although session.unsaved=false and controller refuses it.
- existing name emptied with settings unconfigured -> edits={}, nameRequired issue; controller.edit({}) marks the session clean. A transition from a valid dirty name to blank also triggers the screen's clean-form effect and can reset typed input.
- one setting selected on unconfigured baseline -> edits={}, settingsIncomplete; same false-clean issue.
- description typed for an untranslated active language with blank name -> edits={}, issues=[]; the input is silently ignored.
These are local pure-function executions, not browser tests. Separate raw form changes from valid wire patches without creating another state store; invalid work must remain dirty, block Apply and require discard confirmation. Compare configured settings to baseline, omit unchanged wrapper and detect revert.

2. Cancel Edit directly resets form/controller.edit({}) and switches view without the existing discard Dialog. It is also enabled during an in-flight save/reconcile. Back uses confirmation but this action bypasses it. Route cancel-edit and Back through explicit discard intent with correct destination; cancelling the Dialog preserves exact form/session. Prevent destructive reset during save.

3. useEditorController captures first draftId in a ref, callbacks/effect have empty dependencies and there is no cleanup despite its comment. Thin route does not key the screen. A reused screen receiving B remains bound to A; disposal is never called. Repair the actual React/controller lifecycle (including StrictMode effect setup/cleanup/setup), not only the already-correct pure session epoch tests. Retire obsolete controller callbacks and local draft form identity. Do not fabricate success after disposal or a rejected stale completion.

## Decision and continuation

Only TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1 is issued, bounded General UI state/lifecycle repair. No203, regions/rates/week editing, backend proof replay, new framework or broad audit. Use focused regressions plus existing feature suite/typecheck once; preserve accepted200 uncertainty/epoch semantics. No new server/browser setup required; report visual limitations. Production functional acceptance remains PENDING. Worker202 scope/time claims do not override these source defects.
