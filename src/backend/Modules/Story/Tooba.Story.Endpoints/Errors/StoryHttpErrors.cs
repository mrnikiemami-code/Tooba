using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Endpoints.Errors;

/// <summary>
/// کمکی‌های متعارف HTTP برای Story؛ همهٔ نگاشت‌ها از مسیر <see cref="Result"/> انجام می‌شود و
/// هیچ‌گاه بر پایهٔ متن پیام خطا تصمیم‌گیری نمی‌شود تا قرارداد خطا پایدار بماند.
/// </summary>
internal static class StoryHttpErrors
{
    /// <summary>
    /// شناسهٔ tenant مؤثر را resolve می‌کند و خطای معنایی را به شکست نوع‌دار <see cref="Result"/> تبدیل می‌کند.
    /// حالت نبود tenant fail-closed است و هرگز با مقدار پیش‌فرض جبران نمی‌شود.
    /// </summary>
    /// <param name="tenant">زمینهٔ tenant جاری که از Host تأمین می‌شود.</param>
    /// <returns>نتیجهٔ موفق حاوی شناسهٔ tenant یا شکست با کد خطای پایدار.</returns>
    public static Result<Guid> ResolveTenantId(ICurrentTenant tenant)
    {
        try
        {
            return Result.Success(StoryPresentationComposer.RequireTenantId(tenant));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<Guid>(ex.Error);
        }
    }
}
