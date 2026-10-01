namespace Tooba.Party.Application.Admin.Sellers.Validators;

/// <summary>کدهای پایدار FluentValidation برای Admin sellers — بدون متن کاربرپسند.</summary>
public static class PartyAdminSellersValidationCodes
{
    /// <summary>بدنهٔ GridQueryRequest برای POST sellers/query الزامی است.</summary>
    public const string GridRequestRequired = "party.admin.sellers.validation.grid_request_required";
}
