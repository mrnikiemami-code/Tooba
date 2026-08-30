# Git HEAD reconciliation — TB-P07-T034-R1

## Prior conflict (T034 Result)
- Top-level `Git-HEAD` and narrative “final HEAD” disagreed (56567a0… vs de5dfcb…).
- Tip-sync commits for RESULT created ambiguous dual hashes.

## R1 rule
Single authoritative tip after push:

```text
git rev-parse HEAD
git rev-parse origin/main
```

Require `HEAD == origin/main` and Result `Git-HEAD` equals that same tip (Bridge payload uses the tip at post time).

Unrelated dirty files preserved; only R1-scoped paths committed.
