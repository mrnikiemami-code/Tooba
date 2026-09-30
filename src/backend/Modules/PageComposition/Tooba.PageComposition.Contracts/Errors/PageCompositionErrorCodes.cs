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
}
