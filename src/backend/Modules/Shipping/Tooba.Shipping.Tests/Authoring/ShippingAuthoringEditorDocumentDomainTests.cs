using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-199 domain proof for the existing-draft editor document: prospective (detached) new groups with
/// nested rates/titles, ONE root revision advance, honest no-op, and late-invalid rejection that touches
/// nothing.
/// </summary>
public sealed class ShippingAuthoringEditorDocumentDomainTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddHours(5);
    private static readonly DateTimeOffset Later = Now.AddMinutes(30);
    private static readonly Guid LanguageFa = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LanguageEn = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tehran = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Shiraz = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void One_document_composes_new_group_rate_titles_and_reorder_with_exactly_one_revision_advance()
    {
        var draft = Draft([Tehran]);
        var retained = Assert.Single(draft.RegionGroups).GroupId;

        var input = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                // Brand-new group: no client GUID, yet it carries rates AND titles in the SAME PUT.
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Isfahan)],
                    [Row(0, 1000, null, null, null, null, 1, 3)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(LanguageFa, "شمال")]),
                new ShippingAuthoringDraft.EditorGroupInput(
                    retained,
                    [Node(Tehran)],
                    [],
                    []),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(LanguageFa, "پست پیشتاز", null)],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);

        var plan = draft.PreflightEditorDocument(input, ActiveLanguages(), "IRR");
        Assert.True(plan.HasChanges);

        draft.ApplyEditorDocument(plan, Later, staged: false);

        Assert.Equal(2, draft.Revision);
        Assert.Equal(Later, draft.UpdatedAt);
        var ordered = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
        Assert.Equal(2, ordered.Length);

        // The retained group keeps its identity and stays retained; the new one is server-identified.
        Assert.Equal(retained, ordered[1].GroupId);
        Assert.NotEqual(Guid.Empty, ordered[0].GroupId);
        Assert.NotEqual(retained, ordered[0].GroupId);

        var created = ordered[0];
        Assert.Equal(0, created.Order);
        Assert.Equal("IRR", created.CurrencyCode);
        var row = Assert.Single(created.RateRows);
        Assert.Equal(1000, row.MaxWeightGrams);
        Assert.Equal("شمال", Assert.Single(created.Titles).Title);
        Assert.Equal("پست پیشتاز", Assert.Single(draft.Translations).Name);
    }

    [Fact]
    public void Late_invalid_row_after_earlier_valid_edits_leaves_the_whole_tree_unchanged()
    {
        var draft = Draft([Tehran]);
        var group = Assert.Single(draft.RegionGroups);
        draft.ReplaceGroupRateRows(group.GroupId, [Row(0, 1000, null, null, null, null, 1, 3)], "IRR", Now);

        var revisionBefore = draft.Revision;
        var updatedBefore = draft.UpdatedAt;
        var rowIdBefore = Assert.Single(group.RateRows).RateRowId;

        var input = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                // Valid earlier edit: a new title for the retained group.
                new ShippingAuthoringDraft.EditorGroupInput(
                    group.GroupId,
                    [Node(Tehran)],
                    [Row(0, 1000, null, null, null, null, 1, 3, rowIdBefore)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(LanguageFa, "مرکز")]),
                // Last child is invalid: a brand-new group whose interval is reversed.
                new ShippingAuthoringDraft.EditorGroupInput(
                    null,
                    [Node(Isfahan)],
                    [Row(5000, 1000, null, null, null, null, 3, 1)],
                    []),
            ],
            [],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);

        Assert.Throws<SemanticException>(
            () => draft.PreflightEditorDocument(input, ActiveLanguages(), "IRR"));

        Assert.Equal(revisionBefore, draft.Revision);
        Assert.Equal(updatedBefore, draft.UpdatedAt);
        Assert.Empty(group.Titles);
        Assert.Empty(draft.Translations);
        var row = Assert.Single(group.RateRows);
        Assert.Equal(rowIdBefore, row.RateRowId);
        Assert.Equal(1000, row.MaxWeightGrams);
    }

    [Fact]
    public void Identical_document_preserves_revision_and_timestamp_and_reports_no_changes()
    {
        var draft = Draft([Tehran]);
        var group = Assert.Single(draft.RegionGroups);
        draft.ReplaceGroupRateRows(group.GroupId, [Row(0, 1000, null, null, null, null, 1, 3)], "IRR", Now);
        var presentationPlan = draft.PreflightPresentation(
            [new ShippingAuthoringDraft.MethodTranslationInput(LanguageFa, "پست", "توضیح")],
            [],
            ActiveLanguages());
        draft.ApplyPresentation(presentationPlan, Now);
        var rowId = Assert.Single(group.RateRows).RateRowId;

        var revisionBefore = draft.Revision;
        var updatedBefore = draft.UpdatedAt;

        var input = new ShippingAuthoringDraft.EditorDocumentInput(
            [
                new ShippingAuthoringDraft.EditorGroupInput(
                    group.GroupId,
                    [Node(Tehran)],
                    [Row(0, 1000, null, null, null, null, 1, 3, rowId)],
                    [new ShippingAuthoringDraft.EditorGroupTitleInput(LanguageFa, null)]),
            ],
            [new ShippingAuthoringDraft.MethodTranslationInput(LanguageFa, "پست", "توضیح")],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);

        var plan = draft.PreflightEditorDocument(input, ActiveLanguages(), "IRR");
        Assert.False(plan.HasChanges);

        draft.ApplyEditorDocument(plan, Later, staged: false);

        Assert.Equal(revisionBefore, draft.Revision);
        Assert.Equal(updatedBefore, draft.UpdatedAt);
        Assert.Single(draft.Translations);
        Assert.Empty(group.Titles);
    }

    [Fact]
    public void Foreign_group_id_is_rejected_before_any_revision_change()
    {
        var draft = Draft([Tehran]);
        var revisionBefore = draft.Revision;

        var input = new ShippingAuthoringDraft.EditorDocumentInput(
            [new ShippingAuthoringDraft.EditorGroupInput(Guid.NewGuid(), [Node(Isfahan)], [], [])],
            [],
            false,
            null,
            null,
            false,
            null,
            null,
            false,
            null);

        var exception = Assert.Throws<SemanticException>(
            () => draft.PreflightEditorDocument(input, ActiveLanguages(), "IRR"));
        Assert.Equal(ShippingErrorCodes.AuthoringRegionPayloadInvalid, exception.Error.Code);
        Assert.Equal(revisionBefore, draft.Revision);
        Assert.Single(draft.RegionGroups);
    }

    private static ShippingAuthoringDraft Draft(IReadOnlyList<Guid> areas)
        => ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore("tb199-store"),
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, areas.Select(Node).ToArray())],
            Now);

    private static HashSet<Guid> ActiveLanguages() => [LanguageFa, LanguageEn];

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.AdministrativeArea, id);

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput Row(
        long? minWeight,
        long? maxWeight,
        int? minCount,
        int? maxCount,
        decimal? minAmount,
        decimal? maxAmount,
        int? minDays,
        int? maxDays,
        Guid? rateRowId = null,
        decimal? freight = 25000m,
        decimal? packaging = 5000m)
        => new(rateRowId, minWeight, maxWeight, minCount, maxCount, minAmount, maxAmount, freight, packaging, minDays, maxDays);
}
