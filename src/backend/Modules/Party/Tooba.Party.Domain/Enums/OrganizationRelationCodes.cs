namespace Tooba.Party.Domain.Enums;

/// <summary>
/// کدهای رابطهٔ سازمان‌به‌سازمان. فهرست بستهٔ B2B نیست؛ کد جدید بدون بازنویسی هسته اضافه می‌شود.
/// </summary>
public static class OrganizationRelationCodes
{
    /// <summary>
    /// رابطهٔ والد/فرزند سازمانی برای سلسله‌مراتب آینده.
    /// </summary>
    public const string ParentOf = "parent_of";

    /// <summary>
    /// درز «فروشنده توسط» بدون قفل Seller module.
    /// </summary>
    public const string OperatedBy = "operated_by";

    /// <summary>
    /// درز نمایندگی آژانس بدون پورتال آژانس.
    /// </summary>
    public const string Represents = "represents";
}
