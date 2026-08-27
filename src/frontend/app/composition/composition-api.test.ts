import assert from "node:assert/strict";
import test from "node:test";
import {
  DEFAULT_HOME_SECTION_ORDER,
  defaultHomeCompositionSections,
  mapAdminHomeComposition,
  mapHomeComposition,
  mapSectionCatalog,
  parseSectionDisplayConfig,
} from "./composition-api.ts";

test("mapHomeComposition maps PascalCase storefront payload", () => {
  const composition = mapHomeComposition({
    PageKey: "home",
    TenantId: "a0000000-0001-4000-8000-000000000001",
    Locale: null,
    VersionToken: 2,
    Sections: [
      {
        PageSectionId: "11111111-1111-4111-8111-111111111111",
        SectionType: "hero",
        DisplayOrder: 0,
        Variant: "default",
        ConfigurationJson: "{}",
      },
      {
        PageSectionId: "22222222-2222-4222-8222-222222222222",
        SectionType: "stories",
        DisplayOrder: 1,
        Variant: "default",
        ConfigurationJson: "{}",
      },
    ],
  });
  assert.ok(composition);
  assert.equal(composition?.sections.length, 2);
  assert.equal(composition?.sections[0]?.sectionType, "hero");
});

test("mapAdminHomeComposition includes visibility", () => {
  const composition = mapAdminHomeComposition({
    pageDefinitionId: "33333333-3333-4333-8333-333333333333",
    pageKey: "home",
    tenantId: "a0000000-0001-4000-8000-000000000001",
    locale: null,
    versionToken: 3,
    updatedAt: "2026-08-27T02:00:00Z",
    sections: [
      {
        pageSectionId: "44444444-4444-4444-8444-444444444444",
        sectionType: "brands",
        displayOrder: 0,
        isVisible: false,
        variant: "default",
        configurationJson: "{}",
      },
    ],
  });
  assert.equal(composition?.sections[0]?.isVisible, false);
});

test("mapSectionCatalog maps catalog entries", () => {
  const catalog = mapSectionCatalog({
    sectionTypes: [
      {
        sectionType: "hero",
        allowedVariants: ["default"],
        supportedConfigKeys: ["title"],
      },
    ],
    configSchemaMetadata: {
      title: ["string"],
    },
  });
  assert.equal(catalog?.sectionTypes[0]?.sectionType, "hero");
});

test("parseSectionDisplayConfig reads safe keys only", () => {
  assert.deepEqual(parseSectionDisplayConfig('{"title":"عنوان","href":"/offers"}'), {
    title: "عنوان",
    href: "/offers",
  });
  assert.deepEqual(parseSectionDisplayConfig("not-json"), {});
});

test("defaultHomeCompositionSections preserves canonical order", () => {
  const sections = defaultHomeCompositionSections();
  assert.equal(sections.length, DEFAULT_HOME_SECTION_ORDER.length);
  assert.deepEqual(
    sections.map((section) => section.sectionType),
    [...DEFAULT_HOME_SECTION_ORDER],
  );
});
