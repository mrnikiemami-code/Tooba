# Tooba — TB-P01-T006-REPAIR — Restore Original Envelope & Validate

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P01-T006-REPAIR
Phase: P01 — Platform Foundation
Type: Repair / Envelope Restoration
Repository: https://github.com/mrnikiemami-code/Tooba
Primary-Branch: main
Implementation-Agent: Cursor
Architect: ChatGPT
Execution-Mode: PIPELINE
Depends-On: TB-P01-T006
Predecessor-SHA: 4fcc4a075da5c528604129713d618f56b7833571

## Objective

Restore the complete original Architect envelope for TB-P01-T006.

Do NOT replace `docs/ai/tasks/TB-P01-T006.task.md` with this repair text.

Do NOT change T006 architecture or implementation code unless validation fails.

Do NOT mark T006 accepted.

Do NOT invent TB-P01-T007.

## Required actions

1. Write the complete original T006 task (not this repair) to:

```text
docs/ai/tasks/TB-P01-T006.task.md
```

Title:

```text
# Tooba — TB-P01-T006 — Outbox, Domain Events & Background Work Foundation
```

Markers:

```text
BEGIN_TOOBA_CURSOR_TASK_V1
END_TOOBA_CURSOR_TASK_V1
```

Include original sections 1–41 (Domain Event Abstraction through UI/UX Protection), including `## 40. Pipeline Continuity`.

Predecessor SHA of the original task:

```text
e619d8b2c5cbb29ea62daeec9b9e62372cf291cb
```

2. Keep this repair envelope at:

```text
docs/ai/tasks/TB-P01-T006-REPAIR.task.md
```

3. Minimally update PROJECT-STATE and RECOVERY-CONTEXT:

```text
TB-P01-T006 = REPAIR IN PROGRESS / AWAITING ARCHITECT ACCEPT
```

Last accepted remains TB-P01-T005.

4. Run from repository root:

```bash
dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx
```

Frontend:

```bash
cd src/frontend && npm ci && npm run typecheck && npm run lint && npm run build
```

Then from root:

```bash
git diff --check
```

5. Commit:

```text
docs repair T006 envelope and validation [TB-P01-T006]
```

Push `origin main`. Verify `HEAD == origin/main`.

No force push.

END_TOOBA_CURSOR_TASK_V1
