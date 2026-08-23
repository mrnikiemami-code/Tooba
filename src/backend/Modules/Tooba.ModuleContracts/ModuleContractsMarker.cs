namespace Tooba.ModuleContracts;

/// <summary>
/// نشانگر قراردادهای بین‌ماژولی. ماژول‌ها persistence یکدیگر را reference نمی‌کنند و join بین‌schema ندارند.
/// پوشه‌های Domain/Application/Infrastructure هر ماژول در Taskهای بعدی اضافه می‌شوند، نه اینجا.
/// </summary>
public static class ModuleContractsMarker
{
}
