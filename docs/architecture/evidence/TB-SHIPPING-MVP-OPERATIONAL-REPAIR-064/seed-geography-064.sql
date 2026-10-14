-- TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047 — MINIMAL, EXPLICIT geography reference data for the
-- proven LOCAL DEV tenant database (tooba_alpha / tenant store-alpha). GENERATED — do not hand-edit;
-- regenerate with seed-generate.ps1, which transcribes the values verbatim from
-- StorefrontIranGeography.Provinces and derives the ids with RFC 4122 UUIDv5 (see seed-id-map.txt).
--
-- Scope (ADR-036 Phase 0a: 'no blanket nationwide import; nothing enabled by default'):
--   * country IR
--   * the 12 provinces and 47 cities the legacy catalogue already supports, nothing more
--   * Persian name translation rows, keyed by the Localization-owned fa-IR language id
-- NOT seeded here: service areas, coverage/verdicts, foreign countries, districts, serviceability.
--
-- Idempotent: every insert is 'where not exists' on the natural key, so a re-run inserts 0 rows.
-- Fails loud (GEO047_*) instead of guessing when a stable code is already owned by another id, when
-- an existing label for the same place+language carries different text, when the Localization
-- language row is missing, or when any post-condition of the reviewed shape does not hold.

begin;

-- Inputs of this run, kept explicit and reviewable.
create temp table seed_country (country_id uuid primary key, country_code text not null, name text not null, name_translation_id uuid not null) on commit drop;
insert into seed_country (country_id, country_code, name, name_translation_id) values
    ('e3451206-e503-51a8-91ad-afa9ab70e330', 'IR', 'ایران', '6d102556-0173-5ec8-aec3-90db5aa14cdc');

create temp table seed_area (area_id uuid primary key, code text not null, name text not null) on commit drop;
insert into seed_area (area_id, code, name) values
    ('3cefa472-d0ac-5b2e-817b-ae7df7c33382', 'tehran', 'تهران'),
    ('1f8050a8-c325-59fa-959f-82d76b5b2bc5', 'alborz', 'البرز'),
    ('ed83ce72-057e-5222-b05a-12283c27386d', 'isfahan', 'اصفهان'),
    ('47091323-a60a-5e41-b543-1fe898ce97b6', 'razavi_khorasan', 'خراسان رضوی'),
    ('24e00388-c216-531c-a3ca-11f382508760', 'fars', 'فارس'),
    ('995347e5-0388-5d1d-8c0f-1fd7660216af', 'east_azerbaijan', 'آذربایجان شرقی'),
    ('b92c36a7-dd9f-5ea3-8ad7-668083d79695', 'khuzestan', 'خوزستان'),
    ('ee4bc221-5739-5c54-9d27-c0cf5a61f77a', 'mazandaran', 'مازندران'),
    ('fe3fac86-29df-5fa4-9866-7d9c45c3dc78', 'gilan', 'گیلان'),
    ('457be1c4-700e-5f6f-b9eb-052938814f0b', 'qom', 'قم'),
    ('76d77bc6-8b83-521f-9454-45587bac738e', 'yazd', 'یزد'),
    ('ba8c5e1a-88d9-5a0c-81ef-c65071b1a525', 'kerman', 'کرمان');

create temp table seed_area_name (translation_id uuid primary key, area_code text not null, name text not null) on commit drop;
insert into seed_area_name (translation_id, area_code, name) values
    ('17b8e82e-60e1-540f-89cf-77947917ab7e', 'tehran', 'تهران'),
    ('2096f2bf-47e2-52e3-93af-9229e382f5a8', 'alborz', 'البرز'),
    ('fb8c08bd-5c26-5221-a245-0d3d30508333', 'isfahan', 'اصفهان'),
    ('f3e74504-b803-54b5-bb6e-db299e5a3dd3', 'razavi_khorasan', 'خراسان رضوی'),
    ('d15a4a54-c23f-5702-865b-2f09a694611a', 'fars', 'فارس'),
    ('7a888e5e-db76-5070-98fe-da04bd22ecd0', 'east_azerbaijan', 'آذربایجان شرقی'),
    ('f6e38f0c-c9a8-57a8-af36-1c80d8424951', 'khuzestan', 'خوزستان'),
    ('96cba60a-003e-5edf-96bb-0aecc2f1d027', 'mazandaran', 'مازندران'),
    ('80c2b0d1-3390-56da-8a06-03c18086e50f', 'gilan', 'گیلان'),
    ('1357e24a-78bf-583c-871c-0b6383b136be', 'qom', 'قم'),
    ('2facee22-aecb-572f-94da-7b213f9c4d20', 'yazd', 'یزد'),
    ('93ccc1a0-9393-5f60-8cef-de3877b736fc', 'kerman', 'کرمان');

create temp table seed_city (city_id uuid primary key, area_code text not null, position text not null, code text not null, name text not null) on commit drop;
insert into seed_city (city_id, area_code, position, code, name) values
    ('d63c8292-bc14-558a-825f-b39e25c07f61', 'tehran', '01', 'IR-tehran-01', 'تهران'),
    ('95a61214-1d7c-5677-99df-0e22d1e621e4', 'tehran', '02', 'IR-tehran-02', 'ری'),
    ('f57197cd-f2b3-5608-9351-7a3ed445bb97', 'tehran', '03', 'IR-tehran-03', 'شهریار'),
    ('1d4edd3a-9b55-53f8-bc86-b5999fa2636f', 'tehran', '04', 'IR-tehran-04', 'اسلامشهر'),
    ('853ddc6c-211c-5f2d-a2d2-315fe8f21481', 'tehran', '05', 'IR-tehran-05', 'ملارد'),
    ('c0b8bd19-6e32-5e78-b9c5-6d432eebba22', 'tehran', '06', 'IR-tehran-06', 'پاکدشت'),
    ('68255340-c69f-5d20-bf33-e8e0b24edde9', 'alborz', '01', 'IR-alborz-01', 'کرج'),
    ('2fac2578-84a9-52ee-93ba-7928c2f4ec3b', 'alborz', '02', 'IR-alborz-02', 'فردیس'),
    ('70d93cc6-25eb-5ac5-8e38-be042ff09cf5', 'alborz', '03', 'IR-alborz-03', 'نظرآباد'),
    ('3d102fb8-cb8d-580a-8a4f-861638510872', 'alborz', '04', 'IR-alborz-04', 'ساوجبلاغ'),
    ('d8557233-9cc5-5326-b0d4-c8611b75e55a', 'isfahan', '01', 'IR-isfahan-01', 'اصفهان'),
    ('bbd665e5-556f-5b85-a584-95a263ff4f24', 'isfahan', '02', 'IR-isfahan-02', 'کاشان'),
    ('54748d20-bd46-5246-80a5-901f4aed77fe', 'isfahan', '03', 'IR-isfahan-03', 'خمینی‌شهر'),
    ('05802739-e5a4-5513-8810-7c706017575e', 'isfahan', '04', 'IR-isfahan-04', 'شاهین‌شهر'),
    ('6a7376fc-ea8c-56d6-8b60-d1c7d0762eb2', 'isfahan', '05', 'IR-isfahan-05', 'نجف‌آباد'),
    ('8000df3d-53cc-5535-a57e-8368a71daf58', 'razavi_khorasan', '01', 'IR-razavi_khorasan-01', 'مشهد'),
    ('a9e9a5cc-0b2c-567a-9f8c-876aedf1bb5d', 'razavi_khorasan', '02', 'IR-razavi_khorasan-02', 'نیشابور'),
    ('b0044737-8b6f-570c-9409-dfaeaa30f3c3', 'razavi_khorasan', '03', 'IR-razavi_khorasan-03', 'سبزوار'),
    ('395524d0-778b-528c-a38b-9ea713f56109', 'razavi_khorasan', '04', 'IR-razavi_khorasan-04', 'تربت حیدریه'),
    ('530e5a7f-6839-592b-a2e3-de829619efc4', 'fars', '01', 'IR-fars-01', 'شیراز'),
    ('a75f2a5f-a85e-5a5c-9519-893695990298', 'fars', '02', 'IR-fars-02', 'مرودشت'),
    ('e9a212c2-e01b-5fad-82e1-8a2d93c19287', 'fars', '03', 'IR-fars-03', 'جهرم'),
    ('38fce575-ea36-5f4e-92f1-5848d9387f00', 'fars', '04', 'IR-fars-04', 'فسا'),
    ('e3f1dd5c-9d6a-51cf-8f94-1119dd01e9f3', 'fars', '05', 'IR-fars-05', 'کازرون'),
    ('100c649b-5c51-56b0-b384-f8c0ec5062f0', 'east_azerbaijan', '01', 'IR-east_azerbaijan-01', 'تبریز'),
    ('ebf0cb15-c2a0-5366-a08e-2283d9c71f97', 'east_azerbaijan', '02', 'IR-east_azerbaijan-02', 'مرند'),
    ('ac84d520-3676-59fd-a938-02881d1e8023', 'east_azerbaijan', '03', 'IR-east_azerbaijan-03', 'مراغه'),
    ('ec5f1a00-bddf-5692-9d26-238fdaf8d6c4', 'east_azerbaijan', '04', 'IR-east_azerbaijan-04', 'اهر'),
    ('114adf3f-d692-5d3e-b801-78fc599e93eb', 'khuzestan', '01', 'IR-khuzestan-01', 'اهواز'),
    ('aaa01fca-d1ab-5228-ae89-08aba0d727a4', 'khuzestan', '02', 'IR-khuzestan-02', 'آبادان'),
    ('c7924a66-0255-5f66-97aa-9503bf971187', 'khuzestan', '03', 'IR-khuzestan-03', 'دزفول'),
    ('5213e4bf-3f5e-5577-96ff-16bdecd154b0', 'khuzestan', '04', 'IR-khuzestan-04', 'بندر ماهشهر'),
    ('12878f7b-6bf4-5d21-911f-73d7fec3e3d4', 'mazandaran', '01', 'IR-mazandaran-01', 'ساری'),
    ('29c5c48e-e63e-5786-8218-85ba992fb453', 'mazandaran', '02', 'IR-mazandaran-02', 'بابل'),
    ('16b1245c-6be8-52d0-93bd-f89a514bee0c', 'mazandaran', '03', 'IR-mazandaran-03', 'آمل'),
    ('21d11dd2-b604-5e01-abaf-f8328f2e633c', 'mazandaran', '04', 'IR-mazandaran-04', 'قائم‌شهر'),
    ('bdaf61f6-32a7-54c0-9ba7-57ecc7093b63', 'gilan', '01', 'IR-gilan-01', 'رشت'),
    ('32d4598e-bc55-572c-acd2-4fff4c10cf85', 'gilan', '02', 'IR-gilan-02', 'بندرانزلی'),
    ('6386e092-5a07-5a57-924a-eabbe13b6aa3', 'gilan', '03', 'IR-gilan-03', 'لاهیجان'),
    ('bb4f53a6-f93c-5168-b2b2-52142a45b967', 'gilan', '04', 'IR-gilan-04', 'رودسر'),
    ('d3e9e0bd-0671-5ebd-8bfe-629ec5301df6', 'qom', '01', 'IR-qom-01', 'قم'),
    ('d5a23a97-98b5-543c-9237-3a59f0edda25', 'yazd', '01', 'IR-yazd-01', 'یزد'),
    ('a261ffeb-4be6-5764-b719-1bdd7fa9afbc', 'yazd', '02', 'IR-yazd-02', 'اردکان'),
    ('268784e6-1b28-5504-bdad-9f4cb68601a8', 'yazd', '03', 'IR-yazd-03', 'میبد'),
    ('66f39213-5faf-5676-be09-42732165a5bc', 'kerman', '01', 'IR-kerman-01', 'کرمان'),
    ('75f468d4-4001-5633-b2ea-61ed12f754f8', 'kerman', '02', 'IR-kerman-02', 'رفسنجان'),
    ('26db84e9-9a34-5877-81b5-b61f335e3a8e', 'kerman', '03', 'IR-kerman-03', 'سیرجان');

create temp table seed_city_name (translation_id uuid primary key, area_code text not null, position text not null, name text not null) on commit drop;
insert into seed_city_name (translation_id, area_code, position, name) values
    ('bfdfada4-0b18-50dd-aea0-dda32f1a0788', 'tehran', '01', 'تهران'),
    ('7a1bb27a-2bdb-575f-9af2-5a7edacae13c', 'tehran', '02', 'ری'),
    ('9c5af070-2e64-57f2-8aaa-30a51f8b197c', 'tehran', '03', 'شهریار'),
    ('85337181-97cb-55c5-8ae3-eb62c5d58e7c', 'tehran', '04', 'اسلامشهر'),
    ('e97e240b-1999-59c7-aef5-61d4fdab11e8', 'tehran', '05', 'ملارد'),
    ('6c5d7fc9-5802-5441-ae83-c767468eed34', 'tehran', '06', 'پاکدشت'),
    ('f11c7372-012f-5b63-9abb-c7f5fa2de6b6', 'alborz', '01', 'کرج'),
    ('9b460946-ed95-5fb8-8322-62eae98148fd', 'alborz', '02', 'فردیس'),
    ('9f2c40ae-bece-59f0-9ac8-c30c47b6615b', 'alborz', '03', 'نظرآباد'),
    ('92bb501a-680e-5d35-a3ba-ad8ea40bf9f4', 'alborz', '04', 'ساوجبلاغ'),
    ('524f2c53-96da-5ff5-ad69-2249d140520a', 'isfahan', '01', 'اصفهان'),
    ('f3427ba9-4e72-56d0-ab2d-c18975b21c60', 'isfahan', '02', 'کاشان'),
    ('8c6b6822-6f93-517e-8c00-c5506b61bf62', 'isfahan', '03', 'خمینی‌شهر'),
    ('9819f250-f236-5e1f-9a79-832c38573aef', 'isfahan', '04', 'شاهین‌شهر'),
    ('7bdef167-bfa6-5f27-9651-8aa4d3a3f98f', 'isfahan', '05', 'نجف‌آباد'),
    ('c11a4854-756e-5b35-b548-5c55e4c95a38', 'razavi_khorasan', '01', 'مشهد'),
    ('96bb2537-0e0b-5cd0-b24c-1dce4f338d4d', 'razavi_khorasan', '02', 'نیشابور'),
    ('315f066e-3269-54fe-9d19-a5434b516d9f', 'razavi_khorasan', '03', 'سبزوار'),
    ('6a75ed04-f257-5da0-aadd-b9cae2554970', 'razavi_khorasan', '04', 'تربت حیدریه'),
    ('95e99370-ac19-5b3f-872d-7253fb28326d', 'fars', '01', 'شیراز'),
    ('dbcc47cb-735b-504d-bc46-5a25a9c68266', 'fars', '02', 'مرودشت'),
    ('f6059303-22c6-5842-9b03-f801912eaa43', 'fars', '03', 'جهرم'),
    ('7e119500-8c67-5521-ab92-f89219a7f851', 'fars', '04', 'فسا'),
    ('e5c71569-2ec4-5631-b744-7393285f5f8a', 'fars', '05', 'کازرون'),
    ('32302af9-15f7-553e-a4a9-5e72068f0ce7', 'east_azerbaijan', '01', 'تبریز'),
    ('a792f3e4-85bd-5b7e-8376-baa8a2e0d8d0', 'east_azerbaijan', '02', 'مرند'),
    ('004c1477-f9ec-5c95-a3dc-4d12a23f87f1', 'east_azerbaijan', '03', 'مراغه'),
    ('6da6cc63-583b-5cd3-b561-9d1b146b8102', 'east_azerbaijan', '04', 'اهر'),
    ('af7dea53-5781-51ae-bc69-d5078117bd80', 'khuzestan', '01', 'اهواز'),
    ('cb1e8c4b-b1ad-5e69-af26-a7c2636f4cae', 'khuzestan', '02', 'آبادان'),
    ('dc610265-6a63-59ee-b1f6-8ce660ed9f8a', 'khuzestan', '03', 'دزفول'),
    ('2fce930e-2348-5395-aabb-b6c889fda2ce', 'khuzestan', '04', 'بندر ماهشهر'),
    ('d0c16213-5e42-5c35-8795-d418b05e6ded', 'mazandaran', '01', 'ساری'),
    ('27de4a62-03ee-5e3f-b1da-9ae6fa4bfdf1', 'mazandaran', '02', 'بابل'),
    ('cbf351ec-16b4-54a5-b46e-bcac69bf8392', 'mazandaran', '03', 'آمل'),
    ('76ab3e33-bdd1-55f3-b768-8bcbc235208c', 'mazandaran', '04', 'قائم‌شهر'),
    ('068a3a91-6bbb-5a35-b542-9f5e7d0adce3', 'gilan', '01', 'رشت'),
    ('9d10f537-9d5c-5d6a-9365-77b2590c34e2', 'gilan', '02', 'بندرانزلی'),
    ('bbb3e848-c646-5ee8-bc13-17a1b529dd6b', 'gilan', '03', 'لاهیجان'),
    ('654efa43-458b-5e18-94e6-157016805f7a', 'gilan', '04', 'رودسر'),
    ('2b4e534c-2bc3-5687-ae8b-e58625c1b446', 'qom', '01', 'قم'),
    ('23cafbb2-3216-5c40-aea5-aeb22aabd1fd', 'yazd', '01', 'یزد'),
    ('6ace3a6e-edd6-573a-b233-2521737b1772', 'yazd', '02', 'اردکان'),
    ('c364414c-29bc-533e-8ee7-0bc7efa013bd', 'yazd', '03', 'میبد'),
    ('c0f60809-cfd9-5819-a2d2-3e0198c765c9', 'kerman', '01', 'کرمان'),
    ('5f238eb2-b4f7-56db-85e4-6211115575e9', 'kerman', '02', 'رفسنجان'),
    ('d041b663-72d9-50fa-b31a-54e25c5c5795', 'kerman', '03', 'سیرجان');

do $$
declare
    fa_language_id uuid;
    conflicting integer;
    n_country integer;
    n_area integer;
    n_city integer;
begin
    -- Read-only lookup of the Localization-owned Persian language id: Geography never owns the
    -- language list (ADR-036 D9), so the id is read from its owner instead of being duplicated here.
    select language_id into fa_language_id from localization.languages where code = 'fa-IR';
    if fa_language_id is null then
        raise exception 'GEO047_LANGUAGE_MISSING: localization.languages has no fa-IR row';
    end if;

    -- Ambiguity guards: a stable code must never be silently re-pointed at another id.
    select count(*) into conflicting from geography.countries x join seed_country s on x.country_code = s.country_code where x.country_id <> s.country_id;
    if conflicting > 0 then
        raise exception 'GEO047_AMBIGUOUS_COUNTRY: country_code already owned by a different id';
    end if;
    select count(*) into conflicting from geography.administrative_areas x join seed_area s on x.code = s.code where x.administrative_area_id <> s.area_id;
    if conflicting > 0 then
        raise exception 'GEO047_AMBIGUOUS_AREA: area code already owned by a different id';
    end if;
    select count(*) into conflicting from geography.cities x join seed_city s on x.code = s.code where x.city_id <> s.city_id;
    if conflicting > 0 then
        raise exception 'GEO047_AMBIGUOUS_CITY: city code already owned by a different id';
    end if;

    -- Drift guards: an existing label for the same place+language carrying different text is not a
    -- rerun of this seed, it is a conflict; report it instead of overwriting.
    select count(*) into conflicting from geography.country_name_translations x join seed_country s on x.country_id = s.country_id where x.language_id = fa_language_id and x.name <> s.name;
    if conflicting > 0 then
        raise exception 'GEO047_CONFLICTING_COUNTRY_LABEL';
    end if;
    select count(*) into conflicting from geography.administrative_area_name_translations x join geography.administrative_areas a on a.administrative_area_id = x.administrative_area_id join seed_area s on s.code = a.code where x.language_id = fa_language_id and x.name <> s.name;
    if conflicting > 0 then
        raise exception 'GEO047_CONFLICTING_AREA_LABEL';
    end if;
    select count(*) into conflicting from geography.city_name_translations x join geography.cities c on c.city_id = x.city_id join seed_city s on s.code = c.code where x.language_id = fa_language_id and x.name <> s.name;
    if conflicting > 0 then
        raise exception 'GEO047_CONFLICTING_CITY_LABEL';
    end if;

    insert into geography.countries (country_id, country_code, is_active, created_at, updated_at)
    select s.country_id, s.country_code, true, now(), now() from seed_country s
    where not exists (select 1 from geography.countries x where x.country_id = s.country_id);

    -- Provinces are roots here: D10 (administrative level semantics) is undecided, so no level
    -- discriminator and no invented intermediate node is introduced.
    insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
    select s.area_id, c.country_id, null, s.code, true, now(), now()
    from seed_area s cross join seed_country c
    where not exists (select 1 from geography.administrative_areas x where x.administrative_area_id = s.area_id);

    insert into geography.cities (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)
    select s.city_id, c.country_id, a.area_id, s.code, true, now(), now()
    from seed_city s join seed_area a on a.code = s.area_code cross join seed_country c
    where not exists (select 1 from geography.cities x where x.city_id = s.city_id);

    insert into geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
    select s.name_translation_id, s.country_id, fa_language_id, s.name, now(), now() from seed_country s
    where not exists (select 1 from geography.country_name_translations x where x.country_id = s.country_id and x.language_id = fa_language_id);

    insert into geography.administrative_area_name_translations (translation_id, administrative_area_id, language_id, name, created_at, updated_at)
    select n.translation_id, a.area_id, fa_language_id, n.name, now(), now()
    from seed_area_name n join seed_area a on a.code = n.area_code
    where not exists (select 1 from geography.administrative_area_name_translations x where x.administrative_area_id = a.area_id and x.language_id = fa_language_id);

    insert into geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at)
    select n.translation_id, c.city_id, fa_language_id, n.name, now(), now()
    from seed_city_name n
    join seed_city c on c.area_code = n.area_code and c.position = n.position
    where not exists (select 1 from geography.city_name_translations x where x.city_id = c.city_id and x.language_id = fa_language_id);

    -- Post-conditions: the stored shape must equal the reviewed legacy shape exactly.
    select count(*) into n_country from seed_country;
    select count(*) into n_area from seed_area;
    select count(*) into n_city from seed_city;
    if (select count(*) from geography.countries) <> n_country then
        raise exception 'GEO047_POST_COUNTRY_COUNT';
    end if;
    if (select count(*) from geography.administrative_areas) <> n_area then
        raise exception 'GEO047_POST_AREA_COUNT';
    end if;
    if (select count(*) from geography.cities) <> n_city then
        raise exception 'GEO047_POST_CITY_COUNT';
    end if;
    if (select count(*) from geography.country_name_translations) <> n_country then
        raise exception 'GEO047_POST_COUNTRY_LABEL_COUNT';
    end if;
    if (select count(*) from geography.administrative_area_name_translations) <> n_area then
        raise exception 'GEO047_POST_AREA_LABEL_COUNT';
    end if;
    if (select count(*) from geography.city_name_translations) <> n_city then
        raise exception 'GEO047_POST_CITY_LABEL_COUNT';
    end if;
    if exists (select 1 from geography.administrative_areas where parent_administrative_area_id is not null) then
        raise exception 'GEO047_POST_UNEXPECTED_AREA_PARENT';
    end if;
    if exists (select 1 from geography.cities c left join geography.administrative_areas a on a.administrative_area_id = c.administrative_area_id where a.administrative_area_id is null) then
        raise exception 'GEO047_POST_ORPHAN_CITY';
    end if;
    if exists (select 1 from geography.service_areas) then
        raise exception 'GEO047_POST_UNEXPECTED_SERVICE_AREA';
    end if;
end $$;

commit;

-- Observed shape after this run.
select 'countries=' || (select count(*) from geography.countries)
    || ' areas=' || (select count(*) from geography.administrative_areas)
    || ' cities=' || (select count(*) from geography.cities)
    || ' service_areas=' || (select count(*) from geography.service_areas)
    || ' country_names=' || (select count(*) from geography.country_name_translations)
    || ' area_names=' || (select count(*) from geography.administrative_area_name_translations)
    || ' city_names=' || (select count(*) from geography.city_name_translations);
