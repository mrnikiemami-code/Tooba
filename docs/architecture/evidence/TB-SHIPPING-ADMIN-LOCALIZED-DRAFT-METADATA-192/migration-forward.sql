START TRANSACTION;

CREATE TABLE shipping.authoring_draft_translations (
    draft_id uuid NOT NULL,
    language_id uuid NOT NULL,
    name character varying(200) NOT NULL,
    description character varying(2000),
    CONSTRAINT pk_authoring_draft_translations PRIMARY KEY (draft_id, language_id),
    CONSTRAINT ck_authoring_draft_translations_description_length CHECK (description IS NULL OR char_length(description) <= 2000),
    CONSTRAINT ck_authoring_draft_translations_name_length CHECK (char_length(btrim(name)) > 0 AND char_length(name) <= 200),
    CONSTRAINT fk_authoring_draft_translations_authoring_drafts_draft_id FOREIGN KEY (draft_id) REFERENCES shipping.authoring_drafts (draft_id) ON DELETE CASCADE
);

CREATE TABLE shipping.authoring_group_titles (
    group_id uuid NOT NULL,
    language_id uuid NOT NULL,
    draft_id uuid NOT NULL,
    title character varying(200) NOT NULL,
    CONSTRAINT pk_authoring_group_titles PRIMARY KEY (group_id, language_id),
    CONSTRAINT ck_authoring_group_titles_title_length CHECK (char_length(btrim(title)) > 0 AND char_length(title) <= 200),
    CONSTRAINT fk_authoring_group_titles_authoring_region_groups_group_id FOREIGN KEY (group_id) REFERENCES shipping.authoring_region_groups (group_id) ON DELETE CASCADE
);

INSERT INTO shipping.__ef_migrations_history (migration_id, product_version)
VALUES ('20261022081351_AuthoringPresentationTranslations', '8.0.11');

COMMIT;

