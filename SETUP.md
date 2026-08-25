# Tooba Bridge-V2 Pipeline Setup

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
git commit -m "docs: configure Tooba Bridge-V2 governance"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
```

Required:

```text
HEAD == origin/main
```

4. Configure Bridge and the Coding Agent Worker for:

```text
PIPELINE-PROTOCOL: BRIDGE-V2
CHANNEL: tooba-main
```

5. Start the Worker with `Waiting` heartbeat and poll only:

```text
GET /api/tasks/next?channelId=tooba-main
```

Tasks are downloadable `.task.md` artifacts dispatched by Bridge. The user does
not manually paste Tasks into a Worker. Follow
`docs/ai/TOOBA-PIPELINE-PROTOCOL.md` and
`docs/ai/TOOBA-PIPELINE-CONTROLLER.md`.

6. Do not start product implementation unless Bridge dispatches the Task.
