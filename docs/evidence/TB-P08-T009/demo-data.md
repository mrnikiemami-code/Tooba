# demo-data

`ContentDevelopmentSeed` idempotent by category (language+slug), author (slug), article (locale+slug).
FA categories + EN categories (independent, not translations).
Authors: tooba-editorial, maryam-ahmadi, ali-rezaei, jordan-blake.
FA+EN published articles with SEO/tags/cover/category/author; drafts (fa+en); one scheduled FA published-future.
Also runs in Development when `RunLegacyBootstraps=false`.
Does not overwrite unrelated user rows — upsert by keys only.
