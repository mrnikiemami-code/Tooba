# Recovery start — TB-P10-T018

- Branch: main
- HEAD: 726496b394eb094f812e29abf2b98ab5372204ec (== origin/main pin after T017-R5)
- Expected previous impl SHA (task): faf1e1c5d6dccf275fa63624ecb4f12014431659 — pin divergence is intentional SoT pin, not RECOVERY_CONFLICT
- Channel: tooba-main / worker tooba-worker-01
- Bridge UUID: b5da8800-9bff-496d-8159-7d30c4ceb071
- Host :5088: down at preflight (non-blocking for foundation/docs/unit tests)
- FE :3000: down at preflight (non-blocking)
- Bridge :17321 /health: ok
- Unrelated local user changes: many .tmp-* artifacts preserved untracked
- Current architecture: dual stacks — Home PageComposition (snake_case) + Landing PascalCase registry; Appearance four-role system accepted through T017-R5
