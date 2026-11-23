# ISO-RULES — AMSC / architecture specialization (explicit only)
**Forbidden by default.** Read this pack only when the issued TASK explicitly authorizes AMSC or module architectural completion.

Order when authorized:
1. Analyze → 2. Migrate → 3. Structure → 4. Certify  
`Complete` only to repair an incomplete module when the task says so.

Reference docs (read as needed, not always-on):
- `docs/architecture/TMAR-architecture-locks.md`
- `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
- `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`
- `docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`
- Skills under `.cursor/skills/tooba-architecture-*`

MUST NOT:
- Auto-run AMSC for ordinary backend/frontend feature fixes
- Build a parallel architecture or second product repository out of this worktree
- Weaken locks to obtain a green status

If AMSC appears necessary but is not authorized: STOP_FOR_ARCHITECT with options; do not start waves.
