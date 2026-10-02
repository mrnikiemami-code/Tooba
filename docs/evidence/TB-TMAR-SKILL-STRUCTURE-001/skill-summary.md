# TB-TMAR-SKILL-STRUCTURE-001 — Skill Summary

Mode: `SKILL_DEFINITION_ONLY`  
Track: `ARCHITECTURE_STRUCTURE_SKILL`  
Structure-Skill-State: **CREATED**

## Deliverable

Created fourth independent Cursor architecture skill:

`.cursor/skills/tooba-architecture-structure/SKILL.md`

## Four-skill workflow (definition present)

1. `tooba-architecture-analyze`
2. `tooba-architecture-migrate`
3. `tooba-architecture-structure` ← **new**
4. `tooba-architecture-certify`

## Explicit hard rules in skill

| Concern | State |
| --- | --- |
| Capability-first | EXPLICIT |
| Shallow-by-default | EXPLICIT |
| Single-file request leaf | EXPLICIT (`OVER_FOLDERED` by default) |
| Technical-axis-first detection | EXPLICIT |
| Source-file count (not types) | EXPLICIT |
| God-file vs folder-explosion balance | EXPLICIT |
| Filesystem + Solution Explorer | COVERED |
| Path ↔ namespace EXACT | COVERED |
| Root allowlists | COVERED |
| Stale/duplicate copies | COVERED |
| Host final closure | PRESERVED |
| Certified-module drift | COVERED |
| Durable scoped structure guards | COVERED |

## Classification model

All required states defined: Folder-Granularity, Solution-Explorer, Path-Namespace, Physical-Copy, Root-Allowlist, File-Cohesion, Structure-State (incl. `READY_FOR_CERTIFY`).

## Sections

SKILL.md contains required sections 1–28.

## Out of scope (honored)

- No rewrite of Analyze/Migrate/Certify
- No production / module / Host / frontend changes
- No module re-certification
- No AccessControl foldering repair
- No automatic next implementation task
