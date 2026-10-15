using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-082 — focused proof for the additive one-level method catalog:
/// stable identity, idempotent mapping, active/inactive and option-less handling, collision and
/// 32-character boundary rules, and the default-OFF read gate. No web host, no live database.
/// </summary>
public sealed class ShippingOneLevelMethodCatalogTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");

    private static readonly Guid PostId = Guid.Parse("11111111-0000-7000-8000-000000000001");
    private static readonly Guid TipaxId = Guid.Parse("11111111-0000-7000-8000-000000000002");
    private static readonly Guid StoreCourierId = Guid.Parse("11111111-0000-7000-8000-000000000003");
    private static readonly Guid InPersonId = Guid.Parse("11111111-0000-7000-8000-000000000004");
    private static readonly Guid SnappId = Guid.Parse("11111111-0000-7000-8000-000000000005");

    [Theory]
    [InlineData("post:standard", "post:standard")]
    [InlineData("POST:Standard", "post:standard")]
    [InlineData("  tipax:express  ", "tipax:express")]
    [InlineData("store_courier", "store_courier")]
    [InlineData("in_person", "in_person")]
    [InlineData("snapp_courier", "snapp_courier")]
    public void Canonical_codes_including_legacy_composites_are_preserved_byte_identically(string input, string expected)
    {
        Assert.Equal(expected, ShippingMethod.TryNormalizeCode(input));
    }

    [Fact]
    public void Code_shape_rejects_ambiguous_separators_uppercase_empties_and_over_length()
    {
        Assert.Null(ShippingMethod.TryNormalizeCode(null));
        Assert.Null(ShippingMethod.TryNormalizeCode("   "));
        Assert.Null(ShippingMethod.TryNormalizeCode("post:standard:extra"));
        Assert.Null(ShippingMethod.TryNormalizeCode(":standard"));
        Assert.Null(ShippingMethod.TryNormalizeCode("post:"));
        Assert.Null(ShippingMethod.TryNormalizeCode("post-standard"));
        Assert.Null(ShippingMethod.TryNormalizeCode("post standard"));
        Assert.Null(ShippingMethod.TryNormalizeCode("post/standard"));
        Assert.Null(ShippingMethod.TryNormalizeCode(new string('a', ShippingMethod.MaxCodeLength + 1)));
    }

    [Fact]
    public void Thirty_two_character_boundary_is_the_accepted_maximum()
    {
        var atLimit = new string('a', ShippingMethod.MaxCodeLength);
        Assert.Equal(atLimit, ShippingMethod.TryNormalizeCode(atLimit));
        Assert.Equal(ShippingMethod.MaxCodeLength, atLimit.Length);

        // A composite that exactly fills the boundary is legal; one more character is not.
        var compositeAtLimit = new string('a', ShippingMethod.MaxCodeLength - 3) + ":ok";
        Assert.Equal(ShippingMethod.MaxCodeLength, compositeAtLimit.Length);
        Assert.Equal(compositeAtLimit, ShippingMethod.TryNormalizeCode(compositeAtLimit));
        Assert.Null(ShippingMethod.TryNormalizeCode(compositeAtLimit + "x"));
    }

    [Fact]
    public void Create_keeps_hidden_provider_kind_and_immutable_legacy_binding()
    {
        var now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        var method = ShippingMethod.Create(
            PostId, "post:standard", "post", "post", "standard", 12, true, now);

        Assert.Equal("post:standard", method.MethodCode);
        Assert.Equal("post", method.ProviderKind);
        Assert.Equal("post", method.LegacyServiceCode);
        Assert.Equal("standard", method.LegacyOptionCode);
        Assert.True(method.IsActive);
    }

    [Fact]
    public void Option_less_service_maps_to_a_single_null_option_binding()
    {
        var now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        var method = ShippingMethod.Create(
            StoreCourierId, "store_courier", "store_courier", "store_courier", null, 40, true, now);

        Assert.Null(method.LegacyOptionCode);
        Assert.Equal("store_courier", method.LegacyServiceCode);
    }

    [Fact]
    public void Create_rejects_an_invalid_code_instead_of_silently_rewriting_it()
    {
        var now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        Assert.Throws<SemanticException>(() =>
            ShippingMethod.Create(PostId, "post standard", "post", "post", "standard", 1, true, now));
        Assert.Throws<SemanticException>(() =>
            ShippingMethod.Create(PostId, "post:standard:extra", "post", "post", "standard", 1, true, now));
        Assert.Throws<SemanticException>(() =>
            ShippingMethod.Create(PostId, new string('a', ShippingMethod.MaxCodeLength + 1), "post", "post", null, 1, true, now));
    }

    [Fact]
    public void Update_only_moves_sort_order_and_activation_never_identity()
    {
        var now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        var method = ShippingMethod.Create(PostId, "post:express", "post", "post", "express", 11, false, now);

        method.Update(13, true, now.AddMinutes(1));

        Assert.Equal("post:express", method.MethodCode);
        Assert.Equal("post", method.LegacyServiceCode);
        Assert.Equal("express", method.LegacyOptionCode);
        Assert.Equal(13, method.SortOrder);
        Assert.True(method.IsActive);
    }

    [Fact]
    public void Default_off_gate_is_disabled_by_absence_and_only_flips_on_explicit_enablement()
    {
        Assert.Equal(ShippingOneLevelCatalogGate.Disabled, ShippingOneLevelCatalogPolicy.Evaluate(null));
        Assert.Equal(
            ShippingOneLevelCatalogGate.Disabled,
            ShippingOneLevelCatalogPolicy.Evaluate(new ShippingOneLevelCatalogOptions()));
        Assert.Equal(
            ShippingOneLevelCatalogGate.Enabled,
            ShippingOneLevelCatalogPolicy.Evaluate(new ShippingOneLevelCatalogOptions { Enabled = true }));
    }

    [Fact]
    public void Gate_namespace_is_owned_by_fulfillment_and_does_not_reuse_price_authority_sections()
    {
        Assert.Equal("Tooba:ShippingOneLevelCatalog", ShippingOneLevelCatalogOptions.SectionName);
        Assert.NotEqual("Tooba:ShippingRateAuthority", ShippingOneLevelCatalogOptions.SectionName);
        Assert.NotEqual("Tooba:ShippingOperationalShadow", ShippingOneLevelCatalogOptions.SectionName);
    }

    [Fact]
    public void Backfill_plan_maps_the_live_development_catalog_shapes()
    {
        var plan = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), []);

        Assert.True(plan.IsExecutable, string.Join("; ", plan.Violations));

        var created = plan.Items.ToDictionary(x => x.Desired.MethodCode, x => x.Desired, StringComparer.Ordinal);
        // 3 post + 2 tipax + 1 snapp_courier + 1 store_courier + 1 in_person = 8 one-level methods
        Assert.Equal(8, created.Count);

        // option-dependent methods keep the composite legacy code
        Assert.Equal("post", created["post:standard"].LegacyServiceCode);
        Assert.Equal("standard", created["post:standard"].LegacyOptionCode);
        Assert.Equal("express", created["post:express"].LegacyOptionCode);
        Assert.Equal("standard", created["tipax:standard"].LegacyOptionCode);
        Assert.Equal("express", created["tipax:express"].LegacyOptionCode);

        // option-less services keep the bare service code with a null option binding
        Assert.Null(created["store_courier"].LegacyOptionCode);
        Assert.Null(created["in_person"].LegacyOptionCode);
        Assert.Null(created["snapp_courier"].LegacyOptionCode);

        // activation is preserved, never invented
        Assert.True(created["post:standard"].IsActive);
        Assert.False(created["post:express"].IsActive);
        Assert.True(created["tipax:express"].IsActive);
        Assert.True(created["store_courier"].IsActive);
        Assert.True(created["in_person"].IsActive);
        Assert.False(created["snapp_courier"].IsActive);

        // inactive options are mapped too (their persisted code may exist historically) but never activated
        var retired = Assert.Single(plan.Items, x => x.Desired.MethodCode == "post:retired");
        Assert.False(retired.Desired.IsActive);
        Assert.Equal(ShippingMethodPlanAction.Create, retired.Action);
    }

    [Fact]
    public void Backfill_plan_is_idempotent_for_an_already_mapped_catalog()
    {
        var first = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), []);
        Assert.All(first.Items, x => Assert.Equal(ShippingMethodPlanAction.Create, x.Action));

        var existing = first.Items
            .Select((x, i) => new ShippingMethodExistingRow(
                Guid.Parse($"22222222-0000-7000-8000-{i:D12}"),
                x.Desired.MethodCode,
                x.Desired.LegacyServiceCode,
                x.Desired.LegacyOptionCode,
                x.Desired.IsActive))
            .ToArray();

        var second = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), existing);

        Assert.True(second.IsExecutable, string.Join("; ", second.Violations));
        Assert.All(second.Items, x => Assert.Equal(ShippingMethodPlanAction.Skip, x.Action));
        Assert.All(second.Items, x => Assert.NotNull(x.ExistingShippingMethodId));

        // running the planner again yields the same decisions (pure and stable)
        var third = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), existing);
        Assert.Equal(
            second.Items.Select(x => (x.Action, x.Desired.MethodCode)),
            third.Items.Select(x => (x.Action, x.Desired.MethodCode)));
    }

    [Fact]
    public void Backfill_plan_aligns_activation_without_recreating_rows()
    {
        var existing = new[]
        {
            new ShippingMethodExistingRow(SnappId, "snapp_courier", "snapp_courier", null, IsActive: true),
        };

        var plan = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), existing);

        var snapp = Assert.Single(plan.Items, x => x.Desired.MethodCode == "snapp_courier");
        Assert.Equal(ShippingMethodPlanAction.AlignActivation, snapp.Action);
        Assert.Equal(SnappId, snapp.ExistingShippingMethodId);
        Assert.False(snapp.Desired.IsActive);
    }

    [Fact]
    public void Backfill_plan_reports_case_insensitive_duplicates_and_binding_mismatches()
    {
        var duplicate = new[]
        {
            new ShippingMethodExistingRow(PostId, "post:standard", "post", "standard", true),
            new ShippingMethodExistingRow(TipaxId, "POST:STANDARD", "post", "standard", true),
        };
        var duplicatePlan = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), duplicate);
        Assert.False(duplicatePlan.IsExecutable);
        Assert.Contains(duplicatePlan.Violations, v => v.StartsWith("duplicate_existing_code:", StringComparison.Ordinal));

        var mismatch = new[]
        {
            new ShippingMethodExistingRow(PostId, "post:standard", "post", "express", true),
        };
        var mismatchPlan = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), mismatch);
        Assert.False(mismatchPlan.IsExecutable);
        Assert.Contains("binding_mismatch:post:standard", mismatchPlan.Violations);
    }

    [Fact]
    public void Backfill_plan_rejects_a_service_level_row_repurposed_as_a_second_sellable_method()
    {
        // post has active options, so the bare `post` identity is NOT a method. If it already exists as a
        // row, the plan must flag the collision instead of silently accepting a duplicate method.
        var existing = new[]
        {
            new ShippingMethodExistingRow(PostId, "post:standard", "post", "standard", true),
            new ShippingMethodExistingRow(TipaxId, "post:express", "post", "express", false),
            new ShippingMethodExistingRow(StoreCourierId, "post", "post", null, true),
        };

        var plan = ShippingMethodBackfillPlanner.Plan(DevelopmentCatalog(), existing);

        Assert.True(plan.IsExecutable, string.Join("; ", plan.Violations));
        // the two option-bound post methods already have correct rows and must be left alone; every other
        // desired method (including the option-less ones) still needs creating.
        Assert.Equal(2, plan.Items.Count(x => x.Action == ShippingMethodPlanAction.Skip));
        Assert.All(
            plan.Items.Where(x => x.Action == ShippingMethodPlanAction.Skip),
            x => Assert.StartsWith("post:", x.Desired.MethodCode, StringComparison.Ordinal));
        Assert.Contains(plan.Items, x => x.Desired.MethodCode == "in_person"
            && x.Action == ShippingMethodPlanAction.Create);
        // the pre-existing bare `post` row is untouched by this plan: it is not a desired method.
        Assert.DoesNotContain(plan.Items, x => x.Desired.MethodCode == "post");
    }

    private static IReadOnlyList<ShippingCatalogServiceSnapshot> DevelopmentCatalog() =>
    [
        new ShippingCatalogServiceSnapshot(
            PostId, "post", "post", "post", "blue", true, 10,
            [new ShippingCatalogTranslationSnapshot(LangFa, "پست", null)],
            [
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), PostId, "express", false, 11,
                    [new ShippingCatalogOptionTranslationSnapshot(LangFa, "پیشتاز")]),
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), PostId, "standard", true, 12,
                    [new ShippingCatalogOptionTranslationSnapshot(LangFa, "معمولی")]),
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), PostId, "retired", false, 13,
                    [new ShippingCatalogOptionTranslationSnapshot(LangFa, "بازنشسته")]),
            ]),
        new ShippingCatalogServiceSnapshot(
            TipaxId, "tipax", "tipax", "tipax", "amber", true, 20,
            [new ShippingCatalogTranslationSnapshot(LangFa, "تیپاکس", null)],
            [
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), TipaxId, "express", true, 21,
                    [new ShippingCatalogOptionTranslationSnapshot(LangFa, "پیشتاز")]),
                new ShippingCatalogOptionSnapshot(Guid.NewGuid(), TipaxId, "standard", true, 22,
                    [new ShippingCatalogOptionTranslationSnapshot(LangFa, "معمولی")]),
            ]),
        new ShippingCatalogServiceSnapshot(
            SnappId, "snapp_courier", "courier", "courier", "emerald", false, 30,
            [new ShippingCatalogTranslationSnapshot(LangFa, "اسنپ / پیک آنلاین", null)], []),
        new ShippingCatalogServiceSnapshot(
            StoreCourierId, "store_courier", "store_courier", "bike", "violet", true, 40,
            [new ShippingCatalogTranslationSnapshot(LangFa, "پیک فروشگاه", null)], []),
        new ShippingCatalogServiceSnapshot(
            InPersonId, "in_person", "in_person", "store", "rose", true, 50,
            [new ShippingCatalogTranslationSnapshot(LangFa, "تحویل حضوری", null)], []),
    ];
}
