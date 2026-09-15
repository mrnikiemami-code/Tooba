# Performance — TB-P10-T022-R6

- Operational tables unchanged (no width increase)
- New Template parity tables indexed equivalently (unique pairs, TemplateId on roots, category/product FKs)
- Preview still TemplateId-scoped joins; category media URLs resolved in projection (no N+1 media HTTP)
- Structural seed is one-shot idempotent (attribute definition presence guard)
