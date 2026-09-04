# History pagination

- FE page size: 10 (`HISTORY_PAGE_SIZE`).
- API: `GET /v1/admin/content/articles/{id}/history?skip=&take=`.
- UI: Previous/Next + «صفحه N از M»; Spinner while loading.
- Prior-publication readiness uses a bounded history fetch on load (take=20), not unbounded list.
