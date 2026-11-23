# ISO-RULES — Backend ordinary feature (task-type)
Read when the issued task changes `src/backend/**` (not AMSC unless explicitly required).

MUST read:
1. `.cursor/skills/tooba-backend-feature/SKILL.md`
2. Owning module Contracts / Application / Endpoints for the touched capability
3. Relevant architecture locks when Host/module move risk appears:
   - `docs/architecture/TMAR-architecture-locks.md`
   - `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`
   - `docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`

MUST:
- One canonical owning module; no business authority in Host.
- Contracts-only cross-module; CQRS via MediatR; FluentValidation on untrusted input; canonical Result/ApiResponseFactory; module DI lifetimes.
- Permissions fail closed at API; preserve idempotency and typed errors.
- Focused positive / negative / boundary tests for changed behavior; unrun ≠ PASS.
- Do **not** auto-start Analyze→Migrate→Structure→Certify for ordinary feature work.

AMSC needed only if the task explicitly requires ownership relocation, Host evacuation, or ARCH-COMPLETE certification.
