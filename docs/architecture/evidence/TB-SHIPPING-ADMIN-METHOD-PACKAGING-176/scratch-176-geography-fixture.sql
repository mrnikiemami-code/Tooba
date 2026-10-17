-- TB-SHIPPING-ADMIN-METHOD-PACKAGING-176 — SCRATCH-ONLY geography reference fixture.
-- Run ONLY against the task-owned disposable database (tooba_176_scratch). This adds the single
-- country reference row the packaging scope validator needs; it is NOT production data and the
-- task never writes reference data through a production path. All packaging policies are written
-- exclusively through the real authorized Admin owner API by acceptance-176.js.
BEGIN;

INSERT INTO geography.countries (country_id, country_code, is_active, created_at, updated_at)
VALUES ('11111111-1111-1111-1111-111111111176', 'IR', true, now(), now())
ON CONFLICT (country_id) DO NOTHING;

INSERT INTO geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
VALUES ('11111111-aaaa-4aaa-8aaa-111111111176', '11111111-1111-1111-1111-111111111176',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'ایران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

COMMIT;
