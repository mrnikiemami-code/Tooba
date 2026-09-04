# author-availability-root-cause.md

Root cause: FE `fetchActiveContentAuthors` called non-existent `/v1/admin/content/authors/active`.

Host only exposes picker at `GET /v1/admin/content/authors/picker` (`activeOnly`, optional `search`).

Fix: point client at `/v1/admin/content/authors/picker?activeOnly=true` and map via `mapContentAuthorPickerItem` (`id`/`AuthorId`).

Draft-first create no longer blocks on author availability; author is optional until publish (`content.author.required_for_publish`).
