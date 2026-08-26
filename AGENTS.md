# Tooba — Agent Rules

## Core rules

1. One active Task only.
2. Execute only the Task actually received from Bridge on the configured channel
   after `BRIDGE-WAKE`.
3. `Worker PASS != Architect ACCEPT`.
4. Bridge is the transport/orchestration boundary; the repository is durable
   technical Source of Truth.
5. The Coding Agent is normally **IDLE / OFFLINE** between Tasks.
6. Do **not** poll Bridge continuously while idle.
7. Do **not** require permanent online Worker presence or idle heartbeat.
8. `BRIDGE-WAKE` is infrastructure control traffic, not a Task or Result.
9. `SYSTEM-BRIDGE-ALERT` is not a Result and must not be treated as one.
10. Do not emit or interpret an alert merely because the Worker is offline
    between Tasks.
11. Do not invent requirements, broaden scope, or redesign locked architecture.
12. Do not self-authorize the next Task.
13. Do not force-push, rewrite history, silently stash, or destroy unrelated
    work.
14. Do not modify unrelated files.
15. Do not mark Architect ACCEPT unless the Task explicitly authorizes it.
16. Do not replay historical task files from `docs/ai/tasks/` as current work.
17. Home and PDP are critical Shopeiva-locked surfaces. Any Task touching shared
    storefront components must run `npm run test:critical-storefront` and follow
    `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md`. Functional PASS
    does not imply Visual ACCEPT.

## Current pipeline

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
```

```text
ARCHITECT
→ downloadable .task.md
→ Tampermonkey
→ Bridge Task = Pending
→ External Watchdog
→ BRIDGE-WAKE
→ Coding Agent wakes
→ claim exactly one Task
→ implement
→ Result
→ Bridge
→ Tampermonkey
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next .task.md
```

## Worker lifecycle on BRIDGE-WAKE

1. recover `main` and verify `HEAD == origin/main` on a known/safe tree;
2. read governance and recovery docs;
3. check Bridge health;
4. claim exactly one Pending Task on the configured channel;
5. persist the received downloadable `.task.md` artifact;
6. execute only that Task;
7. validate, commit, push, and post the complete Result through Bridge;
8. complete the Bridge task lifecycle;
9. return to **IDLE** and stop.

## Retired transport

The following are historical only:

- manual Architect envelopes pasted into a Coding Agent;
- same-session or same-chat continuation;
- Cursor browser/composer/send-button transport;
- requiring one specific agent product;
- **BRIDGE-V2 continuous polling while idle**;
- **permanent idle heartbeat as a normal prerequisite**;
- **Worker waiting loop between Tasks**.

Historical task/result artifacts may retain legacy syntax as evidence only.

## Source of Truth

Before normal execution:

```text
branch = main
HEAD == origin/main
known/safe working tree
```

After successful execution:

```text
local commit
push origin main
git fetch origin
HEAD == origin/main
clean working tree
```

Unsafe divergence is `RECOVERY_CONFLICT`.

## References

- `docs/ai/TOOBA-PIPELINE-PROTOCOL.md`
- `docs/ai/TOOBA-PIPELINE-CONTROLLER.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md`
- `docs/PROJECT-STATE.md`
- `docs/ROADMAP.md`
