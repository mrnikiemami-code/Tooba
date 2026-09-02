# r1-content-permission-matrix

Canonical ACC codes (existing catalog — no content.article.* invented):

| Code | Backend ops | FE gate |
|---|---|---|
| content.view | article list/get/query; category tree/workspace; author query/picker/workspace; article media GET | nav content.view |
| content.create | create article/category/author | New article / create CTAs |
| content.edit | article update/archive/delete; category mutations; author update/deactivate; article media mutations | edit/delete/archive; workspace EDIT |
| content.publish | publish/unpublish | publish/unpublish actions |

Language/Media keep Localization/Media module permissions — not remapped under Content.
