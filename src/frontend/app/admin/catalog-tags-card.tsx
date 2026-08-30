"use client";

import { AdminTagsPanel } from "./admin-tags-panel.tsx";
import {
  assignCategoryTag,
  assignProductTag,
  listCategoryTags,
  listProductTags,
  removeCategoryTag,
  removeProductTag,
} from "./catalog-tag-api.ts";

/**
 * کارت برچسب‌های تاکسونومی Product/Category — نه meta keywords.
 * پیاده‌سازی واقعی در AdminTagsPanel است.
 */
export function CatalogTagsCard({
  ownerKind,
  ownerId,
  canEdit,
}: {
  ownerKind: "product" | "category";
  ownerId: string;
  canEdit: boolean;
  locale?: string;
}) {
  return (
    <div data-testid="catalog-tags-card">
      <AdminTagsPanel
        ownerKind={ownerKind}
        ownerId={ownerId}
        canEdit={canEdit}
        testIdPrefix={ownerKind === "product" ? "product-tags" : "category-tags"}
        loadAssigned={
          ownerKind === "product"
            ? (id) => listProductTags(id, "fa-IR")
            : (id) => listCategoryTags(id, "fa-IR")
        }
        assignTag={
          ownerKind === "product"
            ? (id, tagId) => assignProductTag(id, tagId)
            : (id, tagId) => assignCategoryTag(id, tagId)
        }
        removeTag={
          ownerKind === "product"
            ? (id, tagId) => removeProductTag(id, tagId)
            : (id, tagId) => removeCategoryTag(id, tagId)
        }
      />
    </div>
  );
}
