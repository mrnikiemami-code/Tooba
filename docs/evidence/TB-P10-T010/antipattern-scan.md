# Anti-pattern scan

| Check | Result |
| --- | --- |
| Arbitrary HTML/CSS/JS on Page | CLEAN — no such fields |
| User-authored SQL/query | CLEAN — Admin UI deferred; API is typed fields only |
| Physical Next page per Page | CLEAN — one `[slug]` shell |
| Rebuild/redeploy to add Page | CLEAN — DB + cache invalidate |
| Dynamic page shadows system routes | CLEAN — reserved list + static routes first |
| Client-trusted StoreId | CLEAN — commerce context / catalog DB |
| Cross-store lookup | CLEAN — isolated catalogs; missing page 404 |
| Draft publicly visible | CLEAN — Published only |
| Catch-all before canonical routes | CLEAN — `app/[slug]` after static routes |
| Giant JSON blob / Section builder | CLEAN |

Scan: CLEAN.
