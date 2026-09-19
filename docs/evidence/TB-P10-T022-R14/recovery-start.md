# TB-P10-T022-R14 — Recovery Start

- Bridge claim: `GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
- Bridge task UUID: `b2ff684a-1d07-4743-9ec0-b55c22cca5b3`
- Task-ID: `TB-P10-T022-R14`
- Channel: `tooba-main` (verified)
- Protocol: BRIDGE-WAKE-V1
- Heartbeat: none (task forbids)
- branch: `main`
- HEAD at claim: `83cf2b89b21409322112901acfe7f93e657b984e`
- `origin/main`: `83cf2b89b21409322112901acfe7f93e657b984e`
- `HEAD == origin/main`: YES
- Task expected previous HEAD: `7f80a34d…` (ancestor of tip; tip advanced by post-R13-R4 user/admin polish)
- `18ca10c9` ancestor of HEAD: YES
- Stashes preserved: `stash@{0} unrelated-pre-r10`, `stash@{1} temp-before-push`
- Unrelated `.tmp-*` left untracked/unmodified
- Scope: AUDIT ONLY — no schema / UI / storefront / production code changes

## Last 12 commits at claim

```
83cf2b89 fix(admin,storefront): pin RTL select buttons and honor banner height presets.
981eaf68 feat(admin): banner settings mirror hero slider with per-banner tabs.
b542ce83 feat(admin): remove story display checkbox.
83f614d7 feat(admin): category level filters, grid page-size fix, collapsible selected chips.
04785f63 feat(hero): Saba split panel image, color, size, opacity, and side.
45e6627e fix(catalog): persist Kimia diagonal panel fields on hero save.
4a08e788 feat(hero): optional Kimia diagonal panel image, color, and size.
e120c9d5 fix(storefront): polish showcase explorer banner and hero slide transitions.
7f80a34d docs(TB-P10-T022-R13-R4): set Result Git SHA to tip after Result artifact.
be172292 docs(TB-P10-T022-R13-R4): set Result Git SHA to evidence tip.
2be222c2 fix(TB-P10-T022-R13-R4): calm Product Showcase motion and proportion polish.
3351481b docs(TB-P10-T022-R13-R3): set Result Git SHA to tip after Result artifact.
```

## Recovery SoT pointers

- `docs/PROJECT-STATE.md` — Last Implementation TB-P10-T022-R13-R4; Appearance YES; Builder NO
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md` — BRIDGE-WAKE-V1 / tooba-main
- Locks LOCK-SF-001…390 retained; no new locks written this audit
