# 18 — Settings E2E (API)

- Customer preference PUT locale en → GET en → restored fa: PASS
- Seller settings PUT supportPhone → GET persists → restored: PASS
- Seller foreign deny (owner A → seller B): 403 PASS
- Employee without seller.settings.*: GET 403 + PUT 403 PASS
- Admin operator profile PUT bio → GET persists: PASS
- Direct DB mutation: NONE
