# TB-P08-T014 — Runtime smoke

## Attempted

- Focused BE unit readiness tests (no Docker).
- Integration workflow test (Docker/Testcontainers when available).
- FE source asserts for readiness/preview/history/CKEditor/category/tags.

## Blockers (honest)

- Full interactive fa/en browser smoke against live Host may be blocked if Host process locks binaries or Docker unavailable in CI/agent environment.
- When Docker available: workflow test covers readiness gate, publish/unpublish/republish history, scheduled visibility, draft preview flags.
