# TB-P08-T016 — Runtime URLs

| Surface | URL | Notes |
| --- | --- | --- |
| Backend Host | http://127.0.0.1:5088 | **Healthy** (gate session); keep alive after result |
| Frontend Admin | http://127.0.0.1:3000 | **Healthy** (gate session); also observed on `:3002`; keep alive after result |
| Content list | http://127.0.0.1:3000/admin/content | Browser smoke PASS |
| Article create | http://127.0.0.1:3000/admin/content/articles/new | Draft-first (`?language=fa-IR`) |
| Categories | http://127.0.0.1:3000/admin/content/categories | AppCategoryTree |
| Authors | http://127.0.0.1:3000/admin/content/authors | Global picker source; API requires `activeOnly` |
| Help | http://127.0.0.1:3000/admin/content/help | Central Help |
| Languages | http://127.0.0.1:3000/admin/languages | Language tabs source |
| Public blogs (fa) | http://127.0.0.1:3000/fa/blogs | List + detail slug **200** |

## Health observations

- Host `http://127.0.0.1:5088` healthy
- FE `http://127.0.0.1:3000` healthy (also saw `:3002`)
- **Keep runtimes alive after result**
