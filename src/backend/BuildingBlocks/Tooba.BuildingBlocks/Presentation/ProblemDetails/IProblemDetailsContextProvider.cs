namespace Tooba.BuildingBlocks.Presentation.ProblemDetails;

/// <summary>قرارداد استخراج زمینهٔ ProblemDetails برای هر درخواست.</summary>
public interface IProblemDetailsContextProvider
{
    /// <summary>
    /// زمینهٔ جاری را می‌سازد. نباید throw کند اگر auth/tenant در دسترس نباشد.
    /// </summary>
    ToobaProblemDetailsContext GetCurrentContext();
}
