# ISO-RULES — Task layer loader
For a concrete OpenCode task `<Task-ID>` ALSO read, in order:
1. `docs/opencode/tasks/<Task-ID>/TASK.md`
2. `docs/opencode/tasks/<Task-ID>/ACCEPTANCE-MATRIX.json` (if present)
3. task-local OpenSpec change under `openspec/changes/**` referenced by the task
4. any paths named in TASK as in-scope source

Do not load historical AGENTS story dump. Full historical AGENTS remains on branch `main` only.
This worktree is measurement/activation-candidate only until architect approval.
