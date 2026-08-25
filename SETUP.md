# Tooba Bridge-Wake-V1 Pipeline Setup

1. Extract this pack into the root of:

```text
https://github.com/mrnikiemami-code/Tooba
```

2. From repository root:

```bash
git fetch origin
git checkout main
git pull --ff-only origin main
git status --short --branch
```

3. Review files and commit:

```bash
git add AGENTS.md README.md SETUP.md docs
git commit -m "docs: configure Tooba Bridge-Wake-V1 governance"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
```

Required:

```text
HEAD == origin/main
```

4. Configure Bridge, External Watchdog, and the Coding Agent Worker for:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
```

5. Between Tasks the Worker remains **IDLE / OFFLINE**. No continuous polling
   or idle heartbeat is required.

6. When Tampermonkey dispatches a downloadable `.task.md` to Bridge and the Task
   becomes Pending, the External Watchdog sends:

```text
BRIDGE-WAKE
```

7. On wake, the Worker claims exactly one Task:

```text
GET /api/tasks/next?channelId=tooba-main
```

Tasks are downloadable `.task.md` artifacts dispatched by Bridge. The user does
not manually paste Tasks into a Worker. Follow
`docs/ai/TOOBA-PIPELINE-PROTOCOL.md` and
`docs/ai/TOOBA-PIPELINE-CONTROLLER.md`.

8. After Result delivery, the Worker returns to **IDLE** and stops. Do not
   resume continuous polling.
