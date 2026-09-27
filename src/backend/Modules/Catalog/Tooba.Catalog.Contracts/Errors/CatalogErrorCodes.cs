namespace Tooba.Catalog.Contracts.Errors;

/// <summary>Stable Catalog semantic error codes for HTTP presentation.</summary>
public static class CatalogErrorCodes
{
    /// <summary>Invalid global quantity rounding mode on admin settings write.</summary>
    public const string QuantityRoundingInvalid = "quantity.rounding.invalid";

    /// <summary>Requested unit of measure was not found.</summary>
    public const string UnitMissing = "unit.missing";

    /// <summary>Unit dimension string is not a known enum value.</summary>
    public const string UnitDimensionInvalid = "unit.dimension.invalid";

    /// <summary>Unit code already exists.</summary>
    public const string UnitCodeDuplicate = "unit.code.duplicate";

    /// <summary>Translation LanguageId is not in the language registry.</summary>
    public const string UnitLanguageUnknown = "unit.language.unknown";

    /// <summary>Tag create input is invalid (e.g. missing localized name).</summary>
    public const string TagInvalid = "catalog.tag.invalid";

    /// <summary>Explicit tag code already exists.</summary>
    public const string TagCodeDuplicate = "catalog.tag.code.duplicate";

    /// <summary>Requested tag was not found.</summary>
    public const string TagMissing = "catalog.tag.missing";

    /// <summary>Tag already assigned to product or category.</summary>
    public const string TagAssignDuplicate = "catalog.tag.assign.duplicate";

    /// <summary>Referenced product was not found for tag assignment.</summary>
    public const string TagProductMissing = "catalog.tag.product.missing";

    /// <summary>Referenced category was not found for tag assignment.</summary>
    public const string TagCategoryMissing = "catalog.tag.category.missing";

    /// <summary>Referenced category was not found for MegaMenu Admin operations.</summary>
    public const string MegaMenuCategoryMissing = "catalog.megamenu.category.missing";

    /// <summary>MegaMenu presentation tree placement rule violated.</summary>
    public const string MegaMenuPlacementInvalid = "catalog.megamenu.placement.invalid";

    /// <summary>Cannot remove a MegaMenu item that still has presentation children.</summary>
    public const string MegaMenuRemoveHasChildren = "catalog.megamenu.remove.has_children";

    /// <summary>Referenced category was not found for Facet operations.</summary>
    public const string FacetCategoryMissing = "catalog.facet.category.missing";

    /// <summary>Referenced attribute definition was not found for Facet operations.</summary>
    public const string FacetDefinitionMissing = "catalog.facet.definition.missing";

    /// <summary>Definition is not present in the category effective schema.</summary>
    public const string FacetSchemaMissing = "catalog.facet.schema.missing";

    /// <summary>Only filterable effective-schema attributes may be configured as facets.</summary>
    public const string FacetNotFilterable = "catalog.facet.not_filterable";

    /// <summary>Facet display type is incompatible with the attribute ValueKind.</summary>
    public const string FacetDisplayTypeInvalid = "catalog.facet.display_type.invalid";

    /// <summary>Local facet override row was not found for remove.</summary>
    public const string FacetOverrideMissing = "catalog.facet.override.missing";

    /// <summary>Reorder list is incomplete, duplicate, or mismatched vs local facet set.</summary>
    public const string FacetReorderInvalid = "catalog.facet.reorder.invalid";

    /// <summary>Requested category was not found.</summary>
    public const string CategoryMissing = "catalog.category.missing";

    /// <summary>Category input or business precondition is invalid.</summary>
    public const string CategoryInvalid = "catalog.category.invalid";

    /// <summary>Category slug is empty or invalid after normalization.</summary>
    public const string CategorySlugInvalid = "catalog.category.slug.invalid";

    /// <summary>Category slug already used by another category for the locale.</summary>
    public const string CategorySlugDuplicate = "catalog.category.slug.duplicate";

    /// <summary>Referenced parent category was not found.</summary>
    public const string CategoryParentMissing = "catalog.category.parent.missing";

    /// <summary>Category cannot be its own parent.</summary>
    public const string CategorySelfParent = "catalog.category.parent.self";

    /// <summary>Category cannot be moved under its own descendant.</summary>
    public const string CategoryDescendantParent = "catalog.category.parent.descendant";

    /// <summary>Category hierarchy would exceed max depth of 3.</summary>
    public const string CategoryMaxDepth = "catalog.category.depth.max";

    /// <summary>Reorder list must match the exact sibling set under the parent.</summary>
    public const string CategoryReorderInvalid = "catalog.category.reorder.invalid";

    /// <summary>ExpectedUpdatedAt concurrency token does not match.</summary>
    public const string CategoryConcurrencyConflict = "catalog.category.concurrency.conflict";

    /// <summary>Storefront/admin category route resolve input is invalid.</summary>
    public const string CategoryRouteInvalid = "catalog.category.route.invalid";

    /// <summary>Category route locale+slug could not be resolved.</summary>
    public const string CategoryRouteMissing = "catalog.category.route.missing";

    /// <summary>Requested attribute definition was not found.</summary>
    public const string AttributeMissing = "catalog.attribute.missing";

    /// <summary>Attribute definition input or business precondition is invalid.</summary>
    public const string AttributeInvalid = "catalog.attribute.invalid";

    /// <summary>Attribute definition code already exists.</summary>
    public const string AttributeCodeDuplicate = "catalog.attribute.code.duplicate";

    /// <summary>Localized attribute definition name already exists for the locale.</summary>
    public const string AttributeNameDuplicate = "catalog.attribute.name.duplicate";

    /// <summary>ValueKind cannot be used as a variant axis.</summary>
    public const string AttributeVariantAxisValueKindInvalid = "catalog.attribute.variant_axis.value_kind.invalid";

    /// <summary>Variant-axis capability cannot be disabled while in use.</summary>
    public const string AttributeVariantAxisInUse = "catalog.attribute.variant_axis.in_use";

    /// <summary>Variant-axis assignment blocked because definition capability is disabled.</summary>
    public const string AttributeVariantAxisCapabilityDisabled = "catalog.attribute.variant_axis.capability_disabled";

    /// <summary>Category was not found for attribute-schema operations.</summary>
    public const string SchemaCategoryMissing = "catalog.schema.category.missing";

    /// <summary>Category attribute binding already exists.</summary>
    public const string SchemaBindingDuplicate = "catalog.schema.binding.duplicate";

    /// <summary>Category attribute binding was not found.</summary>
    public const string SchemaBindingMissing = "catalog.schema.binding.missing";

    /// <summary>Reorder list is incomplete, duplicate, or mismatched vs local binding set.</summary>
    public const string SchemaReorderInvalid = "catalog.schema.reorder.invalid";

    /// <summary>Generic category attribute-schema precondition failure.</summary>
    public const string SchemaInvalid = "catalog.schema.invalid";

    /// <summary>Referenced product was not found for product-attribute operations.</summary>
    public const string ProductMissing = "catalog.product.missing";

    /// <summary>Attribute definition exists but is inactive.</summary>
    public const string AttributeDefinitionInactive = "catalog.attribute.definition.inactive";

    /// <summary>Definition is not allowed by the product primary-category effective schema.</summary>
    public const string AttributeSchemaNotAllowed = "catalog.attribute.schema.not_allowed";

    /// <summary>Effective variant-axis definition cannot be stored as a normal product attribute.</summary>
    public const string AttributeVariantAxisOnProductForbidden = "catalog.attribute.variant_axis.on_product_forbidden";

    /// <summary>Enumeration value requires an option id.</summary>
    public const string AttributeEnumOptionRequired = "catalog.attribute.enum_option.required";

    /// <summary>Enumeration option does not belong to the definition.</summary>
    public const string AttributeEnumOptionMismatch = "catalog.attribute.enum_option.mismatch";

    /// <summary>Enumeration option is inactive.</summary>
    public const string AttributeEnumOptionInactive = "catalog.attribute.enum_option.inactive";

    /// <summary>Enumeration option payload is malformed (e.g. multivalue parse).</summary>
    public const string AttributeEnumOptionInvalid = "catalog.attribute.enum_option.invalid";

    /// <summary>Attribute raw/canonical value failed canonicalization.</summary>
    public const string AttributeValueInvalid = "catalog.attribute.value.invalid";

    /// <summary>Attribute value violates definition validation bounds.</summary>
    public const string AttributeValidationBounds = "catalog.attribute.validation.bounds";

    /// <summary>Clearing a required non-axis schema field is not allowed.</summary>
    public const string AttributeClearRequiredForbidden = "catalog.attribute.clear.required_forbidden";

    /// <summary>Non-enumeration attribute value is empty.</summary>
    public const string AttributeValueEmpty = "catalog.attribute.value.empty";

    /// <summary>Product variant-axis list contains duplicate definition ids.</summary>
    public const string VariantAxesDuplicate = "catalog.variant.axes.duplicate";

    /// <summary>Selected axis is not enabled as a variant axis in the effective schema.</summary>
    public const string VariantAxisSchemaNotEnabled = "catalog.variant.axis.schema_not_enabled";

    /// <summary>Operation requires effective variant axes but none are available.</summary>
    public const string VariantEffectiveAxesMissing = "catalog.variant.effective_axes.missing";

    /// <summary>Desired combination count exceeds the safe MaxVariantCombinations cap.</summary>
    public const string VariantCombinationLimitExceeded = "catalog.variant.combination.limit_exceeded";

    /// <summary>Variant patch targets a variant id that is not on the product.</summary>
    public const string VariantPatchTargetMissing = "catalog.variant.patch.target_missing";

    /// <summary>Variant patch Status string is not a valid CatalogPublicationStatus.</summary>
    public const string VariantPatchStatusInvalid = "catalog.variant.patch.status_invalid";

    /// <summary>Requested default variant id is not on the product.</summary>
    public const string VariantDefaultMissing = "catalog.variant.default.missing";

    /// <summary>Archived variant cannot be selected as default.</summary>
    public const string VariantArchivedCannotBeDefault = "catalog.variant.default.archived_forbidden";

    /// <summary>Workspace variant create missing axes.</summary>
    public const string WorkspaceVariantAxesMissing = "workspace.variant.axes.missing";

    /// <summary>Workspace variant create rejected (domain/IOE parity).</summary>
    public const string WorkspaceVariantCreateRejected = "workspace.variant.create.rejected";

    /// <summary>Workspace variant not found on product.</summary>
    public const string WorkspaceVariantMissing = "workspace.variant.missing";

    /// <summary>Workspace variant status string invalid.</summary>
    public const string WorkspaceVariantStatusInvalid = "workspace.variant.status.invalid";

    /// <summary>Hard delete blocked because Offer references product variants; product soft-archived.</summary>
    public const string WorkspaceProductDeleteReferenced = "workspace.product.delete.referenced";

    /// <summary>Workspace product create rejected title empty.</summary>
    public const string WorkspaceProductTitleMissing = "workspace.product.title.missing";

    /// <summary>Workspace product create missing category id.</summary>
    public const string WorkspaceProductCategoryMissing = "workspace.product.category.missing";

    /// <summary>Workspace product create category id not found.</summary>
    public const string WorkspaceProductCategoryInvalid = "workspace.product.category.invalid";

    /// <summary>Workspace product create rejected (domain/IOE parity).</summary>
    public const string WorkspaceProductCreateRejected = "workspace.product.create.rejected";

    /// <summary>Primary slug missing when editing a non-primary locale.</summary>
    public const string WorkspaceProductSlugMissing = "workspace.product.slug.missing";

    /// <summary>Quantity policy update rejected (domain gate).</summary>
    public const string WorkspaceQuantityRejected = "workspace.quantity.rejected";

    /// <summary>
    /// Product category assignment rejected because target is not Level 3.
    /// Canonical code matches <c>CatalogCategoryTreeRules.AssignmentLevelInvalidErrorCode</c>.
    /// </summary>
    public const string CategoryAssignmentLevelInvalid = "catalog.category.assignment.level.invalid";

    /// <summary>Category-change preview/replace precondition failed (non-assignment-level).</summary>
    public const string CategoryChangeInvalid = "catalog.category_change.invalid";

    /// <summary>Primary category change needs explicit ConfirmSchemaImpact when attrs/variants exist.</summary>
    public const string WorkspaceProductCategorySchemaImpact = "workspace.product.category.schema-impact";

    /// <summary>Primary category assign/replace rejected (non-level domain gate).</summary>
    public const string WorkspaceProductCategoryAssignRejected = "workspace.product.category.assign.rejected";

    /// <summary>Brand id not found for product brand assign.</summary>
    public const string WorkspaceProductBrandInvalid = "workspace.product.brand.invalid";

    /// <summary>Additional category is already the product primary.</summary>
    public const string CategoryAssignmentDuplicatePrimary = "catalog.category.assignment.duplicate_primary";

    /// <summary>Additional category already assigned.</summary>
    public const string CategoryAssignmentDuplicate = "catalog.category.assignment.duplicate";

    /// <summary>Additional category assign rejected for other business reasons.</summary>
    public const string CategoryAssignmentInvalid = "catalog.category.assignment.invalid";

    /// <summary>Cannot remove the primary category via additional-remove.</summary>
    public const string CategoryAssignmentCannotRemovePrimary = "catalog.category.assignment.cannot_remove_primary";

    /// <summary>Additional category link was not found.</summary>
    public const string CategoryAssignmentMissing = "catalog.category.assignment.missing";

    /// <summary>expectedUpdatedAt query missing on additional-category DELETE.</summary>
    public const string CategoryAssignmentStale = "catalog.category.assignment.stale";

    /// <summary>Workspace product was not found (preserved Product Workspace media code).</summary>
    public const string WorkspaceProductMissing = "workspace.product.missing";

    /// <summary>Workspace catalog edit denied (e.g. X-Tooba-Workspace-Scope=view).</summary>
    public const string WorkspacePermissionDenied = "workspace.permission.denied";

    /// <summary>Attach body MediaAssetId was empty.</summary>
    public const string WorkspaceMediaAssetMissing = "workspace.media.asset.missing";

    /// <summary>Attach existing media reference rejected (duplicate or other).</summary>
    public const string WorkspaceMediaAttachRejected = "workspace.media.attach.rejected";

    /// <summary>Generated placeholder media attach rejected.</summary>
    public const string WorkspaceMediaPlaceholderRejected = "workspace.media.placeholder.rejected";

    /// <summary>Reorder requested but product has no media.</summary>
    public const string WorkspaceMediaEmpty = "workspace.media.empty";

    /// <summary>Reorder list is not an exact match of current media set.</summary>
    public const string WorkspaceMediaOrderInvalid = "workspace.media.order.invalid";

    /// <summary>Reorder rejected for other business reasons.</summary>
    public const string WorkspaceMediaOrderRejected = "workspace.media.order.rejected";

    /// <summary>Primary/patch/detach target media reference missing on product.</summary>
    public const string WorkspaceMediaMissing = "workspace.media.missing";

    /// <summary>Optimistic concurrency conflict on catalog product update.</summary>
    public const string WorkspaceCatalogStale = "workspace.catalog.stale";

    /// <summary>Product slug already used by another product in the tenant.</summary>
    public const string WorkspaceProductSlugDuplicate = "workspace.product.slug.duplicate";

    /// <summary>Product slug invalid or cannot be derived from product name.</summary>
    public const string WorkspaceProductSlugInvalid = "workspace.product.slug.invalid";

    /// <summary>Product SEO update rejected for other business reasons.</summary>
    public const string WorkspaceProductSeoRejected = "workspace.product.seo.rejected";

    /// <summary>Product publish rejected (readiness / archived / domain gate).</summary>
    public const string WorkspaceProductPublishRejected = "workspace.product.publish.rejected";

    /// <summary>Product unpublish rejected.</summary>
    public const string WorkspaceProductUnpublishRejected = "workspace.product.unpublish.rejected";

    /// <summary>Product archive rejected.</summary>
    public const string WorkspaceProductArchiveRejected = "workspace.product.archive.rejected";

    /// <summary>Product restore-from-archive rejected.</summary>
    public const string WorkspaceProductRestoreRejected = "workspace.product.restore.rejected";

    /// <summary>Checkout-abuse PUT missing required fields (Host parity).</summary>
    public const string CheckoutAbuseInvalid = "settings.checkout_abuse.invalid";

    /// <summary>Max open unpaid orders out of domain range.</summary>
    public const string CheckoutAbuseMaxOpenUnpaidInvalid = "settings.max_open_unpaid.invalid";

    /// <summary>Reservation commit window minutes out of domain range.</summary>
    public const string CheckoutAbuseWindowInvalid = "settings.reservation_commit_window.invalid";

    /// <summary>Max checkout commits out of domain range.</summary>
    public const string CheckoutAbuseMaxCommitsInvalid = "settings.max_checkout_commits.invalid";

    /// <summary>Cart persistence hours out of range (Host: cart.persistence.invalid).</summary>
    public const string HoldPolicyCartPersistenceInvalid = "cart.persistence.invalid";

    /// <summary>Online payment hold hours out of range (Host: hold.online.invalid).</summary>
    public const string HoldPolicyOnlineInvalid = "hold.online.invalid";

    /// <summary>Manual initial hold hours out of range (Host: hold.manual_initial.invalid).</summary>
    public const string HoldPolicyManualInitialInvalid = "hold.manual_initial.invalid";

    /// <summary>Manual review hold hours out of range (Host: hold.manual_review.invalid).</summary>
    public const string HoldPolicyManualReviewInvalid = "hold.manual_review.invalid";

    /// <summary>Payment-method hold override hours out of range (Host: hold.method.invalid).</summary>
    public const string HoldPolicyMethodInvalid = "hold.method.invalid";

    /// <summary>Initial reservation minutes out of range (Host/Order: reservation.policy.initial.invalid).</summary>
    public const string HoldPolicyReservationInitialInvalid = "reservation.policy.initial.invalid";

    /// <summary>Retry reservation minutes out of range.</summary>
    public const string HoldPolicyReservationRetryInvalid = "reservation.policy.retry.invalid";

    /// <summary>Max reservation cycles out of range.</summary>
    public const string HoldPolicyReservationMaxInvalid = "reservation.policy.max.invalid";

    /// <summary>Store landing page was not found (preserved landing.page.missing).</summary>
    public const string LandingPageMissing = "landing.page.missing";

    /// <summary>Store menu was not found.</summary>
    public const string MenuMissing = "menu.missing";

    /// <summary>Menu title is required.</summary>
    public const string MenuTitleRequired = "menu.title.required";

    /// <summary>Menu key is required.</summary>
    public const string MenuKeyRequired = "menu.key.required";

    /// <summary>Menu key already exists for locale.</summary>
    public const string MenuKeyDuplicate = "menu.key.duplicate";

    /// <summary>Menu delete blocked by references.</summary>
    public const string MenuDeleteReferenced = "menu.delete.referenced";

    /// <summary>Menu item was not found.</summary>
    public const string MenuItemMissing = "menu.item.missing";

    /// <summary>Menu item label is required.</summary>
    public const string MenuItemLabelRequired = "menu.item.label.required";

    /// <summary>Menu item parent is invalid.</summary>
    public const string MenuItemParentInvalid = "menu.item.parent.invalid";

    /// <summary>Menu item tree cycle rejected.</summary>
    public const string MenuItemCycle = "menu.item.cycle";

    /// <summary>Menu item depth exceeded.</summary>
    public const string MenuItemDepth = "menu.item.depth";

    /// <summary>Menu item reorder list incomplete.</summary>
    public const string MenuItemReorderInvalid = "menu.item.reorder.invalid";

    /// <summary>Menu link type invalid.</summary>
    public const string MenuLinkInvalid = "menu.link.invalid";

    /// <summary>External URL required.</summary>
    public const string MenuUrlRequired = "menu.url.required";

    /// <summary>External URL unsafe.</summary>
    public const string MenuUrlUnsafe = "menu.url.unsafe";

    /// <summary>Internal target required.</summary>
    public const string MenuTargetRequired = "menu.target.required";

    /// <summary>Internal target missing in store.</summary>
    public const string MenuTargetMissing = "menu.target.missing";

    /// <summary>Disabled menu cannot be header.</summary>
    public const string MenuHeaderIneligible = "menu.header.ineligible";
}
