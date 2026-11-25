# ISO-RULES — Task layer loader
For a concrete OpenCode task `<Task-ID>` ALSO read, in order:
0. `docs/opencode/DEV-HOST-TEST-POLICY.md` and any task-local owner authorization addenda (higher precedence than older environment clauses).
1. `docs/opencode/tasks/<Task-ID>/TASK.md`
2. `docs/opencode/tasks/<Task-ID>/REQUIREMENTS.json` and `ACCEPTANCE-MATRIX.json` (when issued)
3. task-local OpenSpec change under `openspec/changes/**` referenced by the task
4. any paths named in TASK as in-scope source
5. Matching task-type pack only:
   - backend → `06-backend-layer.md`
   - frontend → `07-frontend-layer.md`
   - AMSC/architecture relocate/certify (explicit) → `08-amsc-layer.md`

Do not load historical AGENTS story dump. Full historical AGENTS remains on branch `main` only.
Do not load unrelated past task evidence unless the issued TASK names it.
This worktree remains optional until architect activation for general product execution.
