using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.Admin.Completeness.Commands.AddAdminOrderNote;
using Tooba.Order.Application.Admin.Completeness.Commands.DeleteAdminOrderNote;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;
using Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderInvoice;
using Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderOperationalHistory;
using Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderReceipt;
using Tooba.Order.Application.Admin.Completeness.Queries.ListAdminOrderNotes;
using Xunit;

namespace Tooba.Order.Tests.Application;

/// <summary>شش use case کامل‌بودن سفارش از مسیر واقعی <c>ISender</c>.</summary>
public sealed class AdminOrderCompletenessHandlerTests
{
    private static readonly Guid CheckoutId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly AdminOrderActor Actor = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public async Task List_notes_returns_store_rows()
    {
        var sender = BuildSender(out var store);
        await sender.Send(new AddAdminOrderNoteCommand(CheckoutId, Actor, "اول"));

        var result = await sender.Send(new ListAdminOrderNotesQuery(CheckoutId, Actor));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("اول", store.Notes[0].Body);
    }

    [Fact]
    public async Task List_notes_fails_semantically_when_checkout_is_missing()
    {
        var sender = BuildSender(out var store);
        store.Exists = false;

        var result = await sender.Send(new ListAdminOrderNotesQuery(CheckoutId, Actor));

        Assert.True(result.IsFailure);
        Assert.Equal(AdminOrderCompletenessErrors.Missing, result.FirstError!.Code);
    }

    [Fact]
    public async Task Add_note_trims_body_and_rejects_blank()
    {
        var sender = BuildSender(out var store);

        var added = await sender.Send(new AddAdminOrderNoteCommand(CheckoutId, Actor, "  یادداشت  "));
        Assert.True(added.IsSuccess);
        Assert.Equal("یادداشت", Assert.Single(store.AddedBodies));

        var blank = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            sender.Send(new AddAdminOrderNoteCommand(CheckoutId, Actor, "   ")));
        Assert.Contains(blank.Errors, e => e.ErrorCode == Tooba.Order.Application.Validation.OrderValidationCodes.NoteBodyRequired);
    }

    [Fact]
    public async Task History_query_rejects_out_of_range_paging_before_handler()
    {
        var sender = BuildSender(out _);

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            sender.Send(new GetAdminOrderOperationalHistoryQuery(CheckoutId, Actor, -3, 500)));

        Assert.Contains(ex.Errors, e => e.ErrorCode == Tooba.Order.Application.Validation.OrderValidationCodes.PageMin);
        Assert.Contains(ex.Errors, e => e.ErrorCode == Tooba.Order.Application.Validation.OrderValidationCodes.PageSizeRange);
    }

    [Fact]
    public async Task History_query_forwards_valid_paging_to_the_store()
    {
        var sender = BuildSender(out var store);

        var result = await sender.Send(new GetAdminOrderOperationalHistoryQuery(CheckoutId, Actor, 2, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, store.RecordedPage);
        Assert.Equal(20, store.RecordedPageSize);
    }

    [Theory]
    [InlineData(AdminOrderNoteDeleteOutcome.Deleted, true)]
    [InlineData(AdminOrderNoteDeleteOutcome.Forbidden, false)]
    [InlineData(AdminOrderNoteDeleteOutcome.NotFound, false)]
    public async Task Delete_note_maps_typed_outcome_without_message_matching(
        AdminOrderNoteDeleteOutcome outcome,
        bool expectSuccess)
    {
        var sender = BuildSender(out var store);
        store.DeleteOutcome = outcome;

        var result = await sender.Send(new DeleteAdminOrderNoteCommand(CheckoutId, Guid.NewGuid(), Actor));

        Assert.Equal(expectSuccess, result.IsSuccess);
        if (!expectSuccess)
        {
            Assert.Equal(AdminOrderCompletenessErrors.DeleteForbidden, result.FirstError!.Code);
        }
    }

    [Fact]
    public async Task History_query_fails_semantically_when_checkout_is_missing()
    {
        var sender = BuildSender(out var store);
        store.Exists = false;

        var result = await sender.Send(new GetAdminOrderOperationalHistoryQuery(CheckoutId, Actor, 1, 20));

        Assert.True(result.IsFailure);
        Assert.Equal(AdminOrderCompletenessErrors.Missing, result.FirstError!.Code);
    }

    [Fact]
    public async Task Invoice_and_receipt_queries_return_documents_or_unavailable_errors()
    {
        var sender = BuildSender(out var store);

        var invoice = await sender.Send(new GetAdminOrderInvoiceQuery(CheckoutId, Actor));
        var receipt = await sender.Send(new GetAdminOrderReceiptQuery(CheckoutId, Actor));
        Assert.True(invoice.IsSuccess);
        Assert.Equal("<html>invoice</html>", invoice.Value!.Html);
        Assert.True(receipt.IsSuccess);
        Assert.Equal("<html>receipt</html>", receipt.Value!.Html);

        store.InvoiceHtml = null;
        store.ReceiptHtml = null;
        var missingInvoice = await sender.Send(new GetAdminOrderInvoiceQuery(CheckoutId, Actor));
        var missingReceipt = await sender.Send(new GetAdminOrderReceiptQuery(CheckoutId, Actor));

        Assert.Equal(AdminOrderCompletenessErrors.InvoiceUnavailable, missingInvoice.FirstError!.Code);
        Assert.Equal(AdminOrderCompletenessErrors.ReceiptUnavailable, missingReceipt.FirstError!.Code);
    }

    private static ISender BuildSender(out FakeAdminOrderCompletenessStore store)
    {
        store = new FakeAdminOrderCompletenessStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(ListAdminOrderNotesQuery).Assembly);
        services.AddSingleton<IAdminOrderCompletenessStore>(store);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
