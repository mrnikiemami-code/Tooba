# Visual verify (TB-P08-T016-R1)

Surfaces checked in browser (admin FE `:3000`):

| Surface | Result |
|---------|--------|
| `/admin/content` (FA articles) | No `?` / mojibake in title, author, category columns |
| `/admin/content/categories` | Active FA labels clean; archived rows use `Archived seed…` style names |
| `/admin/content/authors` | Includes `نویسنده نمونه` (inactive); no `???????` rows |

Screenshots:

- `articles-admin.png`
- `categories-admin.png`
- `authors-admin.png`

`USER_VISUAL_ACCEPTED=NO` (worker visual cleanliness only; not product visual accept).
