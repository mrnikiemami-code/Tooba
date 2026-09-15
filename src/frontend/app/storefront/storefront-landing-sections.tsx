"use client";

import {
  adaptLandingSectionToComposition,
} from "../../lib/storefront-composition/landing-adapter.ts";
import {
  renderSharedLandingSection,
} from "../../lib/storefront-composition/shared-composition-renderer.tsx";
import type {
  LandingRenderContext,
  StorefrontLandingPage,
  StorefrontLandingSection,
} from "./storefront-landing-api.ts";
import { landingSectionSurfaceRole, surfaceRoleClass } from "../../lib/storefront-appearance/surface-role.ts";

export type { LandingRenderContext };

function parseConfig(raw: string): Record<string, unknown> {
  try {
    const value = JSON.parse(raw) as unknown;
    return value && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};
  } catch {
    return {};
  }
}

export function StorefrontLandingSections({
  page,
  context,
  preview = false,
}: {
  page: StorefrontLandingPage;
  context: LandingRenderContext;
  preview?: boolean;
}) {
  return (
    <div className="space-y-6 py-6 overflow-x-hidden" data-testid="storefront-landing-page" data-landing-slug={page.slug} data-landing-preview={preview ? "1" : "0"}>
      <h1 className="sr-only">{page.title}</h1>
      {page.sections.map((section) => {
        const rendered = renderLandingSection(section, context);
        if (!rendered) return null;
        const role = landingSectionSurfaceRole(section.sectionType);
        return (
          <div
            key={section.pageSectionId}
            className={surfaceRoleClass(role)}
            data-storefront-surface-role={role}
            data-landing-section-type={section.sectionType}
          >
            {rendered}
          </div>
        );
      })}
    </div>
  );
}

function renderLandingSection(section: StorefrontLandingSection, context: LandingRenderContext) {
  const config = parseConfig(section.config);
  switch (section.sectionType) {
    case "Hero":
    case "ProductCollection":
    case "CategoryGrid":
    case "BrandStrip":
    case "PromoBanner":
    case "ArticleList":
    case "Reviews":
    case "RichText":
    case "NavigationMenu": {
      const composition = adaptLandingSectionToComposition({
        pageSectionId: section.pageSectionId,
        sectionType: section.sectionType,
        displayOrder: section.sortOrder,
        config,
      });
      return renderSharedLandingSection({ section, composition, config, context });
    }
    default:
      return null;
  }
}
