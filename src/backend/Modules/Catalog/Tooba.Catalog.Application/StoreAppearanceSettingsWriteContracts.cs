using MediatR;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>نوشتن ظاهر فروشگاه روی مالک فعلی Catalog (orchestration موقت تا جابه‌جایی BC).</summary>
public interface IStoreAppearanceSettingsDirectory
{
    /// <summary>پالت/تم/پوسته/پس‌زمینه را اتمیک می‌نویسد.</summary>
    Task SaveAsync(StoreAppearanceSettingsWriteModel model, CancellationToken cancellationToken);
}

/// <summary>مدل نوشتن ظاهر (مرز Application).</summary>
/// <param name="PaletteKey">کلید پالت.</param>
/// <param name="ThemeMode">حالت تم؛ null یعنی حفظ.</param>
/// <param name="ProductCardSkin">پوسته کارت؛ null یعنی حفظ.</param>
/// <param name="BackgroundStyle">پس‌زمینه؛ null یعنی حفظ.</param>
public sealed record StoreAppearanceSettingsWriteModel(
    string? PaletteKey,
    string? ThemeMode,
    string? ProductCardSkin,
    string? BackgroundStyle);

/// <summary>فرمان ذخیره ظاهر فروشگاه.</summary>
/// <param name="Model">مدل.</param>
public sealed record SaveStoreAppearanceSettingsCommand(StoreAppearanceSettingsWriteModel Model) : IRequest<Unit>;
