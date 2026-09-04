# Browser validation (TB-P08-T016-R4)

Host: `http://127.0.0.1:5088` health `{"status":"ok"}`  
FE: `http://127.0.0.1:3000` (dev; root returns 308, admin routes 200)

## API (admin `X-Tooba-Dev-Actor-User-Id` from `/v1/admin/dev-context`)

| Check | Result |
|-------|--------|
| English article | Found `01a06ac1-135f-7000-b45f-7089ee3d1add` |
| GET article locale | `en-US` |
| History `skip=0&take=10` | 1 item (`article.draft_created`); `totalCount=1` |
| History `skip=10&take=10` | `items=[]`, `skip=10`, `take=10`, `totalCount=1` (paging OK) |
| Media `?contentTypePrefix=video/` | empty before upload (`totalCount=0`) |
| Media `?kind=video` | empty before upload |
| Upload tiny `video/webm` | OK → `mediaAssetId=01a06b5b-9d4a-7000-a1bc-219ab95d8e8b` |
| Video filter after upload | `totalCount=1` (filter shows uploaded webm) |

## Browser UI

- Interactive `cursor-ide-browser` MCP: **unavailable** in this session (tabs create then vanish; `browser_navigate` fails with “No browser tab available”).
- HTTP probe: `GET http://127.0.0.1:3000/admin/content/articles/01a06ac1-135f-7000-b45f-7089ee3d1add?mode=edit` → **200**; SSR/shell HTML includes `en-US` / `content-article` markers.
- Locale identity, history pager Previous/Next, CKEditor fontFamily, DAM video filter: covered by FE unit tests + live API above (not visually clicked in MCP).

## Fonts / video UI note

- Fonts: unit tests assert CKEditor `fontFamily` config + sanitizer allowlist (Times New Roman, B Nazanin).
- Video: live upload + `contentTypePrefix=video/` list confirmed; CKEditor DAM picker not interactively opened (MCP blocker).
