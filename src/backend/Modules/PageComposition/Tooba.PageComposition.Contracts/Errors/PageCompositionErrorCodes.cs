namespace Tooba.PageComposition.Contracts.Errors;

/// <summary>Stable semantic error codes owned by PageComposition.</summary>
public static class PageCompositionErrorCodes
{
    /// <summary>Tenant could not be resolved for Page Composition.</summary>
    public const string TenantMissing = "page-composition.tenant.missing";

    /// <summary>Page section was not found.</summary>
    public const string SectionMissing = "page-composition.section.missing";

    /// <summary>Section type was rejected by the approved catalog.</summary>
    public const string SectionTypeRejected = "page-composition.section-type.rejected";

    /// <summary>Section configuration was rejected.</summary>
    public const string ConfigRejected = "page-composition.config.rejected";

    /// <summary>Mutation was rejected by domain rules.</summary>
    public const string MutationRejected = "page-composition.mutation.rejected";

    /// <summary>Section type transport field is required.</summary>
    public const string SectionTypeRequired = "page-composition.sectionType.required";

    /// <summary>Section ids transport field is required.</summary>
    public const string SectionIdsRequired = "page-composition.sectionIds.required";

    /// <summary>Section id transport field is required.</summary>
    public const string SectionIdRequired = "page-composition.sectionId.required";
}
