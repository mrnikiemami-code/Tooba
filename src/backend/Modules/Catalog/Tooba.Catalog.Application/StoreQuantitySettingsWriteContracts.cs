using MediatR;
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Application;

/// <summary>نوشتن تنظیم گرد کردن مقدار روی مالک فعلی Catalog.</summary>
public interface IStoreQuantitySettingsDirectory
{
    /// <summary>حالت گرد کردن سراسری را می‌نویسد و مقدار کاننیکال را برمی‌گرداند.</summary>
    Task<QuantityRoundingMode> SaveAsync(string? globalRoundingMode, CancellationToken cancellationToken);
}

/// <summary>فرمان ذخیره گرد کردن سراسری مقدار.</summary>
/// <param name="GlobalRoundingMode">Floor / Ceiling / Nearest.</param>
public sealed record SaveStoreQuantitySettingsCommand(string? GlobalRoundingMode) : IRequest<QuantityRoundingMode>;
