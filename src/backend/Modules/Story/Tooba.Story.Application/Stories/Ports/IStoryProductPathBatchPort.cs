namespace Tooba.Story.Application.Stories.Ports;

/// <summary>
/// قابلیت اختیاری و دسته‌ای مسیر عمومی محصول برای خواندن عمومی استوری.
/// بدون این قابلیت، خواندن عمومی برای هر محصول یک فراخوانی جدا می‌زند؛ با آن، همهٔ
/// شناسه‌های همان صفحه در یک فراخوانی Catalog حل می‌شوند (بدون تغییر قرارداد تک‌شناسه).
/// </summary>
public interface IStoryProductPathBatchPort
{
    /// <summary>مسیرهای عمومی canonical برای مجموعهٔ شناسه‌ها؛ غایب ⇒ در نتیجه نیست.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolvePublicProductPathsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
