# Tooba — Repository Agent Skills (durable registry)

This file is the durable Source of Truth for the repository-owned agent skills.
It exists so that after a Cursor reset / environment wipe / new chat, an agent can
immediately discover which skills exist, when to use them, and where they live —
**without depending on any personal/global Cursor skill store.**

## How a reset agent uses this

1. Read `AGENTS.md` (root) — it points here.
2. Scan the table below for a skill matching the user's intent / trigger phrase.
3. Read the full skill file at `.cursor/skills/<name>/SKILL.md` with the Read tool.
4. Follow that skill exactly. Do not invent requirements or broaden scope.

The skills live **inside this repository** under `.cursor/skills/`. Cursor indexes
them automatically when the workspace is opened, and they are versioned in git, so
they survive any Cursor reinstall, cache clear, or machine change.

## Skills

| Skill | Trigger phrases | File |
| --- | --- | --- |
| `asli-grid` | «اصلی‌گرید», `asli-grid`, «نمای اصلی را پیش‌فرض کن» | `.cursor/skills/asli-grid/SKILL.md` |
| `tooba-architecture-analyze` | analyze a file/capability, decide true module ownership, migration plan | `.cursor/skills/tooba-architecture-analyze/SKILL.md` |
| `tooba-architecture-migrate` | migrate / Host evacuation / ownership correction / decoupling | `.cursor/skills/tooba-architecture-migrate/SKILL.md` |
| `tooba-architecture-structure` | physical/folder structure, .slnx grouping, path↔namespace exactness | `.cursor/skills/tooba-architecture-structure/SKILL.md` |
| `tooba-architecture-complete` | complete/harden a destination module after migration | `.cursor/skills/tooba-architecture-complete/SKILL.md` |
| `tooba-architecture-certify` | certify a module against ARCH-COMPLETE-002 after migration | `.cursor/skills/tooba-architecture-certify/SKILL.md` |

## `asli-grid` summary

Promotes an Admin `AppDataGrid` saved view named exactly **«اصلی»** into
code-owned grid defaults, then deletes that saved view.

- Reads `user_preference.ui_preferences` for the grid preference key.
- Extracts only: `layout.order`, `layout.widths`, `layout.visibility`, `sorts`,
  `pageSize`.
- Applies them as code defaults (column defs + `defaultQuery`) on the list screen.
- Removes the «اصلی» view afterward.

Related trigger rule: `.cursor/rules/saved-view-asli-defaults.mdc`.

## Notes

- Personal/global skill stores (`C:\Users\<user>\.cursor\skills-cursor`,
  `C:\Users\<user>\.agents\skills`) are convenience mirrors only. The repository
  copy under `.cursor/skills/` is authoritative.
- When adding a new repository skill, add its row to the table above in the same
  commit so the registry never drifts from reality.
