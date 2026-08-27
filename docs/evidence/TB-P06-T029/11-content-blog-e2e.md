# 11 — Content / Blog E2E (TB-P06-T029)

## Probed this session

| Check | Result |
| --- | --- |
| Storefront listing `GET http://localhost:3000/fa/blogs` | **200** |
| Host content articles list | **200** (`GET /v1/content/articles` class) |
| Canonical vs typo | `/blog` **404**; canonical `/fa/blogs` (see `02-dead-fake-ux-sweep.md`) |

## Seeded article (Development preview)

Inherited Content seed (T013 / T016-R1); still the commercial demo slug:

| Surface | URL |
| --- | --- |
| Article (fa) | http://localhost:3000/fa/blogs/guide-online-shopping |
| Admin content | http://localhost:3000/admin/content |

Articles are Content-owner published data — not fake FE stubs. Locale-prefixed routes and SEO metadata proven under ACCEPTED **TB-P06-T016-R1** (canonical self, no invented hreflang pairs).

## Fake articles

None claimed on `/fa/blogs` listing for this gate; listing binds Host published set.

## Verdict

Content/Blog commercial surface **LIVE**. Listing + Host articles confirmed this session; detail/SEO depth inherits T016-R1 ACCEPT.
