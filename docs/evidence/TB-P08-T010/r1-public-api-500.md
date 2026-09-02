Public GET /v1/content/articles|categories|authors returned 500 PostgresException because Content schema/migrations and demo rows were never applied while Catalog legacy bootstraps were skipped.

After scoped migrate+seed: articles fa 200 (4 published), en 200 (4 published), categories 200, authors 200. Root cause was missing schema/data, not a catch-empty fallback.
