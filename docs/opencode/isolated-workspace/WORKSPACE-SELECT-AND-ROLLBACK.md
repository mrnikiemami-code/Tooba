# Optional isolated OpenCode workspace — select & rollback

Evidence-only. Does **not** replace the main Cursor/OpenCode workspace until architect activation.

## Use isolated worktree (optional)

```powershell
# Open OpenCode / analysis against the iso tree only
cd D:\Users\User\source\repos\SarvNewVer-opencode-iso-002
# short AGENTS.md + docs/opencode/iso-rules/01..05
```

Keep main tree for Cursor, task 286, and canonical `AGENTS.md`:
`D:\Users\User\source\repos\SarvNewVer`

## Return to previous state

```powershell
cd D:\Users\User\source\repos\SarvNewVer
git worktree remove --force D:\Users\User\source\repos\SarvNewVer-opencode-iso-002
git branch -D opencode/iso-agents-002
```

Do **not** merge `opencode/iso-agents-002` into `main`.  
Main `AGENTS.md` must stay SHA256 `06DC5A5FEAC45958667BB8F7DE0174BAE478B11F2A7F806C492E33BC6339F090`.
