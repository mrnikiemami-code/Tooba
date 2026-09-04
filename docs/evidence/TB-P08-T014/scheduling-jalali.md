# TB-P08-T014 — Scheduling / Jalali

- Storage remains UTC `DateTimeOffset` (Content module historical; not Jalali components in DB).
- fa Admin: Jalali date picker + time (`ContentArticlePublishDateField`).
- en Admin: Gregorian datetime-local.
- Future PublishDate + Published status = not publicly visible until due (`PubliclyVisibleArticles`).
