# WORKSPACE SELECT / ROLLBACK — OpenCode isolated execution (004)

## Ready worktree (optional until architect activation)

| Item | Value |
|---|---|
| Path | `D:\Users\User\source\repos\SarvNewVer-opencode-iso-002` |
| Branch | `opencode/iso-agents-002` (local) |
| Short AGENTS | yes (main `AGENTS.md` untouched) |
| Rules | `docs/opencode/iso-rules/01–08` |
| Model | `metisdeepseek/deepseek-v4-flash` |

## Open OpenCode on the iso tree (do not stop task 286)

```powershell
# Preferred: safety check only (no Desktop retarget)
powershell -NoProfile -File D:\Users\User\source\repos\SarvNewVer-opencode-iso-002\docs\opencode\OPENCODE-ISO-LAUNCH.ps1 -Mode DocumentOnly

# Optional NEW Desktop window (existing sessions stay up):
powershell -NoProfile -File D:\Users\User\source\repos\SarvNewVer-opencode-iso-002\docs\opencode\OPENCODE-ISO-LAUNCH.ps1 -Mode NewDesktopWindow
# Then File → Open Folder → SarvNewVer-opencode-iso-002
```

Keep Cursor / task 286 on main: `D:\Users\User\source\repos\SarvNewVer`.

## Tooling

- OpenSpec / Impeccable wrappers: `docs/opencode/iso-bin/*.cmd` (lab install 2.1.1 / 4.10.0)
- FE/BE skills: `.cursor/skills/tooba-*-feature/SKILL.md`
- Delivery: `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` @ **1.3.0**

## Publish product changes

Still commit/push from the **main repository** under approved Git rules. Do not treat the worktree as a second product remote. Do not merge `opencode/iso-agents-002` into `main` as a replacement for main `AGENTS.md`.

## Rollback

```powershell
cd D:\Users\User\source\repos\SarvNewVer
git worktree remove --force D:\Users\User\source\repos\SarvNewVer-opencode-iso-002
git branch -D opencode/iso-agents-002
```

Main `AGENTS.md` must remain SHA256 `06DC5A5FEAC45958667BB8F7DE0174BAE478B11F2A7F806C492E33BC6339F090`.
