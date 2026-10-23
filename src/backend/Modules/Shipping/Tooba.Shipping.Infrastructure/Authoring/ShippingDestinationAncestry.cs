using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// استخراج <b>تنها</b> قاعدهٔ canonical زنجیرهٔ ناحیهٔ اداری که هم اعتبارسنجی نوشتن و هم خواندن
/// فکت‌های مقصد از آن استفاده می‌کنند؛ هیچ قاعدهٔ رقیب دوّمی ساخته نمی‌شود.
/// </summary>
/// <remarks>
/// <para>
/// <b>Proof, not GUID equality.</b> والدِ اعلام‌شده ادعاست؛ زنجیرهٔ واقعی از فکت‌های مرجع پیموده
/// می‌شود. والد غایب، غیرفعال، متعلق به کشور دیگر یا تکرار در مسیر (چرخه) زنجیره را نامعتبر می‌کند.
/// </para>
/// <para>
/// خروجی، زنجیرهٔ مرتب از ریشه تا خودِ ناحیه است (همان ترتیب canonical سند).
/// </para>
/// </remarks>
internal static class ShippingDestinationAncestry
{
    /// <summary>
    /// زنجیرهٔ واقعی یک ناحیهٔ اداری را می‌سازد.
    /// </summary>
    /// <param name="areasById">فکت‌های ناحیهٔ اداری در دسترس همین عملیات.</param>
    /// <param name="areaId">ناحیهٔ مقصد.</param>
    /// <param name="countryId">کشور مورد انتظار؛ زنجیره با هر کشور دیگری نامعتبر است.</param>
    /// <param name="chain">زنجیرهٔ مرتب ریشه تا ناحیه.</param>
    /// <returns>آیا زنجیره اثبات شد.</returns>
    public static bool TryBuildAdministrativeChain(
        IReadOnlyDictionary<Guid, AdministrativeAreaSnapshot> areasById,
        Guid areaId,
        Guid countryId,
        List<ShippingDestinationNode> chain)
    {
        ArgumentNullException.ThrowIfNull(areasById);
        ArgumentNullException.ThrowIfNull(chain);

        var reversed = new List<ShippingDestinationNode>();
        var visited = new HashSet<Guid>();
        Guid? current = areaId;
        while (current is { } id)
        {
            if (!visited.Add(id)
                || !areasById.TryGetValue(id, out var area)
                || !area.IsActive
                || area.CountryId != countryId)
            {
                return false;
            }

            reversed.Add(new ShippingDestinationNode(ShippingDestinationNodeKind.AdministrativeArea, area.AdministrativeAreaId));
            current = area.ParentAdministrativeAreaId;
        }

        if (reversed.Count == 0)
        {
            return false;
        }

        reversed.Reverse();
        chain.AddRange(reversed);
        return true;
    }
}
