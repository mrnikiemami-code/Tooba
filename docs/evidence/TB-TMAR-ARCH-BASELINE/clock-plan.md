# Time Abstraction Plan

Target: `IClock` at Application/Infrastructure orchestration boundaries.

- Canonical type: `DateTimeOffset` UTC
- Implementation: Host/BuildingBlocks; test fixed clock
- Domain: receive explicit `now` — no per-entity IClock injection
- Migration: strangler on touched paths; architecture test forbids new UtcNow in Domain/Application after gate
