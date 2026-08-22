# Tooba Pipeline Setup

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
git commit -m "docs: bootstrap Tooba architect-cursor pipeline"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
```

Required:

```text
HEAD == origin/main
```

4. New ChatGPT chat:
paste the full content of:

```text
docs/prompts/TOOBA-ARCHITECT-NEW-CHAT.md
```

5. Cursor:
give it the full content of:

```text
docs/prompts/TOOBA-CURSOR-PIPELINE-START.md
```

6. Do not start product implementation until you explain the Tooba product/template in the new Architect chat and P00 discovery is complete.
