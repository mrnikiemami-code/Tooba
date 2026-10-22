using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1 — executed PostgreSQL negatives for the TWO new
/// owner-local presentation tables, plus retained-text preservation across reorder and rate edits.
/// </summary>
/// <remarks>
/// <para>
/// The target is the same parsed, task-owned disposable instance
/// (<see cref="ShippingRelationalTestTarget"/>): 127.0.0.1:55432 / shipping_tb190r1 / tb190r1. A missing or
/// mismatched target throws (fail closed); no fact is skipped or faked.
/// </para>
/// <para>
/// Every rejected raw statement runs in its OWN transaction and is rolled back, because an aborted
/// statement poisons the rest of its transaction. Assertions name the exact SQLSTATE and the exact owned
/// constraint/column, so "something threw" is never accepted as proof.
/// </para>
/// <para>
/// Migration/model presence is NOT proof of these negatives; only these executions are.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringPresentationStorageTests
{
    private const int NameMax = 200;
    private const int DescriptionMax = 2000;
    private const int TitleMax = 200;

    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveTargetVerified;

    [Fact]
    public async Task Raw_storage_rejects_duplicate_foreign_null_blank_and_over_length_presentation_rows()
    {
        await using var context = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb192r1-raw-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(context).CreateAsync(
            owner,
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)])],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var groupId = created.Value.RegionGroups.Single().GroupId;
        var languageId = Guid.NewGuid();

        // A valid Unicode row is accepted on BOTH new tables; the transaction is rolled back so the seeded
        // draft stays the single baseline for the negatives below.
        await AssertRawAcceptedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, @description)",
            ("draft", draftId),
            ("language", languageId),
            ("name", "ارسال سریع — Доставка"),
            ("description", "توضیح چندزبانه"));
        await AssertRawAcceptedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, @title)",
            ("group", groupId),
            ("language", languageId),
            ("draft", draftId),
            ("title", "گروه تهران"));

        // A committed pair proves the same Unicode survives a NEW-context readback (no transliteration).
        await using (var accepted = await NewContextAsync())
        {
            await using var transaction = await accepted.Database.BeginTransactionAsync();
            await ExecuteAsync(
                accepted,
                transaction,
                "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
                + "VALUES (@draft, @language, @name, NULL)",
                ("draft", draftId),
                ("language", languageId),
                ("name", "ارسال سریع"));
            await ExecuteAsync(
                accepted,
                transaction,
                "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
                + "VALUES (@group, @language, @draft, @title)",
                ("group", groupId),
                ("language", languageId),
                ("draft", draftId),
                ("title", "گروه تهران"));
            await transaction.CommitAsync();
        }

        await using (var readback = await NewContextAsync())
        {
            var stored = await readback.AuthoringDrafts
                .AsNoTracking()
                .Include(draft => draft.Translations)
                .Include(draft => draft.RegionGroups)
                .ThenInclude(group => group.Titles)
                .SingleAsync(draft => draft.DraftId == draftId);
            Assert.Equal("ارسال سریع", Assert.Single(stored.Translations).Name);
            Assert.Equal("گروه تهران", stored.RegionGroups.Single().Titles.Single().Title);
        }

        // Duplicate (draft,language) / (group,language): the composite PRIMARY KEY is the invariant.
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, NULL)",
            "23505",
            "pk_authoring_draft_translations",
            null,
            ("draft", draftId),
            ("language", languageId),
            ("name", "دوباره"));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, @title)",
            "23505",
            "pk_authoring_group_titles",
            null,
            ("group", groupId),
            ("language", languageId),
            ("draft", draftId),
            ("title", "دوباره"));

        // Invalid own parent ids only: there is NO cross-schema Localization foreign key to violate.
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, NULL)",
            "23503",
            "fk_authoring_draft_translations_authoring_drafts_draft_id",
            null,
            ("draft", Guid.NewGuid()),
            ("language", Guid.NewGuid()),
            ("name", "یتیم"));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, @title)",
            "23503",
            "fk_authoring_group_titles_authoring_region_groups_group_id",
            null,
            ("group", Guid.NewGuid()),
            ("language", Guid.NewGuid()),
            ("draft", draftId),
            ("title", "یتیم"));

        // NOT NULL and the owned length/blank CHECKs, each with its exact identity.
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, NULL, NULL)",
            "23502",
            null,
            "name",
            ("draft", draftId),
            ("language", Guid.NewGuid()));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, NULL)",
            "23514",
            "ck_authoring_draft_translations_name_length",
            null,
            ("draft", draftId),
            ("language", Guid.NewGuid()),
            ("name", "   "));
        // Over-length text is rejected by the owned character varying bounds (SQLSTATE 22001) before the
        // CHECK can even be evaluated; the limit is the same 200/2000/200 value the domain enforces.
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, NULL)",
            "22001",
            null,
            null,
            ("draft", draftId),
            ("language", Guid.NewGuid()),
            ("name", new string('ن', NameMax + 1)));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, @description)",
            "22001",
            null,
            null,
            ("draft", draftId),
            ("language", Guid.NewGuid()),
            ("name", "معتبر"),
            ("description", new string('d', DescriptionMax + 1)));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, NULL)",
            "23502",
            null,
            "title",
            ("group", groupId),
            ("language", Guid.NewGuid()),
            ("draft", draftId));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, @title)",
            "23514",
            "ck_authoring_group_titles_title_length",
            null,
            ("group", groupId),
            ("language", Guid.NewGuid()),
            ("draft", draftId),
            ("title", "   "));
        await AssertRawRejectedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, @title)",
            "22001",
            null,
            null,
            ("group", groupId),
            ("language", Guid.NewGuid()),
            ("draft", draftId),
            ("title", new string('t', TitleMax + 1)));

        // At-limit values are accepted (the boundary is not off by one).
        await AssertRawAcceptedAsync(
            context,
            "INSERT INTO shipping.authoring_draft_translations (draft_id, language_id, name, description) "
            + "VALUES (@draft, @language, @name, @description)",
            ("draft", draftId),
            ("language", Guid.NewGuid()),
            ("name", new string('ن', NameMax)),
            ("description", new string('d', DescriptionMax)));
        await AssertRawAcceptedAsync(
            context,
            "INSERT INTO shipping.authoring_group_titles (group_id, language_id, draft_id, title) "
            + "VALUES (@group, @language, @draft, @title)",
            ("group", groupId),
            ("language", Guid.NewGuid()),
            ("draft", draftId),
            ("title", new string('t', TitleMax)));
    }

    [Fact]
    public async Task Retained_group_reorder_and_a_rate_edit_keep_the_exact_method_and_group_text()
    {
        var fa = Guid.Parse("01900000-0000-7000-8000-00000000cc01");
        var en = Guid.Parse("01900000-0000-7000-8000-00000000cc02");

        await using var seed = await NewContextAsync();
        var owner = ShippingAuthoringOwnerScope.ForStore("tb192r1-ret-" + Guid.NewGuid().ToString("N"));
        var created = await new ShippingAuthoringDraftDirectory(seed).CreateAsync(
            owner,
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Tehran)]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Isfahan)]),
            ],
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        var draftId = created.Value.DraftId;
        var first = created.Value.RegionGroups.OrderBy(group => group.Order).First().GroupId;
        var second = created.Value.RegionGroups.OrderBy(group => group.Order).Last().GroupId;

        var rates = await new ShippingAuthoringDraftDirectory(seed).ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            first,
            1,
            [RowInput(null, 0, 1000, 2500m)],
            "IRR",
            CancellationToken.None);
        Assert.True(rates.IsSuccess, rates.IsFailure ? rates.Errors[0].Code : null);

        var text = await new ShippingAuthoringDraftDirectory(seed).ReplacePresentationAsync(
            owner,
            draftId,
            2,
            [
                new ShippingAuthoringDraft.MethodTranslationInput(fa, "ارسال سریع", "یک روزه"),
                new ShippingAuthoringDraft.MethodTranslationInput(en, "Express post", "Next day"),
            ],
            [
                new ShippingAuthoringDraft.GroupTitleInput(first, fa, "گروه تهران"),
                new ShippingAuthoringDraft.GroupTitleInput(second, en, "Isfahan group"),
            ],
            new HashSet<Guid> { fa, en },
            CancellationToken.None);
        Assert.True(text.IsSuccess, text.IsFailure ? text.Errors[0].Code : null);

        // A retained group REORDER keeps ids, so both the method text and every group title survive it.
        var reordered = await new ShippingAuthoringDraftDirectory(seed).ReplaceRegionGroupsAsync(
            owner,
            draftId,
            3,
            [
                new ShippingAuthoringRegionGroupInput(second, [Node(Isfahan)]),
                new ShippingAuthoringRegionGroupInput(first, [Node(Tehran)]),
            ],
            CancellationToken.None);
        Assert.True(reordered.IsSuccess, reordered.IsFailure ? reordered.Errors[0].Code : null);
        Assert.Equal(1, reordered.Value.FindRegionGroup(first)!.Order);

        // A later rate edit on the same group touches only rate rows/currency and keeps the text.
        var afterReorder = await new ShippingAuthoringDraftDirectory(seed).ReplaceGroupRateRowsAsync(
            owner,
            draftId,
            first,
            4,
            [RowInput(null, 0, 1500, 3900m)],
            "IRR",
            CancellationToken.None);
        Assert.True(afterReorder.IsSuccess, afterReorder.IsFailure ? afterReorder.Errors[0].Code : null);

        await using var verification = await NewContextAsync();
        var stored = await verification.AuthoringDrafts
            .AsNoTracking()
            .Include(draft => draft.Translations)
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Titles)
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.RateRows)
            .SingleAsync(draft => draft.DraftId == draftId);

        Assert.Equal(5, stored.Revision);
        var translations = stored.Translations.ToDictionary(translation => translation.LanguageId);
        Assert.Equal("ارسال سریع", translations[fa].Name);
        Assert.Equal("یک روزه", translations[fa].Description);
        Assert.Equal("Express post", translations[en].Name);
        Assert.Equal("Next day", translations[en].Description);

        var titles = stored.RegionGroups
            .SelectMany(group => group.Titles)
            .Select(title => (title.GroupId, title.LanguageId, title.Title))
            .ToArray();
        Assert.Equal(2, titles.Length);
        Assert.Contains((first, fa, "گروه تهران"), titles);
        Assert.Contains((second, en, "Isfahan group"), titles);

        // The reorder really happened and the rate edit really landed: the retention is not a stale read.
        Assert.Equal(1, stored.RegionGroups.Single(group => group.GroupId == first).Order);
        var firstGroup = stored.RegionGroups.Single(group => group.GroupId == first);
        Assert.Equal(3900m, Assert.Single(firstGroup.RateRows).FreightAmount);
        Assert.Equal("IRR", firstGroup.CurrencyCode);
    }

    private static async Task AssertRawRejectedAsync(
        ShippingDbContext context,
        string sql,
        string expectedSqlState,
        string? expectedConstraint,
        string? expectedColumn,
        params (string Name, object? Value)[] parameters)
    {
        var connection = await OpenAsync(context);
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = BuildCommand(connection, transaction, sql, parameters);
        var failure = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(expectedSqlState, failure.SqlState);
        if (expectedConstraint is not null)
        {
            Assert.Equal(expectedConstraint, failure.ConstraintName);
        }

        if (expectedColumn is not null)
        {
            Assert.Equal(expectedColumn, failure.ColumnName);
        }

        await transaction.RollbackAsync();
    }

    private static async Task AssertRawAcceptedAsync(
        ShippingDbContext context,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        var connection = await OpenAsync(context);
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = BuildCommand(connection, transaction, sql, parameters);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
        await transaction.RollbackAsync();
    }

    private static async Task ExecuteAsync(
        ShippingDbContext context,
        IDbContextTransaction transaction,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await using var command = BuildCommand(
            connection,
            (NpgsqlTransaction)transaction.GetDbTransaction(),
            sql,
            parameters);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private static async Task<NpgsqlConnection> OpenAsync(ShippingDbContext context)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    private static NpgsqlCommand BuildCommand(
        NpgsqlConnection connection,
        DbTransaction? transaction,
        string sql,
        (string Name, object? Value)[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection, (NpgsqlTransaction?)transaction);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    private static async Task<ShippingDbContext> NewContextAsync()
    {
        // Parsed, test-owned target: exact loopback host/port/database/role. A missing or mismatched
        // configuration throws instead of skipping or faking the relational proof.
        var target = ShippingRelationalTestTarget.FromEnvironment();
        var builder = new DbContextOptionsBuilder<ShippingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
            ShippingDbContext.Schema,
            typeof(ShippingDbContext));
        var context = new ShippingDbContext(builder.Options);

        await MigrationGate.WaitAsync();
        try
        {
            // Prove the live target before any write, so the parsed string cannot mask a different server.
            if (!_liveTargetVerified)
            {
                var facts = await context.Database
                    .SqlQueryRaw<string>("SELECT (current_database() || '|' || current_user) AS \"Value\"")
                    .SingleAsync();
                var parts = facts.Split('|');
                Assert.Equal(ShippingRelationalTestTarget.ExpectedDatabase, parts[0]);
                Assert.Equal(ShippingRelationalTestTarget.ExpectedUser, parts[1]);
                _liveTargetVerified = true;
            }

            if (!_migrated)
            {
                await context.GetService<IMigrator>().MigrateAsync();
                _migrated = true;
            }
        }
        finally
        {
            MigrationGate.Release();
        }

        return context;
    }

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.AdministrativeArea, id);

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput RowInput(
        Guid? rateRowId,
        long? minWeightGrams,
        long? maxWeightGrams,
        decimal freightAmount,
        decimal packagingAmount = 0m,
        int minDeliveryDays = 1,
        int maxDeliveryDays = 3)
        => new(
            rateRowId,
            minWeightGrams,
            maxWeightGrams,
            null,
            null,
            null,
            null,
            freightAmount,
            packagingAmount,
            minDeliveryDays,
            maxDeliveryDays);
}
