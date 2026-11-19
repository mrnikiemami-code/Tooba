# TB-282-R13 COMPLETION LEDGER

| Gate | Method | Observed |
| --- | --- | --- |
| A row/detail approve | mounted Dialog + fetch | POST `/approve` version+key |
| A row/detail reject | mounted + reason gate | cancel 0; confirm reason frozen |
| A delete admin | admin-op Dialog | cancel 0 DELETE; confirm durable |
| A bulk toolbar | AppDataGrid + live list | pageSelectionOnly; تأیید/رد/حذف |
| A mixed eligibility | classifyAdminOp + Dialog lists | skipped with Persian reason |
| A partial/retry | pending + recovery | exact body replay; no new key for success |
| A seller isolation | mounted seller detail | no approve/reject; no rowSelection |
| B no-end placement | DOM + CDP | beside END label; not legend corner |
| B align / mobile | CDP 1440/390 | tops 0px; start-then-end; no overflow |
| Zero backend | git diff | FE-only |
| Zero live write | — | controlled tests only |
