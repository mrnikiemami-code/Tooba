# PostgreSQL rollback-only proof (R3)

**Status: OPEN**

No caller-owned PG rollback harness was executed in this wake for Story/Media G1–G7 (fresh-context Items, scoped occurrence uniqueness/winner+version concurrency, query counts/isolation).

Backup+apply of the additive Story occurrence PK migration used the real `tooba_alpha` database and is recorded in RECEIPTS.md; that is schema apply evidence, not the rollback-only behavior matrix.
