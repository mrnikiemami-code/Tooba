# TB-P10-T022-R16 — Recovery Start

- Task-ID: `TB-P10-T022-R16`
- Parent: `TB-P10-T022-R15` (ACCEPTED)
- Channel: `tooba-main`
- Protocol: BRIDGE-WAKE-V1
- Heartbeat: none
- branch: `main`
- HEAD at start: `c0c4e6be569d4ae2e5cda4660720f225724ce9d1`
- `origin/main`: `c0c4e6be569d4ae2e5cda4660720f225724ce9d1`
- `HEAD == origin/main`: YES
- Task expected previous HEAD: `afe628e33558161a77394f7029947f72ec6d2973` — present as ancestor (tip advanced by R15 Result SHA chore + cleanup)
- `18ca10c9` ancestor of HEAD: YES
- R15 migration/schema present: YES (`20260919134400_MerchandisingCampaignFoundation`)
- AMAZING master seed path present: YES (`EnsureAmazingTypeSeededAsync`)
- Unrelated `.tmp-*` left untracked/unmodified
- Stashes preserved (not touched)
- Scope: merchandising campaign runtime read model + resolver + Dev seed + tests; no Builder/Storefront/Admin Campaign UI

## Last 12 commits at start

```
c0c4e6be chore: remove accidental Promotion .tmp-t014-test-out binaries from tree.
afe628e3 docs(TB-P10-T022-R15): set Result Git SHA to tip.
b3d96fed feat(promotion): merchandising campaign foundation with AMAZING type.
6cdd5e72 docs(TB-P10-T022-R14): set Result Git SHA to final tip.
060b87b6 docs(TB-P10-T022-R14): align Result Git SHA to tip after push.
0c8bd0d2 docs(TB-P10-T022-R14): set Result Git SHA to tip.
279089e5 docs(TB-P10-T022-R14): restore Result bridge artifact.
68f58a4f docs(TB-P10-T022-R14): set Result Git SHA to tip after Result artifact.
8e3bdedd docs(TB-P10-T022-R14): set Result Git SHA to evidence tip.
fad28fe1 docs(TB-P10-T022-R14): Amazing Offers promotion architecture audit evidence.
83cf2b89 fix(admin,storefront): pin RTL select buttons and honor banner height presets.
981eaf68 feat(admin): banner settings mirror hero slider with per-banner tabs.
```

## Recovery SoT pointers

- Last Architect-accepted: TB-P10-T022-R15
- Last Implementation before this task: TB-P10-T022-R15
- Appearance YES / Builder NO retained
- Locks at start: LOCK-SF-001…398
