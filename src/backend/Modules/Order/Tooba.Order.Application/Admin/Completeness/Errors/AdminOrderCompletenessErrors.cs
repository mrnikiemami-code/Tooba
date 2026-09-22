namespace Tooba.Order.Application.Admin.Completeness;

public static class AdminOrderCompletenessErrors
{
    public const string Missing = "order.operation.invalid";
    public const string InvalidNote = "order.note.invalid";
    public const string DeleteForbidden = "order.note.delete.forbidden";
    public const string InvoiceUnavailable = "order.invoice.unavailable";
    public const string ReceiptUnavailable = "order.receipt.unavailable";
}
