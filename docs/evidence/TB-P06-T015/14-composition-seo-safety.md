# 14 — Composition SEO safety (TB-P06-T015)

| Concern | Status |
|---|---|
| Home `<h1>` | Remains sr-only hero title (existing pattern) |
| Section config | Cannot inject raw HTML/JS into head or body |
| Blog / article SEO | Still owned by Content (T013); composition only places `latest_articles` slot |
| Canonical / hreflang | Unchanged from T014 policy (no fake hreflang) |
| Sitemap | No admin-authored free-form routes via composition |

Composition changes order/visibility only — not SEO engine ownership.
